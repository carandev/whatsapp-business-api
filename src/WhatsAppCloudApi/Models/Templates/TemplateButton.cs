using System.Text.Json.Serialization;

namespace WhatsAppCloudApi.Models.Templates;

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
