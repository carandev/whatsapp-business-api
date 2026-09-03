using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WhatsAppCloudApi.Models.Webhooks;
using WhatsAppCloudApi.Serialization;

namespace WhatsAppCloudApi.Extensions.AspNetCore;

/// <summary>
/// Handles the GET handshake and POST delivery for a single <c>UseWhatsAppWebhooks</c>
/// registration. Kept separate from <see cref="ApplicationBuilderExtensions"/> so the dispatch
/// logic is directly testable via <see cref="InvokeAsync"/> in addition to full-pipeline
/// <c>TestServer</c> coverage.
/// </summary>
internal sealed class WhatsAppWebhookMiddleware
{
    private const string SignatureHeaderName = "X-Hub-Signature-256";

    private readonly RequestDelegate _next;
    private readonly PathString _path;
    private readonly WhatsAppWebhookHandlers _handlers;

    /// <summary>
    /// Creates a middleware instance bound to a specific webhook <paramref name="path"/> and
    /// <paramref name="handlers"/>.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline, invoked for non-matching requests.</param>
    /// <param name="path">The request path this middleware handles.</param>
    /// <param name="handlers">The handlers invoked for each parsed message/status item.</param>
    public WhatsAppWebhookMiddleware(RequestDelegate next, string path, WhatsAppWebhookHandlers handlers)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentException.ThrowIfNullOrEmpty(path);
        ArgumentNullException.ThrowIfNull(handlers);

        _next = next;
        _path = new PathString(path);
        _handlers = handlers;
    }

    /// <summary>
    /// Routes the request: the configured GET/POST handling for a matching
    /// <see cref="HttpRequest.Path"/>, or pass-through to <see cref="_next"/> otherwise.
    /// </summary>
    /// <param name="context">The current request context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!context.Request.Path.Equals(_path, StringComparison.OrdinalIgnoreCase))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        if (HttpMethods.IsGet(context.Request.Method))
        {
            await HandleGetAsync(context).ConfigureAwait(false);
            return;
        }

        if (HttpMethods.IsPost(context.Request.Method))
        {
            await HandlePostAsync(context).ConfigureAwait(false);
            return;
        }

        await _next(context).ConfigureAwait(false);
    }

    /// <summary>
    /// Handles the subscription handshake: echoes <c>hub.challenge</c> with <c>200 OK</c> when
    /// <c>hub.verify_token</c> matches <see cref="WhatsAppOptions.WebhookVerifyToken"/>, otherwise
    /// responds <c>403 Forbidden</c> without echoing anything.
    /// </summary>
    private static async Task HandleGetAsync(HttpContext context)
    {
        var options = context.RequestServices.GetRequiredService<IOptions<WhatsAppOptions>>().Value;
        var verifyToken = context.Request.Query["hub.verify_token"].ToString();
        var challenge = context.Request.Query["hub.challenge"].ToString();

        if (!string.IsNullOrEmpty(options.WebhookVerifyToken) &&
            string.Equals(verifyToken, options.WebhookVerifyToken, StringComparison.Ordinal))
        {
            context.Response.StatusCode = StatusCodes.Status200OK;
            context.Response.ContentType = "text/plain";
            await context.Response.WriteAsync(challenge).ConfigureAwait(false);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
    }

    /// <summary>
    /// Verifies the raw body against <c>X-Hub-Signature-256</c> before any JSON parsing, then fans
    /// each batched <c>messages[]</c>/<c>statuses[]</c> item out to <see cref="_handlers"/> once
    /// per item. Always responds <c>200 OK</c> once the signature has been accepted — malformed
    /// JSON and per-item handler exceptions are surfaced via <see cref="WhatsAppWebhookHandlers.OnErrorAsync"/>
    /// instead of the HTTP status, to avoid triggering Meta's aggressive non-2xx retry behavior.
    /// </summary>
    private async Task HandlePostAsync(HttpContext context)
    {
        string rawBody;
        using (var reader = new StreamReader(context.Request.Body, leaveOpen: true))
        {
            rawBody = await reader.ReadToEndAsync(context.RequestAborted).ConfigureAwait(false);
        }

        var options = context.RequestServices.GetRequiredService<IOptions<WhatsAppOptions>>().Value;
        var validator = new WhatsAppSignatureValidator(options.AppSecret);
        var signatureHeader = context.Request.Headers[SignatureHeaderName].ToString();

        if (!validator.Verify(rawBody, signatureHeader))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        WebhookEnvelope? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize(rawBody, WhatsAppJsonSerializerContext.Default.WebhookEnvelope);
        }
        catch (JsonException ex)
        {
            await _handlers.OnErrorAsync(ex, context.RequestAborted).ConfigureAwait(false);
            context.Response.StatusCode = StatusCodes.Status200OK;
            return;
        }

        if (envelope?.Entry is not null)
        {
            foreach (var entry in envelope.Entry)
            {
                if (entry.Changes is null)
                {
                    continue;
                }

                foreach (var change in entry.Changes)
                {
                    await DispatchChangeAsync(change, context.RequestAborted).ConfigureAwait(false);
                }
            }
        }

        context.Response.StatusCode = StatusCodes.Status200OK;
    }

    /// <summary>
    /// Probes a single <see cref="WebhookChange"/> for a <c>messages</c> or <c>statuses</c>
    /// property (both may be present or absent) and dispatches each contained item once.
    /// </summary>
    private async Task DispatchChangeAsync(WebhookChange change, CancellationToken cancellationToken)
    {
        if (change.Value.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        if (change.Value.TryGetProperty("messages", out _))
        {
            await DispatchMessagesAsync(change.Value, cancellationToken).ConfigureAwait(false);
        }

        if (change.Value.TryGetProperty("statuses", out _))
        {
            await DispatchStatusesAsync(change.Value, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task DispatchMessagesAsync(JsonElement value, CancellationToken cancellationToken)
    {
        MessagesWebhookPayload? payload;
        try
        {
            payload = value.Deserialize(WhatsAppJsonSerializerContext.Default.MessagesWebhookPayload);
        }
        catch (JsonException ex)
        {
            await _handlers.OnErrorAsync(ex, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (payload?.Messages is null)
        {
            return;
        }

        foreach (var message in payload.Messages)
        {
            await InvokeHandlerSafelyAsync(() => _handlers.OnMessageAsync(message, cancellationToken), cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task DispatchStatusesAsync(JsonElement value, CancellationToken cancellationToken)
    {
        StatusesWebhookPayload? payload;
        try
        {
            payload = value.Deserialize(WhatsAppJsonSerializerContext.Default.StatusesWebhookPayload);
        }
        catch (JsonException ex)
        {
            await _handlers.OnErrorAsync(ex, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (payload?.Statuses is null)
        {
            return;
        }

        foreach (var status in payload.Statuses)
        {
            await InvokeHandlerSafelyAsync(() => _handlers.OnStatusAsync(status, cancellationToken), cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Invokes a single per-item handler call, routing any exception it raises to
    /// <see cref="WhatsAppWebhookHandlers.OnErrorAsync"/> so one failing item never aborts the
    /// rest of the batch.
    /// </summary>
    private async Task InvokeHandlerSafelyAsync(Func<Task> handlerCall, CancellationToken cancellationToken)
    {
        try
        {
            await handlerCall().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await _handlers.OnErrorAsync(ex, cancellationToken).ConfigureAwait(false);
        }
    }
}
