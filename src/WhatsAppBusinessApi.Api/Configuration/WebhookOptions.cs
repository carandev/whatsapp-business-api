namespace WhatsAppBusinessApi.Api.Configuration;

/// <summary>
/// Meta webhook verification and signature-validation configuration.
/// Bound from the "Webhook" configuration section.
/// </summary>
public sealed class WebhookOptions
{
    /// <summary>
    /// Configuration section name this type binds to.
    /// </summary>
    public const string SectionName = "Webhook";

    /// <summary>
    /// Token compared against the `hub.verify_token` query parameter during
    /// Meta's GET verification handshake.
    /// </summary>
    public string VerifyToken { get; set; } = string.Empty;

    /// <summary>
    /// App secret used to compute the HMAC-SHA256 signature validated against
    /// the `X-Hub-Signature-256` header on inbound POST requests.
    /// </summary>
    public string AppSecret { get; set; } = string.Empty;
}
