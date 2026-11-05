namespace Carandev.WhatsApp.Sdk.Models.PhoneNumbers;

/// <summary>
/// Optional pagination parameters for
/// <c>IWhatsAppPhoneNumbersClient.ListPhoneNumbersAsync(PhoneNumbersListOptions, string?, CancellationToken)</c>.
/// Each field is translated to a query-string parameter when non-null; a <see langword="null"/>
/// field is omitted from the request rather than defaulted.
/// </summary>
/// <remarks>
/// To page through every number, pass the previous page's <c>Paging.Cursors.After</c> as
/// <see cref="After"/> until it comes back <see langword="null"/>.
/// </remarks>
public sealed record PhoneNumbersListOptions
{
    /// <summary>
    /// The maximum number of phone numbers to return on this page.
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
