using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Carandev.WhatsApp.Sdk.Clients;
using Carandev.WhatsApp.Sdk.Models.BusinessProfile;
using Carandev.WhatsApp.Sdk.Tests.Fakes;

namespace Carandev.WhatsApp.Sdk.Tests.BusinessProfile;

public class WhatsAppBusinessProfileClientTests
{
    private const string BusinessProfileJson =
        """{"about":"We sell widgets","address":"123 Main St","description":"Widget makers since 1999","email":"support@acme.example","websites":["https://acme.example"],"vertical":"RETAIL","profile_picture_handle":"handle-123"}""";

    private const string ErrorBody = """{"error":{"message":"Invalid parameter","type":"OAuthException","code":100}}""";

    private const string ExpectedFieldsQuery = "fields=about,address,description,email,websites,vertical,profile_picture_handle";

    private static WhatsAppOptions CreateOptions() => new()
    {
        AccessToken = "test-access-token",
        PhoneNumberId = "default-phone-id",
        WabaId = "default-waba-id",
        ApiVersion = "v21.0",
        BaseUrl = "https://graph.facebook.com/{0}/",
    };

    private static (WhatsAppBusinessProfileClient Client, RecordingHandler Handler) CreateClient(
        WhatsAppOptions options,
        Func<HttpRequestMessage, HttpResponseMessage>? respond = null)
    {
        var handler = respond is null ? new RecordingHandler() : new RecordingHandler(respond);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(string.Format(options.BaseUrl, options.ApiVersion)),
        };
        var whatsAppClient = new WhatsAppClient(httpClient, Options.Create(options), NullLogger<WhatsAppClient>.Instance);
        var client = new WhatsAppBusinessProfileClient(whatsAppClient);
        return (client, handler);
    }

    private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string json) =>
        new(statusCode) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private static string ExpectedRequestUri(WhatsAppOptions options, string relativePath) =>
        string.Format(options.BaseUrl, options.ApiVersion) + relativePath;

    private static async Task<string> ReadRequestBodyAsync(HttpRequestMessage request) =>
        await request.Content!.ReadAsStringAsync();

    // ---------- GetBusinessProfileAsync ----------

    [Fact]
    public async Task GetBusinessProfileAsync_SendsGetToBusinessProfileEndpoint_WithFieldsQueryParam()
    {
        var options = CreateOptions();
        var (client, handler) = CreateClient(
            options,
            _ => JsonResponse(HttpStatusCode.OK, "{\"data\":[" + BusinessProfileJson + "]}"));

        await client.GetBusinessProfileAsync();

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Equal(
            ExpectedRequestUri(options, $"{options.PhoneNumberId}/whatsapp_business_profile?{ExpectedFieldsQuery}"),
            handler.LastRequest.RequestUri!.ToString());
    }

    [Fact]
    public async Task GetBusinessProfileAsync_UsesOverride_WhenProvided()
    {
        var options = CreateOptions();
        var (client, handler) = CreateClient(
            options,
            _ => JsonResponse(HttpStatusCode.OK, "{\"data\":[" + BusinessProfileJson + "]}"));

        await client.GetBusinessProfileAsync("override-phone-id");

        Assert.Equal(
            ExpectedRequestUri(options, $"override-phone-id/whatsapp_business_profile?{ExpectedFieldsQuery}"),
            handler.LastRequest!.RequestUri!.ToString());
    }

    [Fact]
    public async Task GetBusinessProfileAsync_ReturnsSuccessResult_UnwrappingDataEnvelope_On2xxResponse()
    {
        var options = CreateOptions();
        var (client, _) = CreateClient(
            options,
            _ => JsonResponse(HttpStatusCode.OK, "{\"data\":[" + BusinessProfileJson + "]}"));

        var result = await client.GetBusinessProfileAsync();

        Assert.True(result.IsSuccess);
        var profile = result.Value!;
        Assert.Equal("We sell widgets", profile.About);
        Assert.Equal("123 Main St", profile.Address);
        Assert.Equal("Widget makers since 1999", profile.Description);
        Assert.Equal("support@acme.example", profile.Email);
        Assert.Equal(["https://acme.example"], profile.Websites);
        Assert.Equal("RETAIL", profile.Vertical);
        Assert.Equal("handle-123", profile.ProfilePictureHandle);
    }

    [Fact]
    public async Task GetBusinessProfileAsync_ReturnsFailureResult_WhenDataEnvelopeIsEmpty()
    {
        var options = CreateOptions();
        var (client, _) = CreateClient(options, _ => JsonResponse(HttpStatusCode.OK, """{"data":[]}"""));

        var result = await client.GetBusinessProfileAsync();

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task GetBusinessProfileAsync_ReturnsFailureResult_OnApiFailure_WithoutThrowing()
    {
        var options = CreateOptions();
        var (client, _) = CreateClient(options, _ => JsonResponse(HttpStatusCode.InternalServerError, ErrorBody));

        var result = await client.GetBusinessProfileAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid parameter", result.Error!.Message);
        Assert.Equal("OAuthException", result.Error.Type);
        Assert.Equal(100, result.Error.Code);
    }

    // ---------- UpdateBusinessProfileAsync ----------

    [Fact]
    public async Task UpdateBusinessProfileAsync_SendsPostToBusinessProfileEndpoint_WithAllFieldsSet()
    {
        var options = CreateOptions();
        var (client, handler) = CreateClient(options, _ => JsonResponse(HttpStatusCode.OK, """{"success":true}"""));
        var request = new UpdateBusinessProfileRequest
        {
            About = "We sell widgets",
            Address = "123 Main St",
            Description = "Widget makers since 1999",
            Email = "support@acme.example",
            Websites = ["https://acme.example"],
            Vertical = "RETAIL",
            ProfilePictureHandle = "handle-123",
        };

        await client.UpdateBusinessProfileAsync(request);

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal(
            ExpectedRequestUri(options, $"{options.PhoneNumberId}/whatsapp_business_profile"),
            handler.LastRequest.RequestUri!.ToString());
        var body = await ReadRequestBodyAsync(handler.LastRequest);
        Assert.Equal(
            """{"about":"We sell widgets","address":"123 Main St","description":"Widget makers since 1999","email":"support@acme.example","websites":["https://acme.example"],"vertical":"RETAIL","profile_picture_handle":"handle-123"}""",
            body);
    }

    [Fact]
    public async Task UpdateBusinessProfileAsync_OmitsUnsetFields_OnPartialUpdate()
    {
        var options = CreateOptions();
        var (client, handler) = CreateClient(options, _ => JsonResponse(HttpStatusCode.OK, """{"success":true}"""));
        var request = new UpdateBusinessProfileRequest { About = "Only about is set" };

        await client.UpdateBusinessProfileAsync(request);

        var body = await ReadRequestBodyAsync(handler.LastRequest!);
        Assert.Equal("""{"about":"Only about is set"}""", body);
    }

    [Fact]
    public async Task UpdateBusinessProfileAsync_UsesOverride_WhenProvided()
    {
        var options = CreateOptions();
        var (client, handler) = CreateClient(options, _ => JsonResponse(HttpStatusCode.OK, """{"success":true}"""));
        var request = new UpdateBusinessProfileRequest { About = "Updated" };

        await client.UpdateBusinessProfileAsync(request, "override-phone-id");

        Assert.Equal(
            ExpectedRequestUri(options, "override-phone-id/whatsapp_business_profile"),
            handler.LastRequest!.RequestUri!.ToString());
    }

    [Fact]
    public async Task UpdateBusinessProfileAsync_ReturnsSuccess_On2xxResponse()
    {
        var options = CreateOptions();
        var (client, _) = CreateClient(options, _ => JsonResponse(HttpStatusCode.OK, """{"success":true}"""));
        var request = new UpdateBusinessProfileRequest { About = "Updated" };

        var result = await client.UpdateBusinessProfileAsync(request);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task UpdateBusinessProfileAsync_ReturnsFailureResult_OnApiFailure_WithoutThrowing()
    {
        var options = CreateOptions();
        var (client, _) = CreateClient(options, _ => JsonResponse(HttpStatusCode.BadRequest, ErrorBody));
        var request = new UpdateBusinessProfileRequest { About = "Updated" };

        var result = await client.UpdateBusinessProfileAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid parameter", result.Error!.Message);
    }
}
