namespace Carandev.WhatsApp.Sdk.Models.Webhooks;

/// <summary>
/// Envelope data of one webhook change, passed with the whole list of its items to the batch
/// handlers of <see cref="Sdk.Webhooks.WhatsAppWebhookHandlers"/>.
/// </summary>
/// <remarks>
/// Unlike <see cref="WebhookContext"/>, it carries every contact of the change instead of the one
/// resolved for a given message. Match a message to its sender with
/// <see cref="WebhookContact.WaId"/> and <see cref="WebhookMessage.From"/>.
/// </remarks>
public sealed record WebhookBatchContext
{
    /// <summary>
    /// The WhatsApp Business Account id the change belongs to (<c>entry[].id</c>).
    /// </summary>
    public string? WabaId { get; init; }

    /// <summary>
    /// The business phone number that received the messages, or that sent the messages the
    /// statuses refer to (<c>value.metadata</c>), exactly as Meta sends it.
    /// </summary>
    public WebhookMetadata? Metadata { get; init; }

    /// <summary>
    /// Every sender profile of the change (<c>value.contacts[]</c>). Never <see langword="null"/>:
    /// empty when Meta sends no contacts, and always empty for statuses.
    /// </summary>
    public IReadOnlyList<WebhookContact> Contacts { get; init; } = [];
}
