using System.Text.Json.Serialization;

namespace Carandev.WhatsApp.Sdk.Models.PhoneNumbers;

/// <summary>
/// Represents a business phone number registered to a WhatsApp Business Account (WABA), as
/// returned by <c>IWhatsAppPhoneNumbersClient.ListPhoneNumbersAsync</c>/
/// <c>GetPhoneNumberAsync</c>. Mutable and attribute-driven like
/// <see cref="Carandev.WhatsApp.Sdk.Serialization.WhatsAppErrorResponse"/>, since response DTOs carry
/// no construction-time invariants.
/// </summary>
public sealed class PhoneNumber
{
    /// <summary>
    /// The phone number's Meta-assigned identifier.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// The phone number's display name, as it appears to message recipients.
    /// </summary>
    [JsonPropertyName("display_name")]
    public string? DisplayName { get; set; }

    /// <summary>
    /// The phone number in display format, e.g. <c>+1 555-0100</c>.
    /// </summary>
    [JsonPropertyName("display_phone_number")]
    public string? DisplayPhoneNumber { get; set; }

    /// <summary>
    /// The phone number's current messaging quality rating.
    /// </summary>
    [JsonPropertyName("quality_rating")]
    public PhoneNumberQualityRating QualityRating { get; set; }

    /// <summary>
    /// The business name verified by Meta for this phone number.
    /// </summary>
    [JsonPropertyName("verified_name")]
    public string? VerifiedName { get; set; }

    /// <summary>
    /// The review status of the phone number's display name.
    /// </summary>
    [JsonPropertyName("name_status")]
    public PhoneNumberNameStatus NameStatus { get; set; }

    /// <summary>
    /// The phone number's verification status, e.g. <c>VERIFIED</c>, <c>NOT_VERIFIED</c>,
    /// <c>EXPIRED</c>. Left as a plain string — Meta's documented vocabulary for this field is
    /// not treated as closed, unlike <see cref="QualityRating"/>/<see cref="NameStatus"/>.
    /// </summary>
    [JsonPropertyName("code_verification_status")]
    public string? CodeVerificationStatus { get; set; }

    /// <summary>
    /// The platform this phone number is registered on, e.g. <c>CLOUD_API</c>,
    /// <c>ON_PREMISE</c>. Left as a plain string for the same reason as
    /// <see cref="CodeVerificationStatus"/>.
    /// </summary>
    [JsonPropertyName("platform_type")]
    public string? PlatformType { get; set; }

    /// <summary>
    /// The phone number's current messaging throughput tier.
    /// </summary>
    [JsonPropertyName("throughput")]
    public PhoneNumberThroughput? Throughput { get; set; }

    /// <summary>
    /// Whether this phone number belongs to an Official Business Account.
    /// </summary>
    [JsonPropertyName("is_official_business_account")]
    public bool IsOfficialBusinessAccount { get; set; }

    /// <summary>
    /// The webhook configuration currently associated with this phone number, if any.
    /// </summary>
    [JsonPropertyName("webhook_configuration")]
    public PhoneNumberWebhookConfiguration? WebhookConfiguration { get; set; }
}

/// <summary>
/// A business phone number's current messaging throughput tier, as reported by
/// <see cref="PhoneNumber.Throughput"/>.
/// </summary>
public sealed class PhoneNumberThroughput
{
    /// <summary>
    /// The throughput level, e.g. <c>STANDARD</c>, <c>HIGH</c>. Left as a plain string, matching
    /// the same closed-vocabulary handling as <see cref="PhoneNumber.PlatformType"/>.
    /// </summary>
    [JsonPropertyName("level")]
    public string? Level { get; set; }
}

/// <summary>
/// A business phone number's webhook configuration, as reported by
/// <see cref="PhoneNumber.WebhookConfiguration"/>.
/// </summary>
public sealed class PhoneNumberWebhookConfiguration
{
    /// <summary>
    /// The Meta application currently subscribed to this phone number's webhooks.
    /// </summary>
    [JsonPropertyName("application")]
    public string? Application { get; set; }
}
