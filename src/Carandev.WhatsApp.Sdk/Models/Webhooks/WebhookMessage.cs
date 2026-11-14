using System.Text.Json;
using System.Text.Json.Serialization;
using Carandev.WhatsApp.Sdk.Serialization;

namespace Carandev.WhatsApp.Sdk.Models.Webhooks;

/// <summary>
/// Base type for a single inbound message item within
/// <see cref="MessagesWebhookPayload.Messages"/>. Discriminated on the wire by the Meta
/// <c>type</c> property, dispatched by <see cref="WebhookMessageConverter"/>. Deliberately
/// <b>concrete</b> (not <c>abstract</c>): an unrecognized <c>type</c> value falls back to an
/// instance of this base type itself rather than throwing.
/// </summary>
/// <remarks>
/// Not decorated with the built-in <c>[JsonPolymorphic]</c>/<c>[JsonDerivedType]</c> attributes
/// used elsewhere in this codebase (e.g. <see cref="Templates.TemplateComponent"/>): System.Text.Json's
/// source-generated serializer requires the type discriminator to be the <b>first</b> JSON
/// property when using that built-in mechanism, but Meta's real inbound webhook payloads place
/// <c>type</c> after <c>from</c>/<c>id</c>/<c>timestamp</c> (and, for some kinds, even after the
/// kind-specific content object). <see cref="WebhookMessageConverter"/> instead buffers the raw
/// JSON and dispatches on <c>type</c> regardless of property order.
/// </remarks>
[JsonConverter(typeof(WebhookMessageConverter))]
public record WebhookMessage
{
    /// <summary>
    /// The sender's WhatsApp id (phone number, without a leading <c>+</c>).
    /// </summary>
    [JsonPropertyName("from")]
    public string From { get; init; } = default!;

    /// <summary>
    /// The Meta-assigned id of this message, usable as a <c>context</c> reference on a reply.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; init; } = default!;

    /// <summary>
    /// The Unix timestamp (seconds, as a string) this message was sent.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public string Timestamp { get; init; } = default!;

    /// <summary>
    /// Present when this message is a reply to, or forward of, a prior message (e.g. a template
    /// quick-reply button tap, or a forwarded message).
    /// </summary>
    [JsonPropertyName("context")]
    public WebhookMessageContext? Context { get; init; }

    /// <summary>
    /// The raw Meta <c>type</c> discriminator this message was read with (e.g. <c>"image"</c>, or
    /// <c>"order"</c> for an unrecognized kind), set by <see cref="WebhookMessageConverter"/>.
    /// <see langword="null"/> on a message constructed in code. When writing, the converter
    /// emits the concrete type's own discriminator, and falls back to this value only for a
    /// bare <see cref="WebhookMessage"/>, so an unrecognized kind round-trips unchanged.
    /// </summary>
    [JsonIgnore]
    public string? Type { get; init; }

    /// <summary>
    /// Captures the raw kind-specific fields (e.g. <c>"order"</c>) that this concrete base type
    /// does not model, populated by <see cref="WebhookMessageConverter"/> only when an
    /// unrecognized <c>type</c> falls back to this base type — so an unrecognized kind never
    /// silently drops data.
    /// </summary>
    public IReadOnlyDictionary<string, JsonElement>? Extra { get; init; }
}

/// <summary>
/// References a prior message a <see cref="WebhookMessage"/> replies to or forwards.
/// </summary>
public sealed record WebhookMessageContext
{
    /// <summary>
    /// The WhatsApp id of the sender of the original message being referenced.
    /// </summary>
    [JsonPropertyName("from")]
    public string? From { get; init; }

    /// <summary>
    /// The id of the original message being referenced.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    /// <summary>
    /// <see langword="true"/> when the referenced message was forwarded.
    /// </summary>
    [JsonPropertyName("forwarded")]
    public bool? Forwarded { get; init; }

