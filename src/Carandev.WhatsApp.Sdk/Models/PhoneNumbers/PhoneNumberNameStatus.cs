using System.Text.Json;
using System.Text.Json.Serialization;

namespace Carandev.WhatsApp.Sdk.Models.PhoneNumbers;

/// <summary>
/// Meta's closed-vocabulary review status for a business phone number's display name.
/// </summary>
[JsonConverter(typeof(PhoneNumberNameStatusJsonConverter))]
public enum PhoneNumberNameStatus
{
    /// <summary>
    /// The display name has been reviewed and approved.
    /// </summary>
    Approved,

    /// <summary>
    /// The display name is available for use without having gone through review.
    /// </summary>
    AvailableWithoutReview,

    /// <summary>
    /// The display name was reviewed and declined.
    /// </summary>
    Declined,

    /// <summary>
    /// A previously approved display name has expired.
    /// </summary>
    Expired,

    /// <summary>
    /// The display name is currently under review.
    /// </summary>
    PendingReview,

    /// <summary>
    /// No display name has been submitted.
    /// </summary>
    None,
}

/// <summary>
/// Converts <see cref="PhoneNumberNameStatus"/> to and from Meta's upper-snake-case JSON string
/// values (<c>APPROVED</c>, <c>AVAILABLE_WITHOUT_REVIEW</c>, <c>DECLINED</c>, <c>EXPIRED</c>,
/// <c>PENDING_REVIEW</c>, <c>NONE</c>).
/// </summary>
public sealed class PhoneNumberNameStatusJsonConverter : JsonConverter<PhoneNumberNameStatus>
{
    /// <inheritdoc />
    public override PhoneNumberNameStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return value switch
        {
            "APPROVED" => PhoneNumberNameStatus.Approved,
            "AVAILABLE_WITHOUT_REVIEW" => PhoneNumberNameStatus.AvailableWithoutReview,
            "DECLINED" => PhoneNumberNameStatus.Declined,
            "EXPIRED" => PhoneNumberNameStatus.Expired,
            "PENDING_REVIEW" => PhoneNumberNameStatus.PendingReview,
            "NONE" => PhoneNumberNameStatus.None,
            _ => throw new JsonException($"Unknown phone number name status '{value}'."),
        };
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, PhoneNumberNameStatus value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);

        writer.WriteStringValue(value switch
        {
            PhoneNumberNameStatus.Approved => "APPROVED",
            PhoneNumberNameStatus.AvailableWithoutReview => "AVAILABLE_WITHOUT_REVIEW",
            PhoneNumberNameStatus.Declined => "DECLINED",
            PhoneNumberNameStatus.Expired => "EXPIRED",
            PhoneNumberNameStatus.PendingReview => "PENDING_REVIEW",
            PhoneNumberNameStatus.None => "NONE",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown phone number name status."),
        });
    }
}
