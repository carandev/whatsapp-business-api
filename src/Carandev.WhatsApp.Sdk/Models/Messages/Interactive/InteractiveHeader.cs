using System.Text.Json.Serialization;

namespace Carandev.WhatsApp.Sdk.Models.Messages.Interactive;

/// <summary>
/// Base type for an interactive message's optional <c>header</c>, shared by reply-button and
/// CTA-URL interactive messages. Discriminated on the wire by Meta's <c>type</c> property into one
/// of <see cref="TextInteractiveHeader"/>, <see cref="ImageInteractiveHeader"/>,
/// <see cref="VideoInteractiveHeader"/>, or <see cref="DocumentInteractiveHeader"/>.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(TextInteractiveHeader), "text")]
[JsonDerivedType(typeof(ImageInteractiveHeader), "image")]
[JsonDerivedType(typeof(VideoInteractiveHeader), "video")]
[JsonDerivedType(typeof(DocumentInteractiveHeader), "document")]
public abstract record InteractiveHeader
{
    private protected InteractiveHeader()
    {
    }
}

/// <summary>
/// An interactive message header whose content is plain text.
/// </summary>
public sealed record TextInteractiveHeader : InteractiveHeader
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TextInteractiveHeader"/> class.
    /// </summary>
    /// <param name="text">The header text. Must not be null, empty, or whitespace.</param>
    public TextInteractiveHeader(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        Text = text;
    }

    /// <summary>
    /// The header text.
    /// </summary>
    [JsonPropertyName("text")]
    public string Text { get; }
}

/// <summary>
/// An interactive message header whose content is an image.
/// </summary>
public sealed record ImageInteractiveHeader : InteractiveHeader
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ImageInteractiveHeader"/> class. Exactly one of
    /// <paramref name="mediaId"/> or <paramref name="link"/> must be supplied.
    /// </summary>
    /// <param name="mediaId">The uploaded media's opaque id. Mutually exclusive with <paramref name="link"/>.</param>
    /// <param name="link">A public URL Meta will fetch the media from. Mutually exclusive with <paramref name="mediaId"/>.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when both or neither of <paramref name="mediaId"/> and <paramref name="link"/> are supplied.
    /// </exception>
    public ImageInteractiveHeader(string? mediaId = null, string? link = null)
    {
        Image = MediaReference.Create(mediaId, link);
    }

    /// <summary>
    /// The header image's media reference.
    /// </summary>
    [JsonPropertyName("image")]
    public MediaReference Image { get; }
}

/// <summary>
/// An interactive message header whose content is a video.
/// </summary>
public sealed record VideoInteractiveHeader : InteractiveHeader
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VideoInteractiveHeader"/> class. Exactly one of
    /// <paramref name="mediaId"/> or <paramref name="link"/> must be supplied.
    /// </summary>
    /// <param name="mediaId">The uploaded media's opaque id. Mutually exclusive with <paramref name="link"/>.</param>
    /// <param name="link">A public URL Meta will fetch the media from. Mutually exclusive with <paramref name="mediaId"/>.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when both or neither of <paramref name="mediaId"/> and <paramref name="link"/> are supplied.
    /// </exception>
    public VideoInteractiveHeader(string? mediaId = null, string? link = null)
    {
        Video = MediaReference.Create(mediaId, link);
    }

    /// <summary>
    /// The header video's media reference.
    /// </summary>
    [JsonPropertyName("video")]
    public MediaReference Video { get; }
}

/// <summary>
/// An interactive message header whose content is a document.
/// </summary>
public sealed record DocumentInteractiveHeader : InteractiveHeader
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentInteractiveHeader"/> class. Exactly one
    /// of <paramref name="mediaId"/> or <paramref name="link"/> must be supplied.
    /// </summary>
    /// <param name="mediaId">The uploaded media's opaque id. Mutually exclusive with <paramref name="link"/>.</param>
    /// <param name="link">A public URL Meta will fetch the media from. Mutually exclusive with <paramref name="mediaId"/>.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when both or neither of <paramref name="mediaId"/> and <paramref name="link"/> are supplied.
    /// </exception>
    public DocumentInteractiveHeader(string? mediaId = null, string? link = null)
    {
        Document = MediaReference.Create(mediaId, link);
    }

    /// <summary>
    /// The header document's media reference.
    /// </summary>
    [JsonPropertyName("document")]
    public MediaReference Document { get; }
}
