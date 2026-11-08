using System.Text.Json;
using System.Text.Json.Serialization;

namespace Carandev.WhatsApp.Sdk.Models.Templates;

/// <summary>
/// Base type for a single button within a template's <c>BUTTONS</c> component. Discriminated on
/// the wire by the Meta <c>type</c> property.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(UrlButton), "URL")]
[JsonDerivedType(typeof(PhoneNumberButton), "PHONE_NUMBER")]
[JsonDerivedType(typeof(QuickReplyButton), "QUICK_REPLY")]
[JsonDerivedType(typeof(OtpButton), "OTP")]
[JsonDerivedType(typeof(CopyCodeButton), "COPY_CODE")]
[JsonDerivedType(typeof(CatalogButton), "CATALOG")]
[JsonDerivedType(typeof(FlowButton), "FLOW")]
public abstract record TemplateButton
{
    private protected TemplateButton()
    {
    }
}

/// <summary>
/// A button that opens a URL when tapped.
/// </summary>
public sealed record UrlButton : TemplateButton
{
    private readonly IReadOnlyList<string>? _examples;

    /// <summary>
    /// Initializes a new instance of the <see cref="UrlButton"/> class.
    /// </summary>
    /// <param name="text">The button's display label. Must not be null, empty, or whitespace.</param>
    /// <param name="url">The URL opened when the button is tapped. Must not be null, empty, or whitespace.</param>
    public UrlButton(string text, string url)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        Text = text;
        Url = url;
    }

    /// <summary>
    /// The button's display label.
    /// </summary>
    [JsonPropertyName("text")]
    public string Text { get; }

    /// <summary>
    /// The URL opened when the button is tapped.
    /// </summary>
    [JsonPropertyName("url")]
    public string Url { get; }

    /// <summary>
    /// Optional example values for the URL's positional placeholders, in order. Serialized as
    /// <c>"example": [...]</c>; omitted when null or empty. Meta requires one when the URL contains
    /// a variable.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when the count differs from the number of distinct positional placeholders in the
    /// URL, or when a value is null, empty, or whitespace.
    /// </exception>
    [JsonIgnore]
    public IReadOnlyList<string>? Examples
    {
        get => _examples;
        init
        {
            TemplatePlaceholders.ValidatePositionalExamples(Url, value, "Button URL", nameof(Examples));
            _examples = value;
        }
    }

    /// <summary>
    /// The URL example in Meta's wire shape, omitted when no examples were provided.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("example")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    internal IReadOnlyList<string>? WireExample
    {
        get => _examples is { Count: > 0 } ? _examples : null;

        // Read path for templates returned by Meta; Meta already validated these examples.
        init => _examples = value;
    }
}

/// <summary>
/// A button that dials a phone number when tapped.
/// </summary>
public sealed record PhoneNumberButton : TemplateButton
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PhoneNumberButton"/> class.
    /// </summary>
    /// <param name="text">The button's display label. Must not be null, empty, or whitespace.</param>
    /// <param name="phoneNumber">The phone number dialed when the button is tapped. Must not be null, empty, or whitespace.</param>
    public PhoneNumberButton(string text, string phoneNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        ArgumentException.ThrowIfNullOrWhiteSpace(phoneNumber);
        Text = text;
        PhoneNumber = phoneNumber;
    }

    /// <summary>
    /// The button's display label.
    /// </summary>
    [JsonPropertyName("text")]
    public string Text { get; }

    /// <summary>
    /// The phone number dialed when the button is tapped.
    /// </summary>
    [JsonPropertyName("phone_number")]
    public string PhoneNumber { get; }
}

/// <summary>
/// A button that sends a predefined quick-reply payload when tapped.
/// </summary>
public sealed record QuickReplyButton : TemplateButton
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QuickReplyButton"/> class.
    /// </summary>
    /// <param name="text">The button's display label. Must not be null, empty, or whitespace.</param>
    public QuickReplyButton(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        Text = text;
    }

    /// <summary>
    /// The button's display label.
    /// </summary>
    [JsonPropertyName("text")]
    public string Text { get; }
}

