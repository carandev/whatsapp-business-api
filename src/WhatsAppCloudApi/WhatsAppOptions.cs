using System.ComponentModel.DataAnnotations;

namespace WhatsAppCloudApi;

/// <summary>
/// Configuration options for connecting to the WhatsApp Cloud API.
/// </summary>
public sealed class WhatsAppOptions
{
    /// <summary>
    /// The configuration section name used to bind <see cref="WhatsAppOptions"/>.
    /// </summary>
    public const string SectionName = "WhatsApp";

    /// <summary>
    /// The permanent or temporary access token used to authenticate requests against the Graph API.
    /// </summary>
    [Required]
    public string AccessToken { get; set; } = default!;

    /// <summary>
    /// The WhatsApp Business phone number ID used as the default sender identity for outgoing requests.
    /// </summary>
    [Required]
    public string PhoneNumberId { get; set; } = default!;

    /// <summary>
    /// The WhatsApp Business Account (WABA) ID used as the default owner for WABA-scoped
    /// resources, such as message templates. Optional — only required when a consumer calls a
    /// WABA-scoped method (e.g. <c>IWhatsAppTemplatesClient</c>) without a per-call override.
    /// </summary>
    public string? WabaId { get; set; }

    /// <summary>
    /// The Meta Graph API version segment, e.g. <c>v21.0</c>. Defaults to <c>v21.0</c>.
    /// </summary>
    public string ApiVersion { get; set; } = "v21.0";

    /// <summary>
    /// The Meta Graph API base URL as a format string, where <c>{0}</c> is replaced with
    /// <see cref="ApiVersion"/>. Defaults to <c>https://graph.facebook.com/{0}/</c>.
    /// </summary>
    public string BaseUrl { get; set; } = "https://graph.facebook.com/{0}/";

    /// <summary>
    /// The maximum number of retry attempts for transient failures (5xx responses and recognized
    /// transient Graph error codes/subcodes). Enforced by the <c>"whatsapp-retry"</c> resilience
    /// pipeline registered by <c>AddWhatsAppCloudApi</c>.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// The HTTP request timeout. Not yet enforced by this scaffold; reserved for a future release.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// The Meta App Secret used by <see cref="WhatsAppSignatureValidator"/> to verify the
    /// <c>X-Hub-Signature-256</c> header on inbound webhook requests. Optional — only required
    /// when a consumer verifies webhook signatures. A missing value causes verification to fail
    /// closed rather than fail at options-binding time.
    /// </summary>
    public string? AppSecret { get; set; }

    /// <summary>
    /// The verify token compared against <c>hub.verify_token</c> during the webhook GET
    /// handshake. Optional — only required when a consumer wires up the webhook subscription
    /// handshake.
    /// </summary>
    public string? WebhookVerifyToken { get; set; }
}
