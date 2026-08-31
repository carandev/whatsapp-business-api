using System.Text.Json.Serialization;

namespace WhatsAppBusinessApi.Api.Webhooks;

/// <summary>
/// Inbound webhook payload sent by Meta's WhatsApp Cloud API on <c>POST /webhook</c>.
/// Only the subset of fields needed for message-type reporting (per design.md's
/// "Inbound Payload Parsing" requirement) is modeled — fields such as contacts,
/// pricing, and errors are intentionally omitted.
/// </summary>
public sealed record WhatsAppWebhookPayload(
    [property: JsonPropertyName("object")] string? Object,
    [property: JsonPropertyName("entry")] IReadOnlyList<WebhookEntry>? Entry);

public sealed record WebhookEntry(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("changes")] IReadOnlyList<WebhookChange>? Changes);

public sealed record WebhookChange(
    [property: JsonPropertyName("value")] WebhookChangeValue? Value,
    [property: JsonPropertyName("field")] string? Field);

public sealed record WebhookChangeValue(
    [property: JsonPropertyName("messaging_product")] string? MessagingProduct,
    [property: JsonPropertyName("metadata")] WebhookMetadata? Metadata,
    [property: JsonPropertyName("messages")] IReadOnlyList<WebhookMessage>? Messages,
    [property: JsonPropertyName("statuses")] IReadOnlyList<WebhookStatus>? Statuses);

public sealed record WebhookMetadata(
    [property: JsonPropertyName("display_phone_number")] string? DisplayPhoneNumber,
    [property: JsonPropertyName("phone_number_id")] string? PhoneNumberId);

/// <summary>
/// An inbound message event. <see cref="Type"/> reports the kind reflected back
/// per the "no reply/echo, just report the parsed message type" requirement
/// (e.g. <c>text</c>, <c>image</c>).
/// </summary>
public sealed record WebhookMessage(
    [property: JsonPropertyName("from")] string? From,
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("timestamp")] string? Timestamp,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("text")] WebhookTextContent? Text,
    [property: JsonPropertyName("image")] WebhookMediaContent? Image);

public sealed record WebhookTextContent(
    [property: JsonPropertyName("body")] string? Body);

public sealed record WebhookMediaContent(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("mime_type")] string? MimeType,
    [property: JsonPropertyName("sha256")] string? Sha256,
    [property: JsonPropertyName("caption")] string? Caption);

/// <summary>
/// A delivery/read status update. Reported as message type <c>status</c> — see
/// <see cref="WhatsAppBusinessApi.Api.Webhooks.WebhookEndpoints"/>.
/// </summary>
public sealed record WebhookStatus(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("status")] string? Status,
    [property: JsonPropertyName("timestamp")] string? Timestamp,
    [property: JsonPropertyName("recipient_id")] string? RecipientId);
