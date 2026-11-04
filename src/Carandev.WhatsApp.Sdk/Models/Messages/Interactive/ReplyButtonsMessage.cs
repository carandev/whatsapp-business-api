using System.Text.Json.Serialization;

namespace Carandev.WhatsApp.Sdk.Models.Messages.Interactive;

/// <summary>
/// One reply button offered to the recipient, identified by an opaque postback <see cref="Id"/>
/// and a user-facing <see cref="Title"/>. Construction validates synchronously, before any HTTP
/// request is sent: <c>id</c> must not be null, empty, whitespace, or padded with leading/trailing
/// whitespace; <c>title</c> must not be null, empty, whitespace, or longer than 20 characters
/// (Meta's documented limit).
/// </summary>
public sealed record ReplyButton
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReplyButton"/> class.
    /// </summary>
    /// <param name="id">
    /// The opaque postback id returned when the recipient taps this button. Must not be null,
    /// empty, whitespace, or have leading/trailing whitespace.
    /// </param>
    /// <param name="title">
    /// The user-facing button label. Must not be null, empty, whitespace, or longer than 20
    /// characters.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="id"/> is null, empty, whitespace, or has leading/trailing
    /// whitespace; or when <paramref name="title"/> is null, empty, whitespace, or exceeds 20
    /// characters.
    /// </exception>
    public ReplyButton(string id, string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        if (id != id.Trim())
        {
            throw new ArgumentException("id must not have leading or trailing whitespace.", nameof(id));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        if (title.Length > 20)
        {
            throw new ArgumentException("title must not exceed 20 characters.", nameof(title));
        }

        Id = id;
        Title = title;
    }

    /// <summary>
    /// The opaque postback id returned when the recipient taps this button.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; }

    /// <summary>
    /// The user-facing button label.
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; }
}

/// <summary>
/// The internal wire-shape wrapper Meta expects for each entry in
/// <c>interactive.action.buttons</c>: <c>{"type":"reply","reply":{"id":...,"title":...}}</c>. Kept
/// separate from the public <see cref="ReplyButton"/> so that type's API surface stays exactly
/// <c>ReplyButton(id, title)</c> as specified.
/// </summary>
public sealed record ReplyButtonEnvelope
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReplyButtonEnvelope"/> class.
    /// </summary>
    /// <param name="reply">The reply button being wrapped.</param>
    public ReplyButtonEnvelope(ReplyButton reply)
    {
        ArgumentNullException.ThrowIfNull(reply);
        Reply = reply;
    }

    /// <summary>
    /// The button kind. Always <c>reply</c>.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type => "reply";

    /// <summary>
    /// The wrapped reply button's id and title.
    /// </summary>
    [JsonPropertyName("reply")]
    public ReplyButton Reply { get; }
}

/// <summary>
/// The <c>action</c> object of a reply-buttons interactive message: 1-3
/// <see cref="ReplyButtonEnvelope"/> entries. Construction validates the count synchronously,
/// before any HTTP request is sent.
/// </summary>
public sealed record ReplyButtonsAction
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReplyButtonsAction"/> class.
    /// </summary>
    /// <param name="buttons">The 1-3 reply buttons offered to the recipient.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="buttons"/> is null, empty, or contains more than 3 items.
    /// </exception>
    public ReplyButtonsAction(IReadOnlyList<ReplyButton> buttons)
    {
        if (buttons is null || buttons.Count is < 1 or > 3)
        {
            throw new ArgumentException("buttons must contain between 1 and 3 items.", nameof(buttons));
        }

        Buttons = buttons.Select(button => new ReplyButtonEnvelope(button)).ToList();
    }

    /// <summary>
    /// The 1-3 reply buttons offered to the recipient.
    /// </summary>
    [JsonPropertyName("buttons")]
    public IReadOnlyList<ReplyButtonEnvelope> Buttons { get; }
}

/// <summary>
/// The <c>interactive</c> object of a reply-buttons message. Not <c>[JsonPolymorphic]</c> — see
/// remarks on <see cref="SendMessageRequest"/> for the rationale shared by all interactive kinds.
/// </summary>
public sealed record ReplyButtonsInteractive
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReplyButtonsInteractive"/> class.
    /// </summary>
    /// <param name="body">The required message body.</param>
    /// <param name="action">The 1-3 reply buttons offered to the recipient.</param>
    /// <param name="header">An optional header of any kind (text, image, video, document).</param>
    /// <param name="footer">Optional footer text.</param>
    public ReplyButtonsInteractive(InteractiveBody body, ReplyButtonsAction action, InteractiveHeader? header = null, InteractiveFooter? footer = null)
    {
        ArgumentNullException.ThrowIfNull(body);
        ArgumentNullException.ThrowIfNull(action);
        Body = body;
        Action = action;
        Header = header;
        Footer = footer;
    }

    /// <summary>
    /// The interactive message kind. Always <c>button</c>.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type => "button";

    /// <summary>
    /// An optional header of any kind (text, image, video, document). Omitted from the payload
    /// when <see langword="null"/>.
    /// </summary>
    [JsonPropertyName("header")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public InteractiveHeader? Header { get; }

    /// <summary>
    /// The required message body.
    /// </summary>
    [JsonPropertyName("body")]
    public InteractiveBody Body { get; }

    /// <summary>
    /// Optional footer text. Omitted from the payload when <see langword="null"/>.
    /// </summary>
    [JsonPropertyName("footer")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public InteractiveFooter? Footer { get; }

    /// <summary>
    /// The 1-3 reply buttons offered to the recipient.
    /// </summary>
    [JsonPropertyName("action")]
    public ReplyButtonsAction Action { get; }
}

/// <summary>
/// A reply-buttons interactive message envelope sent via <c>SendReplyButtonsAsync</c>.
/// </summary>
public sealed record SendReplyButtonsMessageRequest : SendMessageRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SendReplyButtonsMessageRequest"/> class.
    /// </summary>
    /// <param name="to">The recipient's phone number, in E.164 format. Must not be null, empty, or whitespace.</param>
    /// <param name="bodyText">The message body text. Must not be null, empty, or whitespace.</param>
    /// <param name="buttons">The 1-3 reply buttons offered to the recipient.</param>
    /// <param name="header">An optional header of any kind (text, image, video, document).</param>
    /// <param name="footerText">Optional footer text.</param>
    /// <param name="context">An optional reference to a prior inbound message being replied to.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="bodyText"/> is null, empty, or whitespace; or when
    /// <paramref name="buttons"/> is null, empty, or contains more than 3 items.
    /// </exception>
    public SendReplyButtonsMessageRequest(string to, string bodyText, IReadOnlyList<ReplyButton> buttons, InteractiveHeader? header = null, string? footerText = null, Context? context = null)
        : base(to, context)
    {
        Interactive = new ReplyButtonsInteractive(
            new InteractiveBody(bodyText),
            new ReplyButtonsAction(buttons),
            header,
            footerText is null ? null : new InteractiveFooter(footerText));
    }

    /// <inheritdoc />
    [JsonPropertyName("type")]
    public override string Type => "interactive";

    /// <summary>
    /// The reply-buttons interactive payload.
    /// </summary>
    [JsonPropertyName("interactive")]
    public ReplyButtonsInteractive Interactive { get; }
}