    /// <summary>
    /// <see langword="true"/> when the referenced message was forwarded frequently (more than 5
    /// times), per Meta's forwarding-limit heuristics.
    /// </summary>
    [JsonPropertyName("frequently_forwarded")]
    public bool? FrequentlyForwarded { get; init; }
}

/// <summary>
/// A free-form text message (<c>type: "text"</c>).
/// </summary>
public sealed record TextWebhookMessage : WebhookMessage
{
    /// <summary>
    /// The text content.
    /// </summary>
    [JsonPropertyName("text")]
    public WebhookTextContent Text { get; init; } = default!;
}

/// <summary>
/// The text content of a <see cref="TextWebhookMessage"/>.
/// </summary>
public sealed record WebhookTextContent
{
    /// <summary>
    /// The message body text.
    /// </summary>
    [JsonPropertyName("body")]
    public string Body { get; init; } = default!;
}

/// <summary>
/// An inbound image message (<c>type: "image"</c>).
/// </summary>
public sealed record ImageWebhookMessage : WebhookMessage
{
    /// <summary>
    /// The image content metadata.
    /// </summary>
    [JsonPropertyName("image")]
    public WebhookMediaContent Image { get; init; } = default!;
}

/// <summary>
/// An inbound document message (<c>type: "document"</c>).
/// </summary>
public sealed record DocumentWebhookMessage : WebhookMessage
{
    /// <summary>
    /// The document content metadata.
    /// </summary>
    [JsonPropertyName("document")]
    public WebhookMediaContent Document { get; init; } = default!;
}

/// <summary>
/// An inbound audio message (<c>type: "audio"</c>), including voice notes.
/// </summary>
public sealed record AudioWebhookMessage : WebhookMessage
{
    /// <summary>
    /// The audio content metadata.
    /// </summary>
    [JsonPropertyName("audio")]
    public WebhookMediaContent Audio { get; init; } = default!;
}

/// <summary>
/// An inbound video message (<c>type: "video"</c>).
/// </summary>
public sealed record VideoWebhookMessage : WebhookMessage
{
    /// <summary>
    /// The video content metadata.
    /// </summary>
    [JsonPropertyName("video")]
    public WebhookMediaContent Video { get; init; } = default!;
}

/// <summary>
/// An inbound sticker message (<c>type: "sticker"</c>).
/// </summary>
public sealed record StickerWebhookMessage : WebhookMessage
{
    /// <summary>
    /// The sticker content metadata.
    /// </summary>
    [JsonPropertyName("sticker")]
    public WebhookMediaContent Sticker { get; init; } = default!;
}

/// <summary>
/// The shared media metadata shape for image/document/audio/video/sticker messages. Not every
/// field applies to every kind: <see cref="Caption"/> applies to image/document/video only;
/// <see cref="Filename"/> to document only; <see cref="Voice"/> to audio only; <see cref="Animated"/>
/// to sticker only.
/// </summary>
public sealed record WebhookMediaContent
{
    /// <summary>
    /// The Meta-assigned media id, usable with the Media API to download the content.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; init; } = default!;

    /// <summary>
    /// The media's MIME type.
    /// </summary>
    [JsonPropertyName("mime_type")]
    public string MimeType { get; init; } = default!;

    /// <summary>
    /// The SHA256 hash of the media content, for integrity verification after download.
    /// </summary>
    [JsonPropertyName("sha256")]
    public string Sha256 { get; init; } = default!;

    /// <summary>
    /// The optional caption accompanying image/document/video content.
    /// </summary>
    [JsonPropertyName("caption")]
    public string? Caption { get; init; }

    /// <summary>
    /// The original filename of document content.
    /// </summary>
    [JsonPropertyName("filename")]
    public string? Filename { get; init; }

    /// <summary>
    /// <see langword="true"/> when audio content is a voice note (recorded in-app) rather than an
    /// uploaded audio file.
    /// </summary>
    [JsonPropertyName("voice")]
    public bool? Voice { get; init; }

