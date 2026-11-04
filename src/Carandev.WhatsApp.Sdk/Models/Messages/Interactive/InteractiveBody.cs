using System.Text.Json.Serialization;

namespace Carandev.WhatsApp.Sdk.Models.Messages.Interactive;

/// <summary>
/// The required <c>body</c> text of an interactive message.
/// </summary>
public sealed record InteractiveBody
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InteractiveBody"/> class.
    /// </summary>
    /// <param name="text">The body text. Must not be null, empty, or whitespace.</param>
    public InteractiveBody(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        Text = text;
    }

    /// <summary>
    /// The body text.
    /// </summary>
    [JsonPropertyName("text")]
    public string Text { get; }
}

/// <summary>
/// The optional <c>footer</c> text of an interactive message.
/// </summary>
public sealed record InteractiveFooter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InteractiveFooter"/> class.
    /// </summary>
    /// <param name="text">The footer text. Must not be null, empty, or whitespace.</param>
    public InteractiveFooter(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        Text = text;
    }

    /// <summary>
    /// The footer text.
    /// </summary>
    [JsonPropertyName("text")]
    public string Text { get; }
}
