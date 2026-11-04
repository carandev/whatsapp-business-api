using System.Text.Json.Serialization;

namespace Carandev.WhatsApp.Sdk.Models.Templates;

/// <summary>
/// Represents a message template as returned by the Meta Graph API. Mutable and attribute-driven
/// like <see cref="Carandev.WhatsApp.Sdk.Serialization.WhatsAppErrorResponse"/>, since response DTOs
/// carry no construction-time invariants.
/// </summary>
public sealed class Template
{
    /// <summary>
    /// The template's Meta-assigned identifier.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// The template's name.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// The template's category.
    /// </summary>
    [JsonPropertyName("category")]
    public TemplateCategory Category { get; set; }

    /// <summary>
    /// The template's language code, e.g. <c>en_US</c>.
    /// </summary>
    [JsonPropertyName("language")]
    public string? Language { get; set; }

    /// <summary>
    /// The template's review/approval status, e.g. <c>APPROVED</c>, <c>PENDING</c>,
    /// <c>REJECTED</c>.
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// The template's components. Populated on full fetches (e.g. <c>GetAsync</c>); Meta's create
    /// response does not echo them back.
    /// </summary>
    [JsonPropertyName("components")]
    public IReadOnlyList<TemplateComponent>? Components { get; set; }
}
