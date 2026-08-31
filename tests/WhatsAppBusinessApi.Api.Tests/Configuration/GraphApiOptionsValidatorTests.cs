using WhatsAppBusinessApi.Api.Configuration;

namespace WhatsAppBusinessApi.Api.Tests.Configuration;

public class GraphApiOptionsValidatorTests
{
    private readonly GraphApiOptionsValidator _sut = new();

    public static TheoryData<GraphApiOptions, string[]> FailureCases()
    {
        var data = new TheoryData<GraphApiOptions, string[]>
        {
            {
                new GraphApiOptions { PhoneNumberId = "", AccessToken = "" },
                [
                    "GraphApi:PhoneNumberId is required and was not found or empty.",
                    "GraphApi:AccessToken is required and was not found or empty.",
                ]
            },
            {
                new GraphApiOptions { PhoneNumberId = "   ", AccessToken = "token" },
                ["GraphApi:PhoneNumberId is required and was not found or empty."]
            },
            {
                new GraphApiOptions { PhoneNumberId = "12345", AccessToken = "" },
                ["GraphApi:AccessToken is required and was not found or empty."]
            },
        };

        return data;
    }

    [Theory]
    [MemberData(nameof(FailureCases))]
    public void Validate_MissingOrEmptyRequiredKeys_ReturnsFailWithExpectedMessages(
        GraphApiOptions options, string[] expectedFailures)
    {
        var result = _sut.Validate(name: null, options);

        Assert.True(result.Failed);
        Assert.Equal(expectedFailures, result.Failures);
    }

    [Fact]
    public void Validate_AllRequiredKeysPresent_ReturnsSuccess()
    {
        var options = new GraphApiOptions
        {
            PhoneNumberId = "1234567890",
            AccessToken = "EAAG...token",
        };

        var result = _sut.Validate(name: null, options);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Failures ?? []);
    }
}
