using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Carandev.WhatsApp.Sdk.Models.Messages;
using Carandev.WhatsApp.Sdk.Serialization;

namespace Carandev.WhatsApp.Sdk.Models.Webhooks;

/// <summary>
/// An inbound shared-contact-card message (<c>type: "contacts"</c>). Reuses the outbound
/// <see cref="Contact"/> shape, since Meta documents the same contact-card object for both
/// sending and receiving.
/// </summary>
public sealed record ContactsWebhookMessage : WebhookMessage
{
    /// <summary>
    /// The one or more contact cards shared by the sender.
    /// </summary>
    [JsonPropertyName("contacts")]
    public IReadOnlyList<Contact> Contacts { get; init; } = [];
}

/// <summary>
/// A reply to a quick-reply template button (<c>type: "button"</c>). Distinct from
/// <see cref="InteractiveWebhookMessage"/>, which covers replies to interactive (non-template)
/// button/list messages.
/// </summary>
public sealed record ButtonWebhookMessage : WebhookMessage
{
    /// <summary>
    /// The tapped button's payload and display text.
    /// </summary>
    [JsonPropertyName("button")]
    public WebhookButtonPayload Button { get; init; } = default!;
}

/// <summary>
/// The button content of a <see cref="ButtonWebhookMessage"/>.
/// </summary>
public sealed record WebhookButtonPayload
{
    /// <summary>
    /// The developer-defined payload configured on the originating template button.
    /// </summary>
    [JsonPropertyName("payload")]
    public string? Payload { get; init; }

    /// <summary>
    /// The button's display text as shown to the recipient.
    /// </summary>
    [JsonPropertyName("text")]
    public string Text { get; init; } = default!;
}

/// <summary>
/// A reply to an interactive reply-buttons or list message (<c>type: "interactive"</c>). The
/// nested <see cref="WebhookInteractiveReply"/> further discriminates on
/// <c>interactive.type</c> into <see cref="WebhookButtonReply"/> (<c>button_reply</c>),
/// <see cref="WebhookListReply"/> (<c>list_reply</c>), or <see cref="WebhookFlowReply"/>
/// (<c>nfm_reply</c>).
/// </summary>
public sealed record InteractiveWebhookMessage : WebhookMessage
{
    /// <summary>
    /// The interactive reply payload.
    /// </summary>
    [JsonPropertyName("interactive")]
    public WebhookInteractiveReply Interactive { get; init; } = default!;
}

/// <summary>
/// Base type for the <c>interactive</c> object of an <see cref="InteractiveWebhookMessage"/>,
/// discriminated on the wire by the nested <c>type</c> property, dispatched by
/// <see cref="WebhookInteractiveReplyConverter"/> into <see cref="WebhookButtonReply"/>,
/// <see cref="WebhookListReply"/>, or <see cref="WebhookFlowReply"/> (not the built-in <c>[JsonPolymorphic]</c>
/// mechanism — see the remarks on <see cref="WebhookMessage"/> for why).
/// </summary>
[JsonConverter(typeof(WebhookInteractiveReplyConverter))]
public record WebhookInteractiveReply
{
}

/// <summary>
/// A reply to an interactive reply-buttons message (<c>interactive.type: "button_reply"</c>).
/// </summary>
public sealed record WebhookButtonReply : WebhookInteractiveReply
{
    /// <summary>
    /// The tapped button's id and title.
    /// </summary>
    [JsonPropertyName("button_reply")]
    public WebhookInteractiveReplyDetail ButtonReply { get; init; } = default!;
}

/// <summary>
/// A reply to an interactive list message (<c>interactive.type: "list_reply"</c>).
/// </summary>
public sealed record WebhookListReply : WebhookInteractiveReply
{
    /// <summary>
    /// The selected row's id, title, and optional description.
    /// </summary>
    [JsonPropertyName("list_reply")]
    public WebhookInteractiveReplyDetail ListReply { get; init; } = default!;
}

