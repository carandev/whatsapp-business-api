using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Carandev.WhatsApp.Sdk.Clients;
using Carandev.WhatsApp.Sdk.Tests.Fakes;

namespace Carandev.WhatsApp.Sdk.Tests.PhoneNumbers;

/// <summary>
/// #64 (register, deregister, two-step verification PIN) and #66 (per phone number webhook
/// override) on <see cref="WhatsAppPhoneNumbersClient"/>.
/// </summary>
public class PhoneNumberRegistrationTests
{
    private const string SuccessBody = """{"success":true}""";
    private const string ErrorBody = """{"error":{"message":"Rate limit hit","type":"OAuthException","code":133016}}""";

    private static readonly WhatsAppOptions Options = new()
    {
        AccessToken = "test-access-token",
        PhoneNumberId = "default-phone-id",
        WabaId = "default-waba-id",
        ApiVersion = "v21.0",
        BaseUrl = "https://graph.facebook.com/{0}/",
    };

    private static (WhatsAppPhoneNumbersClient Client, RecordingHandler Handler) CreateClient(
        HttpStatusCode status = HttpStatusCode.OK,
        string body = SuccessBody)
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(status)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        });
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(string.Format(Options.BaseUrl, Options.ApiVersion)),
        };
        var whatsAppClient = new WhatsAppClient(httpClient, Microsoft.Extensions.Options.Options.Create(Options), NullLogger<WhatsAppClient>.Instance);
        return (new WhatsAppPhoneNumbersClient(whatsAppClient, Microsoft.Extensions.Options.Options.Create(Options)), handler);
    }

    private static string Uri(string relativePath) =>
        string.Format(Options.BaseUrl, Options.ApiVersion) + relativePath;

    private static Task<string> BodyAsync(RecordingHandler handler) =>
        handler.LastRequest!.Content!.ReadAsStringAsync();

    // ---------- RegisterAsync ----------

    [Fact]
    public async Task RegisterAsync_PostsPinAndMessagingProduct_ToRegisterEndpoint()
    {
        var (client, handler) = CreateClient();

        var result = await client.RegisterAsync("123456");

        Assert.True(result.IsSuccess);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal(Uri("default-phone-id/register"), handler.LastRequest.RequestUri!.ToString());
        Assert.Equal("""{"messaging_product":"whatsapp","pin":"123456"}""", await BodyAsync(handler));
    }

    [Fact]
    public async Task RegisterAsync_WithRegionAndOverride_SendsRegion_ToOverriddenNumber()
    {
        var (client, handler) = CreateClient();

        await client.RegisterAsync("123456", "CH", "other-phone-id");

        Assert.Equal(Uri("other-phone-id/register"), handler.LastRequest!.RequestUri!.ToString());
        Assert.Equal(
            """{"messaging_product":"whatsapp","pin":"123456","data_localization_region":"CH"}""",
            await BodyAsync(handler));
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("1234567")]
    [InlineData("12a456")]
    [InlineData("１２３４５６")]
    public async Task RegisterAsync_RejectsPinThatIsNotSixDigits_WithoutSending(string pin)
    {
        var (client, handler) = CreateClient();

        await Assert.ThrowsAsync<ArgumentException>(() => client.RegisterAsync(pin));

        Assert.Equal(0, handler.RequestCount);
    }

    [Theory]
    [InlineData("C")]
    [InlineData("CHE")]
    [InlineData("C1")]
    public async Task RegisterAsync_RejectsRegionThatIsNotTwoLetters_WithoutSending(string region)
    {
        var (client, handler) = CreateClient();

        await Assert.ThrowsAsync<ArgumentException>(() => client.RegisterAsync("123456", region));

        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public async Task RegisterAsync_ReturnsFailure_WithMetaError()
    {
        var (client, _) = CreateClient(HttpStatusCode.BadRequest, ErrorBody);

        var result = await client.RegisterAsync("123456");

        Assert.False(result.IsSuccess);
        Assert.Equal(133016, result.Error!.Code);
        Assert.Equal("Rate limit hit", result.Error.Message);
    }

    // ---------- DeregisterAsync ----------

    [Fact]
    public async Task DeregisterAsync_PostsToDeregisterEndpoint_WithoutBody()
    {
        var (client, handler) = CreateClient();

        var result = await client.DeregisterAsync("other-phone-id");

        Assert.True(result.IsSuccess);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal(Uri("other-phone-id/deregister"), handler.LastRequest.RequestUri!.ToString());
        Assert.Null(handler.LastRequest.Content);
    }

    [Fact]
    public async Task DeregisterAsync_ReturnsFailure_WithMetaError()
    {
        var (client, _) = CreateClient(HttpStatusCode.BadRequest, ErrorBody);

        var result = await client.DeregisterAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(133016, result.Error!.Code);
    }

    // ---------- SetTwoStepVerificationPinAsync ----------

    [Fact]
    public async Task SetTwoStepVerificationPinAsync_PostsPin_ToPhoneNumberNode()
    {
        var (client, handler) = CreateClient();

        var result = await client.SetTwoStepVerificationPinAsync("654321");

        Assert.True(result.IsSuccess);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal(Uri("default-phone-id"), handler.LastRequest.RequestUri!.ToString());
        Assert.Equal("""{"pin":"654321"}""", await BodyAsync(handler));
    }

    [Fact]
    public async Task SetTwoStepVerificationPinAsync_RejectsInvalidPin_WithoutSending()
    {
        var (client, handler) = CreateClient();

        await Assert.ThrowsAsync<ArgumentException>(() => client.SetTwoStepVerificationPinAsync("12 456"));

        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public async Task SetTwoStepVerificationPinAsync_ReturnsFailure_WithMetaError()
    {
        var (client, _) = CreateClient(HttpStatusCode.BadRequest, ErrorBody);

        var result = await client.SetTwoStepVerificationPinAsync("654321");

        Assert.False(result.IsSuccess);
        Assert.Equal(133016, result.Error!.Code);
    }

    // ---------- SetWebhookOverrideAsync / ClearWebhookOverrideAsync ----------

    [Fact]
    public async Task SetWebhookOverrideAsync_PostsWebhookConfiguration_ToPhoneNumberNode()
    {
        var (client, handler) = CreateClient();

        var result = await client.SetWebhookOverrideAsync(new Uri("https://tunnel.example.com/webhook"), "my-token", "other-phone-id");

        Assert.True(result.IsSuccess);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal(Uri("other-phone-id"), handler.LastRequest.RequestUri!.ToString());
        Assert.Equal(
            """{"webhook_configuration":{"override_callback_uri":"https://tunnel.example.com/webhook","verify_token":"my-token"}}""",
            await BodyAsync(handler));
    }

    [Theory]
    [InlineData("http://tunnel.example.com/webhook")]
    [InlineData("ftp://tunnel.example.com/webhook")]
    [InlineData("/webhook")]
    public async Task SetWebhookOverrideAsync_RejectsCallbackThatIsNotAbsoluteHttps_WithoutSending(string callback)
    {
        var (client, handler) = CreateClient();

        await Assert.ThrowsAsync<ArgumentException>(
            () => client.SetWebhookOverrideAsync(new Uri(callback, UriKind.RelativeOrAbsolute), "my-token"));

        Assert.Equal(0, handler.RequestCount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public async Task SetWebhookOverrideAsync_RejectsEmptyVerifyToken_WithoutSending(string token)
    {
        var (client, handler) = CreateClient();

        await Assert.ThrowsAsync<ArgumentException>(
            () => client.SetWebhookOverrideAsync(new Uri("https://tunnel.example.com/webhook"), token));

        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public async Task SetWebhookOverrideAsync_ReturnsFailure_WithMetaError()
    {
        var (client, _) = CreateClient(HttpStatusCode.BadRequest, ErrorBody);

        var result = await client.SetWebhookOverrideAsync(new Uri("https://tunnel.example.com/webhook"), "my-token");

        Assert.False(result.IsSuccess);
        Assert.Equal(133016, result.Error!.Code);
    }

    [Fact]
    public async Task ClearWebhookOverrideAsync_PostsEmptyOverrideCallbackUri()
    {
        var (client, handler) = CreateClient();

        var result = await client.ClearWebhookOverrideAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal(Uri("default-phone-id"), handler.LastRequest.RequestUri!.ToString());
        Assert.Equal("""{"webhook_configuration":{"override_callback_uri":""}}""", await BodyAsync(handler));
    }

    [Fact]
    public async Task ClearWebhookOverrideAsync_ReturnsFailure_WithMetaError()
    {
        var (client, _) = CreateClient(HttpStatusCode.BadRequest, ErrorBody);

        var result = await client.ClearWebhookOverrideAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(133016, result.Error!.Code);
    }

    [Fact]
    public async Task GetPhoneNumberAsync_ReadsEveryWebhookConfigurationCallback()
    {
        var (client, _) = CreateClient(
            body: """{"id":"1","webhook_configuration":{"phone_number":"https://phone.example.com","whatsapp_business_account":"https://waba.example.com","application":"https://app.example.com"}}""");

        var result = await client.GetPhoneNumberAsync();

        Assert.True(result.IsSuccess);
        var configuration = result.Value!.WebhookConfiguration!;
        Assert.Equal("https://phone.example.com", configuration.PhoneNumber);
        Assert.Equal("https://waba.example.com", configuration.WhatsAppBusinessAccount);
        Assert.Equal("https://app.example.com", configuration.Application);
    }

    [Fact]
    public async Task GetWebhookConfigurationAsync_RequestsWebhookConfigurationField_AndReadsIt()
    {
        var (client, handler) = CreateClient(
            body: """{"id":"1","webhook_configuration":{"phone_number":"https://phone.example.com","application":"https://app.example.com"}}""");

        var result = await client.GetWebhookConfigurationAsync("other-phone-id");

        Assert.True(result.IsSuccess);
        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Equal(Uri("other-phone-id?fields=webhook_configuration"), handler.LastRequest.RequestUri!.ToString());
        Assert.Equal("https://phone.example.com", result.Value!.PhoneNumber);
        Assert.Null(result.Value.WhatsAppBusinessAccount);
        Assert.Equal("https://app.example.com", result.Value.Application);
    }

    [Fact]
    public async Task GetWebhookConfigurationAsync_WithoutConfigurationInResponse_ReturnsEmptyConfiguration()
    {
        var (client, _) = CreateClient(body: """{"id":"1"}""");

        var result = await client.GetWebhookConfigurationAsync();

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.PhoneNumber);
        Assert.Null(result.Value.Application);
    }

    [Fact]
    public async Task GetWebhookConfigurationAsync_ReturnsFailure_WithMetaError()
    {
        var (client, _) = CreateClient(HttpStatusCode.BadRequest, ErrorBody);

        var result = await client.GetWebhookConfigurationAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(133016, result.Error!.Code);
    }
}