/// <summary>
/// The delivery mechanism for an <see cref="OtpButton"/>.
/// </summary>
public enum OtpButtonType
{
    /// <summary>
    /// The user copies the one-time passcode manually.
    /// </summary>
    CopyCode,

    /// <summary>
    /// The passcode is autofilled with a single tap.
    /// </summary>
    OneTap,

    /// <summary>
    /// The passcode is autofilled without any tap.
    /// </summary>
    ZeroTap,
}

/// <summary>
/// A button that delivers a one-time passcode when tapped.
/// </summary>
public sealed record OtpButton : TemplateButton
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OtpButton"/> class.
    /// </summary>
    /// <param name="otpType">The one-time-passcode delivery mechanism.</param>
    /// <param name="text">The button's display label. Must not be null, empty, or whitespace.</param>
    public OtpButton(OtpButtonType otpType, string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        OtpType = otpType;
        Text = text;
    }

    /// <summary>
    /// The one-time-passcode delivery mechanism.
    /// </summary>
    [JsonPropertyName("otp_type")]
    public OtpButtonType OtpType { get; }

    /// <summary>
    /// The button's display label.
    /// </summary>
    [JsonPropertyName("text")]
    public string Text { get; }
}

/// <summary>
/// A button that copies an offer code to the clipboard when tapped.
/// </summary>
public sealed record CopyCodeButton : TemplateButton
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CopyCodeButton"/> class.
    /// </summary>
    /// <param name="example">An example offer code, shown during template review. Must not be null, empty, or whitespace.</param>
    public CopyCodeButton(string example)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(example);
        Example = example;
    }

    /// <summary>
    /// An example offer code, shown during template review.
    /// </summary>
    [JsonPropertyName("example")]
    public string Example { get; }
}

/// <summary>
/// A button that opens the business's product catalog when tapped.
/// </summary>
public sealed record CatalogButton : TemplateButton
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CatalogButton"/> class.
    /// </summary>
    /// <param name="text">The button's display label. Must not be null, empty, or whitespace.</param>
    public CatalogButton(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        Text = text;
    }

    /// <summary>
    /// The button's display label.
    /// </summary>
    [JsonPropertyName("text")]
    public string Text { get; }
}

/// <summary>
/// A button that launches a WhatsApp Flow when tapped.
/// </summary>
public sealed record FlowButton : TemplateButton
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FlowButton"/> class.
    /// </summary>
    /// <param name="text">The button's display label. Must not be null, empty, or whitespace.</param>
    /// <param name="flowId">The id of the WhatsApp Flow to launch. Must not be null, empty, or whitespace.</param>
    public FlowButton(string text, string flowId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        ArgumentException.ThrowIfNullOrWhiteSpace(flowId);
        Text = text;
        FlowId = flowId;
    }

    /// <summary>
    /// The button's display label.
    /// </summary>
    [JsonPropertyName("text")]
    public string Text { get; }

    /// <summary>
    /// The id of the WhatsApp Flow to launch.
    /// </summary>
    [JsonPropertyName("flow_id")]
    public string FlowId { get; }
}

/// <summary>
/// A button read from Meta whose <c>type</c> the SDK does not model, for example
/// <c>VOICE_CALL</c> or <c>MPM</c>. It keeps the button in <see cref="ButtonsComponent.Buttons"/>
/// instead of failing the whole response.
/// </summary>
/// <remarks>
/// Serializing it writes <see cref="RawJson"/> unchanged. The SDK creates it only when reading
/// templates.
/// </remarks>
public sealed record UnknownTemplateButton : TemplateButton
{
    internal UnknownTemplateButton(string type, JsonElement rawJson)
    {
        Type = type;
        RawJson = rawJson;
    }

    /// <summary>
    /// Meta's <c>type</c> value exactly as returned, for example <c>VOICE_CALL</c>. Empty when the
    /// button has no string <c>type</c>.
    /// </summary>
    public string Type { get; }

    /// <summary>
    /// The complete button object exactly as Meta returned it, including <c>type</c>.
    /// </summary>
    public JsonElement RawJson { get; }
}
