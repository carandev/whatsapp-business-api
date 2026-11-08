using System.Text.Json;
using System.Text.Json.Serialization;

namespace Carandev.WhatsApp.Sdk.Models.Templates;

/// <summary>
/// Base type for a single component of a message template (header, body, footer, or buttons).
/// Discriminated on the wire by the Meta <c>type</c> property.
/// </summary>
[JsonPolymorphic(
    TypeDiscriminatorPropertyName = "type",
    UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToNearestAncestor)]
[JsonDerivedType(typeof(HeaderComponent), "HEADER")]
[JsonDerivedType(typeof(BodyComponent), "BODY")]
[JsonDerivedType(typeof(FooterComponent), "FOOTER")]
[JsonDerivedType(typeof(ButtonsComponent), "BUTTONS")]
public abstract record TemplateComponent
{
    private protected TemplateComponent()
    {
    }
}

/// <summary>
/// A component read from Meta whose <c>type</c> the SDK does not model, for example
/// <c>CAROUSEL</c> or <c>LIMITED_TIME_OFFER</c>. It keeps the component in
/// <see cref="Template.Components"/> instead of failing the whole response, so a template type
/// Meta adds later never breaks reading.
/// </summary>
/// <remarks>
/// Serializing it writes <see cref="RawJson"/> unchanged, so a template read from Meta can be sent
/// back with the component intact. The SDK creates it only when reading templates.
/// </remarks>
public sealed record UnknownTemplateComponent : TemplateComponent
{
    internal UnknownTemplateComponent(string type, JsonElement rawJson)
    {
        Type = type;
        RawJson = rawJson;
    }

    /// <summary>
    /// Meta's <c>type</c> value exactly as returned, for example <c>CAROUSEL</c>. Empty when the
    /// component has no string <c>type</c>.
    /// </summary>
    public string Type { get; }

    /// <summary>
    /// The complete component object exactly as Meta returned it, including <c>type</c>.
    /// </summary>
    public JsonElement RawJson { get; }
}
