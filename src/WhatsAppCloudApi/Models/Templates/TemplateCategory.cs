using System.Text.Json;
using System.Text.Json.Serialization;

namespace WhatsAppCloudApi.Models.Templates;

/// <summary>
/// The Meta-defined category of a message template.
/// </summary>
[JsonConverter(typeof(TemplateCategoryJsonConverter))]
public enum TemplateCategory
{
    /// <summary>
    /// A template used for marketing or promotional content.
    /// </summary>
    Marketing,

    /// <summary>
    /// A template used for utility/transactional content, e.g. order updates.
    /// </summary>
    Utility,

    /// <summary>
    /// A template used to deliver one-time passcodes.
    /// </summary>
    Authentication,
}

/// <summary>
/// Converts <see cref="TemplateCategory"/> to and from Meta's upper-snake-case JSON string values
/// (<c>MARKETING</c>, <c>UTILITY</c>, <c>AUTHENTICATION</c>).
/// </summary>
public sealed class TemplateCategoryJsonConverter : JsonConverter<TemplateCategory>
{
    /// <inheritdoc />
    public override TemplateCategory Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return value switch
        {
            "MARKETING" => TemplateCategory.Marketing,
            "UTILITY" => TemplateCategory.Utility,
            "AUTHENTICATION" => TemplateCategory.Authentication,
            _ => throw new JsonException($"Unknown template category '{value}'."),
        };
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, TemplateCategory value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);

        writer.WriteStringValue(value switch
        {
            TemplateCategory.Marketing => "MARKETING",
            TemplateCategory.Utility => "UTILITY",
            TemplateCategory.Authentication => "AUTHENTICATION",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown template category."),
        });
    }
}
