using System.Security.Cryptography;
using System.Text;

namespace WhatsAppBusinessApi.Api.Webhooks;

/// <summary>
/// Validates the <c>X-Hub-Signature-256</c> header Meta attaches to every
/// inbound webhook POST. Per design.md's "Signature validation" decision, the
/// caller MUST pass the exact raw request body bytes read before any JSON
/// model binding — model binding would consume/transform the stream before a
/// byte-exact HMAC could be computed.
/// </summary>
public static class WebhookSignatureValidator
{
    private const string SignaturePrefix = "sha256=";
    private const int Sha256HashSizeInBytes = 32;

    /// <summary>
    /// Computes the HMAC-SHA256 of <paramref name="rawBody"/> using
    /// <paramref name="appSecret"/> and compares it against
    /// <paramref name="signatureHeader"/> (the raw <c>X-Hub-Signature-256</c>
    /// header value, expected in <c>sha256=&lt;hex&gt;</c> form) using a
    /// constant-time comparison.
    /// </summary>
    public static bool IsValid(ReadOnlySpan<byte> rawBody, string? signatureHeader, string? appSecret)
    {
        if (string.IsNullOrEmpty(signatureHeader) || string.IsNullOrEmpty(appSecret))
        {
            return false;
        }

        if (!signatureHeader.StartsWith(SignaturePrefix, StringComparison.Ordinal))
        {
            return false;
        }

        var providedHex = signatureHeader[SignaturePrefix.Length..];

        byte[] providedSignature;
        try
        {
            providedSignature = Convert.FromHexString(providedHex);
        }
        catch (FormatException)
        {
            return false;
        }

        if (providedSignature.Length != Sha256HashSizeInBytes)
        {
            return false;
        }

        var keyBytes = Encoding.UTF8.GetBytes(appSecret);
        var computedSignature = HMACSHA256.HashData(keyBytes, rawBody);

        return CryptographicOperations.FixedTimeEquals(providedSignature, computedSignature);
    }
}