    /// <summary>
    /// <see langword="true"/> when sticker content is animated.
    /// </summary>
    [JsonPropertyName("animated")]
    public bool? Animated { get; init; }
}

/// <summary>
/// An inbound location message (<c>type: "location"</c>).
/// </summary>
public sealed record LocationWebhookMessage : WebhookMessage
{
    /// <summary>
    /// The shared location.
    /// </summary>
    [JsonPropertyName("location")]
    public WebhookLocation Location { get; init; } = default!;
}

/// <summary>
/// The location content of a <see cref="LocationWebhookMessage"/>.
/// </summary>
public sealed record WebhookLocation
{
    /// <summary>
    /// The shared location's latitude.
    /// </summary>
    [JsonPropertyName("latitude")]
    public double Latitude { get; init; }

    /// <summary>
    /// The shared location's longitude.
    /// </summary>
    [JsonPropertyName("longitude")]
    public double Longitude { get; init; }

    /// <summary>
    /// An optional name for the location.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>
    /// An optional address for the location.
    /// </summary>
    [JsonPropertyName("address")]
    public string? Address { get; init; }
}

/// <summary>
/// Dispatches <see cref="WebhookMessage"/> deserialization to the concrete type matching the raw
/// <c>type</c> property, regardless of where that property appears in the JSON object — unlike
/// the built-in <c>[JsonPolymorphic]</c> converter, this does not require the discriminator to be
/// first. Falls back to a bare <see cref="WebhookMessage"/> (with unmapped fields captured in
/// <see cref="WebhookMessage.Extra"/>) for an unrecognized <c>type</c>, never throwing.
/// </summary>
public sealed class WebhookMessageConverter : JsonConverter<WebhookMessage>
{
    private static readonly string[] KnownBaseProperties = ["from", "id", "timestamp", "context", "type"];

    /// <inheritdoc />
    public override WebhookMessage Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;
        var type = root.TryGetProperty("type", out var typeProperty) ? typeProperty.GetString() : null;
        var context = WhatsAppJsonSerializerContext.Default;

        WebhookMessage message = type switch
        {
            "text" => JsonSerializer.Deserialize(root, context.TextWebhookMessage)!,
            "image" => JsonSerializer.Deserialize(root, context.ImageWebhookMessage)!,
            "document" => JsonSerializer.Deserialize(root, context.DocumentWebhookMessage)!,
            "audio" => JsonSerializer.Deserialize(root, context.AudioWebhookMessage)!,
            "video" => JsonSerializer.Deserialize(root, context.VideoWebhookMessage)!,
            "sticker" => JsonSerializer.Deserialize(root, context.StickerWebhookMessage)!,
            "location" => JsonSerializer.Deserialize(root, context.LocationWebhookMessage)!,
            "contacts" => JsonSerializer.Deserialize(root, context.ContactsWebhookMessage)!,
            "button" => JsonSerializer.Deserialize(root, context.ButtonWebhookMessage)!,
            "interactive" => JsonSerializer.Deserialize(root, context.InteractiveWebhookMessage)!,
            "reaction" => JsonSerializer.Deserialize(root, context.ReactionWebhookMessage)!,
            "request_welcome" => JsonSerializer.Deserialize(root, context.RequestWelcomeWebhookMessage)!,
            "system" => JsonSerializer.Deserialize(root, context.SystemWebhookMessage)!,
            _ => ReadUnknown(root, context),
        };

        return message with { Type = type };
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, WebhookMessage value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(value);

        var context = WhatsAppJsonSerializerContext.Default;

