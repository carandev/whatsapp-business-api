using System.Security.Cryptography;
using System.Text;
using Carandev.WhatsApp.Sdk;

namespace Carandev.WhatsApp.Sdk.Tests.Webhooks;

public class WhatsAppSignatureValidatorTests
{
    private const string AppSecret = "test-app-secret";
    private const string RawBody = "{\"object\":\"whatsapp_business_account\"}";

    [Fact]
    public void Verify_ReturnsTrue_ForValidSignature()
    {
        var validator = new WhatsAppSignatureValidator(AppSecret);
        var signatureHeader = ComputeSignatureHeader(AppSecret, RawBody);

        var result = validator.Verify(RawBody, signatureHeader);

        Assert.True(result);
    }

    [Fact]
    public void Verify_ReturnsFalse_ForMismatchedSignature()
    {
        var validator = new WhatsAppSignatureValidator(AppSecret);
        var wrongSignatureHeader = ComputeSignatureHeader("a-different-secret", RawBody);

        var result = validator.Verify(RawBody, wrongSignatureHeader);

        Assert.False(result);
    }

    [Fact]
    public void Verify_ReturnsFalse_ForSignatureOfDifferentBody()
    {
        var validator = new WhatsAppSignatureValidator(AppSecret);
        var signatureHeader = ComputeSignatureHeader(AppSecret, "{\"object\":\"different\"}");

        var result = validator.Verify(RawBody, signatureHeader);

        Assert.False(result);
    }

    [Fact]
    public void Verify_ReturnsFalse_WhenSignatureHeaderIsNull()
    {
        var validator = new WhatsAppSignatureValidator(AppSecret);

        var result = validator.Verify(RawBody, null);

        Assert.False(result);
    }

    [Fact]
    public void Verify_ReturnsFalse_WhenSignatureHeaderIsEmpty()
    {
        var validator = new WhatsAppSignatureValidator(AppSecret);

        var result = validator.Verify(RawBody, string.Empty);

        Assert.False(result);
    }

    [Fact]
    public void Verify_ReturnsFalse_WhenHeaderIsMissingShaPrefix()
    {
        var validator = new WhatsAppSignatureValidator(AppSecret);
        var digestHexOnly = ComputeSignatureHeader(AppSecret, RawBody).Replace("sha256=", string.Empty, StringComparison.Ordinal);

        var result = validator.Verify(RawBody, digestHexOnly);

        Assert.False(result);
    }

    [Fact]
    public void Verify_ReturnsFalse_WhenHeaderHasInvalidHex()
    {
        var validator = new WhatsAppSignatureValidator(AppSecret);

        var result = validator.Verify(RawBody, "sha256=not-valid-hex-zzz");

        Assert.False(result);
    }

    [Fact]
    public void Verify_ReturnsFalse_WhenAppSecretIsNull()
    {
        var validator = new WhatsAppSignatureValidator(null);
        var signatureHeader = ComputeSignatureHeader(AppSecret, RawBody);

        var result = validator.Verify(RawBody, signatureHeader);

        Assert.False(result);
    }

    [Fact]
    public void Verify_ReturnsFalse_WhenAppSecretIsEmpty()
    {
        var validator = new WhatsAppSignatureValidator(string.Empty);
        var signatureHeader = ComputeSignatureHeader(AppSecret, RawBody);

        var result = validator.Verify(RawBody, signatureHeader);

        Assert.False(result);
    }

    private static string ComputeSignatureHeader(string secret, string rawBody)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawBody));
        return "sha256=" + Convert.ToHexString(hash).ToLowerInvariant();
    }
}
