using System.Text.Json.Serialization;

namespace WhatsAppCloudApi.Models.Media;

/// <summary>
/// Represents the response returned by Meta when resolving a media id to a downloadable CDN URL
/// via <c>GET /{media-id}</c>. Mutable and attribute-driven like <see cref="MediaUploadResponse"/>,
/// since response DTOs carry no construction-time invariants.
/// </summary>
public sealed class MediaUrlResponse
{
    /// <summary>
    /// The absolute, short-lived CDN URL the media content can be downloaded from. Requires the
    /// same <c>Authorization: Bearer</c> header used for other Graph API calls.
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>
    /// The media's MIME type (e.g. <c>image/jpeg</c>).
    /// </summary>
    [JsonPropertyName("mime_type")]
    public string? MimeType { get; set; }

    /// <summary>
    /// The SHA-256 hash of the media content, as reported by Meta.
    /// </summary>
    [JsonPropertyName("sha256")]
    public string? Sha256 { get; set; }

    /// <summary>
    /// The media content's size, in bytes.
    /// </summary>
    [JsonPropertyName("file_size")]
    public long FileSize { get; set; }

    /// <summary>
    /// The media's Meta-assigned identifier, echoed back from the request.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }
}
