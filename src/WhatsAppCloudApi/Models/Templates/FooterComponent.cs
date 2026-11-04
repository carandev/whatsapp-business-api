using System.Text.Json.Serialization;

namespace WhatsAppCloudApi.Models.Templates;

/// <summary>
/// A template's <c>FOOTER</c> component: plain text only.
/// </summary>
public sealed record FooterComponent : TemplateComponent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FooterComponent"/> class.
    /// </summary>
    /// <param name="text">The footer text. Must not be null, empty, or whitespace.</param>
    public FooterComponent(string text)
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
