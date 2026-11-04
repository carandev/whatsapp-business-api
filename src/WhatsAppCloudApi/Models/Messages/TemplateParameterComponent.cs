using System.Text.Json;
using System.Text.Json.Serialization;

namespace WhatsAppCloudApi.Models.Messages;

/// <summary>
/// Base type for a single send-time template component carrying parameter VALUES for an approved
/// template, used by <c>SendTemplateAsync</c>. Discriminated on the wire by the Meta <c>type</c>
/// property (<c>header</c>/<c>body</c>/<c>button</c>).
/// </summary>
/// <remarks>
/// This hierarchy is intentionally separate from <c>Models.Templates.TemplateComponent</c>, which
/// describes template DEFINITION structure for <c>/message_templates</c>
/// (<c>HeaderComponent</c>/<c>BodyComponent</c>/<c>FooterComponent</c>/<c>ButtonsComponent</c>,
/// upper-snake-case <c>type</c> values). Send-time parameter substitution has a different wire
/// shape — parameter values nested under <c>parameters</c>, lower-snake-case discriminators — and
/// conflating the two produces an incorrect payload.
/// </remarks>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(HeaderParameterComponent), "header")]
[JsonDerivedType(typeof(BodyParameterComponent), "body")]
[JsonDerivedType(typeof(ButtonParameterComponent), "button")]
public abstract record TemplateParameterComponent
{
    private protected TemplateParameterComponent(IReadOnlyList<TemplateParameter> parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        Parameters = parameters;
    }

    /// <summary>
    /// The ordered parameter values substituted into this component's placeholders.
    /// </summary>
    [JsonPropertyName("parameters")]
    public IReadOnlyList<TemplateParameter> Parameters { get; }
}

/// <summary>
/// Parameter values substituted into the template's <c>HEADER</c> component.
/// </summary>
public sealed record HeaderParameterComponent : TemplateParameterComponent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HeaderParameterComponent"/> class.
    /// </summary>
    /// <param name="parameters">The ordered parameter values for the header placeholder(s).</param>
    public HeaderParameterComponent(IReadOnlyList<TemplateParameter> parameters)
        : base(parameters)
    {
    }
}

/// <summary>
/// Parameter values substituted into the template's <c>BODY</c> component.
/// </summary>
public sealed record BodyParameterComponent : TemplateParameterComponent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BodyParameterComponent"/> class.
    /// </summary>
    /// <param name="parameters">The ordered parameter values for the body placeholder(s).</param>
    public BodyParameterComponent(IReadOnlyList<TemplateParameter> parameters)
        : base(parameters)
    {
    }
}

/// <summary>
/// The sub-type of a <see cref="ButtonParameterComponent"/>, identifying which kind of button
/// behavior the parameter values apply to.
/// </summary>
[JsonConverter(typeof(ButtonParameterSubTypeJsonConverter))]
public enum ButtonParameterSubType
{
    /// <summary>
    /// A quick-reply button; carries a <see cref="PayloadParameter"/>.
    /// </summary>
    QuickReply,

    /// <summary>
    /// A dynamic-URL button; carries a <see cref="TextParameter"/> for the URL's variable suffix.
    /// </summary>
    Url,
}

/// <summary>
/// Converts <see cref="ButtonParameterSubType"/> to and from Meta's lower-snake-case JSON string
/// values (<c>quick_reply</c>, <c>url</c>).
/// </summary>
public sealed class ButtonParameterSubTypeJsonConverter : JsonConverter<ButtonParameterSubType>
{
    /// <inheritdoc />
    public override ButtonParameterSubType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return value switch
        {
            "quick_reply" => ButtonParameterSubType.QuickReply,
            "url" => ButtonParameterSubType.Url,
            _ => throw new JsonException($"Unknown button parameter sub_type '{value}'."),
        };
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, ButtonParameterSubType value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);

        writer.WriteStringValue(value switch
        {
            ButtonParameterSubType.QuickReply => "quick_reply",
            ButtonParameterSubType.Url => "url",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown button parameter sub_type."),
        });
    }
}

/// <summary>
/// Parameter values substituted into one button of the template's <c>BUTTONS</c> component.
/// </summary>
public sealed record ButtonParameterComponent : TemplateParameterComponent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ButtonParameterComponent"/> class.
    /// </summary>
    /// <param name="subType">Which kind of button behavior the parameter values apply to.</param>
    /// <param name="index">
    /// The zero-based position of the button within the template's <c>BUTTONS</c> component,
    /// serialized as a string per Meta's wire format. Must not be null, empty, or whitespace.
    /// </param>
    /// <param name="parameters">The ordered parameter values for this button.</param>
    public ButtonParameterComponent(ButtonParameterSubType subType, string index, IReadOnlyList<TemplateParameter> parameters)
        : base(parameters)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(index);
        SubType = subType;
        Index = index;
    }

    /// <summary>
    /// Which kind of button behavior the parameter values apply to.
    /// </summary>
    [JsonPropertyName("sub_type")]
    public ButtonParameterSubType SubType { get; }

    /// <summary>
    /// The zero-based position of the button within the template's <c>BUTTONS</c> component,
    /// serialized as a string per Meta's wire format.
    /// </summary>
    [JsonPropertyName("index")]
    public string Index { get; }
}
