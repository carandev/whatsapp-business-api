using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WhatsAppCloudApi.Clients;
using WhatsAppCloudApi.Extensions.DependencyInjection;

namespace WhatsAppCloudApi.Tests;

public class ServiceCollectionExtensionsTests
{
    private static ServiceProvider BuildProvider(out IConfiguration configuration)
    {
        configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["WhatsApp:AccessToken"] = "test-access-token",
                ["WhatsApp:PhoneNumberId"] = "123456789",
                ["WhatsApp:ApiVersion"] = "v21.0",
                ["WhatsApp:BaseUrl"] = "https://graph.facebook.com/{0}/",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddWhatsAppCloudApi(configuration);
        return services.BuildServiceProvider();
    }

    [Fact]
    public void AllSevenClientInterfaces_ResolveSuccessfully()
    {
        using var provider = BuildProvider(out _);

        Assert.NotNull(provider.GetRequiredService<IWhatsAppClient>());
        Assert.NotNull(provider.GetRequiredService<IWhatsAppMessagesClient>());
        Assert.NotNull(provider.GetRequiredService<IWhatsAppMediaClient>());
        Assert.NotNull(provider.GetRequiredService<IWhatsAppTemplatesClient>());
        Assert.NotNull(provider.GetRequiredService<IWhatsAppPhoneNumbersClient>());
        Assert.NotNull(provider.GetRequiredService<IWhatsAppBusinessProfileClient>());
        Assert.NotNull(provider.GetRequiredService<IWhatsAppWebhooksClient>());
    }

    [Fact]
    public void Options_ResolvableAfterRegistration_AndReflectSuppliedConfiguration()
    {
        using var provider = BuildProvider(out _);

        var options = provider.GetRequiredService<IOptions<WhatsAppOptions>>().Value;

        Assert.Equal("test-access-token", options.AccessToken);
        Assert.Equal("123456789", options.PhoneNumberId);
    }

    [Fact]
    public void TypedClient_BaseAddress_ComposedFromBaseUrlAndApiVersion()
    {
        using var provider = BuildProvider(out _);

        var factory = provider.GetRequiredService<IHttpClientFactory>();
        var httpClient = factory.CreateClient(nameof(IWhatsAppClient));

        Assert.Equal(new Uri("https://graph.facebook.com/v21.0/"), httpClient.BaseAddress);
    }
}
