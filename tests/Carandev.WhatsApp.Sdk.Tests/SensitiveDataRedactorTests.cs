using Carandev.WhatsApp.Sdk.Logging;

namespace Carandev.WhatsApp.Sdk.Tests;

public class SensitiveDataRedactorTests
{
    public static IEnumerable<object[]> SensitiveKeyVariants =>
        new List<object[]>
        {
            new object[] { "token" },
            new object[] { "Token" },
            new object[] { "TOKEN" },
            new object[] { "access_token" },
            new object[] { "Access_Token" },
            new object[] { "ACCESS_TOKEN" },
            new object[] { "Authorization" },
            new object[] { "authorization" },
            new object[] { "AUTHORIZATION" },
        };

    [Theory]
    [MemberData(nameof(SensitiveKeyVariants))]
    public void Redact_MasksSensitiveKeys_CaseInsensitively(string key)
    {
        var fields = new Dictionary<string, string?> { [key] = "super-secret-value" };

        var redacted = SensitiveDataRedactor.Redact(fields);

        Assert.Equal(SensitiveDataRedactor.Mask, redacted[key]);
    }

    [Fact]
    public void Redact_MasksSensitiveKeys_LeavesUnrelatedFieldsUntouched()
    {
        var fields = new Dictionary<string, string?>
        {
            ["access_token"] = "super-secret-value",
            ["Authorization"] = "Bearer super-secret-value",
            ["messaging_product"] = "whatsapp",
        };

        var redacted = SensitiveDataRedactor.Redact(fields);

        Assert.Equal(SensitiveDataRedactor.Mask, redacted["access_token"]);
        Assert.Equal(SensitiveDataRedactor.Mask, redacted["Authorization"]);
        Assert.Equal("whatsapp", redacted["messaging_product"]);
    }

    [Fact]
    public void Redact_NoFalsePositive_WhenNoSensitiveKeysPresent()
    {
        var fields = new Dictionary<string, string?>
        {
            ["messaging_product"] = "whatsapp",
            ["to"] = "15551234567",
            ["type"] = "text",
        };

        var redacted = SensitiveDataRedactor.Redact(fields);

        Assert.Equal(fields, redacted);
    }
}
