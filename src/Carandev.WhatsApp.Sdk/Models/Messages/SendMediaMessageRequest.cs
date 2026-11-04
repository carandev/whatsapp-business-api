using System.Text.Json.Serialization;

namespace Carandev.WhatsApp.Sdk.Models.Messages;

/// <summary>
/// An image message envelope sent via <c>SendImageAsync</c>.
/// </summary>
public sealed record SendImageMessageRequest : SendMessageRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SendImageMessageRequest"/> class.
    /// </summary>
    /// <param name="to">The recipient's phone number, in E.164 format. Must not be null, empty, or whitespace.</param>
    /// <param name="mediaId">The uploaded media's opaque id. Mutually exclusive with <paramref name="link"/>.</param>
    /// <param name="link">A public URL Meta will fetch the media from. Mutually exclusive with <paramref name="mediaId"/>.</param>
    /// <param name="caption">An optional caption shown alongside the image.</param>
    /// <param name="context">An optional reference to a prior inbound message being replied to.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when both or neither of <paramref name="mediaId"/> and <paramref name="link"/> are supplied.
    /// </exception>
    public SendImageMessageRequest(string to, string? mediaId = null, string? link = null, string? caption = null, Context? context = null)
        : base(to, context)
    {
        Image = MediaReference.Create(mediaId, link) with { Caption = caption };
    }

    /// <inheritdoc />
    [JsonPropertyName("type")]
    public override string Type => "image";

    /// <summary>
    /// The image's media reference, including an optional caption.
    /// </summary>
    [JsonPropertyName("image")]
    public MediaReference Image { get; }
}

/// <summary>
/// A document message envelope sent via <c>SendDocumentAsync</c>.
/// </summary>
public sealed record SendDocumentMessageRequest : SendMessageRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SendDocumentMessageRequest"/> class.
    /// </summary>
    /// <param name="to">The recipient's phone number, in E.164 format. Must not be null, empty, or whitespace.</param>
    /// <param name="mediaId">The uploaded media's opaque id. Mutually exclusive with <paramref name="link"/>.</param>
    /// <param name="link">A public URL Meta will fetch the media from. Mutually exclusive with <paramref name="mediaId"/>.</param>
    /// <param name="caption">An optional caption shown alongside the document.</param>
    /// <param name="context">An optional reference to a prior inbound message being replied to.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when both or neither of <paramref name="mediaId"/> and <paramref name="link"/> are supplied.
    /// </exception>
    public SendDocumentMessageRequest(string to, string? mediaId = null, string? link = null, string? caption = null, Context? context = null)
        : base(to, context)
    {
        Document = MediaReference.Create(mediaId, link) with { Caption = caption };
    }

    /// <inheritdoc />
    [JsonPropertyName("type")]
    public override string Type => "document";

    /// <summary>
    /// The document's media reference, including an optional caption.
    /// </summary>
    [JsonPropertyName("document")]
    public MediaReference Document { get; }
}

/// <summary>
/// An audio message envelope sent via <c>SendAudioAsync</c>. Meta does not support a caption on
/// audio messages.
/// </summary>
public sealed record SendAudioMessageRequest : SendMessageRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SendAudioMessageRequest"/> class.
    /// </summary>
    /// <param name="to">The recipient's phone number, in E.164 format. Must not be null, empty, or whitespace.</param>
    /// <param name="mediaId">The uploaded media's opaque id. Mutually exclusive with <paramref name="link"/>.</param>
    /// <param name="link">A public URL Meta will fetch the media from. Mutually exclusive with <paramref name="mediaId"/>.</param>
    /// <param name="context">An optional reference to a prior inbound message being replied to.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when both or neither of <paramref name="mediaId"/> and <paramref name="link"/> are supplied.
    /// </exception>
    public SendAudioMessageRequest(string to, string? mediaId = null, string? link = null, Context? context = null)
        : base(to, context)
    {
        Audio = MediaReference.Create(mediaId, link);
    }

    /// <inheritdoc />
    [JsonPropertyName("type")]
    public override string Type => "audio";

    /// <summary>
    /// The audio's media reference.
    /// </summary>
    [JsonPropertyName("audio")]
    public MediaReference Audio { get; }
}

/// <summary>
/// A video message envelope sent via <c>SendVideoAsync</c>.
/// </summary>
public sealed record SendVideoMessageRequest : SendMessageRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SendVideoMessageRequest"/> class.
    /// </summary>
    /// <param name="to">The recipient's phone number, in E.164 format. Must not be null, empty, or whitespace.</param>
    /// <param name="mediaId">The uploaded media's opaque id. Mutually exclusive with <paramref name="link"/>.</param>
    /// <param name="link">A public URL Meta will fetch the media from. Mutually exclusive with <paramref name="mediaId"/>.</param>
    /// <param name="caption">An optional caption shown alongside the video.</param>
    /// <param name="context">An optional reference to a prior inbound message being replied to.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when both or neither of <paramref name="mediaId"/> and <paramref name="link"/> are supplied.
    /// </exception>
    public SendVideoMessageRequest(string to, string? mediaId = null, string? link = null, string? caption = null, Context? context = null)
        : base(to, context)
    {
        Video = MediaReference.Create(mediaId, link) with { Caption = caption };
    }

    /// <inheritdoc />
    [JsonPropertyName("type")]
    public override string Type => "video";

    /// <summary>
    /// The video's media reference, including an optional caption.
    /// </summary>
    [JsonPropertyName("video")]
    public MediaReference Video { get; }
}

/// <summary>
/// A sticker message envelope sent via <c>SendStickerAsync</c>. Meta does not support a caption on
/// sticker messages.
/// </summary>
public sealed record SendStickerMessageRequest : SendMessageRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SendStickerMessageRequest"/> class.
    /// </summary>
    /// <param name="to">The recipient's phone number, in E.164 format. Must not be null, empty, or whitespace.</param>
    /// <param name="mediaId">The uploaded media's opaque id. Mutually exclusive with <paramref name="link"/>.</param>
    /// <param name="link">A public URL Meta will fetch the media from. Mutually exclusive with <paramref name="mediaId"/>.</param>
    /// <param name="context">An optional reference to a prior inbound message being replied to.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when both or neither of <paramref name="mediaId"/> and <paramref name="link"/> are supplied.
    /// </exception>
    public SendStickerMessageRequest(string to, string? mediaId = null, string? link = null, Context? context = null)
        : base(to, context)
    {
        Sticker = MediaReference.Create(mediaId, link);
    }

    /// <inheritdoc />
    [JsonPropertyName("type")]
    public override string Type => "sticker";

    /// <summary>
    /// The sticker's media reference.
    /// </summary>
    [JsonPropertyName("sticker")]
    public MediaReference Sticker { get; }
}
