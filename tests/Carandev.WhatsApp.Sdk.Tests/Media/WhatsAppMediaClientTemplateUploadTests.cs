using System.Net;
using System.Text;
using System.Web;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Carandev.WhatsApp.Sdk.Clients;
using Carandev.WhatsApp.Sdk.DependencyInjection;
using Carandev.WhatsApp.Sdk.Tests.Fakes;

namespace Carandev.WhatsApp.Sdk.Tests.Media;

public class WhatsAppMediaClientTemplateUploadTests
{
    private const string SessionId = "upload:MTphdHRhY2htZW50OjEyMw==?sig=ARZabc";
    private const string Handle = "4::aW1hZ2UvanBlZw==:ARYhandle";

    private static WhatsAppOptions CreateOptions(string? appId = "app-123") => new()
    {
        AccessToken = "test-access-token",
        PhoneNumberId = "default-phone-id",
        AppId = appId,
        ApiVersion = "v21.0",
        BaseUrl = "https://graph.facebook.com/{0}/",
    };

    private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string json) =>
        new(statusCode) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private static string BaseAddress(WhatsAppOptions options) => string.Format(options.BaseUrl, options.ApiVersion);

    /// <summary>
    /// A snapshot of one request, captured synchronously inside the handler callback, because the
    /// client disposes request content once the call returns.
    /// </summary>
    private sealed record CapturedRequest(
        HttpMethod Method,
        Uri Uri,
        string? AuthScheme,
        string? AuthParameter,
        string? FileOffset,
        string? ContentType,
        byte[]? Body);

    private static (WhatsAppMediaClient Client, List<CapturedRequest> Requests) CreateClient(
        WhatsAppOptions options,
        Func<int, HttpResponseMessage> respond)
    {
        var requests = new List<CapturedRequest>();
        var handler = new RecordingHandler(request =>
        {
            var fileOffset = request.Headers.TryGetValues("file_offset", out var values) ? values.Single() : null;
            var body = request.Content?.ReadAsByteArrayAsync().GetAwaiter().GetResult();
            requests.Add(new CapturedRequest(
                request.Method,
                request.RequestUri!,
                request.Headers.Authorization?.Scheme,
                request.Headers.Authorization?.Parameter,
                fileOffset,
                request.Content?.Headers.ContentType?.MediaType,
                body));
            return respond(requests.Count);
        });
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri(BaseAddress(options)) };
        var whatsAppClient = new WhatsAppClient(httpClient, Options.Create(options), NullLogger<WhatsAppClient>.Instance);
        return (new WhatsAppMediaClient(whatsAppClient, Options.Create(options)), requests);
    }

    private static HttpResponseMessage HappyPath(int requestNumber) => requestNumber switch
    {
        1 => JsonResponse(HttpStatusCode.OK, $$"""{"id":"{{SessionId}}"}"""),
        _ => JsonResponse(HttpStatusCode.OK, $$"""{"h":"{{Handle}}"}"""),
    };

    [Fact]
    public async Task UploadTemplateMediaAsync_StartsSession_WithAppIdPathAndFileQuery()
    {
        var options = CreateOptions();
        var (client, requests) = CreateClient(options, HappyPath);

        await client.UploadTemplateMediaAsync(new MemoryStream([1, 2, 3, 4, 5]), "image/jpeg", "header photo.jpg");

        var start = requests[0];
        Assert.Equal(HttpMethod.Post, start.Method);
        Assert.Equal(BaseAddress(options) + "app-123/uploads", start.Uri.GetLeftPart(UriPartial.Path));
        var query = HttpUtility.ParseQueryString(start.Uri.Query);
        Assert.Equal("header photo.jpg", query["file_name"]);
        Assert.Equal("5", query["file_length"]);
        Assert.Equal("image/jpeg", query["file_type"]);
        Assert.Null(start.Body);
    }

    [Fact]
    public async Task UploadTemplateMediaAsync_UploadsBytes_ToSessionWithOAuthAndZeroOffset()
    {
        var options = CreateOptions();
        var (client, requests) = CreateClient(options, HappyPath);

        await client.UploadTemplateMediaAsync(new MemoryStream([1, 2, 3, 4, 5]), "image/jpeg", "photo.jpg");

        Assert.Equal(2, requests.Count);
        var upload = requests[1];
        Assert.Equal(HttpMethod.Post, upload.Method);
        Assert.Equal(BaseAddress(options) + SessionId, upload.Uri.ToString());
        Assert.Equal("OAuth", upload.AuthScheme);
        Assert.Equal("test-access-token", upload.AuthParameter);
        Assert.Equal("0", upload.FileOffset);
        Assert.Equal("image/jpeg", upload.ContentType);
        Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, upload.Body);
    }

    [Fact]
    public async Task UploadTemplateMediaAsync_ReturnsHandle()
    {
        var (client, _) = CreateClient(CreateOptions(), HappyPath);

        var result = await client.UploadTemplateMediaAsync(new MemoryStream([1, 2, 3]), "image/png", "logo.png");

        Assert.True(result.IsSuccess);
        Assert.Equal(Handle, result.Value);
    }

    [Fact]
    public async Task UploadTemplateMediaAsync_NonSeekableStream_ReportsBufferedLengthAndBody()
    {
        var (client, requests) = CreateClient(CreateOptions(), HappyPath);

        var result = await client.UploadTemplateMediaAsync(new NonSeekableStream(new byte[] { 9, 8, 7 }), "video/mp4", "clip.mp4");

        Assert.True(result.IsSuccess);
        Assert.Equal("3", HttpUtility.ParseQueryString(requests[0].Uri.Query)["file_length"]);
        Assert.Equal(new byte[] { 9, 8, 7 }, requests[1].Body);
    }

    [Fact]
    public async Task UploadTemplateMediaAsync_AppIdOverride_TakesPrecedenceOverOptions()
    {
        var options = CreateOptions();
        var (client, requests) = CreateClient(options, HappyPath);

        await client.UploadTemplateMediaAsync(new MemoryStream([1]), "image/jpeg", "a.jpg", appIdOverride: "other-app");

        Assert.Equal(BaseAddress(options) + "other-app/uploads", requests[0].Uri.GetLeftPart(UriPartial.Path));
    }

    [Fact]
    public async Task UploadTemplateMediaAsync_MissingAppId_ReturnsFailure_WithoutHttpCall()
    {
        var (client, requests) = CreateClient(CreateOptions(appId: null), HappyPath);

        var result = await client.UploadTemplateMediaAsync(new MemoryStream([1]), "image/jpeg", "a.jpg");

        Assert.False(result.IsSuccess);
        Assert.Equal("AppId is not configured; set WhatsAppOptions.AppId or pass appIdOverride.", result.Error!.Message);
        Assert.Empty(requests);
    }

    [Fact]
    public async Task UploadTemplateMediaAsync_StartSessionGraphError_ReturnsFailure_WithoutUpload()
    {
        var (client, requests) = CreateClient(
            CreateOptions(),
            _ => JsonResponse(HttpStatusCode.BadRequest, """{"error":{"message":"Invalid app id","type":"OAuthException","code":100}}"""));

        var result = await client.UploadTemplateMediaAsync(new MemoryStream([1]), "image/jpeg", "a.jpg");

        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid app id", result.Error!.Message);
        Assert.Equal(100, result.Error.Code);
        Assert.Single(requests);
    }

    [Fact]
    public async Task UploadTemplateMediaAsync_UploadGraphError_ReturnsFailure()
    {
        var (client, requests) = CreateClient(CreateOptions(), n => n switch
        {
            1 => JsonResponse(HttpStatusCode.OK, $$"""{"id":"{{SessionId}}"}"""),
            _ => JsonResponse(HttpStatusCode.BadRequest, """{"error":{"message":"Invalid file","type":"OAuthException","code":1}}"""),
        });

        var result = await client.UploadTemplateMediaAsync(new MemoryStream([1]), "image/jpeg", "a.jpg");

        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid file", result.Error!.Message);
        Assert.Equal(1, result.Error.Code);
        Assert.Equal(2, requests.Count);
    }

    [Fact]
    public async Task UploadTemplateMediaAsync_SessionResponseWithoutId_ReturnsFailure_WithoutUpload()
    {
        var (client, requests) = CreateClient(CreateOptions(), _ => JsonResponse(HttpStatusCode.OK, "{}"));

        var result = await client.UploadTemplateMediaAsync(new MemoryStream([1]), "image/jpeg", "a.jpg");

        Assert.False(result.IsSuccess);
        Assert.Equal("The upload session response did not contain an id.", result.Error!.Message);
        Assert.Single(requests);
    }

    [Fact]
    public async Task UploadTemplateMediaAsync_UploadResponseWithoutHandle_ReturnsFailure()
    {
        var (client, _) = CreateClient(CreateOptions(), n => n switch
        {
            1 => JsonResponse(HttpStatusCode.OK, $$"""{"id":"{{SessionId}}"}"""),
            _ => JsonResponse(HttpStatusCode.OK, "{}"),
        });

        var result = await client.UploadTemplateMediaAsync(new MemoryStream([1]), "image/jpeg", "a.jpg");

        Assert.False(result.IsSuccess);
        Assert.Equal("The upload response did not contain a file handle (h).", result.Error!.Message);
    }

    [Fact]
    public async Task UploadTemplateMediaAsync_OversizedContent_ThrowsBeforeAnyHttpCall()
    {
        var (client, requests) = CreateClient(CreateOptions(), HappyPath);

        await Assert.ThrowsAsync<MediaSizeExceededException>(() =>
            client.UploadTemplateMediaAsync(new SizedSeekableStream(5 * 1024 * 1024L + 1), "image/jpeg", "big.jpg"));

        Assert.Empty(requests);
    }

    [Fact]
    public async Task UploadTemplateMediaAsync_ResolvedFromContainer_UsesConfiguredAppId()
    {
        var requests = new List<(Uri Uri, string? AuthScheme)>();
        var handler = new RecordingHandler(request =>
        {
            requests.Add((request.RequestUri!, request.Headers.Authorization?.Scheme));
            return HappyPath(requests.Count);
        });
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["WhatsApp:AccessToken"] = "test-access-token",
                ["WhatsApp:PhoneNumberId"] = "default-phone-id",
                ["WhatsApp:AppId"] = "configured-app",
            })
            .Build();
        var services = new ServiceCollection();
        services.AddWhatsAppCloudApi(configuration);
        services.AddHttpClient(nameof(IWhatsAppClient)).ConfigurePrimaryHttpMessageHandler(() => handler);
        using var provider = services.BuildServiceProvider();

        var media = provider.GetRequiredService<IWhatsAppMediaClient>();
        var result = await media.UploadTemplateMediaAsync(new MemoryStream([1, 2]), "image/jpeg", "a.jpg");

        Assert.True(result.IsSuccess);
        Assert.Equal(Handle, result.Value);
        Assert.Equal("https://graph.facebook.com/v21.0/configured-app/uploads", requests[0].Uri.GetLeftPart(UriPartial.Path));
        Assert.Equal("Bearer", requests[0].AuthScheme);
        Assert.Equal("OAuth", requests[1].AuthScheme);
    }

    [Fact]
    public async Task UploadAsync_StillUsesBearerAndPhoneNumberMediaEndpoint()
    {
        var options = CreateOptions();
        var (client, requests) = CreateClient(options, _ => JsonResponse(HttpStatusCode.OK, """{"id":"media-123"}"""));

        var result = await client.UploadAsync(new MemoryStream([1, 2, 3]), "image/jpeg", "photo.jpg");

        Assert.True(result.IsSuccess);
        Assert.Equal("media-123", result.Value!.Id);
        var request = Assert.Single(requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal(BaseAddress(options) + "default-phone-id/media", request.Uri.ToString());
        Assert.Equal("Bearer", request.AuthScheme);
        Assert.Equal("test-access-token", request.AuthParameter);
        Assert.Null(request.FileOffset);
    }
}
