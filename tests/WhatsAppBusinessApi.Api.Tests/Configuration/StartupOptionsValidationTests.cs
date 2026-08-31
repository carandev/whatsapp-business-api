using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace WhatsAppBusinessApi.Api.Tests.Configuration;

/// <summary>
/// Integration coverage for the fail-fast startup contract: hosting must
/// refuse to start when required GraphApi/Webhook configuration keys are
/// missing or empty, per design.md "Configuration".
/// </summary>
public class StartupOptionsValidationTests
{
    [Fact]
    public void Host_WithMissingRequiredConfiguration_ThrowsOptionsValidationExceptionOnStartup()
    {
        // appsettings.json ships GraphApi/Webhook keys as empty stubs by
        // design (no secrets committed), so the default factory config
        // already exercises the "missing/empty required config" scenario.
        using var factory = new WebApplicationFactory<Program>();

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

        var validationException = FindOptionsValidationException(exception);

        Assert.NotNull(validationException);
        Assert.Contains("GraphApi:PhoneNumberId", validationException!.Message);
        Assert.Contains("GraphApi:AccessToken", validationException.Message);
    }

    [Fact]
    public void Host_WithAllRequiredConfigurationPresent_StartsSuccessfully()
    {
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configBuilder) =>
                {
                    configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["GraphApi:PhoneNumberId"] = "1234567890",
                        ["GraphApi:AccessToken"] = "test-access-token",
                        ["Webhook:VerifyToken"] = "test-verify-token",
                        ["Webhook:AppSecret"] = "test-app-secret",
                    });
                });
            });

        using var client = factory.CreateClient();

        Assert.NotNull(client);
    }

    private static OptionsValidationException? FindOptionsValidationException(Exception? exception)
    {
        while (exception is not null)
        {
            if (exception is OptionsValidationException optionsValidationException)
            {
                return optionsValidationException;
            }

            if (exception is AggregateException aggregate)
            {
                foreach (var inner in aggregate.InnerExceptions)
                {
                    var found = FindOptionsValidationException(inner);
                    if (found is not null)
                    {
                        return found;
                    }
                }
            }

            exception = exception.InnerException;
        }

        return null;
    }
}
