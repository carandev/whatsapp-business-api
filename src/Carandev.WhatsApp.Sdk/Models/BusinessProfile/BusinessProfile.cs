using System.Text.Json.Serialization;

namespace Carandev.WhatsApp.Sdk.Models.BusinessProfile;

/// <summary>
/// Represents a WhatsApp Business Profile for a phone number, as returned by
/// <c>IWhatsAppBusinessProfileClient.GetBusinessProfileAsync</c>. Mutable and attribute-driven
/// like <see cref="Carandev.WhatsApp.Sdk.Serialization.WhatsAppErrorResponse"/>, since response DTOs
/// carry no construction-time invariants.
/// </summary>
public sealed class BusinessProfile
{
    /// <summary>
    /// The business's "About" text.
    /// </summary>
    [JsonPropertyName("about")]
    public string? About { get; set; }

    /// <summary>
    /// The business's physical address.
    /// </summary>
    [JsonPropertyName("address")]
    public string? Address { get; set; }

    /// <summary>
    /// The business's description.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// The business's contact email address.
    /// </summary>
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    /// <summary>
    /// The business's website URLs.
    /// </summary>
    [JsonPropertyName("websites")]
    public IReadOnlyList<string>? Websites { get; set; }

    /// <summary>
    /// The business's industry vertical, e.g. <c>RETAIL</c>, <c>PROF_SERVICES</c>. Left as a
    /// plain string — Meta's documented vocabulary for this field is not treated as closed,
    /// unlike <c>QualityRating</c>/<c>NameStatus</c> on <c>PhoneNumber</c>.
    /// </summary>
    [JsonPropertyName("vertical")]
    public string? Vertical { get; set; }

    /// <summary>
    /// The opaque handle of the business's uploaded profile picture.
    /// </summary>
    [JsonPropertyName("profile_picture_handle")]
    public string? ProfilePictureHandle { get; set; }
}

/// <summary>
/// The envelope Meta wraps a <see cref="Models.BusinessProfile.BusinessProfile"/> in when
/// responding to <c>GET /{phone-number-id}/whatsapp_business_profile</c>, shaped as
/// <c>{ data: [{...}] }</c>.
/// </summary>
public sealed class BusinessProfileResponse
{
    /// <summary>
    /// The single-element list containing the requested business profile.
    /// </summary>
    [JsonPropertyName("data")]
    public IReadOnlyList<BusinessProfile>? Data { get; set; }
}
