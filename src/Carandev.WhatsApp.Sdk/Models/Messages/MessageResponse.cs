using System.Text.Json.Serialization;

namespace Carandev.WhatsApp.Sdk.Models.Messages;

/// <summary>
/// Represents the outcome of a successful <c>Send{X}Async</c> call. Mutable and attribute-driven
/// like <see cref="Carandev.WhatsApp.Sdk.Serialization.WhatsAppErrorResponse"/>, since response DTOs
/// carry no construction-time invariants. Flattened from Meta's <c>{"messages":[{"id":"..."}]}</c>
/// array wrapper via <see cref="Carandev.WhatsApp.Sdk.Serialization.SendMessageResponseEnvelope"/> — the
/// array is a Graph API implementation detail, not part of this public surface.
/// </summary>
public sealed class MessageResponse
{
    /// <summary>
    /// The Meta-assigned id of the sent message.
    /// </summary>
    [JsonPropertyName("message_id")]
    public string? MessageId { get; set; }
}
