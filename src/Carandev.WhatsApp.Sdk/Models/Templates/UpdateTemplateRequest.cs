using System.Text.Json.Serialization;

namespace Carandev.WhatsApp.Sdk.Models.Templates;

/// <summary>
/// An immutable, construction-validated request to update a message template's category and/or
/// components via <c>IWhatsAppTemplatesClient.UpdateAsync</c>. At least one of
/// <see cref="Category"/> or <see cref="Components"/> must be supplied.
/// </summary>
public sealed record UpdateTemplateRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateTemplateRequest"/> class.
    /// </summary>
    /// <param name="category">The template's new category, or <see langword="null"/> to leave unchanged.</param>
    /// <param name="components">
    /// The template's new components, or <see langword="null"/> to leave unchanged. When supplied,
    /// it is validated the same way as <see cref="CreateTemplateRequest"/>'s components.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when both <paramref name="category"/> and <paramref name="components"/> are
    /// <see langword="null"/>, or when a supplied <paramref name="components"/> fails structural
    /// validation. See <see cref="TemplateComponentValidation.ValidateStructure"/>.
    /// </exception>
    public UpdateTemplateRequest(TemplateCategory? category = null, IReadOnlyList<TemplateComponent>? components = null)
    {
        if (category is null && components is null)
        {
            throw new ArgumentException("At least one of category or components must be provided.");
        }

        if (components is not null)
        {
            TemplateComponentValidation.ValidateStructure(components, nameof(components));
        }

        Category = category;
        Components = components;
    }

    /// <summary>
    /// The template's new category. Omitted from the request payload when
    /// <see langword="null"/>.
    /// </summary>
    [JsonPropertyName("category")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TemplateCategory? Category { get; }

    /// <summary>
    /// The template's new components. Omitted from the request payload when
    /// <see langword="null"/>.
    /// </summary>
    [JsonPropertyName("components")]
    [JsonConverter(typeof(TemplateComponentListConverter))]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<TemplateComponent>? Components { get; }
}
