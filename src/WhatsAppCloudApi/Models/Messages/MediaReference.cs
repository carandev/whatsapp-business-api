using System.Text.Json.Serialization;

namespace WhatsAppCloudApi.Models.Messages;

/// <summary>
/// A reference to media content for image/document/audio/video/sticker messages, expressed as
/// either an uploaded media id or a public link. Exactly one of <see cref="MediaId"/> or
/// <see cref="Link"/> must be supplied — construct via <see cref="Create"/>, which validates this
/// mutual exclusivity synchronously, before any HTTP request is made.
/// </summary>
public sealed record MediaReference
{
    private MediaReference(string? mediaId, string? link)
    {
        MediaId = mediaId;
        Link = link;
    }

    /// <summary>
    /// The uploaded media's opaque Meta-assigned id. Mutually exclusive with <see cref="Link"/>.
    /// </summary>
    [JsonPropertyName("id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MediaId { get; }

    /// <summary>
    /// A public URL Meta will fetch the media from. Mutually exclusive with <see cref="MediaId"/>.
    /// </summary>
    [JsonPropertyName("link")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Link { get; }

    /// <summary>
    /// An optional caption shown alongside the media, supported by image/document/video message
    /// content (not by audio/sticker, and not by send-time template parameters). Set via a
    /// <c>with</c> expression after <see cref="Create"/>; omitted from the payload when
    /// <see langword="null"/>.
    /// </summary>
    [JsonPropertyName("caption")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Caption { get; init; }

    /// <summary>
    /// Creates a <see cref="MediaReference"/>, validating that exactly one of
    /// <paramref name="mediaId"/> or <paramref name="link"/> is supplied.
    /// </summary>
    /// <param name="mediaId">The uploaded media's opaque id, or <see langword="null"/>.</param>
    /// <param name="link">A public URL Meta will fetch the media from, or <see langword="null"/>.</param>
    /// <returns>A validated <see cref="MediaReference"/>.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when both <paramref name="mediaId"/> and <paramref name="link"/> are supplied, or
    /// when neither is supplied. Thrown synchronously before any HTTP request is sent.
    /// </exception>
    internal static MediaReference Create(string? mediaId, string? link)
    {
        var hasMediaId = !string.IsNullOrWhiteSpace(mediaId);
        var hasLink = !string.IsNullOrWhiteSpace(link);

        if (hasMediaId == hasLink)
        {
            throw new ArgumentException(
                hasMediaId
                    ? "Exactly one of mediaId or link must be supplied, not both."
                    : "Exactly one of mediaId or link must be supplied.");
        }

        return new MediaReference(mediaId, link);
    }
}
