using System.Text.Json.Serialization;

namespace WhatsAppCloudApi.Models.Messages;

/// <summary>
/// A text message envelope sent via <c>SendTextAsync</c>.
/// </summary>
public sealed record SendTextMessageRequest : SendMessageRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SendTextMessageRequest"/> class.
    /// </summary>
    /// <param name="to">The recipient's phone number, in E.164 format. Must not be null, empty, or whitespace.</param>
    /// <param name="text">The text content of the message.</param>
    /// <param name="context">An optional reference to a prior inbound message being replied to.</param>
    public SendTextMessageRequest(string to, TextContent text, Context? context = null)
        : base(to, context)
    {
        ArgumentNullException.ThrowIfNull(text);
        Text = text;
    }

    /// <inheritdoc />
    [JsonPropertyName("type")]
    public override string Type => "text";

    /// <summary>
    /// The text content of the message.
    /// </summary>
    [JsonPropertyName("text")]
    public TextContent Text { get; }
}

/// <summary>
/// The text content of a <see cref="SendTextMessageRequest"/>.
/// </summary>
public sealed record TextContent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TextContent"/> class.
    /// </summary>
    /// <param name="body">The message body. Must not be null, empty, or whitespace.</param>
    /// <param name="previewUrl">Whether Meta should render a link preview for a URL found in <paramref name="body"/>.</param>
    public TextContent(string body, bool previewUrl = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(body);
        Body = body;
        PreviewUrl = previewUrl;
    }

    /// <summary>
    /// The message body.
    /// </summary>
    [JsonPropertyName("body")]
    public string Body { get; }

    /// <summary>
    /// Whether Meta should render a link preview for a URL found in <see cref="Body"/>.
    /// </summary>
    [JsonPropertyName("preview_url")]
    public bool PreviewUrl { get; }
}
