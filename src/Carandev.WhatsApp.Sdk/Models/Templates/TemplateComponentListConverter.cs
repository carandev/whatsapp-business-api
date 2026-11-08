using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Carandev.WhatsApp.Sdk.Serialization;

namespace Carandev.WhatsApp.Sdk.Models.Templates;

/// <summary>
/// Reads and writes a list of template components. <c>HEADER</c> objects are built by
/// <see cref="HeaderComponent.FromWire"/> from their <c>format</c>, because System.Text.Json
/// cannot apply a discriminator nested under <see cref="TemplateComponent"/>'s <c>type</c>, and a
/// custom converter on a derived polymorphic type is not supported. A <c>type</c> the SDK does not
/// model becomes an <see cref="UnknownTemplateComponent"/>. Every other component reads through
/// the generated <see cref="TemplateComponent"/> contract.
/// </summary>
/// <remarks>
/// Writing serializes each component through the generated contract, so the output is identical
/// to a list without this converter, except that an <see cref="UnknownTemplateComponent"/> is
/// written as its raw JSON.
/// </remarks>
internal sealed class TemplateComponentListConverter : JsonConverter<IReadOnlyList<TemplateComponent>>
{
    private static readonly HashSet<string> ModeledTypes = new(StringComparer.Ordinal)
    {
        "BODY",
        "FOOTER",
        "BUTTONS",
    };

    /// <inheritdoc />
    public override IReadOnlyList<TemplateComponent> Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options) =>
        PolymorphicList.Read<TemplateComponent>(
            ref reader,
            "template components",
            element =>
            {
                var type = PolymorphicList.GetType(element);
                if (type == "HEADER")
                {
                    return HeaderComponent.FromWire(element);
                }

                return type is not null && ModeledTypes.Contains(type)
                    ? JsonSerializer.Deserialize(element, WhatsAppJsonSerializerContext.Default.TemplateComponent)!
                    : new UnknownTemplateComponent(type ?? string.Empty, element.Clone());
            });

    /// <inheritdoc />
    public override void Write(
        Utf8JsonWriter writer,
        IReadOnlyList<TemplateComponent> value,
        JsonSerializerOptions options) =>
        PolymorphicList.Write(
            writer,
            value,
            WhatsAppJsonSerializerContext.Default.TemplateComponent,
            component => component is UnknownTemplateComponent unknown ? unknown.RawJson : null);
}

/// <summary>
/// Reads and writes <see cref="ButtonsComponent.Buttons"/>. A <c>type</c> the SDK does not model
/// becomes an <see cref="UnknownTemplateButton"/>, which is written back as its raw JSON.
/// </summary>
internal sealed class TemplateButtonListConverter : JsonConverter<IReadOnlyList<TemplateButton>>
{
    private static readonly HashSet<string> ModeledTypes = new(StringComparer.Ordinal)
    {
        "URL",
        "PHONE_NUMBER",
        "QUICK_REPLY",
        "OTP",
        "COPY_CODE",
        "CATALOG",
        "FLOW",
    };

    /// <inheritdoc />
    public override IReadOnlyList<TemplateButton> Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options) =>
        PolymorphicList.Read<TemplateButton>(
            ref reader,
            "template buttons",
            element =>
            {
                var type = PolymorphicList.GetType(element);
                return type is not null && ModeledTypes.Contains(type)
                    ? JsonSerializer.Deserialize(element, WhatsAppJsonSerializerContext.Default.TemplateButton)!
                    : new UnknownTemplateButton(type ?? string.Empty, element.Clone());
            });

    /// <inheritdoc />
    public override void Write(
        Utf8JsonWriter writer,
        IReadOnlyList<TemplateButton> value,
        JsonSerializerOptions options) =>
        PolymorphicList.Write(
            writer,
            value,
            WhatsAppJsonSerializerContext.Default.TemplateButton,
            button => button is UnknownTemplateButton unknown ? unknown.RawJson : null);
}

/// <summary>
/// Shared element-by-element reading and writing for the polymorphic template lists.
/// </summary>
internal static class PolymorphicList
{
    internal static IReadOnlyList<T> Read<T>(ref Utf8JsonReader reader, string description, Func<JsonElement, T> readElement)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException($"Expected a JSON array of {description}.");
        }

        var items = new List<T>(root.GetArrayLength());
        foreach (var element in root.EnumerateArray())
        {
            items.Add(readElement(element));
        }

        return items;
    }

    internal static void Write<T>(
        Utf8JsonWriter writer,
        IReadOnlyList<T> value,
        JsonTypeInfo<T> typeInfo,
        Func<T, JsonElement?> rawJson)
    {
        writer.WriteStartArray();
        foreach (var item in value)
        {
            if (rawJson(item) is { } raw)
            {
                raw.WriteTo(writer);
            }
            else
            {
                JsonSerializer.Serialize(writer, item, typeInfo);
            }
        }

        writer.WriteEndArray();
    }

    internal static string? GetType(JsonElement element) =>
        element.ValueKind == JsonValueKind.Object
        && element.TryGetProperty("type", out var type)
        && type.ValueKind == JsonValueKind.String
            ? type.GetString()
            : null;
}
