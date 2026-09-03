using System.Text.Json.Serialization;

namespace WhatsAppCloudApi.Models.Messages;

/// <summary>
/// A contacts message envelope sent via <c>SendContactsAsync</c>, carrying one or more
/// <see cref="Contact"/> cards.
/// </summary>
public sealed record SendContactsMessageRequest : SendMessageRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SendContactsMessageRequest"/> class.
    /// </summary>
    /// <param name="to">The recipient's phone number, in E.164 format. Must not be null, empty, or whitespace.</param>
    /// <param name="contacts">The contact cards to send. Must not be null.</param>
    /// <param name="context">An optional reference to a prior inbound message being replied to.</param>
    public SendContactsMessageRequest(string to, IReadOnlyList<Contact> contacts, Context? context = null)
        : base(to, context)
    {
        ArgumentNullException.ThrowIfNull(contacts);
        Contacts = contacts;
    }

    /// <inheritdoc />
    [JsonPropertyName("type")]
    public override string Type => "contacts";

    /// <summary>
    /// The contact cards to send.
    /// </summary>
    [JsonPropertyName("contacts")]
    public IReadOnlyList<Contact> Contacts { get; }
}