        var (typeName, element) = value switch
        {
            TextWebhookMessage text => ("text", JsonSerializer.SerializeToElement(text, context.TextWebhookMessage)),
            ImageWebhookMessage image => ("image", JsonSerializer.SerializeToElement(image, context.ImageWebhookMessage)),
            DocumentWebhookMessage document => ("document", JsonSerializer.SerializeToElement(document, context.DocumentWebhookMessage)),
            AudioWebhookMessage audio => ("audio", JsonSerializer.SerializeToElement(audio, context.AudioWebhookMessage)),
            VideoWebhookMessage video => ("video", JsonSerializer.SerializeToElement(video, context.VideoWebhookMessage)),
            StickerWebhookMessage sticker => ("sticker", JsonSerializer.SerializeToElement(sticker, context.StickerWebhookMessage)),
            LocationWebhookMessage location => ("location", JsonSerializer.SerializeToElement(location, context.LocationWebhookMessage)),
            ContactsWebhookMessage contacts => ("contacts", JsonSerializer.SerializeToElement(contacts, context.ContactsWebhookMessage)),
            ButtonWebhookMessage button => ("button", JsonSerializer.SerializeToElement(button, context.ButtonWebhookMessage)),
            InteractiveWebhookMessage interactive => ("interactive", JsonSerializer.SerializeToElement(interactive, context.InteractiveWebhookMessage)),
            ReactionWebhookMessage reaction => ("reaction", JsonSerializer.SerializeToElement(reaction, context.ReactionWebhookMessage)),
            RequestWelcomeWebhookMessage requestWelcome => ("request_welcome", JsonSerializer.SerializeToElement(requestWelcome, context.RequestWelcomeWebhookMessage)),
            SystemWebhookMessage system => ("system", JsonSerializer.SerializeToElement(system, context.SystemWebhookMessage)),
            _ => (value.Type, default(JsonElement)),
        };

        if (element.ValueKind == JsonValueKind.Undefined)
        {
            WriteUnknown(writer, value, context);
            return;
        }

        // The concrete types carry no "type" property of their own, so it is written here, in
        // the same position Meta uses (after from/id/timestamp), to keep Read able to dispatch.
        writer.WriteStartObject();
        foreach (var property in element.EnumerateObject())
        {
            if (property.NameEquals("type"))
            {
                continue;
            }

            property.WriteTo(writer);
            if (property.NameEquals("timestamp"))
            {
                writer.WriteString("type", typeName);
                typeName = null;
            }
        }

        if (typeName is not null)
        {
            writer.WriteString("type", typeName);
        }

        writer.WriteEndObject();
    }

    private static WebhookMessage ReadUnknown(JsonElement root, WhatsAppJsonSerializerContext context)
    {
        var extra = new Dictionary<string, JsonElement>();
        foreach (var property in root.EnumerateObject())
        {
            if (Array.IndexOf(KnownBaseProperties, property.Name) < 0)
            {
                extra[property.Name] = property.Value.Clone();
            }
        }

        return new WebhookMessage
        {
            From = root.TryGetProperty("from", out var from) ? from.GetString() ?? default! : default!,
            Id = root.TryGetProperty("id", out var id) ? id.GetString() ?? default! : default!,
            Timestamp = root.TryGetProperty("timestamp", out var timestamp) ? timestamp.GetString() ?? default! : default!,
            Context = root.TryGetProperty("context", out var contextElement)
                ? JsonSerializer.Deserialize(contextElement, context.WebhookMessageContext)
                : null,
            Extra = extra.Count > 0 ? extra : null,
        };
    }

    private static void WriteUnknown(Utf8JsonWriter writer, WebhookMessage value, WhatsAppJsonSerializerContext context)
    {
        writer.WriteStartObject();
        writer.WriteString("from", value.From);
        writer.WriteString("id", value.Id);
        writer.WriteString("timestamp", value.Timestamp);
        if (value.Type is not null)
        {
            writer.WriteString("type", value.Type);
        }

        if (value.Context is not null)
        {
            writer.WritePropertyName("context");
            JsonSerializer.Serialize(writer, value.Context, context.WebhookMessageContext);
        }

        if (value.Extra is not null)
        {
            foreach (var (key, element) in value.Extra)
            {
                writer.WritePropertyName(key);
                element.WriteTo(writer);
            }
        }

        writer.WriteEndObject();
    }
}
