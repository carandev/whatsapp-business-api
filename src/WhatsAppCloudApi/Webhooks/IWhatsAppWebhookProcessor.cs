namespace WhatsAppCloudApi.Webhooks;

/// <summary>
/// Host-agnostic processing of WhatsApp Cloud API inbound webhooks: subscription verification
/// (GET handshake), <c>X-Hub-Signature-256</c> validation, payload parsing, and dispatch to
/// <see cref="WhatsAppWebhookHandlers"/>.
/// </summary>
/// <remarks>
/// The processor never touches HTTP. Wire it into any host (ASP.NET Core Minimal APIs,
/// controllers, Azure Functions, AWS Lambda) and keep full control of routing, authentication,
/// dependency-injection scopes, and whether events are handled inline or queued.
/// Always pass the raw, unmodified request body: the signature is computed over its exact bytes.
/// </remarks>
public interface IWhatsAppWebhookProcessor
{
    /// <summary>
    /// Handles Meta's subscription verification request (GET with <c>hub.verify_token</c> and
    /// <c>hub.challenge</c> query parameters).
    /// </summary>
    /// <param name="verifyToken">The <c>hub.verify_token</c> query value.</param>
    /// <param name="challenge">The <c>hub.challenge</c> query value.</param>
    /// <returns>
    /// The challenge to echo back with <c>200 OK</c> when <paramref name="verifyToken"/> matches
    /// <see cref="WhatsAppOptions.WebhookVerifyToken"/>; otherwise <see langword="null"/>, which
    /// should be answered with <c>403 Forbidden</c>.
    /// </returns>
    string? VerifySubscription(string? verifyToken, string? challenge);

    /// <summary>
    /// Checks the <c>X-Hub-Signature-256</c> header against the raw body using
    /// <see cref="WhatsAppOptions.AppSecret"/>.
    /// </summary>
    /// <param name="rawBody">The raw, unmodified request body.</param>
    /// <param name="signatureHeader">The <c>X-Hub-Signature-256</c> header value.</param>
    /// <returns><see langword="true"/> when the signature is valid.</returns>
    bool IsValidSignature(string rawBody, string? signatureHeader);

    /// <summary>
    /// Parses an already-verified webhook body and dispatches each message and status item to
    /// <paramref name="handlers"/>. Use it to process a body that was verified and queued earlier.
    /// </summary>
    /// <param name="rawBody">The raw webhook body.</param>
    /// <param name="handlers">The handlers receiving each item.</param>
    /// <param name="cancellationToken">A token to cancel dispatch.</param>
    /// <returns><see cref="WebhookProcessingResult.Processed"/> or <see cref="WebhookProcessingResult.InvalidPayload"/>.</returns>
    Task<WebhookProcessingResult> DispatchAsync(string rawBody, WhatsAppWebhookHandlers handlers, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies the signature and, when valid, dispatches the payload to <paramref name="handlers"/>.
    /// </summary>
    /// <param name="rawBody">The raw, unmodified request body.</param>
    /// <param name="signatureHeader">The <c>X-Hub-Signature-256</c> header value.</param>
    /// <param name="handlers">The handlers receiving each item.</param>
    /// <param name="cancellationToken">A token to cancel dispatch.</param>
    /// <returns>The processing outcome; see <see cref="WebhookProcessingResult"/> for the suggested HTTP mapping.</returns>
    Task<WebhookProcessingResult> ProcessAsync(string rawBody, string? signatureHeader, WhatsAppWebhookHandlers handlers, CancellationToken cancellationToken = default);
}
