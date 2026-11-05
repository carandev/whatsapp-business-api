using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Carandev.WhatsApp.Sdk.Models.Templates;

/// <summary>
/// An example value for one named placeholder (for example <c>{{first_name}}</c>), shown to Meta
/// during template review.
/// </summary>
public sealed record NamedParameterExample
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NamedParameterExample"/> class.
    /// </summary>
    /// <param name="paramName">
    /// The placeholder name without braces, e.g. <c>first_name</c>. Must not be null, empty, or
    /// whitespace.
    /// </param>
    /// <param name="example">The example value. Must not be null, empty, or whitespace.</param>
    public NamedParameterExample(string paramName, string example)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(paramName);
        ArgumentException.ThrowIfNullOrWhiteSpace(example);
        ParamName = paramName;
        Example = example;
    }

    /// <summary>
    /// The placeholder name without braces.
    /// </summary>
    [JsonPropertyName("param_name")]
    public string ParamName { get; }

    /// <summary>
    /// The example value.
    /// </summary>
    [JsonPropertyName("example")]
    public string Example { get; }
}

/// <summary>
/// Wire shape of a body's <c>example</c> object. Built by the SDK from
/// <see cref="BodyComponent.Examples"/> or <see cref="BodyComponent.NamedExamples"/>.
/// </summary>
public sealed record BodyExample
{
    [JsonConstructor]
    internal BodyExample()
    {
    }

    /// <summary>
    /// Positional examples (<c>body_text</c>), as one row of values for <c>{{1}}</c>, <c>{{2}}</c>, ...
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("body_text")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<IReadOnlyList<string>>? BodyText { get; internal set; }

    /// <summary>
    /// Named examples (<c>body_text_named_params</c>).
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("body_text_named_params")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<NamedParameterExample>? BodyTextNamedParams { get; internal set; }

    internal static BodyExample? From(
        IReadOnlyList<string>? examples,
        IReadOnlyList<NamedParameterExample>? namedExamples)
    {
        if (examples is { Count: > 0 })
        {
            return new BodyExample { BodyText = [examples] };
        }

        if (namedExamples is { Count: > 0 })
        {
            return new BodyExample { BodyTextNamedParams = namedExamples };
        }

        return null;
    }
}

/// <summary>
/// Placeholder parsing and example validation shared by template components.
/// </summary>
internal static partial class TemplatePlaceholders
{
    [GeneratedRegex(@"\{\{([^{}]*)\}\}")]
    internal static partial Regex PlaceholderPattern();

    [GeneratedRegex(@"^[1-9][0-9]*$")]
    internal static partial Regex PositionalPlaceholderPattern();

    /// <summary>
    /// A valid named placeholder. Meta: named parameters are "composed of lowercase characters and
    /// underscores"; digits are accepted after the first character.
    /// </summary>
    [GeneratedRegex(@"^[a-z_][a-z0-9_]*$")]
    internal static partial Regex NamedPlaceholderPattern();

    /// <summary>
    /// A named placeholder that would be valid if it were lowercase.
    /// </summary>
    [GeneratedRegex(@"^[A-Za-z_][A-Za-z0-9_]*$")]
    private static partial Regex AnyCaseNamedPlaceholderPattern();

    /// <summary>
    /// Throws when <paramref name="text"/> contains a named placeholder with uppercase letters.
    /// </summary>
    internal static void ThrowIfUppercaseNamedPlaceholder(string text, string subject, string paramName)
    {
        foreach (Match match in PlaceholderPattern().Matches(text))
        {
            var content = match.Groups[1].Value;
            if (!NamedPlaceholderPattern().IsMatch(content) && AnyCaseNamedPlaceholderPattern().IsMatch(content))
            {
                throw new ArgumentException(
                    $"{subject} contains the named placeholder '{{{{{content}}}}}' with uppercase letters; named placeholders must be lowercase (a-z, 0-9, and _, starting with a letter or _).",
                    paramName);
            }
        }
    }

    /// <summary>
    /// Validates positional examples against the distinct <c>{{n}}</c> placeholders of
    /// <paramref name="text"/>. A null list is not validated.
    /// </summary>
    internal static void ValidatePositionalExamples(
        string text,
        IReadOnlyList<string>? examples,
        string subject,
        string paramName)
    {
        if (examples is null)
        {
            return;
        }

        var (positional, named) = Extract(text);
        if (named.Count > 0)
        {
            throw new ArgumentException(
                $"{subject} uses named placeholders; provide NamedExamples instead.",
                paramName);
        }

        if (examples.Count != positional.Count)
        {
            throw new ArgumentException(
                $"{subject} has {positional.Count} positional placeholder(s) but {examples.Count} example(s) were provided.",
                paramName);
        }

        foreach (var example in examples)
        {
            if (string.IsNullOrWhiteSpace(example))
            {
                throw new ArgumentException($"{subject} examples must not be null, empty, or whitespace.", paramName);
            }
        }
    }

    /// <summary>
    /// Validates that named examples cover exactly the named placeholders of
    /// <paramref name="text"/>. A null list is not validated.
    /// </summary>
    internal static void ValidateNamedExamples(
        string text,
        IReadOnlyList<NamedParameterExample>? examples,
        string subject,
        string paramName)
    {
        if (examples is null)
        {
            return;
        }

        var (positional, named) = Extract(text);
        if (positional.Count > 0)
        {
            throw new ArgumentException(
                $"{subject} uses positional placeholders; provide Examples instead.",
                paramName);
        }

        var provided = new HashSet<string>(StringComparer.Ordinal);
        foreach (var example in examples)
        {
            if (example is null)
            {
                throw new ArgumentException($"{subject} named examples must not contain null entries.", paramName);
            }

            if (!provided.Add(example.ParamName))
            {
                throw new ArgumentException(
                    $"{subject} named examples contain a duplicate parameter '{example.ParamName}'.",
                    paramName);
            }
        }

        if (!provided.SetEquals(named))
        {
            throw new ArgumentException(
                $"{subject} named examples must match its placeholders. Expected: {Describe(named)}. Provided: {Describe(provided)}.",
                paramName);
        }
    }

    /// <summary>
    /// Returns whether <paramref name="text"/> contains at least one named placeholder.
    /// </summary>
    internal static bool HasNamedPlaceholders(string text) => Extract(text).Named.Count > 0;

    private static (HashSet<string> Positional, HashSet<string> Named) Extract(string text)
    {
        var positional = new HashSet<string>(StringComparer.Ordinal);
        var named = new HashSet<string>(StringComparer.Ordinal);

        foreach (Match match in PlaceholderPattern().Matches(text))
        {
            var content = match.Groups[1].Value;
            if (PositionalPlaceholderPattern().IsMatch(content))
            {
                positional.Add(content);
            }
            else if (NamedPlaceholderPattern().IsMatch(content))
            {
                named.Add(content);
            }
        }

        return (positional, named);
    }

    private static string Describe(IEnumerable<string> names)
    {
        var sorted = names.Order(StringComparer.Ordinal).ToList();
        return sorted.Count == 0 ? "(none)" : string.Join(", ", sorted);
    }
}
