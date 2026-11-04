namespace Carandev.WhatsApp.Sdk.Webhooks;

/// <summary>
/// Outcome of processing an inbound webhook request with <see cref="IWhatsAppWebhookProcessor"/>.
/// </summary>
/// <remarks>
/// Suggested HTTP mapping: <see cref="Processed"/> and <see cref="InvalidPayload"/> → <c>200 OK</c>
/// (so Meta does not retry a payload that will never parse), <see cref="InvalidSignature"/> →
/// <c>401 Unauthorized</c>.
/// </remarks>
public enum WebhookProcessingResult
{
    /// <summary>
    /// The payload was parsed and every message/status item was dispatched to the handlers.
    /// Exceptions thrown by individual handlers are routed to
    /// <see cref="WhatsAppWebhookHandlers.OnErrorAsync"/> and do not change this result.
    /// </summary>
    Processed,

    /// <summary>
    /// The <c>X-Hub-Signature-256</c> header was missing, malformed, did not match the body, or no
    /// <see cref="WhatsAppOptions.AppSecret"/> is configured. No handler was invoked.
    /// </summary>
    InvalidSignature,

    /// <summary>
    /// The body is not a valid webhook envelope. <see cref="WhatsAppWebhookHandlers.OnErrorAsync"/>
    /// was invoked with the parsing exception.
    /// </summary>
    InvalidPayload,
}
