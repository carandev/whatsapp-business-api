using System.Text.Json.Serialization;

namespace Carandev.WhatsApp.Sdk.Models.Media;

/// <summary>
/// Represents the response returned by the Graph Resumable Upload API after the file content was
/// uploaded via <c>POST /upload:&lt;session&gt;</c>.
/// </summary>
public sealed class ResumableUploadHandleResponse
{
    /// <summary>
    /// The uploaded file handle, used as a media header example when creating a message template.
    /// </summary>
    [JsonPropertyName("h")]
    public string? Handle { get; set; }
}
