using WhatsAppBusinessApi.Api.Configuration;

namespace WhatsAppBusinessApi.Api.Tests.Configuration;

public class WebhookOptionsValidatorTests
{
    private readonly WebhookOptionsValidator _sut = new();

    public static TheoryData<WebhookOptions, string[]> FailureCases()
    {
        var data = new TheoryData<WebhookOptions, string[]>
        {
            {
                new WebhookOptions { VerifyToken = "", AppSecret = "" },
                [
                    "Webhook:VerifyToken is required and was not found or empty.",
                    "Webhook:AppSecret is required and was not found or empty.",
                ]
            },
            {
                new WebhookOptions { VerifyToken = "   ", AppSecret = "secret" },
                ["Webhook:VerifyToken is required and was not found or empty."]
            },
            {
                new WebhookOptions { VerifyToken = "my-verify-token", AppSecret = "" },
                ["Webhook:AppSecret is required and was not found or empty."]
            },
        };

        return data;
    }

    [Theory]
    [MemberData(nameof(FailureCases))]
    public void Validate_MissingOrEmptyRequiredKeys_ReturnsFailWithExpectedMessages(
        WebhookOptions options, string[] expectedFailures)
    {
        var result = _sut.Validate(name: null, options);

        Assert.True(result.Failed);
        Assert.Equal(expectedFailures, result.Failures);
    }

    [Fact]
    public void Validate_AllRequiredKeysPresent_ReturnsSuccess()
    {
        var options = new WebhookOptions
        {
            VerifyToken = "my-verify-token",
            AppSecret = "shh-its-a-secret",
        };

        var result = _sut.Validate(name: null, options);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Failures ?? []);
    }
}
