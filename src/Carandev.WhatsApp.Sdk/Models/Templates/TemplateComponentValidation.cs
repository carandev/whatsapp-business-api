namespace Carandev.WhatsApp.Sdk.Models.Templates;

/// <summary>
/// Shared structural validation for a template's <see cref="TemplateComponent"/> collection.
/// Consumed by <c>CreateTemplateRequest</c> and <c>UpdateTemplateRequest</c> (added in a later
/// change) to enforce Meta's minimum shape. Validation here is structural only — it does not
/// enforce category-specific business rules (e.g. Meta re-validates those on submission).
/// </summary>
internal static class TemplateComponentValidation
{
    /// <summary>
    /// Validates the structural shape of a components collection: non-empty, exactly one
    /// <see cref="BodyComponent"/>, at most one <see cref="HeaderComponent"/>, and at most one
    /// <see cref="FooterComponent"/>.
    /// </summary>
    /// <param name="components">The components to validate.</param>
    /// <param name="paramName">The caller's parameter name, used in thrown exceptions.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="components"/> is empty, does not contain exactly one
    /// <see cref="BodyComponent"/>, contains more than one <see cref="HeaderComponent"/>, or
    /// contains more than one <see cref="FooterComponent"/>.
    /// </exception>
    public static void ValidateStructure(IReadOnlyList<TemplateComponent> components, string paramName)
    {
        ArgumentNullException.ThrowIfNull(components);

        if (components.Count == 0)
        {
            throw new ArgumentException("At least one component is required.", paramName);
        }

        var bodyCount = 0;
        var headerCount = 0;
        var footerCount = 0;

        foreach (var component in components)
        {
            switch (component)
            {
                case BodyComponent:
                    bodyCount++;
                    break;
                case HeaderComponent:
                    headerCount++;
                    break;
                case FooterComponent:
                    footerCount++;
                    break;
            }
        }

        if (bodyCount != 1)
        {
            throw new ArgumentException("Exactly one BODY component is required.", paramName);
        }

        if (headerCount > 1)
        {
            throw new ArgumentException("At most one HEADER component is allowed.", paramName);
        }

        if (footerCount > 1)
        {
            throw new ArgumentException("At most one FOOTER component is allowed.", paramName);
        }
    }
}
