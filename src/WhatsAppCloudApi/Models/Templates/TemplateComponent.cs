using System.Text.Json.Serialization;

namespace WhatsAppCloudApi.Models.Templates;

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
