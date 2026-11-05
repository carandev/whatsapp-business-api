using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Carandev.WhatsApp.Sdk.Models.Templates;

/// <summary>
/// A template's <c>BODY</c> component: text that may contain either positional placeholders
/// (<c>{{1}}</c>, <c>{{2}}</c>, ...) or named placeholders (<c>{{customer_name}}</c>), but not a
/// mix of both.
/// </summary>
public sealed record BodyComponent : TemplateComponent
{
    private readonly IReadOnlyList<string>? _examples;
    private readonly IReadOnlyList<NamedParameterExample>? _namedExamples;

    /// <summary>
    /// Initializes a new instance of the <see cref="BodyComponent"/> class.
    /// </summary>
    /// <param name="text">The body text. Must not be null, empty, or whitespace.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="text"/> is null/empty/whitespace, contains malformed
    /// placeholder braces, contains a placeholder that is neither a valid positional nor a valid
    /// named placeholder, or mixes positional and named placeholders.
    /// </exception>
    public BodyComponent(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        ValidatePlaceholders(text);
        Text = text;
    }

    /// <summary>
    /// The body text.
    /// </summary>
    [JsonPropertyName("text")]
    public string Text { get; }

    /// <summary>
    /// Optional example values for the positional placeholders, in order: the first value is for
    /// <c>{{1}}</c>, the second for <c>{{2}}</c>, and so on. Serialized as
    /// <c>"example": { "body_text": [[...]] }</c>; omitted when null or empty.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when the text uses named placeholders, when the count differs from the number of
    /// distinct positional placeholders, or when a value is null, empty, or whitespace.
    /// </exception>
    [JsonIgnore]
    public IReadOnlyList<string>? Examples
    {
        get => _examples;
        init
        {
            TemplatePlaceholders.ValidatePositionalExamples(Text, value, "Body text", nameof(Examples));
            _examples = value;
        }
    }

    /// <summary>
    /// Optional example values for the named placeholders, one per distinct placeholder name.
    /// Serialized as <c>"example": { "body_text_named_params": [...] }</c>; omitted when null or
    /// empty.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when the text uses positional placeholders, when a name is repeated, or when the
    /// names differ from the text's named placeholders.
    /// </exception>
    [JsonIgnore]
    public IReadOnlyList<NamedParameterExample>? NamedExamples
    {
        get => _namedExamples;
        init
        {
            TemplatePlaceholders.ValidateNamedExamples(Text, value, "Body text", nameof(NamedExamples));
            _namedExamples = value;
        }
    }

    /// <summary>
    /// The body example in Meta's wire shape, omitted when no examples were provided.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("example")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    internal BodyExample? WireExample
    {
        get => BodyExample.From(_examples, _namedExamples);
        init
        {
            // Read path for templates returned by Meta; Meta already validated these examples.
            _examples = value?.BodyText is { Count: > 0 } rows ? rows[0] : null;
            _namedExamples = value?.BodyTextNamedParams;
        }
    }

    private static void ValidatePlaceholders(string text)
    {
        if (CountOccurrences(text, "{{") != CountOccurrences(text, "}}"))
        {
            throw new ArgumentException("Body text has malformed placeholder braces.", nameof(text));
        }

        bool? isPositional = null;

        foreach (Match match in TemplatePlaceholders.PlaceholderPattern().Matches(text))
        {
            var content = match.Groups[1].Value;
            var positional = TemplatePlaceholders.PositionalPlaceholderPattern().IsMatch(content);
            var named = !positional && TemplatePlaceholders.NamedPlaceholderPattern().IsMatch(content);

            if (!positional && !named)
            {
                throw new ArgumentException(
                    $"Body text contains an invalid placeholder '{{{{{content}}}}}'.",
                    nameof(text));
            }

            if (isPositional is null)
            {
                isPositional = positional;
            }
            else if (isPositional != positional)
            {
                throw new ArgumentException(
                    "Body text cannot mix positional and named placeholders.",
                    nameof(text));
            }
        }
    }

    private static int CountOccurrences(string text, string token)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(token, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += token.Length;
        }

        return count;
    }

}
