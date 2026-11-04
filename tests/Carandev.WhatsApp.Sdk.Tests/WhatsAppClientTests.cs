using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Carandev.WhatsApp.Sdk.Logging;
using Carandev.WhatsApp.Sdk.Resilience;
using Carandev.WhatsApp.Sdk.Tests.Fakes;

namespace Carandev.WhatsApp.Sdk.Tests;

public class WhatsAppClientTests
{
    private static WhatsAppOptions CreateOptions() => new()
    {
        AccessToken = "test-access-token",
        PhoneNumberId = "default-phone-id",
        ApiVersion = "v21.0",
        BaseUrl = "https://graph.facebook.com/{0}/",
    };

    private static (WhatsAppClient Client, RecordingHandler Handler) CreateClient(WhatsAppOptions options)
    {
        var handler = new RecordingHandler();
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(string.Format(options.BaseUrl, options.ApiVersion)),
        };
        var client = new WhatsAppClient(httpClient, Options.Create(options), NullLogger<WhatsAppClient>.Instance);
        return (client, handler);
    }

    private static (WhatsAppClient Client, RecordingHandler Handler, FakeLogger<WhatsAppClient> Logger) CreateClientWithLogger(
        WhatsAppOptions options,
        Func<HttpRequestMessage, HttpResponseMessage>? respond = null,
        LogLevel minLevel = LogLevel.Trace)
    {
        var handler = respond is null ? new RecordingHandler() : new RecordingHandler(respond);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(string.Format(options.BaseUrl, options.ApiVersion)),
        };
        var logger = new FakeLogger<WhatsAppClient>(minLevel);
        var client = new WhatsAppClient(httpClient, Options.Create(options), logger);
        return (client, handler, logger);
    }

    [Fact]
    public void CreateRequest_AttachesBearerAuthorizationHeader()
    {
        var options = CreateOptions();
        var (client, _) = CreateClient(options);

        var request = client.CreateRequest(HttpMethod.Get, "{phone_number_id}/messages");

        Assert.NotNull(request.Headers.Authorization);
        Assert.Equal("Bearer", request.Headers.Authorization!.Scheme);
        Assert.Equal(options.AccessToken, request.Headers.Authorization.Parameter);
    }

    [Fact]
    public void CreateRequest_UsesOptionsPhoneNumberId_WhenNoOverrideSupplied()
    {
        var options = CreateOptions();
        var (client, _) = CreateClient(options);

        var request = client.CreateRequest(HttpMethod.Get, "{phone_number_id}/messages");

        Assert.Equal($"{options.PhoneNumberId}/messages", request.RequestUri!.ToString());
    }

    [Fact]
    public void CreateRequest_PerCallOverrideTakesPrecedence_OverOptionsPhoneNumberId()
    {
        var options = CreateOptions();
        var (client, _) = CreateClient(options);

        var request = client.CreateRequest(HttpMethod.Get, "{phone_number_id}/messages", phoneNumberIdOverride: "override-phone-id");

        Assert.Equal("override-phone-id/messages", request.RequestUri!.ToString());
        Assert.NotEqual($"{options.PhoneNumberId}/messages", request.RequestUri.ToString());
    }

    [Fact]
    public async Task SendAsync_SendsRequestThroughUnderlyingHttpClient()
    {
        var options = CreateOptions();
        var (client, handler) = CreateClient(options);
        var request = client.CreateRequest(HttpMethod.Get, "{phone_number_id}/messages");

        var response = await client.SendAsync(request);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.Same(request, handler.LastRequest);
    }

    [Fact]
    public void BaseAddressFormat_ComposesFromBaseUrlAndApiVersion()
    {
        var options = CreateOptions();

        var baseAddress = string.Format(options.BaseUrl, options.ApiVersion);

        Assert.Equal("https://graph.facebook.com/v21.0/", baseAddress);
    }

    [Fact]
    public void CreateWabaRequest_AttachesBearerAuthorizationHeader()
    {
        var options = CreateOptions();
        options.WabaId = "default-waba-id";
        var (client, _) = CreateClient(options);

        var request = client.CreateWabaRequest(HttpMethod.Get, "{waba_id}/message_templates");

        Assert.NotNull(request.Headers.Authorization);
        Assert.Equal("Bearer", request.Headers.Authorization!.Scheme);
        Assert.Equal(options.AccessToken, request.Headers.Authorization.Parameter);
    }

    [Fact]
    public void CreateWabaRequest_UsesOptionsWabaId_WhenNoOverrideSupplied()
    {
        var options = CreateOptions();
        options.WabaId = "default-waba-id";
        var (client, _) = CreateClient(options);

        var request = client.CreateWabaRequest(HttpMethod.Get, "{waba_id}/message_templates");

        Assert.Equal($"{options.WabaId}/message_templates", request.RequestUri!.ToString());
    }

    [Fact]
    public void CreateWabaRequest_PerCallOverrideTakesPrecedence_OverOptionsWabaId()
    {
        var options = CreateOptions();
        options.WabaId = "default-waba-id";
        var (client, _) = CreateClient(options);

        var request = client.CreateWabaRequest(HttpMethod.Get, "{waba_id}/message_templates", wabaIdOverride: "override-waba-id");

        Assert.Equal("override-waba-id/message_templates", request.RequestUri!.ToString());
        Assert.NotEqual($"{options.WabaId}/message_templates", request.RequestUri.ToString());
    }

    [Fact]
    public void CreateWabaRequest_Throws_WhenNeitherOverrideNorOptionsWabaIdIsSet()
    {
        var options = CreateOptions();
        options.WabaId = null;
        var (client, _) = CreateClient(options);

        Assert.Throws<InvalidOperationException>(
            () => client.CreateWabaRequest(HttpMethod.Get, "{waba_id}/message_templates"));
    }

    [Fact]
    public void CreateAbsoluteRequest_AttachesBearerAuthorizationHeader()
    {
        var options = CreateOptions();
        var (client, _) = CreateClient(options);

        var request = client.CreateAbsoluteRequest(HttpMethod.Get, new Uri("https://cdn.example.com/media/abc123"));

        Assert.NotNull(request.Headers.Authorization);
        Assert.Equal("Bearer", request.Headers.Authorization!.Scheme);
        Assert.Equal(options.AccessToken, request.Headers.Authorization.Parameter);
    }

    [Fact]
    public void CreateAbsoluteRequest_UsesTheGivenUriVerbatim_WithNoTokenSubstitution()
    {
        var options = CreateOptions();
        var (client, _) = CreateClient(options);
        var absoluteUri = new Uri("https://cdn.example.com/media/abc123?token=xyz");

        var request = client.CreateAbsoluteRequest(HttpMethod.Get, absoluteUri);

        Assert.Equal(absoluteUri, request.RequestUri);
    }

    [Fact]
    public async Task CreateAbsoluteRequest_SendsToItsHost_NotTheClientBaseAddressHost()
    {
        var options = CreateOptions();
        var (client, handler) = CreateClient(options);
        var request = client.CreateAbsoluteRequest(HttpMethod.Get, new Uri("https://cdn.example.com/media/abc123"));

        await client.SendAsync(request);

        Assert.Equal("cdn.example.com", handler.LastRequest!.RequestUri!.Host);
        Assert.NotEqual("graph.facebook.com", handler.LastRequest.RequestUri.Host);
    }

    [Fact]
    public async Task SendAsync_LogsInformation_WithMethodUrlStatusAndDuration_OnSuccess()
    {
        var options = CreateOptions();
        var (client, _, logger) = CreateClientWithLogger(
            options,
            _ => new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        var request = client.CreateRequest(HttpMethod.Get, "{phone_number_id}/messages");

        await client.SendAsync(request);

        var infoRecord = Assert.Single(logger.Records, r => r.Level == LogLevel.Information);
        Assert.Contains("GET", infoRecord.Message, StringComparison.Ordinal);
        Assert.Contains("default-phone-id/messages", infoRecord.Message, StringComparison.Ordinal);
        Assert.Contains("OK", infoRecord.Message, StringComparison.Ordinal);
        Assert.Matches(@"\d+ms", infoRecord.Message);
    }

    [Fact]
    public async Task SendAsync_LogsInformation_WithMethodUrlStatusAndDuration_OnFailure()
    {
        var options = CreateOptions();
        var (client, _, logger) = CreateClientWithLogger(
            options,
            _ => new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError));
        var request = client.CreateRequest(HttpMethod.Get, "{phone_number_id}/messages");

        await client.SendAsync(request);

        var infoRecord = Assert.Single(logger.Records, r => r.Level == LogLevel.Information);
        Assert.Contains("InternalServerError", infoRecord.Message, StringComparison.Ordinal);
        Assert.Matches(@"\d+ms", infoRecord.Message);
    }

    [Fact]
    public async Task SendAsync_InformationLog_RedactsSensitiveQueryParams_InUrl()
    {
        var options = CreateOptions();
        var (client, _, logger) = CreateClientWithLogger(
            options,
            _ => new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        var request = client.CreateAbsoluteRequest(HttpMethod.Get, new Uri("https://cdn.example.com/media/abc123?access_token=super-secret&foo=bar"));

        await client.SendAsync(request);

        var infoRecord = Assert.Single(logger.Records, r => r.Level == LogLevel.Information);
        Assert.DoesNotContain("super-secret", infoRecord.Message, StringComparison.Ordinal);
        Assert.Contains(SensitiveDataRedactor.Mask, infoRecord.Message, StringComparison.Ordinal);
        Assert.Contains("foo=bar", infoRecord.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendAsync_DebugLog_RedactsSensitiveFields_InRequestBody()
    {
        var options = CreateOptions();
        var (client, _, logger) = CreateClientWithLogger(
            options,
            _ => new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        var request = client.CreateRequest(HttpMethod.Post, "{phone_number_id}/messages");
        request.Content = new StringContent(
            "{\"messaging_product\":\"whatsapp\",\"access_token\":\"super-secret-value\"}",
            System.Text.Encoding.UTF8,
            "application/json");

        await client.SendAsync(request);

        var requestBodyRecord = Assert.Single(
            logger.Records,
            r => r.Level == LogLevel.Debug && r.Message.StartsWith("Request body:", StringComparison.Ordinal));
        Assert.DoesNotContain("super-secret-value", requestBodyRecord.Message, StringComparison.Ordinal);
        Assert.Contains(SensitiveDataRedactor.Mask, requestBodyRecord.Message, StringComparison.Ordinal);
        Assert.Contains("whatsapp", requestBodyRecord.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendAsync_DebugLog_RedactsSensitiveFields_InResponseBody()
    {
        var options = CreateOptions();
        var (client, _, logger) = CreateClientWithLogger(
            options,
            _ => new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(
                    "{\"messaging_product\":\"whatsapp\",\"token\":\"super-secret-value\"}",
                    System.Text.Encoding.UTF8,
                    "application/json"),
            });
        var request = client.CreateRequest(HttpMethod.Get, "{phone_number_id}/messages");

        await client.SendAsync(request);

        var responseBodyRecord = Assert.Single(
            logger.Records,
            r => r.Level == LogLevel.Debug && r.Message.StartsWith("Response body:", StringComparison.Ordinal));
        Assert.DoesNotContain("super-secret-value", responseBodyRecord.Message, StringComparison.Ordinal);
        Assert.Contains(SensitiveDataRedactor.Mask, responseBodyRecord.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendAsync_DoesNotLogBodies_WhenDebugDisabled()
    {
        var options = CreateOptions();
        var (client, _, logger) = CreateClientWithLogger(
            options,
            _ => new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("{\"messaging_product\":\"whatsapp\"}", System.Text.Encoding.UTF8, "application/json"),
            },
            minLevel: LogLevel.Information);
        var request = client.CreateRequest(HttpMethod.Get, "{phone_number_id}/messages");

        await client.SendAsync(request);

        Assert.DoesNotContain(logger.Records, r => r.Level == LogLevel.Debug);
        Assert.Contains(logger.Records, r => r.Level == LogLevel.Information);
    }

    [Fact]
    public async Task SendAsync_ResponseBodyStaysReadable_AfterDebugLogging_RegressionCheck()
    {
        const string bodyJson = "{\"messaging_product\":\"whatsapp\",\"to\":\"15551234567\"}";
        var options = CreateOptions();
        var (client, _, _) = CreateClientWithLogger(
            options,
            _ => new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(bodyJson, System.Text.Encoding.UTF8, "application/json"),
            });
        var request = client.CreateRequest(HttpMethod.Get, "{phone_number_id}/messages");

        var response = await client.SendAsync(request);

        // Debug-level body logging buffers-and-replaces response.Content (same primitive as
        // GraphErrorPeek); downstream consumers (resource clients' ReadResultAsync/ReadErrorAsync)
        // must still be able to read the exact same body after SendAsync returns.
        var bodyAfterLogging = await response.Content.ReadAsStringAsync();
        Assert.Equal(bodyJson, bodyAfterLogging);
    }

    [Fact]
    public async Task SendAsync_ResponseBodyStaysReadable_ByGraphErrorPeek_AfterDebugLogging_RegressionCheck()
    {
        const string errorJson = "{\"error\":{\"message\":\"Rate limited\",\"type\":\"OAuthException\",\"code\":80007,\"fbtrace_id\":\"trace-1\"}}";
        var options = CreateOptions();
        var (client, _, _) = CreateClientWithLogger(
            options,
            _ => new HttpResponseMessage(System.Net.HttpStatusCode.TooManyRequests)
            {
                Content = new StringContent(errorJson, System.Text.Encoding.UTF8, "application/json"),
            });
        var request = client.CreateRequest(HttpMethod.Get, "{phone_number_id}/messages");

        var response = await client.SendAsync(request);

        var error = await GraphErrorPeek.TryReadAsync(response);
        Assert.NotNull(error);
        Assert.Equal(80007, error!.Code);
        Assert.Equal("trace-1", error.FbtraceId);
    }
}
