using System.Text.Json;
using System.Text.Json.Serialization;

namespace Carandev.WhatsApp.Sdk.Models.Templates;

/// <summary>
/// How a template's placeholders are written: positional (<c>{{1}}</c>) or named
/// (<c>{{first_name}}</c>). Meta treats a template as positional when the format is not sent.
/// </summary>
[JsonConverter(typeof(TemplateParameterFormatJsonConverter))]
public enum TemplateParameterFormat
{
    /// <summary>
    /// Positional placeholders such as <c>{{1}}</c>, <c>{{2}}</c>.
    /// </summary>
    Positional,

    /// <summary>
    /// Named placeholders such as <c>{{first_name}}</c>.
    /// </summary>
    Named,
}

/// <summary>
/// Converts <see cref="TemplateParameterFormat"/> to and from Meta's <c>parameter_format</c>
/// values. Writes <c>positional</c> or <c>named</c>, as in Meta's documentation, and reads them
/// case-insensitively.
/// </summary>
public sealed class TemplateParameterFormatJsonConverter : JsonConverter<TemplateParameterFormat>
{
    /// <inheritdoc />
    public override TemplateParameterFormat Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (string.Equals(value, "positional", StringComparison.OrdinalIgnoreCase))
        {
            return TemplateParameterFormat.Positional;
        }

        if (string.Equals(value, "named", StringComparison.OrdinalIgnoreCase))
        {
            return TemplateParameterFormat.Named;
        }

        throw new JsonException($"Unknown template parameter format '{value}'.");
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, TemplateParameterFormat value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);

        writer.WriteStringValue(value switch
        {
            TemplateParameterFormat.Positional => "positional",
            TemplateParameterFormat.Named => "named",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown template parameter format."),
        });
    }
}
