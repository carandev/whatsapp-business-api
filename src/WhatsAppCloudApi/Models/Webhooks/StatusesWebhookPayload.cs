using System.Text.Json.Serialization;

namespace WhatsAppCloudApi.Models.Webhooks;

/// <summary>
/// The <c>value</c> shape of a <see cref="WebhookChange"/> carrying delivery-status updates:
/// <c>entry[].changes[].value.{messaging_product,metadata,statuses}</c>. Meta always reports
/// <see cref="WebhookChange.Field"/> as <c>"messages"</c> for both this shape and
/// <see cref="MessagesWebhookPayload"/> — callers distinguish the two by probing for a
/// <c>messages</c> or <c>statuses</c> property on the raw <see cref="WebhookChange.Value"/>, not by
/// <see cref="WebhookChange.Field"/>.
/// </summary>
public sealed record StatusesWebhookPayload
{
    /// <summary>
    /// The messaging product. Always <c>whatsapp</c>.
    /// </summary>
    [JsonPropertyName("messaging_product")]
    public string? MessagingProduct { get; init; }

    /// <summary>
    /// Identifies which of the business's phone numbers this batch of status updates relates to.
    /// </summary>
    [JsonPropertyName("metadata")]
    public WebhookMetadata? Metadata { get; init; }

    /// <summary>
    /// The status update items in this batch.
    /// </summary>
    [JsonPropertyName("statuses")]
    public IReadOnlyList<WebhookStatus>? Statuses { get; init; }
}
