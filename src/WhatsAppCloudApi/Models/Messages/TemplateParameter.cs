using System.Text.Json.Serialization;

namespace WhatsAppCloudApi.Models.Messages;

/// <summary>
/// Base type for a single send-time template parameter value, nested inside a
/// <see cref="TemplateParameterComponent"/>. Discriminated on the wire by the Meta <c>type</c>
/// property.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(TextParameter), "text")]
[JsonDerivedType(typeof(CurrencyParameter), "currency")]
[JsonDerivedType(typeof(DateTimeParameter), "date_time")]
[JsonDerivedType(typeof(ImageParameter), "image")]
[JsonDerivedType(typeof(DocumentParameter), "document")]
[JsonDerivedType(typeof(VideoParameter), "video")]
[JsonDerivedType(typeof(PayloadParameter), "payload")]
public abstract record TemplateParameter
{
    private protected TemplateParameter()
    {
    }
}

/// <summary>
/// A plain-text parameter value, used for text placeholders in header/body components and for a
/// dynamic-URL button's variable suffix.
/// </summary>
public sealed record TextParameter : TemplateParameter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TextParameter"/> class.
    /// </summary>
    /// <param name="text">The substituted text. Must not be null, empty, or whitespace.</param>
    public TextParameter(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        Text = text;
    }

    /// <summary>
    /// The substituted text.
    /// </summary>
    [JsonPropertyName("text")]
    public string Text { get; }
}

/// <summary>
/// A currency amount, nested inside a <see cref="CurrencyParameter"/>.
/// </summary>
public sealed record Currency
{
    /// <summary>
    /// The formatted, human-readable amount shown as a fallback when localization is unavailable.
    /// </summary>
    [JsonPropertyName("fallback_value")]
    public required string FallbackValue { get; init; }

    /// <summary>
    /// The ISO 4217 currency code, e.g. <c>USD</c>.
    /// </summary>
    [JsonPropertyName("code")]
    public required string Code { get; init; }

    /// <summary>
    /// The amount multiplied by 1000, per Meta's documented convention (e.g. <c>$1.00</c> is
    /// <c>1000</c>).
    /// </summary>
    [JsonPropertyName("amount_1000")]
    public required long Amount1000 { get; init; }
}

/// <summary>
/// A currency parameter value.
/// </summary>
public sealed record CurrencyParameter : TemplateParameter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CurrencyParameter"/> class.
    /// </summary>
    /// <param name="currency">The currency amount.</param>
    public CurrencyParameter(Currency currency)
    {
        ArgumentNullException.ThrowIfNull(currency);
        Currency = currency;
    }

    /// <summary>
    /// The currency amount.
    /// </summary>
    [JsonPropertyName("currency")]
    public Currency Currency { get; }
}

/// <summary>
/// A date/time value, nested inside a <see cref="DateTimeParameter"/>.
/// </summary>
public sealed record TemplateDateTime
{
    /// <summary>
    /// The formatted, human-readable date/time shown as a fallback when localization is
    /// unavailable.
    /// </summary>
    [JsonPropertyName("fallback_value")]
    public required string FallbackValue { get; init; }
}

/// <summary>
/// A date/time parameter value.
/// </summary>
public sealed record DateTimeParameter : TemplateParameter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DateTimeParameter"/> class.
    /// </summary>
    /// <param name="dateTime">The date/time value.</param>
    public DateTimeParameter(TemplateDateTime dateTime)
    {
        ArgumentNullException.ThrowIfNull(dateTime);
        DateTime = dateTime;
    }

    /// <summary>
    /// The date/time value.
    /// </summary>
    [JsonPropertyName("date_time")]
    public TemplateDateTime DateTime { get; }
}

/// <summary>
/// An image parameter value, referenced by either an uploaded media id or a public link.
/// </summary>
public sealed record ImageParameter : TemplateParameter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ImageParameter"/> class.
    /// </summary>
    /// <param name="mediaId">The uploaded media's opaque id. Mutually exclusive with <paramref name="link"/>.</param>
    /// <param name="link">A public URL Meta will fetch the media from. Mutually exclusive with <paramref name="mediaId"/>.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when both or neither of <paramref name="mediaId"/> and <paramref name="link"/> are supplied.
    /// </exception>
    public ImageParameter(string? mediaId = null, string? link = null)
    {
        Image = MediaReference.Create(mediaId, link);
    }

    /// <summary>
    /// The image's media reference.
    /// </summary>
    [JsonPropertyName("image")]
    public MediaReference Image { get; }
}

/// <summary>
/// A document parameter value, referenced by either an uploaded media id or a public link.
/// </summary>
public sealed record DocumentParameter : TemplateParameter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentParameter"/> class.
    /// </summary>
    /// <param name="mediaId">The uploaded media's opaque id. Mutually exclusive with <paramref name="link"/>.</param>
    /// <param name="link">A public URL Meta will fetch the media from. Mutually exclusive with <paramref name="mediaId"/>.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when both or neither of <paramref name="mediaId"/> and <paramref name="link"/> are supplied.
    /// </exception>
    public DocumentParameter(string? mediaId = null, string? link = null)
    {
        Document = MediaReference.Create(mediaId, link);
    }

    /// <summary>
    /// The document's media reference.
    /// </summary>
    [JsonPropertyName("document")]
    public MediaReference Document { get; }
}

/// <summary>
/// A video parameter value, referenced by either an uploaded media id or a public link.
/// </summary>
public sealed record VideoParameter : TemplateParameter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VideoParameter"/> class.
    /// </summary>
    /// <param name="mediaId">The uploaded media's opaque id. Mutually exclusive with <paramref name="link"/>.</param>
    /// <param name="link">A public URL Meta will fetch the media from. Mutually exclusive with <paramref name="mediaId"/>.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when both or neither of <paramref name="mediaId"/> and <paramref name="link"/> are supplied.
    /// </exception>
    public VideoParameter(string? mediaId = null, string? link = null)
    {
        Video = MediaReference.Create(mediaId, link);
    }

    /// <summary>
    /// The video's media reference.
    /// </summary>
    [JsonPropertyName("video")]
    public MediaReference Video { get; }
}

/// <summary>
/// A quick-reply button's payload parameter value.
/// </summary>
public sealed record PayloadParameter : TemplateParameter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PayloadParameter"/> class.
    /// </summary>
    /// <param name="payload">The developer-defined payload returned when the button is tapped. Must not be null, empty, or whitespace.</param>
    public PayloadParameter(string payload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);
        Payload = payload;
    }

    /// <summary>
    /// The developer-defined payload returned when the button is tapped.
    /// </summary>
    [JsonPropertyName("payload")]
    public string Payload { get; }
}
