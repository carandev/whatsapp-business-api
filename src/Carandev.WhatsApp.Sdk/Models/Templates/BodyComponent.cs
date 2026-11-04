using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Carandev.WhatsApp.Sdk.Models.Templates;

/// <summary>
/// A template's <c>BODY</c> component: text that may contain either positional placeholders
/// (<c>{{1}}</c>, <c>{{2}}</c>, ...) or named placeholders (<c>{{customer_name}}</c>), but not a
/// mix of both.
/// </summary>
public sealed partial record BodyComponent : TemplateComponent
{
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

    private static void ValidatePlaceholders(string text)
    {
        if (CountOccurrences(text, "{{") != CountOccurrences(text, "}}"))
        {
            throw new ArgumentException("Body text has malformed placeholder braces.", nameof(text));
        }

        bool? isPositional = null;

        foreach (Match match in PlaceholderPattern().Matches(text))
        {
            var content = match.Groups[1].Value;
            var positional = PositionalPlaceholderPattern().IsMatch(content);
            var named = !positional && NamedPlaceholderPattern().IsMatch(content);

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

    [GeneratedRegex(@"\{\{([^{}]*)\}\}")]
    private static partial Regex PlaceholderPattern();

    [GeneratedRegex(@"^[1-9][0-9]*$")]
    private static partial Regex PositionalPlaceholderPattern();

    [GeneratedRegex(@"^[A-Za-z_][A-Za-z0-9_]*$")]
    private static partial Regex NamedPlaceholderPattern();
}
