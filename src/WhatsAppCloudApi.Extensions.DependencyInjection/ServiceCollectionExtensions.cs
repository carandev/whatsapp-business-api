using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WhatsAppCloudApi;
using WhatsAppCloudApi.Clients;

namespace WhatsAppCloudApi.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering WhatsApp Cloud API services into an
/// <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Binds <see cref="WhatsAppOptions"/> from the <c>WhatsApp</c> configuration section and
    /// registers <see cref="IWhatsAppClient"/> plus all seven WhatsApp Cloud API resource client
    /// interfaces.
    /// </summary>
    /// <param name="services">The service collection to add registrations to.</param>
    /// <param name="configuration">The configuration root or section to bind options from.</param>
    /// <returns>The same <see cref="IServiceCollection"/> instance for chaining.</returns>
    public static IServiceCollection AddWhatsAppCloudApi(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .AddOptions<WhatsAppOptions>()
            .Bind(configuration.GetSection(WhatsAppOptions.SectionName))
            .ValidateDataAnnotations();

        services.AddHttpClient<IWhatsAppClient, WhatsAppClient>((provider, httpClient) =>
        {
            var options = provider.GetRequiredService<IOptions<WhatsAppOptions>>().Value;
            httpClient.BaseAddress = new Uri(string.Format(options.BaseUrl, options.ApiVersion));
        });

        services.AddTransient<IWhatsAppMessagesClient, WhatsAppMessagesClient>();
        services.AddTransient<IWhatsAppMediaClient, WhatsAppMediaClient>();
        services.AddTransient<IWhatsAppTemplatesClient, WhatsAppTemplatesClient>();
        services.AddTransient<IWhatsAppPhoneNumbersClient, WhatsAppPhoneNumbersClient>();
        services.AddTransient<IWhatsAppBusinessProfileClient, WhatsAppBusinessProfileClient>();
        services.AddTransient<IWhatsAppWebhooksClient, WhatsAppWebhooksClient>();

        return services;
    }
}
