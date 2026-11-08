using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Carandev.WhatsApp.Sdk.Clients;
using Carandev.WhatsApp.Sdk.Tests.Fakes;

namespace Carandev.WhatsApp.Sdk.Tests.Webhooks;

/// <summary>
/// #65: subscribing an app to a WABA through <c>/{waba-id}/subscribed_apps</c>.
/// </summary>
public class WhatsAppWebhooksClientTests
{
    private const string SuccessBody = """{"success":true}""";
    private const string ErrorBody = """{"error":{"message":"Unsupported post request","type":"GraphMethodException","code":100}}""";

    private static WhatsAppOptions CreateOptions(string? wabaId = "default-waba-id") => new()
    {
        AccessToken = "test-access-token",
        PhoneNumberId = "default-phone-id",
        WabaId = wabaId,
        ApiVersion = "v21.0",
        BaseUrl = "https://graph.facebook.com/{0}/",
    };

    private static (WhatsAppWebhooksClient Client, RecordingHandler Handler) CreateClient(
        WhatsAppOptions? options = null,
        HttpStatusCode status = HttpStatusCode.OK,
        string body = SuccessBody)
    {
        options ??= CreateOptions();
        var handler = new RecordingHandler(_ => new HttpResponseMessage(status)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        });
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(string.Format(options.BaseUrl, options.ApiVersion)),
        };
        var whatsAppClient = new WhatsAppClient(httpClient, Options.Create(options), NullLogger<WhatsAppClient>.Instance);
        return (new WhatsAppWebhooksClient(whatsAppClient), handler);
    }

    private static string Uri(string relativePath) => "https://graph.facebook.com/v21.0/" + relativePath;

    // ---------- SubscribeAppAsync ----------

    [Fact]
    public async Task SubscribeAppAsync_WithoutOverride_PostsWithoutBody_ToConfiguredWaba()
    {
        var (client, handler) = CreateClient();

        var result = await client.SubscribeAppAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal(Uri("default-waba-id/subscribed_apps"), handler.LastRequest.RequestUri!.ToString());
        Assert.Null(handler.LastRequest.Content);
    }

    [Fact]
    public async Task SubscribeAppAsync_WithOverride_PostsCallbackAndToken_ToOverriddenWaba()
    {
        var (client, handler) = CreateClient();

        var result = await client.SubscribeAppAsync(new Uri("https://staging.example.com/webhook"), "my-token", "other-waba");

        Assert.True(result.IsSuccess);
        Assert.Equal(Uri("other-waba/subscribed_apps"), handler.LastRequest!.RequestUri!.ToString());
        Assert.Equal(
            """{"override_callback_uri":"https://staging.example.com/webhook","verify_token":"my-token"}""",
            await handler.LastRequest.Content!.ReadAsStringAsync());
    }

    [Fact]
    public async Task SubscribeAppAsync_RejectsCallbackWithoutToken_WithoutSending()
    {
        var (client, handler) = CreateClient();

        await Assert.ThrowsAsync<ArgumentException>(
            () => client.SubscribeAppAsync(new Uri("https://staging.example.com/webhook")));

        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public async Task SubscribeAppAsync_RejectsTokenWithoutCallback_WithoutSending()
    {
        var (client, handler) = CreateClient();

        await Assert.ThrowsAsync<ArgumentException>(() => client.SubscribeAppAsync(verifyToken: "my-token"));

        Assert.Equal(0, handler.RequestCount);
    }

    [Theory]
    [InlineData("http://staging.example.com/webhook")]
    [InlineData("/webhook")]
    public async Task SubscribeAppAsync_RejectsCallbackThatIsNotAbsoluteHttps_WithoutSending(string callback)
    {
        var (client, handler) = CreateClient();

        await Assert.ThrowsAsync<ArgumentException>(
            () => client.SubscribeAppAsync(new Uri(callback, UriKind.RelativeOrAbsolute), "my-token"));

        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public async Task SubscribeAppAsync_ReturnsFailure_WithMetaError()
    {
        var (client, _) = CreateClient(status: HttpStatusCode.BadRequest, body: ErrorBody);

        var result = await client.SubscribeAppAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(100, result.Error!.Code);
        Assert.Equal("Unsupported post request", result.Error.Message);
    }

    [Fact]
    public async Task SubscribeAppAsync_ReturnsFailure_WithoutSending_WhenWabaIdMissing()
    {
        var (client, handler) = CreateClient(CreateOptions(wabaId: null));

        var result = await client.SubscribeAppAsync();

        Assert.False(result.IsSuccess);
        Assert.Contains("WabaId", result.Error!.Message);
        Assert.Equal(0, handler.RequestCount);
    }

    // ---------- ListSubscribedAppsAsync ----------

    [Fact]
    public async Task ListSubscribedAppsAsync_GetsSubscribedApps_AndParsesThem()
    {
        var (client, handler) = CreateClient(body: """
            {"data":[
              {"whatsapp_business_api_data":{"id":"670843887433847","link":"https://www.facebook.com/games/?app_id=670843887433847","name":"Lucky Shrub"},"override_callback_uri":"https://staging.example.com/webhook"},
              {"whatsapp_business_api_data":{"id":"2","name":"Other"}}
            ],"paging":{"cursors":{"before":"b","after":"a"}}}
            """);

        var result = await client.ListSubscribedAppsAsync("other-waba");

        Assert.True(result.IsSuccess);
        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Equal(Uri("other-waba/subscribed_apps"), handler.LastRequest.RequestUri!.ToString());
        var apps = result.Value!.Data!;
        Assert.Equal(2, apps.Count);
        Assert.Equal("670843887433847", apps[0].App!.Id);
        Assert.Equal("Lucky Shrub", apps[0].App!.Name);
        Assert.Equal("https://www.facebook.com/games/?app_id=670843887433847", apps[0].App!.Link);
        Assert.Equal("https://staging.example.com/webhook", apps[0].OverrideCallbackUri);
        Assert.Equal("Other", apps[1].App!.Name);
        Assert.Null(apps[1].OverrideCallbackUri);
        Assert.Equal("a", result.Value.Paging!.Cursors!.After);
    }

    [Fact]
    public async Task ListSubscribedAppsAsync_ReturnsFailure_WithMetaError()
    {
        var (client, _) = CreateClient(status: HttpStatusCode.BadRequest, body: ErrorBody);

        var result = await client.ListSubscribedAppsAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(100, result.Error!.Code);
    }

    [Fact]
    public async Task ListSubscribedAppsAsync_ReturnsFailure_WithoutSending_WhenWabaIdMissing()
    {
        var (client, handler) = CreateClient(CreateOptions(wabaId: null));

        var result = await client.ListSubscribedAppsAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(0, handler.RequestCount);
    }

    // ---------- UnsubscribeAppAsync ----------

    [Fact]
    public async Task UnsubscribeAppAsync_DeletesSubscribedApps()
    {
        var (client, handler) = CreateClient();

        var result = await client.UnsubscribeAppAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(HttpMethod.Delete, handler.LastRequest!.Method);
        Assert.Equal(Uri("default-waba-id/subscribed_apps"), handler.LastRequest.RequestUri!.ToString());
    }

    [Fact]
    public async Task UnsubscribeAppAsync_ReturnsFailure_WithMetaError()
    {
        var (client, _) = CreateClient(status: HttpStatusCode.BadRequest, body: ErrorBody);

        var result = await client.UnsubscribeAppAsync("other-waba");

        Assert.False(result.IsSuccess);
        Assert.Equal(100, result.Error!.Code);
    }

    [Fact]
    public async Task UnsubscribeAppAsync_ReturnsFailure_WithoutSending_WhenWabaIdMissing()
    {
        var (client, handler) = CreateClient(CreateOptions(wabaId: null));

        var result = await client.UnsubscribeAppAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(0, handler.RequestCount);
    }
}
