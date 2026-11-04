using System.Text.Json.Serialization;

namespace WhatsAppCloudApi.Models.Media;

/// <summary>
/// Represents the response returned by Meta after a successful media upload. Mutable and
/// attribute-driven like <see cref="WhatsAppCloudApi.Serialization.WhatsAppErrorResponse"/>, since
/// response DTOs carry no construction-time invariants.
/// </summary>
public sealed class MediaUploadResponse
{
    /// <summary>
    /// The uploaded media's Meta-assigned identifier, used to reference it in outbound messages.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }
}
