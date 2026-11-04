using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;
using Carandev.WhatsApp.Sdk;
using Carandev.WhatsApp.Sdk.Clients;
using Carandev.WhatsApp.Sdk.DependencyInjection.Resilience;
using Carandev.WhatsApp.Sdk.Webhooks;

namespace Carandev.WhatsApp.Sdk.DependencyInjection;

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

        services.AddTransient<WhatsAppRetryExhaustionHandler>();

        services.AddHttpClient<IWhatsAppClient, WhatsAppClient>((provider, httpClient) =>
        {
            var options = provider.GetRequiredService<IOptions<WhatsAppOptions>>().Value;
            httpClient.BaseAddress = new Uri(string.Format(options.BaseUrl, options.ApiVersion));
        })
        // Registered BEFORE (outside) the resilience handler below, so it only sees the pipeline's
        // FINAL post-retry outcome — never an individual retry attempt.
        .AddHttpMessageHandler<WhatsAppRetryExhaustionHandler>()
        .AddResilienceHandler("whatsapp-retry", (builder, context) =>
        {
            var options = context.ServiceProvider.GetRequiredService<IOptions<WhatsAppOptions>>().Value;

            builder.AddRetry(new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = options.MaxRetries,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                // A short starting backoff — Meta's rate-limit/transient-error windows are typically
                // measured in hundreds of milliseconds to a few seconds, and a short base delay keeps
                // total request latency reasonable while a real Retry-After header (see below) or
                // exponential growth on subsequent attempts still provides real backpressure.
                Delay = TimeSpan.FromMilliseconds(250),
                // ShouldRetryAfterHeader defaults to true, so the built-in DelayGenerator already
                // honors a Retry-After header when present and falls back to the exponential+jitter
                // backoff above otherwise — no custom DelayGenerator needed.
                ShouldHandle = args => WhatsAppRetryPredicate.ShouldRetryAsync(args.Outcome.Result, args.Outcome.Exception, args.Context.CancellationToken),
            });
        });

        services.AddTransient<IWhatsAppMessagesClient, WhatsAppMessagesClient>();
        services.AddTransient<IWhatsAppMediaClient, WhatsAppMediaClient>();
        services.AddTransient<IWhatsAppTemplatesClient, WhatsAppTemplatesClient>();
        services.AddTransient<IWhatsAppPhoneNumbersClient, WhatsAppPhoneNumbersClient>();
        services.AddTransient<IWhatsAppBusinessProfileClient, WhatsAppBusinessProfileClient>();
        services.AddTransient<IWhatsAppWebhooksClient, WhatsAppWebhooksClient>();
        services.AddSingleton<IWhatsAppWebhookProcessor, WhatsAppWebhookProcessor>();

        return services;
    }
}
