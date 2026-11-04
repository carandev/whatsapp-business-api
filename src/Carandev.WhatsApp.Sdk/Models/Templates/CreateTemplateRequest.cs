using System.Text.Json.Serialization;

namespace Carandev.WhatsApp.Sdk.Models.Templates;

/// <summary>
/// An immutable, construction-validated request to create a message template via
/// <c>IWhatsAppTemplatesClient.CreateAsync</c>.
/// </summary>
public sealed record CreateTemplateRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateTemplateRequest"/> class.
    /// </summary>
    /// <param name="name">The template's name. Must not be null, empty, or whitespace.</param>
    /// <param name="category">The template's category.</param>
    /// <param name="language">
    /// The template's language code, e.g. <c>en_US</c>. Must not be null, empty, or whitespace.
    /// </param>
    /// <param name="components">
    /// The template's components. Must be non-empty, contain exactly one <see cref="BodyComponent"/>,
    /// and at most one <see cref="HeaderComponent"/>/<see cref="FooterComponent"/>.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name"/> or <paramref name="language"/> is null/empty/whitespace,
    /// or when <paramref name="components"/> fails structural validation. See
    /// <see cref="TemplateComponentValidation.ValidateStructure"/>.
    /// </exception>
    public CreateTemplateRequest(
        string name,
        TemplateCategory category,
        string language,
        IReadOnlyList<TemplateComponent> components)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(language);
        TemplateComponentValidation.ValidateStructure(components, nameof(components));

        Name = name;
        Category = category;
        Language = language;
        Components = components;
    }

    /// <summary>
    /// The template's name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; }

    /// <summary>
    /// The template's category.
    /// </summary>
    [JsonPropertyName("category")]
    public TemplateCategory Category { get; }

    /// <summary>
    /// The template's language code, e.g. <c>en_US</c>.
    /// </summary>
    [JsonPropertyName("language")]
    public string Language { get; }

    /// <summary>
    /// The template's components.
    /// </summary>
    [JsonPropertyName("components")]
    public IReadOnlyList<TemplateComponent> Components { get; }
}
