namespace WhatsAppBusinessApi.Api.Clients;

/// <summary>
/// Sends outbound WhatsApp messages through Meta's Graph API.
/// </summary>
public interface IWhatsAppClient
{
    /// <summary>
    /// Sends a free-form text message to <paramref name="to"/> via Meta Graph
    /// API <c>POST /{phone-number-id}/messages</c>.
    /// </summary>
    /// <exception cref="WhatsAppBusinessApi.Api.ErrorMapping.GraphApiException">
    /// Thrown when the Graph API responds with a non-2xx status, including
    /// the 24h customer-service-window rejection (error code 131047).
    /// </exception>
    Task<SendMessageResult> SendTextMessageAsync(string to, string body, CancellationToken ct);
}

/// <summary>
/// Result of a successful outbound send: the Graph API's message ID.
/// </summary>
public sealed record SendMessageResult(string MessageId);
