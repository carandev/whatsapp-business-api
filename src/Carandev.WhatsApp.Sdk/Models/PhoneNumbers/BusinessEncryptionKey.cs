using System.Text.Json;
using System.Text.Json.Serialization;

namespace Carandev.WhatsApp.Sdk.Models.PhoneNumbers;

/// <summary>
/// The business public key registered on a phone number, returned by
/// <c>GET /{phone-number-id}/whatsapp_business_encryption</c>. WhatsApp encrypts every Flow data
/// endpoint request with this key.
/// </summary>
public sealed record BusinessEncryptionKey
{
    /// <summary>
    /// The registered public key in PEM format, or null when none is registered.
    /// </summary>
    [JsonPropertyName("business_public_key")]
    public string? PublicKey { get; init; }

    /// <summary>
    /// Whether Meta's signature check of the key passed, or null when Meta returns none.
    /// </summary>
    [JsonPropertyName("business_public_key_signature_status")]
    public BusinessPublicKeySignatureStatus? SignatureStatus { get; init; }
}

/// <summary>
/// The signature status of a registered business public key.
/// </summary>
[JsonConverter(typeof(BusinessPublicKeySignatureStatusJsonConverter))]
public enum BusinessPublicKeySignatureStatus
{
    /// <summary>
    /// A status this SDK version does not know yet.
    /// </summary>
    Unknown,

    /// <summary>
    /// <c>VALID</c>.
    /// </summary>
    Valid,

    /// <summary>
    /// <c>MISMATCH</c>.
    /// </summary>
    Mismatch,
}

/// <summary>
/// Reads and writes <see cref="BusinessPublicKeySignatureStatus"/> as Meta's upper case values.
/// Unrecognized values read as <see cref="BusinessPublicKeySignatureStatus.Unknown"/>.
/// </summary>
public sealed class BusinessPublicKeySignatureStatusJsonConverter : JsonConverter<BusinessPublicKeySignatureStatus>
{
    /// <inheritdoc />
    public override BusinessPublicKeySignatureStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.GetString() switch
        {
            "VALID" => BusinessPublicKeySignatureStatus.Valid,
            "MISMATCH" => BusinessPublicKeySignatureStatus.Mismatch,
            _ => BusinessPublicKeySignatureStatus.Unknown,
        };

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, BusinessPublicKeySignatureStatus value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteStringValue(value switch
        {
            BusinessPublicKeySignatureStatus.Valid => "VALID",
            BusinessPublicKeySignatureStatus.Mismatch => "MISMATCH",
            _ => "UNKNOWN",
        });
    }
}
