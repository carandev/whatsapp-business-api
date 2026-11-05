using System.Text.Json;
using System.Text.Json.Serialization;
using Carandev.WhatsApp.Sdk.Serialization;

namespace Carandev.WhatsApp.Sdk.Models.Templates;

/// <summary>
/// Base type for a template's <c>HEADER</c> component: one of <see cref="TextHeaderComponent"/>,
/// <see cref="ImageHeaderComponent"/>, <see cref="VideoHeaderComponent"/>, or
/// <see cref="DocumentHeaderComponent"/>, told apart on the wire by Meta's <c>format</c> property.
/// Templates read from Meta can also carry an <see cref="UnknownHeaderComponent"/>.
/// </summary>
/// <remarks>
/// <see cref="TemplateComponent"/> already discriminates on <c>type</c>, and System.Text.Json does
/// not apply a nested discriminator, so every header serializes through this base contract:
/// <c>format</c>, <c>text</c>, and <c>example</c> are written from the concrete header. Reading
/// goes the other way: <see cref="Template.Components"/> picks the concrete header from
/// <c>format</c> (<c>TEXT</c>, <c>IMAGE</c>, <c>VIDEO</c>, or <c>DOCUMENT</c>, matched without regard
/// to case) and maps anything else to <see cref="UnknownHeaderComponent"/>.
/// </remarks>
public abstract record HeaderComponent : TemplateComponent
{
    private protected HeaderComponent()
    {
    }

    /// <summary>
    /// Meta's <c>format</c> value: <c>TEXT</c>, <c>IMAGE</c>, <c>VIDEO</c>, or <c>DOCUMENT</c>.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("format")]
    internal abstract string WireFormat { get; }

    /// <summary>
    /// The header text, written only for text headers.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("text")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    internal virtual string? WireText => null;

    /// <summary>
    /// The header example in Meta's wire shape, omitted when absent.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("example")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    internal abstract HeaderExample? WireExample { get; }

    /// <summary>
    /// Builds the concrete header for a <c>HEADER</c> object returned by Meta. Meta already
    /// accepted this data, so placeholder and example validation is not repeated.
    /// </summary>
    internal static HeaderComponent FromWire(JsonElement element)
    {
        var format = GetString(element, "format") ?? string.Empty;
        var example = element.TryGetProperty("example", out var exampleElement)
            && exampleElement.ValueKind == JsonValueKind.Object
                ? JsonSerializer.Deserialize(exampleElement, WhatsAppJsonSerializerContext.Default.HeaderExample)
                : null;
        var handle = example?.HeaderHandle is { Count: > 0 } handles ? handles[0] : null;
        var hasHandle = !string.IsNullOrWhiteSpace(handle);
        var text = GetString(element, "text");

        return format.ToUpperInvariant() switch
        {
            "TEXT" when !string.IsNullOrWhiteSpace(text) =>
                TextHeaderComponent.FromWire(text, example?.HeaderText, example?.HeaderTextNamedParams),
            "IMAGE" when hasHandle => new ImageHeaderComponent(handle!),
            "VIDEO" when hasHandle => new VideoHeaderComponent(handle!),
            "DOCUMENT" when hasHandle => new DocumentHeaderComponent(handle!),
            _ => new UnknownHeaderComponent(format),
        };
    }

    private static string? GetString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}

/// <summary>
/// A <c>HEADER</c> component whose content is plain text.
/// </summary>
public sealed record TextHeaderComponent : HeaderComponent
{
    private readonly IReadOnlyList<string>? _examples;
    private readonly IReadOnlyList<NamedParameterExample>? _namedExamples;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextHeaderComponent"/> class.
    /// </summary>
    /// <param name="text">The header text. Must not be null, empty, or whitespace.</param>
    public TextHeaderComponent(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        Text = text;
    }

    private TextHeaderComponent(
        string text,
        IReadOnlyList<string>? examples,
        IReadOnlyList<NamedParameterExample>? namedExamples)
    {
        Text = text;
        _examples = examples;
        _namedExamples = namedExamples;
    }

    /// <summary>
    /// The header text.
    /// </summary>
    [JsonIgnore]
    public string Text { get; }

    /// <summary>
    /// Optional example values for the positional placeholders, in order. Serialized as
    /// <c>"example": { "header_text": [...] }</c>; omitted when null or empty.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when the text uses named placeholders, when the count differs from the number of
    /// distinct positional placeholders, or when a value is null, empty, or whitespace.
    /// </exception>
    [JsonIgnore]
    public IReadOnlyList<string>? Examples
    {
        get => _examples;
        init
        {
            TemplatePlaceholders.ValidatePositionalExamples(Text, value, "Header text", nameof(Examples));
            _examples = value;
        }
    }

    /// <summary>
    /// Optional example values for the named placeholders. Serialized as
    /// <c>"example": { "header_text_named_params": [...] }</c>; omitted when null or empty.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when the text uses positional placeholders, when a name is repeated, or when the
    /// names differ from the text's named placeholders.
    /// </exception>
    [JsonIgnore]
    public IReadOnlyList<NamedParameterExample>? NamedExamples
    {
        get => _namedExamples;
        init
        {
            TemplatePlaceholders.ValidateNamedExamples(Text, value, "Header text", nameof(NamedExamples));
            _namedExamples = value;
        }
    }

    /// <inheritdoc />
    internal override string WireFormat => "TEXT";

    /// <inheritdoc />
    internal override string? WireText => Text;

    /// <inheritdoc />
    internal override HeaderExample? WireExample => HeaderExample.ForText(_examples, _namedExamples);

