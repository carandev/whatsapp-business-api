using System.Text.Json.Serialization;

namespace Carandev.WhatsApp.Sdk.Models.Media;

/// <summary>
/// Represents the response returned by the Graph Resumable Upload API after starting an upload
/// session via <c>POST /{app-id}/uploads</c>.
/// </summary>
public sealed class ResumableUploadSessionResponse
{
    /// <summary>
    /// The upload session identifier, in the form <c>upload:&lt;session&gt;</c>. It is used as the
    /// path of the subsequent upload request.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }
}
