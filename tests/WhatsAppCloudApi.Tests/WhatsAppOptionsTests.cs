using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WhatsAppCloudApi;

namespace WhatsAppCloudApi.Tests;

public class WhatsAppOptionsTests
{
    [Fact]
    public void AllPropertiesBindFromConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["WhatsApp:AccessToken"] = "test-access-token",
                ["WhatsApp:PhoneNumberId"] = "123456789",
                ["WhatsApp:ApiVersion"] = "v99.0",
                ["WhatsApp:BaseUrl"] = "https://example.test/{0}/",
                ["WhatsApp:MaxRetries"] = "7",
                ["WhatsApp:Timeout"] = "00:01:00",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddOptions<WhatsAppOptions>().Bind(configuration.GetSection(WhatsAppOptions.SectionName));
        var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<WhatsAppOptions>>().Value;

        Assert.Equal("test-access-token", options.AccessToken);
        Assert.Equal("123456789", options.PhoneNumberId);
        Assert.Equal("v99.0", options.ApiVersion);
        Assert.Equal("https://example.test/{0}/", options.BaseUrl);
        Assert.Equal(7, options.MaxRetries);
        Assert.Equal(TimeSpan.FromMinutes(1), options.Timeout);
    }

    [Fact]
    public void DefaultsApplyWhenApiVersionAndBaseUrlOmitted()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["WhatsApp:AccessToken"] = "test-access-token",
                ["WhatsApp:PhoneNumberId"] = "123456789",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddOptions<WhatsAppOptions>().Bind(configuration.GetSection(WhatsAppOptions.SectionName));
        var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<WhatsAppOptions>>().Value;

        Assert.Equal("v21.0", options.ApiVersion);
        Assert.Equal("https://graph.facebook.com/{0}/", options.BaseUrl);
        Assert.Equal("https://graph.facebook.com/v21.0/", string.Format(options.BaseUrl, options.ApiVersion));
    }

    [Fact]
    public void ValidationFailsWhenAccessTokenIsMissing()
    {
        var options = new WhatsAppOptions { AccessToken = null!, PhoneNumberId = "123456789" };

        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(
            options,
            new ValidationContext(options),
            results,
            validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(WhatsAppOptions.AccessToken)));
    }

    [Fact]
    public void ValidationFailsWhenPhoneNumberIdIsMissing()
    {
        var options = new WhatsAppOptions { AccessToken = "test-access-token", PhoneNumberId = null! };

        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(
            options,
            new ValidationContext(options),
            results,
            validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(WhatsAppOptions.PhoneNumberId)));
    }
}