/// <summary>
/// A WhatsApp Flow's reply (<c>interactive.type: "nfm_reply"</c>), sent when the recipient
/// completes a Flow opened from a Flow message.
/// </summary>
public sealed record WebhookFlowReply : WebhookInteractiveReply
{
    /// <summary>
    /// The nested reply kind. Always <c>nfm_reply</c>.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type => "nfm_reply";

    /// <summary>
    /// The Flow's name, body, and answers.
    /// </summary>
    [JsonPropertyName("nfm_reply")]
    public WebhookFlowReplyContent NfmReply { get; init; } = default!;
}

/// <summary>
/// The content of a <see cref="WebhookFlowReply"/>. Meta sends the Flow's answers as a JSON
/// string in <see cref="ResponseJson"/>; its shape is whatever the Flow's terminal screen (or your
/// endpoint's final response) put in it, always including <c>flow_token</c>. The Flow id is not
/// included, so correlate replies through <see cref="FlowToken"/> or a field of your own.
/// </summary>
public sealed record WebhookFlowReplyContent
{
    /// <summary>
    /// The reply name. <c>flow</c> for Flow replies.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>
    /// The text shown in the chat for the reply, e.g. <c>Sent</c>.
    /// </summary>
    [JsonPropertyName("body")]
    public string? Body { get; init; }

    /// <summary>
    /// The Flow's answers, as the raw JSON string Meta sent.
    /// </summary>
    [JsonPropertyName("response_json")]
    public string ResponseJson { get; init; } = default!;

