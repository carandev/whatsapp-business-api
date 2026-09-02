using System.Text.Json.Serialization;

namespace WhatsAppCloudApi.Models.Templates;

/// <summary>
/// A template's <c>BUTTONS</c> component: a non-empty, ordered list of <see cref="TemplateButton"/>.
/// </summary>
public sealed record ButtonsComponent : TemplateComponent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ButtonsComponent"/> class.
    /// </summary>
    /// <param name="buttons">The buttons for this component. Must contain at least one entry.</param>
    public ButtonsComponent(IReadOnlyList<TemplateButton> buttons)
    {
        ArgumentNullException.ThrowIfNull(buttons);

        if (buttons.Count == 0)
        {
            throw new ArgumentException("At least one button is required.", nameof(buttons));
        }

        Buttons = buttons;
    }

    /// <summary>
    /// The buttons for this component, in display order.
    /// </summary>
    [JsonPropertyName("buttons")]
    public IReadOnlyList<TemplateButton> Buttons { get; }
}
