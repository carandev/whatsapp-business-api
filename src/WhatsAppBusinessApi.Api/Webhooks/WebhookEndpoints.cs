using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WhatsAppBusinessApi.Api.Configuration;

namespace WhatsAppBusinessApi.Api.Webhooks;

/// <summary>
/// Maps Meta's WhatsApp Cloud API webhook endpoints per design.md's Data Flow:
/// <c>GET /webhook</c> for the verification handshake and <c>POST /webhook</c>
/// for signature-checked inbound event delivery. No controllers — minimal-API
/// extension method only, per the proposal's locked decision.
/// </summary>
public static class WebhookEndpoints
{
    private const string SignatureHeaderName = "X-Hub-Signature-256";

    public static IEndpointRouteBuilder MapWhatsAppWebhooks(this IEndpointRouteBuilder app)
    {
        app.MapGet("/webhook", (
            [FromQuery(Name = "hub.verify_token")] string? verifyToken,
            [FromQuery(Name = "hub.challenge")] string? challenge,
            IOptions<WebhookOptions> options) =>
        {
            if (!string.IsNullOrEmpty(challenge) &&
                string.Equals(verifyToken, options.Value.VerifyToken, StringComparison.Ordinal))
            {
                return Results.Text(challenge, "text/plain");
            }

            return Results.StatusCode(StatusCodes.Status403Forbidden);
        });

        app.MapPost("/webhook", async (
            HttpContext httpContext,
            IOptions<WebhookOptions> options,
            ILogger<WhatsAppWebhookPayload> logger,
            CancellationToken ct) =>
        {
            // Read the raw request body bytes BEFORE any JSON model binding —
            // model binding would consume/transform the stream before a
            // byte-exact HMAC could be computed. See design.md "Signature
            // validation" decision.
            using var bodyBuffer = new MemoryStream();
            await httpContext.Request.Body.CopyToAsync(bodyBuffer, ct).ConfigureAwait(false);
            var rawBody = bodyBuffer.ToArray();

            var signatureHeader = httpContext.Request.Headers[SignatureHeaderName].ToString();

            if (!WebhookSignatureValidator.IsValid(rawBody, signatureHeader, options.Value.AppSecret))
            {
                return Results.StatusCode(StatusCodes.Status401Unauthorized);
            }

            WhatsAppWebhookPayload? payload;
            try
            {
                payload = JsonSerializer.Deserialize<WhatsAppWebhookPayload>(rawBody);
            }
            catch (JsonException ex)
            {
                logger.LogWarning(ex, "Received a signature-valid webhook payload that failed to parse as JSON.");
                return Results.Ok();
            }

            if (payload is not null)
            {
                foreach (var reportedType in DescribeReportedMessageTypes(payload))
                {
                    // No reply/echo is sent back — the spec requires only
                    // logging and reporting the parsed message type.
                    logger.LogInformation("Received WhatsApp webhook event of type {MessageType}.", reportedType);
                }
            }

            return Results.Ok();
        });

        return app;
    }

    /// <summary>
    /// Reports one entry per inbound message (its <c>type</c>, e.g. <c>text</c>/
    /// <c>image</c>) and one entry per status update (reported as <c>status</c>).
    /// Exposed publicly so payload-parsing behavior is unit-testable without a
    /// full HTTP round-trip.
    /// </summary>
    public static IEnumerable<string> DescribeReportedMessageTypes(WhatsAppWebhookPayload payload)
    {
        foreach (var entry in payload.Entry ?? [])
        {
            foreach (var change in entry.Changes ?? [])
            {
                if (change.Value is null)
                {
                    continue;
                }

                foreach (var message in change.Value.Messages ?? [])
                {
                    yield return message.Type;
                }

                foreach (var _ in change.Value.Statuses ?? [])
                {
                    yield return "status";
                }
            }
        }
    }
}