    /// <summary>
    /// Read path for a text header returned by Meta: skips the validation Meta already performed.
    /// </summary>
    internal static TextHeaderComponent FromWire(
        string text,
        IReadOnlyList<string>? examples,
        IReadOnlyList<NamedParameterExample>? namedExamples) =>
        new(
            text,
            examples is { Count: > 0 } ? examples : null,
            namedExamples is { Count: > 0 } ? namedExamples : null);
}

/// <summary>
/// A <c>HEADER</c> component whose content is an image, referenced by its example media handle.
/// </summary>
public sealed record ImageHeaderComponent : HeaderComponent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ImageHeaderComponent"/> class.
    /// </summary>
    /// <param name="exampleHandle">
    /// The uploaded media handle used as the header's example image. Must not be null, empty, or
    /// whitespace.
    /// </param>
    public ImageHeaderComponent(string exampleHandle)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(exampleHandle);
        ExampleHandle = exampleHandle;
    }

    /// <summary>
    /// The uploaded media handle used as the header's example image. Serialized as
    /// <c>"example": { "header_handle": [handle] }</c>.
    /// </summary>
    [JsonIgnore]
    public string ExampleHandle { get; }

    /// <inheritdoc />
    internal override string WireFormat => "IMAGE";

    /// <inheritdoc />
    internal override HeaderExample? WireExample => HeaderExample.ForHandle(ExampleHandle);
}

/// <summary>
/// A <c>HEADER</c> component whose content is a video, referenced by its example media handle.
/// </summary>
public sealed record VideoHeaderComponent : HeaderComponent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VideoHeaderComponent"/> class.
    /// </summary>
    /// <param name="exampleHandle">
    /// The uploaded media handle used as the header's example video. Must not be null, empty, or
    /// whitespace.
    /// </param>
    public VideoHeaderComponent(string exampleHandle)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(exampleHandle);
        ExampleHandle = exampleHandle;
    }

    /// <summary>
    /// The uploaded media handle used as the header's example video. Serialized as
    /// <c>"example": { "header_handle": [handle] }</c>.
    /// </summary>
    [JsonIgnore]
    public string ExampleHandle { get; }

    /// <inheritdoc />
    internal override string WireFormat => "VIDEO";

    /// <inheritdoc />
    internal override HeaderExample? WireExample => HeaderExample.ForHandle(ExampleHandle);
}

/// <summary>
/// A <c>HEADER</c> component whose content is a document, referenced by its example media handle.
/// </summary>
public sealed record DocumentHeaderComponent : HeaderComponent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentHeaderComponent"/> class.
    /// </summary>
    /// <param name="exampleHandle">
    /// The uploaded media handle used as the header's example document. Must not be null, empty,
    /// or whitespace.
    /// </param>
    public DocumentHeaderComponent(string exampleHandle)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(exampleHandle);
        ExampleHandle = exampleHandle;
    }

    /// <summary>
    /// The uploaded media handle used as the header's example document. Serialized as
    /// <c>"example": { "header_handle": [handle] }</c>.
    /// </summary>
    [JsonIgnore]
    public string ExampleHandle { get; }

    /// <inheritdoc />
    internal override string WireFormat => "DOCUMENT";

    /// <inheritdoc />
    internal override HeaderExample? WireExample => HeaderExample.ForHandle(ExampleHandle);
}

/// <summary>
/// A <c>HEADER</c> component read from Meta that the SDK does not model: a format other than
/// <c>TEXT</c>, <c>IMAGE</c>, <c>VIDEO</c>, or <c>DOCUMENT</c> (for example <c>LOCATION</c> or
/// <c>GIF</c>), a text header without text, or a media header without a <c>header_handle</c>
/// example. It keeps the header in <see cref="Template.Components"/> instead of failing the whole
/// response or silently dropping the component.
/// </summary>
/// <remarks>
/// Only <see cref="Format"/> is kept; any other header fields are not preserved. Serializing it
/// writes <c>{"type":"HEADER","format":"&lt;Format&gt;"}</c>, which is the complete shape of a
/// <c>LOCATION</c> header. The SDK creates it only when reading templates.
/// </remarks>
public sealed record UnknownHeaderComponent : HeaderComponent
{
    internal UnknownHeaderComponent(string format)
    {
        Format = format;
    }

    /// <summary>
    /// Meta's <c>format</c> value exactly as returned, for example <c>LOCATION</c>.
    /// </summary>
    [JsonIgnore]
    public string Format { get; }

    /// <inheritdoc />
    internal override string WireFormat => Format;

    /// <inheritdoc />
    internal override HeaderExample? WireExample => null;
}

/// <summary>
/// Wire shape of a header's <c>example</c> object. Built by the SDK from the header's own
/// properties; callers set examples on the header types instead.
/// </summary>
public sealed record HeaderExample
{
    [JsonConstructor]
    internal HeaderExample()
    {
    }

    /// <summary>
    /// Uploaded media handles for a media header (<c>header_handle</c>).
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("header_handle")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? HeaderHandle { get; internal set; }

    /// <summary>
    /// Positional examples for a text header (<c>header_text</c>).
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("header_text")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? HeaderText { get; internal set; }

    /// <summary>
    /// Named examples for a text header (<c>header_text_named_params</c>).
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("header_text_named_params")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<NamedParameterExample>? HeaderTextNamedParams { get; internal set; }

    internal static HeaderExample ForHandle(string handle) => new() { HeaderHandle = [handle] };

    internal static HeaderExample? ForText(
        IReadOnlyList<string>? examples,
        IReadOnlyList<NamedParameterExample>? namedExamples)
    {
        if (examples is { Count: > 0 })
        {
            return new HeaderExample { HeaderText = examples };
        }

        if (namedExamples is { Count: > 0 })
        {
            return new HeaderExample { HeaderTextNamedParams = namedExamples };
        }

        return null;
    }
}
