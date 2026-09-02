namespace WhatsAppCloudApi.Models.Templates;

/// <summary>
/// Optional filters and pagination parameters for <c>IWhatsAppTemplatesClient.ListAsync</c>. Each
/// field is translated to a query-string parameter when non-null; a <see langword="null"/> field
/// is omitted entirely from the request rather than defaulted.
/// </summary>
public sealed record TemplatesListOptions
{
    /// <summary>
    /// Filters results to templates whose name matches this value.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Filters results to templates with this review/approval status.
    /// </summary>
    public string? Status { get; init; }

    /// <summary>
    /// Filters results to templates in this language.
    /// </summary>
    public string? Language { get; init; }

    /// <summary>
    /// The maximum number of items to return on this page.
    /// </summary>
    public int? Limit { get; init; }

    /// <summary>
    /// The pagination cursor for the page after this one.
    /// </summary>
    public string? After { get; init; }

    /// <summary>
    /// The pagination cursor for the page before this one.
    /// </summary>
    public string? Before { get; init; }
}
