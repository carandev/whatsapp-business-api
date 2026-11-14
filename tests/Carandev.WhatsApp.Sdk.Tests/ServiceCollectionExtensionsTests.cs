using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Carandev.WhatsApp.Sdk.Clients;
using Carandev.WhatsApp.Sdk.Exceptions;
using Carandev.WhatsApp.Sdk.DependencyInjection;
using Carandev.WhatsApp.Sdk.DependencyInjection.Resilience;
using Carandev.WhatsApp.Sdk.Serialization;
using Carandev.WhatsApp.Sdk.Tests.Fakes;
using Carandev.WhatsApp.Sdk.Webhooks;

namespace Carandev.WhatsApp.Sdk.Tests;

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

    /// <summary>
    /// Builds a fully DI-wired provider (options + typed client + "whatsapp-retry" resilience
    /// pipeline registered by <see cref="ServiceCollectionExtensions.AddWhatsAppCloudApi"/>), then
    /// swaps out the innermost primary <see cref="HttpMessageHandler"/> for
    /// <paramref name="handler"/> so no real network call is made. The resilience pipeline's
    /// <c>DelegatingHandler</c>s registered via <c>AddResilienceHandler</c> sit OUTSIDE the primary
    /// handler, so they still run against <paramref name="handler"/>'s canned responses.
    /// </summary>
    private static ServiceProvider BuildProviderWithRecordingHandler(HttpMessageHandler handler, int maxRetries, TimeSpan? timeout = null)
    {
        var settings = new Dictionary<string, string?>
        {
            ["WhatsApp:AccessToken"] = "test-access-token",
            ["WhatsApp:PhoneNumberId"] = "123456789",
            ["WhatsApp:ApiVersion"] = "v21.0",
            ["WhatsApp:BaseUrl"] = "https://graph.facebook.com/{0}/",
            ["WhatsApp:MaxRetries"] = maxRetries.ToString(CultureInfo.InvariantCulture),
        };
        if (timeout is { } value)
        {
            settings["WhatsApp:Timeout"] = value.ToString("c", CultureInfo.InvariantCulture);
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var services = new ServiceCollection();
        services.AddWhatsAppCloudApi(configuration);
        services.AddHttpClient(nameof(IWhatsAppClient))
            .ConfigurePrimaryHttpMessageHandler(() => handler);

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
        Assert.NotNull(provider.GetRequiredService<IWhatsAppFlowsClient>());
    }

    [Fact]
    public void WebhookProcessor_ResolvesAsSingleton()
    {
        using var provider = BuildProvider(out _);

        var first = provider.GetRequiredService<IWhatsAppWebhookProcessor>();

        Assert.IsType<WhatsAppWebhookProcessor>(first);
        Assert.Same(first, provider.GetRequiredService<IWhatsAppWebhookProcessor>());
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

    /// <summary>
    /// Answers each request after the delay its attempt number maps to, honoring cancellation so
    /// a per-attempt timeout actually aborts the attempt.
    /// </summary>
    private sealed class DelayingHandler(Func<int, TimeSpan> delayForAttempt) : HttpMessageHandler
    {
        private int _requestCount;

        public int RequestCount => _requestCount;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var attempt = Interlocked.Increment(ref _requestCount);
            await Task.Delay(delayForAttempt(attempt), cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }

    [Fact]
    public void TypedClient_HttpClientTimeout_IsInfinite_SoItNeverCutsRetries()
    {
        using var provider = BuildProvider(out _);

        var httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(IWhatsAppClient));

        Assert.Equal(Timeout.InfiniteTimeSpan, httpClient.Timeout);
    }

    [Fact]
    public async Task RetryPipeline_AttemptExceedingTimeout_IsRetried_AndNextAttemptSucceeds()
    {
        var handler = new DelayingHandler(attempt => attempt == 1 ? TimeSpan.FromSeconds(30) : TimeSpan.Zero);
        using var provider = BuildProviderWithRecordingHandler(handler, maxRetries: 1, timeout: TimeSpan.FromMilliseconds(200));
        var client = provider.GetRequiredService<IWhatsAppClient>();
        var stopwatch = Stopwatch.StartNew();

        var response = await client.SendAsync(client.CreateRequest(HttpMethod.Get, "diagnostics"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, handler.RequestCount);
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(10), $"The slow first attempt was not cut by the 200ms timeout ({stopwatch.Elapsed}).");
    }

    [Fact]
    public async Task RetryPipeline_TimeoutOnEveryAttempt_ThrowsTransientWhatsAppException_AfterMaxRetries()
    {
        var handler = new DelayingHandler(_ => TimeSpan.FromSeconds(30));
        using var provider = BuildProviderWithRecordingHandler(handler, maxRetries: 1, timeout: TimeSpan.FromMilliseconds(100));
        var client = provider.GetRequiredService<IWhatsAppClient>();

        var exception = await Assert.ThrowsAsync<WhatsAppException>(() =>
            client.SendAsync(client.CreateRequest(HttpMethod.Get, "diagnostics")));

        Assert.Equal(2, handler.RequestCount);
        Assert.True(exception.IsTransient);
        Assert.Equal((int)HttpStatusCode.GatewayTimeout, exception.Code);
        Assert.IsType<Polly.Timeout.TimeoutRejectedException>(exception.InnerException);
    }

    [Fact]
    public async Task RetryPipeline_CallerCancellation_IsNotConvertedIntoTimeoutException()
    {
        var handler = new DelayingHandler(_ => TimeSpan.FromSeconds(30));
        using var provider = BuildProviderWithRecordingHandler(handler, maxRetries: 1, timeout: TimeSpan.FromSeconds(20));
        var client = provider.GetRequiredService<IWhatsAppClient>();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            client.SendAsync(client.CreateRequest(HttpMethod.Get, "diagnostics"), cts.Token));
    }

    [Fact]
    public async Task RetryPipeline_MaxRetriesZero_SendsOneAttempt_AndStillThrowsOnTransientFailure()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        using var provider = BuildProviderWithRecordingHandler(handler, maxRetries: 0);
        var client = provider.GetRequiredService<IWhatsAppClient>();

        var exception = await Assert.ThrowsAsync<WhatsAppException>(() =>
            client.SendAsync(client.CreateRequest(HttpMethod.Get, "diagnostics")));

        Assert.Equal(1, handler.RequestCount);
        Assert.True(exception.IsTransient);
    }

    [Fact]
    public async Task RetryPipeline_MaxRetriesZero_StillAppliesTimeout()
    {
        var handler = new DelayingHandler(_ => TimeSpan.FromSeconds(30));
        using var provider = BuildProviderWithRecordingHandler(handler, maxRetries: 0, timeout: TimeSpan.FromMilliseconds(100));
        var client = provider.GetRequiredService<IWhatsAppClient>();

        var exception = await Assert.ThrowsAsync<WhatsAppException>(() =>
            client.SendAsync(client.CreateRequest(HttpMethod.Get, "diagnostics")));

        Assert.Equal(1, handler.RequestCount);
        Assert.Equal((int)HttpStatusCode.GatewayTimeout, exception.Code);
    }

    [Fact]
    public void Options_NegativeMaxRetries_FailsValidation()
    {
        var handler = new RecordingHandler();
        using var provider = BuildProviderWithRecordingHandler(handler, maxRetries: -1);

        var exception = Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IOptions<WhatsAppOptions>>().Value);

        Assert.Contains(nameof(WhatsAppOptions.MaxRetries), exception.Message);
    }

    [Fact]
    public async Task RetryPipeline_TransientStatusThenSuccess_RetriesUntilSuccessfulAndReturnsFinalResponse()
    {
        var statuses = new Queue<HttpStatusCode>([HttpStatusCode.ServiceUnavailable, HttpStatusCode.ServiceUnavailable, HttpStatusCode.OK]);
        var handler = new RecordingHandler(_ => new HttpResponseMessage(statuses.Dequeue()));
        using var provider = BuildProviderWithRecordingHandler(handler, maxRetries: 2);
        var client = provider.GetRequiredService<IWhatsAppClient>();

        var response = await client.SendAsync(client.CreateRequest(HttpMethod.Get, "diagnostics"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(3, handler.RequestCount);
    }

    [Fact]
    public async Task RetryPipeline_TransientGraphSubcodeIn4xxBody_Retries()
    {
        const string transientBody = """{"error":{"message":"Rate limited","type":"OAuthException","code":100,"error_subcode":131048}}""";
        var responded = false;
        var handler = new RecordingHandler(_ =>
        {
            if (!responded)
            {
                responded = true;
                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent(transientBody, Encoding.UTF8, "application/json"),
                };
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        using var provider = BuildProviderWithRecordingHandler(handler, maxRetries: 1);
        var client = provider.GetRequiredService<IWhatsAppClient>();

        var response = await client.SendAsync(client.CreateRequest(HttpMethod.Get, "diagnostics"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, handler.RequestCount);
    }

    /// <summary>
    /// Covers the spec's "Retry-After respected" scenario (resilience-pipeline domain), previously
    /// untested: the pipeline's built-in <c>ShouldRetryAfterHeader</c> default (see
    /// <see cref="ServiceCollectionExtensions.AddWhatsAppCloudApi"/>) must honor a real
    /// <c>Retry-After</c> header rather than falling back to the pipeline's own short 250ms
    /// exponential+jitter base delay. A 1-second header value is deliberately >> the 250ms base
    /// delay, so a measured elapsed time well past 250ms but consistent with ~1s proves the header
    /// — not the default backoff — actually governed the wait.
    /// </summary>
    [Fact]
    public async Task RetryPipeline_RetryAfterHeader_DelaysRetryByAtLeastHeaderValue()
    {
        var responded = false;
        var handler = new RecordingHandler(_ =>
        {
            if (!responded)
            {
                responded = true;
                var retryAfterResponse = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
                retryAfterResponse.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(1));
                return retryAfterResponse;
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        using var provider = BuildProviderWithRecordingHandler(handler, maxRetries: 1);
        var client = provider.GetRequiredService<IWhatsAppClient>();

        var stopwatch = Stopwatch.StartNew();
        var response = await client.SendAsync(client.CreateRequest(HttpMethod.Get, "diagnostics"));
        stopwatch.Stop();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, handler.RequestCount);
        Assert.True(
            stopwatch.Elapsed >= TimeSpan.FromMilliseconds(900),
            $"Expected the retry delay to respect the 1s Retry-After header (>= ~900ms), but only {stopwatch.Elapsed.TotalMilliseconds}ms elapsed — the pipeline's own 250ms default backoff may have been used instead.");
    }

    [Fact]
    public async Task RetryPipeline_MaxRetries_NotExceeded_WhenFailureNeverResolves()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        using var provider = BuildProviderWithRecordingHandler(handler, maxRetries: 2);
        var client = provider.GetRequiredService<IWhatsAppClient>();

        // Retries are bounded (initial attempt + at most MaxRetries retries — never unbounded), but
        // since the failure is transient and never resolves, WhatsAppRetryExhaustionHandler now
        // throws instead of returning the last failed response (see the exhaustion tests below).
        await Assert.ThrowsAsync<WhatsAppException>(() => client.SendAsync(client.CreateRequest(HttpMethod.Get, "diagnostics")));

        Assert.Equal(3, handler.RequestCount);
    }

    [Fact]
    public async Task RetryPipeline_ExhaustedOn5xx_ThrowsWhatsAppException_WithFinalStatusAsCode()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        using var provider = BuildProviderWithRecordingHandler(handler, maxRetries: 1);
        var client = provider.GetRequiredService<IWhatsAppClient>();

        var exception = await Assert.ThrowsAsync<WhatsAppException>(() =>
            client.SendAsync(client.CreateRequest(HttpMethod.Get, "diagnostics")));

        Assert.Equal(2, handler.RequestCount);
        Assert.Equal((int)HttpStatusCode.ServiceUnavailable, exception.Code);
        Assert.True(exception.IsTransient);
    }

    [Fact]
    public async Task RetryPipeline_ExhaustedOnRateLimitCode_ThrowsWhatsAppRateLimitException()
    {
        const string rateLimitedBody = """{"error":{"message":"Too many requests","type":"OAuthException","code":80007,"fbtrace_id":"trace-abc"}}""";
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.TooManyRequests)
        {
            Content = new StringContent(rateLimitedBody, Encoding.UTF8, "application/json"),
        });
        using var provider = BuildProviderWithRecordingHandler(handler, maxRetries: 1);
        var client = provider.GetRequiredService<IWhatsAppClient>();

        var exception = await Assert.ThrowsAsync<WhatsAppRateLimitException>(() =>
            client.SendAsync(client.CreateRequest(HttpMethod.Get, "diagnostics")));

        Assert.Equal(2, handler.RequestCount);
        Assert.Equal(80007, exception.Code);
        Assert.Equal("trace-abc", exception.FbTraceId);
        Assert.True(exception.IsTransient);
    }

    [Fact]
    public async Task RetryPipeline_TransientThenSuccess_DoesNotThrow_RegressionCheck()
    {
        var statuses = new Queue<HttpStatusCode>([HttpStatusCode.ServiceUnavailable, HttpStatusCode.OK]);
        var handler = new RecordingHandler(_ => new HttpResponseMessage(statuses.Dequeue()));
        using var provider = BuildProviderWithRecordingHandler(handler, maxRetries: 2);
        var client = provider.GetRequiredService<IWhatsAppClient>();

        // Must complete normally — no exception — since the pipeline resolved before exhaustion.
        var response = await client.SendAsync(client.CreateRequest(HttpMethod.Get, "diagnostics"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, handler.RequestCount);
    }

    [Fact]
    public async Task RetryPipeline_PermanentNonTransient4xx_DoesNotRetry_PassesThroughUnchanged()
    {
        const string permanentBody = """{"error":{"message":"Invalid parameter","type":"OAuthException","code":100,"error_subcode":2018001}}""";
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(permanentBody, Encoding.UTF8, "application/json"),
        });
        using var provider = BuildProviderWithRecordingHandler(handler, maxRetries: 3);
        var client = provider.GetRequiredService<IWhatsAppClient>();

        var response = await client.SendAsync(client.CreateRequest(HttpMethod.Get, "diagnostics"));

        Assert.Equal(1, handler.RequestCount);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        // The body must still be fully readable downstream — GraphErrorPeek's buffer-and-replace
        // must not have consumed it even though the predicate peeked it to decide not to retry.
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(permanentBody, body);
    }

    /// <summary>
    /// Mandated consistency lock-in (tasks.md 3.5): <see cref="WhatsAppError.IsTransient"/> and the
    /// resilience pipeline's own classification (<see cref="WhatsAppRetryPredicate.ShouldRetryAsync"/>,
    /// which is the exact delegate wired as the "whatsapp-retry" pipeline's <c>ShouldHandle</c> — see
    /// <see cref="ServiceCollectionExtensions.AddWhatsAppCloudApi"/>) must never diverge, for the full
    /// seeded transient-subcode list and for 5xx responses carrying a structured JSON error body.
    /// </summary>
    [Theory]
    [InlineData(100, 131048, "seeded transient subcode: spam rate limit hit")]
    [InlineData(100, 131056, "seeded transient subcode: phone-number pair rate limit")]
    [InlineData(100, 133016, "seeded transient subcode: message throughput exceeded")]
    [InlineData(80007, null, "rate-limit code is always transient")]
    public async Task Consistency_SeededTransientSubcodes_IsTransientAgreesWithPipelinePredicate(int code, int? subcode, string because)
    {
        var subcodeJson = subcode is { } s ? s.ToString(CultureInfo.InvariantCulture) : "null";
        var json = $$$"""{"error":{"message":"m","type":"OAuthException","code":{{{code}}},"error_subcode":{{{subcodeJson}}}}}""";
        using var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };

        var error = new WhatsAppError { Code = code, ErrorSubcode = subcode };
        var pipelineWouldRetry = await WhatsAppRetryPredicate.ShouldRetryAsync(response, exception: null, CancellationToken.None);

        Assert.True(error.IsTransient == pipelineWouldRetry, because);
        Assert.True(error.IsTransient, because);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task Consistency_5xxWithStructuredJsonBody_IsTransientAgreesWithPipelinePredicate(HttpStatusCode statusCode)
    {
        // A 5xx response whose JSON body happens to echo the HTTP status as its own "code" field
        // (common for infra-level gateway errors that still wrap a JSON envelope). The pipeline's
        // predicate must classify this transient purely from the real HttpStatusCode; WhatsAppError
        // must independently agree via Code's dual Graph/HTTP-status meaning.
        var statusAsCode = (int)statusCode;
        var json = $$$"""{"error":{"message":"Upstream failure","type":"OAuthException","code":{{{statusAsCode}}}}}""";
        using var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };

        var error = new WhatsAppError { Code = statusAsCode };
        var pipelineWouldRetry = await WhatsAppRetryPredicate.ShouldRetryAsync(response, exception: null, CancellationToken.None);

        Assert.True(error.IsTransient);
        Assert.True(pipelineWouldRetry);
        Assert.Equal(error.IsTransient, pipelineWouldRetry);
    }

    [Theory]
    [InlineData(190, 463, "expired access token is a permanent business error")]
    [InlineData(100, 2018001, "invalid template parameter is a permanent business error")]
    public async Task Consistency_PermanentBusinessErrors_IsTransientAgreesWithPipelinePredicate(int code, int? subcode, string because)
    {
        var subcodeJson = subcode is { } s ? s.ToString(CultureInfo.InvariantCulture) : "null";
        var json = $$$"""{"error":{"message":"m","type":"OAuthException","code":{{{code}}},"error_subcode":{{{subcodeJson}}}}}""";
        using var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };

        var error = new WhatsAppError { Code = code, ErrorSubcode = subcode };
        var pipelineWouldRetry = await WhatsAppRetryPredicate.ShouldRetryAsync(response, exception: null, CancellationToken.None);

        Assert.False(error.IsTransient, because);
        Assert.False(pipelineWouldRetry, because);
        Assert.Equal(error.IsTransient, pipelineWouldRetry);
    }
}
