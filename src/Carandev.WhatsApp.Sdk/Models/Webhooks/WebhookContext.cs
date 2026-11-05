namespace Carandev.WhatsApp.Sdk.Models.Webhooks;

/// <summary>
/// Envelope data shared by the items of one webhook change: which WhatsApp Business Account and
/// business phone number the event belongs to and, for an inbound message, the sender's profile.
/// Built by <see cref="Sdk.Webhooks.IWhatsAppWebhookProcessor"/> and passed to the context
/// overloads of <see cref="Sdk.Webhooks.WhatsAppWebhookHandlers"/>.
/// </summary>
/// <remarks>
/// Use it to route events when one app serves several business numbers: key on
/// <c>Metadata?.PhoneNumberId</c>, not on <see cref="WebhookMessage.From"/> (the customer).
/// </remarks>
public sealed record WebhookContext
{
    /// <summary>
    /// The WhatsApp Business Account id the change belongs to (<c>entry[].id</c>).
    /// </summary>
    public string? WabaId { get; init; }

    /// <summary>
    /// The business phone number that received the message, or that sent the message a status
    /// refers to (<c>value.metadata</c>), exactly as Meta sends it. <see langword="null"/> when
    /// the change value could not be parsed.
    /// </summary>
    public WebhookMetadata? Metadata { get; init; }

    /// <summary>
    /// The sender's WhatsApp profile for an inbound message (<c>value.contacts[]</c>): the contact
    /// whose <see cref="WebhookContact.WaId"/> equals <see cref="WebhookMessage.From"/>, or the only
    /// contact of the change when none matches. <see langword="null"/> when the change has no
    /// contacts or several that do not match, and always <see langword="null"/> for statuses
    /// (Meta sends no <c>contacts</c> with them).
    /// </summary>
    public WebhookContact? Contact { get; init; }
}
