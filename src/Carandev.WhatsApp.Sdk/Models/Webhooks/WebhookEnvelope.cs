using System.Text.Json;
using System.Text.Json.Serialization;

namespace Carandev.WhatsApp.Sdk.Models.Webhooks;

/// <summary>
/// The top-level object Meta POSTs to a subscribed webhook endpoint. Wraps one or more
/// <see cref="WebhookEntry"/> items, each carrying one or more <see cref="WebhookChange"/> items.
/// </summary>
public sealed record WebhookEnvelope
{
    /// <summary>
    /// The webhook object type. Always <c>whatsapp_business_account</c> for WhatsApp Cloud API
    /// webhooks.
    /// </summary>
    [JsonPropertyName("object")]
    public string? Object { get; init; }

    /// <summary>
    /// The batch of entries carried by this webhook delivery, one per subscribed WhatsApp
    /// Business Account.
    /// </summary>
    [JsonPropertyName("entry")]
    public IReadOnlyList<WebhookEntry>? Entry { get; init; }
}

/// <summary>
/// A single entry within a <see cref="WebhookEnvelope"/>, scoped to one WhatsApp Business
/// Account id.
/// </summary>
public sealed record WebhookEntry
{
    /// <summary>
    /// The WhatsApp Business Account id this entry is scoped to.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    /// <summary>
    /// The batch of changes carried by this entry.
    /// </summary>
    [JsonPropertyName("changes")]
    public IReadOnlyList<WebhookChange>? Changes { get; init; }
}

/// <summary>
/// A single change within a <see cref="WebhookEntry"/>. <see cref="Value"/> is deferred-parsed —
/// its shape depends on <see cref="Field"/> (e.g. <c>"messages"</c>) and is deserialized into
/// <see cref="MessagesWebhookPayload"/> or a statuses-specific payload by probing the raw JSON,
/// so a single POST batching both message and status changes across entries parses correctly.
/// </summary>
public sealed record WebhookChange
{
    /// <summary>
    /// The field this change applies to, e.g. <c>"messages"</c>.
    /// </summary>
    [JsonPropertyName("field")]
    public string? Field { get; init; }

    /// <summary>
    /// The raw, not-yet-typed value payload. Deserialize into <see cref="MessagesWebhookPayload"/>
    /// (or a statuses-specific payload) once the caller knows which shape applies.
    /// </summary>
    [JsonPropertyName("value")]
    public JsonElement Value { get; init; }
}

/// <summary>
/// The phone number metadata carried by a <see cref="MessagesWebhookPayload"/> or a
/// <see cref="StatusesWebhookPayload"/>, identifying which of the business's phone numbers the
/// webhook belongs to. Surfaced to handlers through <see cref="WebhookContext.Metadata"/>.
/// </summary>
public sealed record WebhookMetadata
{
    /// <summary>
    /// The business phone number in display format.
    /// </summary>
    [JsonPropertyName("display_phone_number")]
    public string? DisplayPhoneNumber { get; init; }

    /// <summary>
    /// The Meta-assigned id of the business phone number that received the webhook.
    /// </summary>
    [JsonPropertyName("phone_number_id")]
    public string? PhoneNumberId { get; init; }
}
