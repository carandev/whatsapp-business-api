using System.Text.Json.Serialization;

namespace Carandev.WhatsApp.Sdk.Models.Templates;

/// <summary>
/// A cursor-paginated page of results from the Meta Graph API, shaped as
/// <c>{ data: [...], paging: { cursors: { before, after } } }</c>.
/// </summary>
/// <typeparam name="T">The type of each item in <see cref="Data"/>.</typeparam>
public sealed class TemplatesPage<T>
{
    /// <summary>
    /// The items on this page.
    /// </summary>
    [JsonPropertyName("data")]
    public IReadOnlyList<T>? Data { get; set; }

    /// <summary>
    /// Pagination metadata for retrieving adjacent pages.
    /// </summary>
    [JsonPropertyName("paging")]
    public TemplatesPaging? Paging { get; set; }
}

/// <summary>
/// Pagination metadata for a <see cref="TemplatesPage{T}"/>.
/// </summary>
public sealed class TemplatesPaging
{
    /// <summary>
    /// The before/after cursor pair for this page.
    /// </summary>
    [JsonPropertyName("cursors")]
    public TemplatesPagingCursors? Cursors { get; set; }
}

/// <summary>
/// The before/after cursor pair carried by <see cref="TemplatesPaging"/>.
/// </summary>
public sealed class TemplatesPagingCursors
{
    /// <summary>
    /// The cursor pointing to the page before the current one.
    /// </summary>
    [JsonPropertyName("before")]
    public string? Before { get; set; }

    /// <summary>
    /// The cursor pointing to the page after the current one.
    /// </summary>
    [JsonPropertyName("after")]
    public string? After { get; set; }
}
