using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WhatsAppCloudApi.Tests.Fakes;

namespace WhatsAppCloudApi.Tests;

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
}
