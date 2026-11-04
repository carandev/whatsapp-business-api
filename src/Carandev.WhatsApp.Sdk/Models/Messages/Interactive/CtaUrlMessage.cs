using System.Text.Json.Serialization;

namespace Carandev.WhatsApp.Sdk.Models.Messages.Interactive;

/// <summary>
/// The <c>parameters</c> object of a CTA-URL action: the button's user-facing label and the URL it
/// opens. Construction validates synchronously, before any HTTP request is sent: neither
/// <see cref="DisplayText"/> nor <see cref="Url"/> may be null, empty, or whitespace.
/// </summary>
public sealed record CtaUrlParameters
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CtaUrlParameters"/> class.
    /// </summary>
    /// <param name="displayText">The button's user-facing label. Must not be null, empty, or whitespace.</param>
    /// <param name="url">The URL opened when the recipient taps the button. Must not be null, empty, or whitespace.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="displayText"/> or <paramref name="url"/> is null, empty, or
    /// whitespace.
    /// </exception>
    public CtaUrlParameters(string displayText, string url)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayText);
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        DisplayText = displayText;
        Url = url;
    }

    /// <summary>
    /// The button's user-facing label.
    /// </summary>
    [JsonPropertyName("display_text")]
    public string DisplayText { get; }

    /// <summary>
    /// The URL opened when the recipient taps the button.
    /// </summary>
    [JsonPropertyName("url")]
    public string Url { get; }
}

/// <summary>
/// The <c>action</c> object of a CTA-URL interactive message: Meta's fixed <c>name</c>
/// discriminator (<c>cta_url</c>) plus the button's <see cref="CtaUrlParameters"/>.
/// </summary>
public sealed record CtaUrlAction
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CtaUrlAction"/> class.
    /// </summary>
    /// <param name="parameters">The button's user-facing label and target URL.</param>
    public CtaUrlAction(CtaUrlParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        Parameters = parameters;
    }

    /// <summary>
    /// The action kind. Always <c>cta_url</c>.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name => "cta_url";

    /// <summary>
    /// The button's user-facing label and target URL.
    /// </summary>
    [JsonPropertyName("parameters")]
    public CtaUrlParameters Parameters { get; }
}

/// <summary>
/// The <c>interactive</c> object of a CTA-URL message. Not <c>[JsonPolymorphic]</c> — see remarks
/// on <see cref="SendMessageRequest"/> for the rationale shared by all interactive kinds.
/// </summary>
public sealed record CtaUrlInteractive
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CtaUrlInteractive"/> class.
    /// </summary>
    /// <param name="body">The required message body.</param>
    /// <param name="action">The button's user-facing label and target URL.</param>
    /// <param name="header">An optional header of any kind (text, image, video, document).</param>
    /// <param name="footer">Optional footer text.</param>
    public CtaUrlInteractive(InteractiveBody body, CtaUrlAction action, InteractiveHeader? header = null, InteractiveFooter? footer = null)
    {
        ArgumentNullException.ThrowIfNull(body);
        ArgumentNullException.ThrowIfNull(action);
        Body = body;
        Action = action;
        Header = header;
        Footer = footer;
    }

    /// <summary>
    /// The interactive message kind. Always <c>cta_url</c>.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type => "cta_url";

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
    /// The button's user-facing label and target URL.
    /// </summary>
    [JsonPropertyName("action")]
    public CtaUrlAction Action { get; }
}

/// <summary>
/// A CTA-URL interactive message envelope sent via <c>SendCtaUrlButtonAsync</c>.
/// </summary>
public sealed record SendCtaUrlMessageRequest : SendMessageRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SendCtaUrlMessageRequest"/> class.
    /// </summary>
    /// <param name="to">The recipient's phone number, in E.164 format. Must not be null, empty, or whitespace.</param>
    /// <param name="bodyText">The message body text. Must not be null, empty, or whitespace.</param>
    /// <param name="displayText">The button's user-facing label. Must not be null, empty, or whitespace.</param>
    /// <param name="url">The URL opened when the recipient taps the button. Must not be null, empty, or whitespace.</param>
    /// <param name="header">An optional header of any kind (text, image, video, document).</param>
    /// <param name="footerText">Optional footer text.</param>
    /// <param name="context">An optional reference to a prior inbound message being replied to.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="bodyText"/>, <paramref name="displayText"/>, or
    /// <paramref name="url"/> is null, empty, or whitespace.
    /// </exception>
    public SendCtaUrlMessageRequest(string to, string bodyText, string displayText, string url, InteractiveHeader? header = null, string? footerText = null, Context? context = null)
        : base(to, context)
    {
        Interactive = new CtaUrlInteractive(
            new InteractiveBody(bodyText),
            new CtaUrlAction(new CtaUrlParameters(displayText, url)),
            header,
            footerText is null ? null : new InteractiveFooter(footerText));
    }

    /// <inheritdoc />
    [JsonPropertyName("type")]
    public override string Type => "interactive";

    /// <summary>
    /// The CTA-URL interactive payload.
    /// </summary>
    [JsonPropertyName("interactive")]
    public CtaUrlInteractive Interactive { get; }
}
