using System.Text.Json.Serialization;

namespace Carandev.WhatsApp.Sdk.Models.Templates;

/// <summary>
/// Base type for a template's <c>HEADER</c> component: one of <see cref="TextHeaderComponent"/>,
/// <see cref="ImageHeaderComponent"/>, <see cref="VideoHeaderComponent"/>, or
/// <see cref="DocumentHeaderComponent"/>, told apart on the wire by Meta's <c>format</c> property.
/// </summary>
/// <remarks>
/// <see cref="TemplateComponent"/> already discriminates on <c>type</c>, and System.Text.Json does
/// not apply a nested discriminator, so every header serializes through this base contract:
/// <c>format</c>, <c>text</c>, and <c>example</c> are written from the concrete header.
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
    internal abstract string Format { get; }

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
}

/// <summary>
/// A <c>HEADER</c> component whose content is plain text.
/// </summary>
public sealed record TextHeaderComponent : HeaderComponent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TextHeaderComponent"/> class.
    /// </summary>
    /// <param name="text">The header text. Must not be null, empty, or whitespace.</param>
    public TextHeaderComponent(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        Text = text;
    }

    /// <summary>
    /// The header text.
    /// </summary>
    [JsonIgnore]
    public string Text { get; }

    /// <inheritdoc />
    internal override string Format => "TEXT";

    /// <inheritdoc />
    internal override string? WireText => Text;

    /// <inheritdoc />
    internal override HeaderExample? WireExample => null;
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
    internal override string Format => "IMAGE";

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
    internal override string Format => "VIDEO";

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
    internal override string Format => "DOCUMENT";

    /// <inheritdoc />
    internal override HeaderExample? WireExample => HeaderExample.ForHandle(ExampleHandle);
}

/// <summary>
/// Wire shape of a header's <c>example</c> object. Built by the SDK from the header's own
/// properties; callers set examples on the header types instead.
/// </summary>
public sealed record HeaderExample
{
    internal HeaderExample()
    {
    }

    /// <summary>
    /// Uploaded media handles for a media header (<c>header_handle</c>).
    /// </summary>
    [JsonPropertyName("header_handle")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? HeaderHandle { get; internal init; }

    internal static HeaderExample ForHandle(string handle) => new() { HeaderHandle = [handle] };
}
