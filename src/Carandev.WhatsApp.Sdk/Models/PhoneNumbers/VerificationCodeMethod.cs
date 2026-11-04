using System.Text.Json;
using System.Text.Json.Serialization;

namespace Carandev.WhatsApp.Sdk.Models.PhoneNumbers;

/// <summary>
/// The delivery method used to send a phone number verification code via
/// <c>IWhatsAppPhoneNumbersClient.RequestVerificationCodeAsync</c>.
/// </summary>
[JsonConverter(typeof(VerificationCodeMethodJsonConverter))]
public enum VerificationCodeMethod
{
    /// <summary>
    /// Deliver the verification code via SMS.
    /// </summary>
    Sms,

    /// <summary>
    /// Deliver the verification code via an automated voice call.
    /// </summary>
    Voice,
}

/// <summary>
/// Converts <see cref="VerificationCodeMethod"/> to and from Meta's lowercase JSON string values
/// (<c>sms</c>, <c>voice</c>).
/// </summary>
public sealed class VerificationCodeMethodJsonConverter : JsonConverter<VerificationCodeMethod>
{
    /// <inheritdoc />
    public override VerificationCodeMethod Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return value switch
        {
            "sms" => VerificationCodeMethod.Sms,
            "voice" => VerificationCodeMethod.Voice,
            _ => throw new JsonException($"Unknown verification code method '{value}'."),
        };
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, VerificationCodeMethod value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);

        writer.WriteStringValue(value switch
        {
            VerificationCodeMethod.Sms => "sms",
            VerificationCodeMethod.Voice => "voice",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown verification code method."),
        });
    }
}
