using System.Text.Json.Serialization;

namespace WhatsAppCloudApi.Models.BusinessProfile;

/// <summary>
/// An immutable request to partially update a WhatsApp Business Profile via
/// <c>IWhatsAppBusinessProfileClient.UpdateBusinessProfileAsync</c>. All fields are optional and
/// independently settable; unlike <c>UpdateTemplateRequest</c>, no field combination is required
/// at construction time — a fully-empty update is plausible and harmless (Meta simply changes
/// nothing). Any field left <see langword="null"/> is omitted from the outgoing JSON body, so
/// only explicitly supplied fields are changed.
/// </summary>
public sealed record UpdateBusinessProfileRequest
{
    /// <summary>
    /// The business's new "About" text, or <see langword="null"/> to leave unchanged.
    /// </summary>
    [JsonPropertyName("about")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? About { get; init; }

    /// <summary>
    /// The business's new physical address, or <see langword="null"/> to leave unchanged.
    /// </summary>
    [JsonPropertyName("address")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Address { get; init; }

    /// <summary>
    /// The business's new description, or <see langword="null"/> to leave unchanged.
    /// </summary>
    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; init; }

    /// <summary>
    /// The business's new contact email address, or <see langword="null"/> to leave unchanged.
    /// </summary>
    [JsonPropertyName("email")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Email { get; init; }

    /// <summary>
    /// The business's new website URLs, or <see langword="null"/> to leave unchanged.
    /// </summary>
    [JsonPropertyName("websites")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? Websites { get; init; }

    /// <summary>
    /// The business's new industry vertical, or <see langword="null"/> to leave unchanged.
    /// </summary>
    [JsonPropertyName("vertical")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Vertical { get; init; }

    /// <summary>
    /// The opaque handle of a newly uploaded profile picture, or <see langword="null"/> to leave
    /// unchanged.
    /// </summary>
    [JsonPropertyName("profile_picture_handle")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ProfilePictureHandle { get; init; }
}
