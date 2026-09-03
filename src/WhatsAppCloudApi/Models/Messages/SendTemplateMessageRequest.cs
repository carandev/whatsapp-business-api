using System.Text.Json.Serialization;

namespace WhatsAppCloudApi.Models.Messages;

/// <summary>
/// A template message envelope sent via <c>SendTemplateAsync</c>, carrying send-time parameter
/// values for an approved template. Uses the <see cref="TemplateParameterComponent"/> hierarchy —
/// distinct from <c>Models.Templates.TemplateComponent</c>, which describes template DEFINITION
/// structure.
/// </summary>
public sealed record SendTemplateMessageRequest : SendMessageRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SendTemplateMessageRequest"/> class.
    /// </summary>
    /// <param name="to">The recipient's phone number, in E.164 format. Must not be null, empty, or whitespace.</param>
    /// <param name="template">The approved template reference and its send-time parameter values.</param>
    /// <param name="context">An optional reference to a prior inbound message being replied to.</param>
    public SendTemplateMessageRequest(string to, TemplateReference template, Context? context = null)
        : base(to, context)
    {
        ArgumentNullException.ThrowIfNull(template);
        Template = template;
    }

    /// <inheritdoc />
    [JsonPropertyName("type")]
    public override string Type => "template";

    /// <summary>
    /// The approved template reference and its send-time parameter values.
    /// </summary>
    [JsonPropertyName("template")]
    public TemplateReference Template { get; }
}

/// <summary>
/// References an approved template by name and language, with optional send-time parameter
/// values for its placeholders.
/// </summary>
public sealed record TemplateReference
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TemplateReference"/> class.
    /// </summary>
    /// <param name="name">The template's name. Must not be null, empty, or whitespace.</param>
    /// <param name="language">The template's language.</param>
    /// <param name="components">
    /// The send-time parameter values for the template's placeholders, or <see langword="null"/>
    /// when the template has no placeholders — omitted from the payload in that case.
    /// </param>
    public TemplateReference(string name, LanguageCode language, IReadOnlyList<TemplateParameterComponent>? components = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(language);
        Name = name;
        Language = language;
        Components = components;
    }

    /// <summary>
    /// The template's name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; }

    /// <summary>
    /// The template's language.
    /// </summary>
    [JsonPropertyName("language")]
    public LanguageCode Language { get; }

    /// <summary>
    /// The send-time parameter values for the template's placeholders. Omitted from the payload
    /// when <see langword="null"/>.
    /// </summary>
    [JsonPropertyName("components")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<TemplateParameterComponent>? Components { get; }
}

/// <summary>
/// The language of an approved template referenced by a <see cref="TemplateReference"/>.
/// </summary>
public sealed record LanguageCode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LanguageCode"/> class.
    /// </summary>
    /// <param name="code">The template's language/locale code, e.g. <c>en_US</c>. Must not be null, empty, or whitespace.</param>
    public LanguageCode(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        Code = code;
    }

    /// <summary>
    /// The template's language/locale code, e.g. <c>en_US</c>.
    /// </summary>
    [JsonPropertyName("code")]
    public string Code { get; }
}
