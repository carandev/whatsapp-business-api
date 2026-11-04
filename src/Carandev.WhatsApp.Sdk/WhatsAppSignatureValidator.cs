using System.Security.Cryptography;
using System.Text;

namespace Carandev.WhatsApp.Sdk;

/// <summary>
/// Verifies that an inbound webhook request body genuinely originated from Meta by validating
/// the <c>X-Hub-Signature-256</c> header against an HMAC SHA256 digest of the raw body.
/// </summary>
public sealed class WhatsAppSignatureValidator
{
    private const string SignaturePrefix = "sha256=";

    private readonly string? _appSecret;

    /// <summary>
    /// Creates a validator bound to <paramref name="appSecret"/>. A <see langword="null"/> or
    /// empty secret is not an error at construction time — <see cref="Verify"/> reports it as an
    /// automatic mismatch (fail closed) instead of throwing.
    /// </summary>
    /// <param name="appSecret">The Meta App Secret used to compute the expected HMAC digest.</param>
    public WhatsAppSignatureValidator(string? appSecret)
    {
        _appSecret = appSecret;
    }

    /// <summary>
    /// Computes the HMAC SHA256 digest of <paramref name="rawBody"/> using the configured App
    /// Secret and compares it, in constant time, to the digest carried by
    /// <paramref name="signatureHeader"/> (expected format: <c>sha256=&lt;hex&gt;</c>).
    /// </summary>
    /// <param name="rawBody">The exact, unmodified request body bytes as received, decoded as a string.</param>
    /// <param name="signatureHeader">The value of the <c>X-Hub-Signature-256</c> header, or <see langword="null"/> if absent.</param>
    /// <returns>
    /// <see langword="true"/> when the signature matches; otherwise <see langword="false"/>.
    /// Never throws for a missing/null App Secret, a missing/empty header, or a malformed header —
    /// each of those is treated as a mismatch.
    /// </returns>
    public bool Verify(string rawBody, string? signatureHeader)
    {
        ArgumentNullException.ThrowIfNull(rawBody);

        if (string.IsNullOrEmpty(_appSecret) || string.IsNullOrEmpty(signatureHeader))
        {
            return false;
        }

        if (!signatureHeader.StartsWith(SignaturePrefix, StringComparison.Ordinal))
        {
            return false;
        }

        var providedHex = signatureHeader[SignaturePrefix.Length..];
        if (!TryParseHex(providedHex, out var providedBytes))
        {
            return false;
        }

        var expectedBytes = ComputeHmac(rawBody, _appSecret);

        return CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes);
    }

    private static byte[] ComputeHmac(string rawBody, string secret)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var bodyBytes = Encoding.UTF8.GetBytes(rawBody);
        using var hmac = new HMACSHA256(keyBytes);
        return hmac.ComputeHash(bodyBytes);
    }

    private static bool TryParseHex(string hex, out byte[] bytes)
    {
        try
        {
            bytes = Convert.FromHexString(hex);
            return true;
        }
        catch (FormatException)
        {
            bytes = [];
            return false;
        }
    }
}
