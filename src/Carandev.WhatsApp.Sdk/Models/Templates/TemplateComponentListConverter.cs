using System.Text.Json;
using System.Text.Json.Serialization;
using Carandev.WhatsApp.Sdk.Serialization;

namespace Carandev.WhatsApp.Sdk.Models.Templates;

/// <summary>
/// Reads <see cref="Template.Components"/> from Meta's responses. <c>HEADER</c> objects are built
/// by <see cref="HeaderComponent.FromWire"/> from their <c>format</c>, because System.Text.Json
/// cannot apply a discriminator nested under <see cref="TemplateComponent"/>'s <c>type</c>, and a
/// custom converter on a derived polymorphic type is not supported. Every other component reads
/// through the generated <see cref="TemplateComponent"/> contract exactly as before.
/// </summary>
/// <remarks>
/// Writing delegates unchanged to the generated contract, so serialized output is identical to a
/// list without this converter.
/// </remarks>
internal sealed class TemplateComponentListConverter : JsonConverter<IReadOnlyList<TemplateComponent>>
{
    private const string HeaderType = "HEADER";

    /// <inheritdoc />
    public override IReadOnlyList<TemplateComponent> Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException("Expected a JSON array of template components.");
        }

        var context = WhatsAppJsonSerializerContext.Default;
        var components = new List<TemplateComponent>(root.GetArrayLength());
        foreach (var element in root.EnumerateArray())
        {
            components.Add(IsHeader(element)
                ? HeaderComponent.FromWire(element)
                : JsonSerializer.Deserialize(element, context.TemplateComponent)!);
        }

        return components;
    }

    /// <inheritdoc />
    public override void Write(
        Utf8JsonWriter writer,
        IReadOnlyList<TemplateComponent> value,
        JsonSerializerOptions options) =>
        JsonSerializer.Serialize(writer, value, WhatsAppJsonSerializerContext.Default.IReadOnlyListTemplateComponent);

    private static bool IsHeader(JsonElement element) =>
        element.ValueKind == JsonValueKind.Object
        && element.TryGetProperty("type", out var type)
        && type.ValueKind == JsonValueKind.String
        && type.ValueEquals(HeaderType);
}
