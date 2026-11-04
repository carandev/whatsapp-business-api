using System.Text.Json.Serialization;

namespace WhatsAppCloudApi.Models.Messages;

/// <summary>
/// Base type for a single outbound message envelope sent to <c>POST
/// /{Phone-Number-ID}/messages</c>. Each message kind (text, template, media, location,
/// contacts) is a sealed record deriving from this base and overriding <see cref="Type"/>; unlike
/// <c>Models.Templates.TemplateComponent</c>, this hierarchy is NOT <c>[JsonPolymorphic]</c> —
/// each derived type is serialized directly via its own registered type, since Meta nests each
/// message kind's content under a differently-named key (<c>"text"</c>, <c>"image"</c>, etc.)
/// rather than as flattened sibling properties.
/// </summary>
public abstract record SendMessageRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SendMessageRequest"/> class.
    /// </summary>
    /// <param name="to">The recipient's phone number, in E.164 format. Must not be null, empty, or whitespace.</param>
    /// <param name="context">An optional reference to a prior inbound message being replied to.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="to"/> is null, empty, or whitespace.
    /// </exception>
    private protected SendMessageRequest(string to, Context? context)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(to);
        To = to;
        Context = context;
    }

    /// <summary>
    /// The messaging product. Always <c>whatsapp</c> for the Cloud API.
    /// </summary>
    [JsonPropertyName("messaging_product")]
    public string MessagingProduct { get; init; } = "whatsapp";

    /// <summary>
    /// The recipient's phone number, in E.164 format.
    /// </summary>
    [JsonPropertyName("to")]
    public string To { get; }

    /// <summary>
    /// The message kind, e.g. <c>text</c>, <c>template</c>, <c>image</c>, <c>location</c>,
    /// <c>contacts</c>.
    /// </summary>
    [JsonPropertyName("type")]
    public abstract string Type { get; }

    /// <summary>
    /// An optional reference to a prior inbound message being replied to. Omitted from the
    /// request payload when <see langword="null"/>.
    /// </summary>
    [JsonPropertyName("context")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Context? Context { get; }
}
