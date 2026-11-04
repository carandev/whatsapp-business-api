using System.Text.Json.Serialization;

namespace WhatsAppCloudApi.Serialization;

/// <summary>
/// Mirrors the raw <c>{"messages":[{"id":"..."}]}</c> envelope Meta returns from a successful
/// <c>POST /{Phone-Number-ID}/messages</c> call. Internal — used only to deserialize the wire
/// response and flatten it into the public
/// <see cref="WhatsAppCloudApi.Models.Messages.MessageResponse"/>; the outer array is a Graph API
/// implementation detail that should not leak into the public surface.
/// </summary>
internal sealed class SendMessageResponseEnvelope
{
    /// <summary>
    /// The sent message(s). The Cloud API always returns exactly one element for a single
    /// <c>Send{X}Async</c> call.
    /// </summary>
    [JsonPropertyName("messages")]
    public IReadOnlyList<SendMessageResponseEnvelopeEntry>? Messages { get; set; }
}

/// <summary>
/// A single entry within a <see cref="SendMessageResponseEnvelope"/>.
/// </summary>
internal sealed class SendMessageResponseEnvelopeEntry
{
    /// <summary>
    /// The Meta-assigned id of the sent message.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }
}

/// <summary>
/// Dedicated source-generated <see cref="JsonSerializerContext"/> for the internal
/// <see cref="SendMessageResponseEnvelope"/>/<see cref="SendMessageResponseEnvelopeEntry"/> types.
/// </summary>
/// <remarks>
/// Registering internal types directly on the public <see cref="WhatsAppJsonSerializerContext"/>
/// fails to compile (<c>CS0053</c>): the source generator always emits a <see langword="public"/>
/// <c>JsonTypeInfo&lt;T&gt;</c> property on a public context, which is inaccessible when the
/// registered type is internal. Using a separate, internal context — matching this internal DTO
/// pair's own accessibility — sidesteps the limitation while keeping the source-gen
/// (trimming/AOT-friendly) behavior for these types too.
/// </remarks>
[JsonSerializable(typeof(SendMessageResponseEnvelope))]
[JsonSerializable(typeof(SendMessageResponseEnvelopeEntry))]
internal partial class SendMessageResponseSerializerContext : JsonSerializerContext
{
}
