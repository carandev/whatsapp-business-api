using System.Text.Json.Serialization;

namespace Carandev.WhatsApp.Sdk.Models.Webhooks;

/// <summary>
/// The <c>value</c> shape of a <see cref="WebhookChange"/> whose <see cref="WebhookChange.Field"/>
/// is <c>"messages"</c>: <c>entry[].changes[].value.{messaging_product,metadata,contacts,messages}</c>.
/// </summary>
public sealed record MessagesWebhookPayload
{
    /// <summary>
    /// The messaging product. Always <c>whatsapp</c>.
    /// </summary>
    [JsonPropertyName("messaging_product")]
    public string? MessagingProduct { get; init; }

    /// <summary>
    /// Identifies which of the business's phone numbers received this batch.
    /// </summary>
    [JsonPropertyName("metadata")]
    public WebhookMetadata? Metadata { get; init; }

    /// <summary>
    /// The WhatsApp profile info (name, WhatsApp id) of the sender(s) whose messages are included
    /// in <see cref="Messages"/>. Distinct from the vCard-like <see cref="Models.Messages.Contact"/>
    /// shape used by <see cref="ContactsWebhookMessage"/> message content.
    /// </summary>
    [JsonPropertyName("contacts")]
    public IReadOnlyList<WebhookContact>? Contacts { get; init; }

    /// <summary>
    /// The inbound message items in this batch.
    /// </summary>
    [JsonPropertyName("messages")]
    public IReadOnlyList<WebhookMessage>? Messages { get; init; }
}

/// <summary>
/// The sender's WhatsApp profile info, as carried by <see cref="MessagesWebhookPayload.Contacts"/>.
/// </summary>
public sealed record WebhookContact
{
    /// <summary>
    /// The sender's profile.
    /// </summary>
    [JsonPropertyName("profile")]
    public WebhookContactProfile? Profile { get; init; }

    /// <summary>
    /// The sender's WhatsApp id (phone number, without a leading <c>+</c>).
    /// </summary>
    [JsonPropertyName("wa_id")]
    public string? WaId { get; init; }
}

/// <summary>
/// The profile portion of a <see cref="WebhookContact"/>.
/// </summary>
public sealed record WebhookContactProfile
{
    /// <summary>
    /// The sender's display name.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }
}
