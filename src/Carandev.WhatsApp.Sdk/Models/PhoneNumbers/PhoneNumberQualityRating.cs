using System.Text.Json;
using System.Text.Json.Serialization;

namespace Carandev.WhatsApp.Sdk.Models.PhoneNumbers;

/// <summary>
/// Meta's closed-vocabulary quality rating for a business phone number.
/// </summary>
[JsonConverter(typeof(PhoneNumberQualityRatingJsonConverter))]
public enum PhoneNumberQualityRating
{
    /// <summary>
    /// High quality — the number is unlikely to be restricted.
    /// </summary>
    Green,

    /// <summary>
    /// Medium quality — the number is at risk of restriction.
    /// </summary>
    Yellow,

    /// <summary>
    /// Low quality — the number is likely to be restricted.
    /// </summary>
    Red,

    /// <summary>
    /// Quality rating not yet available, e.g. for a newly registered number.
    /// </summary>
    Unknown,
}

/// <summary>
/// Converts <see cref="PhoneNumberQualityRating"/> to and from Meta's upper-snake-case JSON string
/// values (<c>GREEN</c>, <c>YELLOW</c>, <c>RED</c>, <c>UNKNOWN</c>).
/// </summary>
public sealed class PhoneNumberQualityRatingJsonConverter : JsonConverter<PhoneNumberQualityRating>
{
    /// <inheritdoc />
    public override PhoneNumberQualityRating Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return value switch
        {
            "GREEN" => PhoneNumberQualityRating.Green,
            "YELLOW" => PhoneNumberQualityRating.Yellow,
            "RED" => PhoneNumberQualityRating.Red,
            "UNKNOWN" => PhoneNumberQualityRating.Unknown,
            _ => throw new JsonException($"Unknown phone number quality rating '{value}'."),
        };
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, PhoneNumberQualityRating value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);

        writer.WriteStringValue(value switch
        {
            PhoneNumberQualityRating.Green => "GREEN",
            PhoneNumberQualityRating.Yellow => "YELLOW",
            PhoneNumberQualityRating.Red => "RED",
            PhoneNumberQualityRating.Unknown => "UNKNOWN",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown phone number quality rating."),
        });
    }
}
