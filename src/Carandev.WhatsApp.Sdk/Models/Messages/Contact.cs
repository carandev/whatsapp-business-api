using System.Text.Json.Serialization;

namespace Carandev.WhatsApp.Sdk.Models.Messages;

/// <summary>
/// A single contact card sent via <c>SendContactsAsync</c>, following Meta's full documented
/// contact object shape. All fields are optional and passed through as-is — no local validation
/// is performed; a malformed contact accepted locally may still be rejected by the Graph API.
/// </summary>
public sealed record Contact
{
    /// <summary>
    /// The contact's postal addresses.
    /// </summary>
    [JsonPropertyName("addresses")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<ContactAddress>? Addresses { get; init; }

    /// <summary>
    /// The contact's birthday, formatted as <c>YYYY-MM-DD</c>.
    /// </summary>
    [JsonPropertyName("birthday")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Birthday { get; init; }

    /// <summary>
    /// The contact's email addresses.
    /// </summary>
    [JsonPropertyName("emails")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<ContactEmail>? Emails { get; init; }

    /// <summary>
    /// The contact's name.
    /// </summary>
    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ContactName? Name { get; init; }

    /// <summary>
    /// The contact's organization.
    /// </summary>
    [JsonPropertyName("org")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ContactOrg? Org { get; init; }

    /// <summary>
    /// The contact's phone numbers.
    /// </summary>
    [JsonPropertyName("phones")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<ContactPhone>? Phones { get; init; }

    /// <summary>
    /// The contact's URLs.
    /// </summary>
    [JsonPropertyName("urls")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<ContactUrl>? Urls { get; init; }
}

/// <summary>
/// The name portion of a <see cref="Contact"/>.
/// </summary>
public sealed record ContactName
{
    /// <summary>
    /// The contact's full display name.
    /// </summary>
    [JsonPropertyName("formatted_name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FormattedName { get; init; }

    /// <summary>
    /// The contact's first name.
    /// </summary>
    [JsonPropertyName("first_name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FirstName { get; init; }

    /// <summary>
    /// The contact's last name.
    /// </summary>
    [JsonPropertyName("last_name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? LastName { get; init; }

    /// <summary>
    /// The contact's middle name.
    /// </summary>
    [JsonPropertyName("middle_name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MiddleName { get; init; }

    /// <summary>
    /// The contact's name suffix (e.g. <c>Jr.</c>).
    /// </summary>
    [JsonPropertyName("suffix")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Suffix { get; init; }

    /// <summary>
    /// The contact's name prefix (e.g. <c>Dr.</c>).
    /// </summary>
    [JsonPropertyName("prefix")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Prefix { get; init; }
}

/// <summary>
/// A single postal address belonging to a <see cref="Contact"/>.
/// </summary>
public sealed record ContactAddress
{
    /// <summary>
    /// The street portion of the address.
    /// </summary>
    [JsonPropertyName("street")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Street { get; init; }

    /// <summary>
    /// The city portion of the address.
    /// </summary>
    [JsonPropertyName("city")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? City { get; init; }

    /// <summary>
    /// The state portion of the address.
    /// </summary>
    [JsonPropertyName("state")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? State { get; init; }

    /// <summary>
    /// The postal/zip code.
    /// </summary>
    [JsonPropertyName("zip")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Zip { get; init; }

    /// <summary>
    /// The country name.
    /// </summary>
    [JsonPropertyName("country")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Country { get; init; }

    /// <summary>
    /// The two-letter country code.
    /// </summary>
    [JsonPropertyName("country_code")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CountryCode { get; init; }

    /// <summary>
    /// The address type, e.g. <c>HOME</c> or <c>WORK</c>.
    /// </summary>
    [JsonPropertyName("type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Type { get; init; }
}

/// <summary>
/// A single email address belonging to a <see cref="Contact"/>.
/// </summary>
public sealed record ContactEmail
{
    /// <summary>
    /// The email address.
    /// </summary>
    [JsonPropertyName("email")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Email { get; init; }

    /// <summary>
    /// The email type, e.g. <c>HOME</c> or <c>WORK</c>.
    /// </summary>
    [JsonPropertyName("type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Type { get; init; }
}

/// <summary>
/// A single phone number belonging to a <see cref="Contact"/>.
/// </summary>
public sealed record ContactPhone
{
    /// <summary>
    /// The phone number.
    /// </summary>
    [JsonPropertyName("phone")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Phone { get; init; }

    /// <summary>
    /// The phone type, e.g. <c>HOME</c>, <c>WORK</c>, or <c>CELL</c>.
    /// </summary>
    [JsonPropertyName("type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Type { get; init; }

    /// <summary>
    /// The WhatsApp id associated with this phone number, if known.
    /// </summary>
    [JsonPropertyName("wa_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? WaId { get; init; }
}

/// <summary>
/// The organization portion of a <see cref="Contact"/>.
/// </summary>
public sealed record ContactOrg
{
    /// <summary>
    /// The company name.
    /// </summary>
    [JsonPropertyName("company")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Company { get; init; }

    /// <summary>
    /// The department name.
    /// </summary>
    [JsonPropertyName("department")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Department { get; init; }

    /// <summary>
    /// The contact's job title.
    /// </summary>
    [JsonPropertyName("title")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Title { get; init; }
}

/// <summary>
/// A single URL belonging to a <see cref="Contact"/>.
/// </summary>
public sealed record ContactUrl
{
    /// <summary>
    /// The URL.
    /// </summary>
    [JsonPropertyName("url")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Url { get; init; }

    /// <summary>
    /// The URL type, e.g. <c>HOME</c> or <c>WORK</c>.
    /// </summary>
    [JsonPropertyName("type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Type { get; init; }
}
