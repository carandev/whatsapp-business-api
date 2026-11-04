using System.Text.Json.Serialization;

namespace Carandev.WhatsApp.Sdk.Models.Messages;

/// <summary>
/// References a prior inbound message so a reply lands within the customer service window.
/// Passed through unmodified as <c>{"context":{"message_id":"..."}}</c> on any
/// <c>Send{X}Async</c> call; no local window-tracking logic is performed.
/// </summary>
public sealed record Context
{
    /// <summary>
    /// The id of the prior inbound message being replied to.
    /// </summary>
    [JsonPropertyName("message_id")]
    public required string MessageId { get; init; }
}
