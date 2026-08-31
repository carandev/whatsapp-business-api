using System.Security.Cryptography;
using System.Text;
using WhatsAppBusinessApi.Api.Webhooks;

namespace WhatsAppBusinessApi.Api.Tests.Webhooks;

/// <summary>
/// Unit coverage for <see cref="WebhookSignatureValidator"/>: valid, invalid,
/// and missing <c>X-Hub-Signature-256</c> signatures, per design.md's
/// "Unit — signature" testing strategy row.
/// </summary>
public class WebhookSignatureValidatorTests
{
    private const string AppSecret = "test-app-secret";
    private static readonly byte[] Body = Encoding.UTF8.GetBytes("""{"object":"whatsapp_business_account"}""");

    [Fact]
    public void IsValid_CorrectSignature_ReturnsTrue()
    {
        var signatureHeader = ComputeSignatureHeader(Body, AppSecret);

        var result = WebhookSignatureValidator.IsValid(Body, signatureHeader, AppSecret);

        Assert.True(result);
    }

    [Fact]
    public void IsValid_SignatureComputedWithWrongSecret_ReturnsFalse()
    {
        var signatureHeader = ComputeSignatureHeader(Body, "a-different-secret");

        var result = WebhookSignatureValidator.IsValid(Body, signatureHeader, AppSecret);

        Assert.False(result);
    }

    [Fact]
    public void IsValid_TamperedBody_ReturnsFalse()
    {
        var signatureHeader = ComputeSignatureHeader(Body, AppSecret);
        var tamperedBody = Encoding.UTF8.GetBytes("""{"object":"tampered"}""");

        var result = WebhookSignatureValidator.IsValid(tamperedBody, signatureHeader, AppSecret);

        Assert.False(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void IsValid_MissingSignatureHeader_ReturnsFalse(string? signatureHeader)
    {
        var result = WebhookSignatureValidator.IsValid(Body, signatureHeader, AppSecret);

        Assert.False(result);
    }

    [Fact]
    public void IsValid_SignatureMissingShaPrefix_ReturnsFalse()
    {
        var rawHex = Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(AppSecret), Body)).ToLowerInvariant();

        var result = WebhookSignatureValidator.IsValid(Body, rawHex, AppSecret);

        Assert.False(result);
    }

    [Fact]
    public void IsValid_MalformedHexSignature_ReturnsFalse()
    {
        var result = WebhookSignatureValidator.IsValid(Body, "sha256=not-hex", AppSecret);

        Assert.False(result);
    }

    [Fact]
    public void IsValid_EmptyAppSecret_ReturnsFalse()
    {
        var signatureHeader = ComputeSignatureHeader(Body, AppSecret);

        var result = WebhookSignatureValidator.IsValid(Body, signatureHeader, appSecret: "");

        Assert.False(result);
    }

    private static string ComputeSignatureHeader(byte[] body, string appSecret)
    {
        var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(appSecret), body);
        return "sha256=" + Convert.ToHexString(hash).ToLowerInvariant();
    }
}
