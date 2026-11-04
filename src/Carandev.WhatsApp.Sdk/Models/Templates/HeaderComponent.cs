using System.Text.Json.Serialization;

namespace Carandev.WhatsApp.Sdk.Models.Templates;

/// <summary>
/// Base type for a template's <c>HEADER</c> component. Discriminated on the wire by Meta's
/// <c>format</c> property into one of <see cref="TextHeaderComponent"/>,
/// <see cref="ImageHeaderComponent"/>, <see cref="VideoHeaderComponent"/>, or
/// <see cref="DocumentHeaderComponent"/>.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "format")]
[JsonDerivedType(typeof(TextHeaderComponent), "TEXT")]
[JsonDerivedType(typeof(ImageHeaderComponent), "IMAGE")]
[JsonDerivedType(typeof(VideoHeaderComponent), "VIDEO")]
[JsonDerivedType(typeof(DocumentHeaderComponent), "DOCUMENT")]
public abstract record HeaderComponent : TemplateComponent
{
    private protected HeaderComponent()
    {
    }
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
    [JsonPropertyName("text")]
    public string Text { get; }
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
    /// The uploaded media handle used as the header's example image.
    /// </summary>
    [JsonPropertyName("example")]
    public string ExampleHandle { get; }
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
    /// The uploaded media handle used as the header's example video.
    /// </summary>
    [JsonPropertyName("example")]
    public string ExampleHandle { get; }
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
    /// The uploaded media handle used as the header's example document.
    /// </summary>
    [JsonPropertyName("example")]
    public string ExampleHandle { get; }
}