    /// <summary>
    /// The <c>flow_token</c> from <see cref="ResponseJson"/>, or <see langword="null"/> when the
    /// JSON is malformed, is not an object, or has no string <c>flow_token</c>. Never throws.
    /// </summary>
    [JsonIgnore]
    public string? FlowToken
    {
        get
        {
            if (string.IsNullOrEmpty(ResponseJson))
            {
                return null;
            }

            try
            {
                using var document = JsonDocument.Parse(ResponseJson);
                return document.RootElement.ValueKind == JsonValueKind.Object
                    && document.RootElement.TryGetProperty("flow_token", out var token)
                    && token.ValueKind == JsonValueKind.String
                    ? token.GetString()
                    : null;
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Parses <see cref="ResponseJson"/> into a detached <see cref="JsonElement"/>.
    /// </summary>
    /// <returns>The parsed answers.</returns>
    /// <exception cref="JsonException"><see cref="ResponseJson"/> is not valid JSON.</exception>
    public JsonElement GetResponse()
    {
        using var document = JsonDocument.Parse(ResponseJson);
        return document.RootElement.Clone();
    }

    /// <summary>
    /// Deserializes <see cref="ResponseJson"/> into <typeparamref name="T"/> through
    /// <paramref name="typeInfo"/>, typically from your own source-generated
    /// <see cref="JsonSerializerContext"/>, so it stays trimming/AOT friendly.
    /// </summary>
    /// <typeparam name="T">The type that models your Flow's answers.</typeparam>
    /// <param name="typeInfo">The metadata used to deserialize <typeparamref name="T"/>.</param>
    /// <returns>The deserialized answers, or <see langword="null"/> for a JSON <c>null</c>.</returns>
    /// <exception cref="JsonException"><see cref="ResponseJson"/> is not valid JSON for <typeparamref name="T"/>.</exception>
    public T? GetResponse<T>(JsonTypeInfo<T> typeInfo)
    {
        ArgumentNullException.ThrowIfNull(typeInfo);
        return JsonSerializer.Deserialize(ResponseJson, typeInfo);
    }
}

/// <summary>
/// The tapped button or selected row detail shared by <see cref="WebhookButtonReply"/> and
/// <see cref="WebhookListReply"/>.
/// </summary>
public sealed record WebhookInteractiveReplyDetail
{
    /// <summary>
    /// The tapped button's, or selected row's, opaque postback id.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; init; } = default!;

    /// <summary>
    /// The tapped button's, or selected row's, display title.
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; init; } = default!;

    /// <summary>
    /// The selected row's optional description. Never present for a button reply.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }
}

/// <summary>
/// A reaction (emoji) applied to a prior message (<c>type: "reaction"</c>).
/// </summary>
public sealed record ReactionWebhookMessage : WebhookMessage
{
    /// <summary>
    /// The reaction content.
    /// </summary>
    [JsonPropertyName("reaction")]
    public WebhookReaction Reaction { get; init; } = default!;
}

/// <summary>
/// The reaction content of a <see cref="ReactionWebhookMessage"/>.
/// </summary>
public sealed record WebhookReaction
{
    /// <summary>
    /// The id of the message being reacted to.
    /// </summary>
    [JsonPropertyName("message_id")]
    public string MessageId { get; init; } = default!;

    /// <summary>
    /// The reaction emoji. Absent or empty when the sender removed a previously applied reaction.
    /// </summary>
    [JsonPropertyName("emoji")]
    public string? Emoji { get; init; }
}

/// <summary>
/// A "Get started" trigger from a click-to-WhatsApp welcome flow (<c>type: "request_welcome"</c>).
/// Carries no fields beyond the common <see cref="WebhookMessage"/> envelope.
/// </summary>
public sealed record RequestWelcomeWebhookMessage : WebhookMessage
{
}

/// <summary>
/// A system-generated notification, e.g. a customer's phone number changing
/// (<c>type: "system"</c>).
/// </summary>
public sealed record SystemWebhookMessage : WebhookMessage
{
    /// <summary>
    /// The system event content.
    /// </summary>
    [JsonPropertyName("system")]
    public WebhookSystemContent System { get; init; } = default!;
}

/// <summary>
/// The system event content of a <see cref="SystemWebhookMessage"/>.
/// </summary>
public sealed record WebhookSystemContent
{
    /// <summary>
    /// A human-readable description of the system event.
    /// </summary>
    [JsonPropertyName("body")]
    public string? Body { get; init; }

    /// <summary>
    /// The affected customer's WhatsApp id before the change.
    /// </summary>
    [JsonPropertyName("wa_id")]
    public string? WaId { get; init; }

    /// <summary>
    /// The affected customer's new WhatsApp id, present for a number-change event.
    /// </summary>
    [JsonPropertyName("new_wa_id")]
    public string? NewWaId { get; init; }

    /// <summary>
    /// The system event kind, e.g. <c>customer_changed_number</c>.
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; init; }
}

/// <summary>
/// Dispatches <see cref="WebhookInteractiveReply"/> deserialization to
/// <see cref="WebhookButtonReply"/>, <see cref="WebhookListReply"/>, or
/// <see cref="WebhookFlowReply"/> based on the raw nested
/// <c>type</c> property, regardless of property order. Falls back to a bare
/// <see cref="WebhookInteractiveReply"/> for an unrecognized nested type, never throwing.
/// </summary>
public sealed class WebhookInteractiveReplyConverter : JsonConverter<WebhookInteractiveReply>
{
    /// <inheritdoc />
    public override WebhookInteractiveReply Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;
        var type = root.TryGetProperty("type", out var typeProperty) ? typeProperty.GetString() : null;
        var context = WhatsAppJsonSerializerContext.Default;

        return type switch
        {
            "button_reply" => JsonSerializer.Deserialize(root, context.WebhookButtonReply)!,
            "list_reply" => JsonSerializer.Deserialize(root, context.WebhookListReply)!,
            "nfm_reply" => JsonSerializer.Deserialize(root, context.WebhookFlowReply)!,
            _ => new WebhookInteractiveReply(),
        };
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, WebhookInteractiveReply value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(value);

        var context = WhatsAppJsonSerializerContext.Default;

        switch (value)
        {
            case WebhookButtonReply buttonReply:
                JsonSerializer.Serialize(writer, buttonReply, context.WebhookButtonReply);
                return;
            case WebhookListReply listReply:
                JsonSerializer.Serialize(writer, listReply, context.WebhookListReply);
                return;
            case WebhookFlowReply flowReply:
                JsonSerializer.Serialize(writer, flowReply, context.WebhookFlowReply);
                return;
            default:
                writer.WriteStartObject();
                writer.WriteEndObject();
                return;
        }
    }
}
