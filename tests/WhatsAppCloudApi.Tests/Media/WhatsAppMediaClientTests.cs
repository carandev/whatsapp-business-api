using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WhatsAppCloudApi.Clients;
using WhatsAppCloudApi.Tests.Fakes;

namespace WhatsAppCloudApi.Tests.Media;

public class WhatsAppMediaClientTests
{
    private const long ImageMaxBytes = 5 * 1024 * 1024L;
    private const long VideoMaxBytes = 16 * 1024 * 1024L;
    private const long AudioMaxBytes = 16 * 1024 * 1024L;
    private const long DocumentMaxBytes = 100 * 1024 * 1024L;
    private const long StickerMaxBytes = 500 * 1024L;

    private static WhatsAppOptions CreateOptions() => new()
    {
        AccessToken = "test-access-token",
        PhoneNumberId = "default-phone-id",
        ApiVersion = "v21.0",
        BaseUrl = "https://graph.facebook.com/{0}/",
    };

    private static (WhatsAppMediaClient Client, RecordingHandler Handler) CreateClient(
        WhatsAppOptions options,
        Func<HttpRequestMessage, HttpResponseMessage>? respond = null)
    {
        var handler = respond is null ? new RecordingHandler() : new RecordingHandler(respond);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(string.Format(options.BaseUrl, options.ApiVersion)),
        };
        var whatsAppClient = new WhatsAppClient(httpClient, Options.Create(options), NullLogger<WhatsAppClient>.Instance);
        var client = new WhatsAppMediaClient(whatsAppClient);
        return (client, handler);
    }

    private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string json) =>
        new(statusCode) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private static string ExpectedRequestUri(WhatsAppOptions options, string relativePath) =>
        string.Format(options.BaseUrl, options.ApiVersion) + relativePath;

    // ---------- Multipart shape (Stream overload) ----------

    [Fact]
    public async Task UploadAsync_Stream_SendsPostToMediaEndpoint()
    {
        var options = CreateOptions();
        var (client, handler) = CreateClient(
            options,
            _ => JsonResponse(HttpStatusCode.OK, """{"id":"media-123"}"""));

        await client.UploadAsync(new MemoryStream([1, 2, 3]), "image/jpeg", "photo.jpg");

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal(ExpectedRequestUri(options, $"{options.PhoneNumberId}/media"), handler.LastRequest.RequestUri!.ToString());
    }

    // NOTE: These tests capture multipart shape from *inside* the RecordingHandler's respond
    // callback, synchronously, while the request is still being sent. WhatsAppMediaClient disposes
    // its MultipartFormDataContent (via `using`) once UploadAsync returns, which clears the
    // content's nested-parts list — inspecting handler.LastRequest.Content *after* awaiting
    // UploadAsync would observe an already-disposed, empty multipart.

    [Fact]
    public async Task UploadAsync_Stream_SendsGenuineMultipartFormData_WithMessagingProductField()
    {
        var options = CreateOptions();
        string? messagingProductValue = null;
        var (client, _) = CreateClient(options, request =>
        {
            var multipart = Assert.IsType<MultipartFormDataContent>(request.Content);
            var messagingProductPart = multipart.Single(part => part.Headers.ContentDisposition?.Name?.Trim('"') == "messaging_product");
            messagingProductValue = messagingProductPart.ReadAsStringAsync().GetAwaiter().GetResult();
            return JsonResponse(HttpStatusCode.OK, """{"id":"media-123"}""");
        });

        await client.UploadAsync(new MemoryStream([1, 2, 3]), "image/jpeg", "photo.jpg");

        Assert.Equal("whatsapp", messagingProductValue);
    }

    [Fact]
    public async Task UploadAsync_Stream_SendsFilePart_WithCorrectContentTypeAndFilename()
    {
        var options = CreateOptions();
        string? fileContentType = null;
        string? fileName = null;
        string? filePartName = null;
        var (client, _) = CreateClient(options, request =>
        {
            var multipart = Assert.IsType<MultipartFormDataContent>(request.Content);
            var filePart = multipart.Single(part => part.Headers.ContentDisposition?.FileName is not null);
            fileContentType = filePart.Headers.ContentType?.MediaType;
            fileName = filePart.Headers.ContentDisposition?.FileName?.Trim('"');
            filePartName = filePart.Headers.ContentDisposition?.Name?.Trim('"');
            return JsonResponse(HttpStatusCode.OK, """{"id":"media-123"}""");
        });

        await client.UploadAsync(new MemoryStream([1, 2, 3]), "image/jpeg", "photo.jpg");

        Assert.Equal("image/jpeg", fileContentType);
        Assert.Equal("photo.jpg", fileName);
        Assert.Equal("file", filePartName);
    }

    // ---------- Result mapping (Stream overload) ----------

    [Fact]
    public async Task UploadAsync_ReturnsSuccessResult_WithDeserializedId_On2xxResponse()
    {
        var options = CreateOptions();
        var (client, _) = CreateClient(
            options,
            _ => JsonResponse(HttpStatusCode.OK, """{"id":"media-123"}"""));

        var result = await client.UploadAsync(new MemoryStream([1, 2, 3]), "image/jpeg", "photo.jpg");

        Assert.True(result.IsSuccess);
        Assert.Equal("media-123", result.Value!.Id);
    }

    [Fact]
    public async Task UploadAsync_ReturnsFailureResult_WithDeserializedError_OnNon2xxResponse()
    {
        var options = CreateOptions();
        var (client, _) = CreateClient(
            options,
            _ => JsonResponse(HttpStatusCode.BadRequest, """{"error":{"message":"Invalid parameter","type":"OAuthException","code":100}}"""));

        var result = await client.UploadAsync(new MemoryStream([1, 2, 3]), "image/jpeg", "photo.jpg");

        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid parameter", result.Error!.Message);
        Assert.Equal("OAuthException", result.Error.Type);
        Assert.Equal(100, result.Error.Code);
    }

    // ---------- Size validation — seekable streams ----------

    [Theory]
    [InlineData("image/jpeg", ImageMaxBytes)]
    [InlineData("video/mp4", VideoMaxBytes)]
    [InlineData("audio/mpeg", AudioMaxBytes)]
    [InlineData("application/pdf", DocumentMaxBytes)]
    [InlineData("image/webp", StickerMaxBytes)]
    public async Task UploadAsync_Throws_MediaSizeExceededException_ForEachOversizedMimeCategory(string mimeType, long maxAllowedBytes)
    {
        var options = CreateOptions();
        var (client, handler) = CreateClient(options);
        var oversizedStream = new SizedSeekableStream(maxAllowedBytes + 1);

        var exception = await Assert.ThrowsAsync<MediaSizeExceededException>(
            () => client.UploadAsync(oversizedStream, mimeType, "file"));

        Assert.Equal(mimeType, exception.MimeType);
        Assert.Equal(maxAllowedBytes, exception.MaxAllowedBytes);
        Assert.Equal(maxAllowedBytes + 1, exception.ContentLength);
        Assert.Equal(0, handler.RequestCount);
    }

    [Theory]
    [InlineData("image/jpeg", ImageMaxBytes)]
    [InlineData("video/mp4", VideoMaxBytes)]
    [InlineData("audio/mpeg", AudioMaxBytes)]
    [InlineData("application/pdf", DocumentMaxBytes)]
    [InlineData("image/webp", StickerMaxBytes)]
    public async Task UploadAsync_DoesNotThrow_WhenContentIsExactlyAtTheCeiling(string mimeType, long maxAllowedBytes)
    {
        var options = CreateOptions();
        var (client, handler) = CreateClient(
            options,
            _ => JsonResponse(HttpStatusCode.OK, """{"id":"media-123"}"""));
        var exactSizeStream = new SizedSeekableStream(maxAllowedBytes);

        // The stream reports its Length via SizedSeekableStream; no bytes are actually read for
        // this assertion, since the canned handler never reads the request content and the
        // pre-flight check only inspects Length before proceeding.
        var result = await client.UploadAsync(exactSizeStream, mimeType, "file");

        Assert.True(result.IsSuccess);
        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task UploadAsync_DoesNotThrow_ForSameSizeContent_WhenDeclaredCategoryAllowsIt()
    {
        // 10MB is over the sticker (500KB) and image (5MB) ceilings, but under the video/document
        // (16MB/100MB) ceilings — validation must be evaluated per the declared MIME category, not
        // a single flat limit.
        const long tenMegabytes = 10 * 1024 * 1024L;
        var options = CreateOptions();

        var (imageClient, imageHandler) = CreateClient(options);
        await Assert.ThrowsAsync<MediaSizeExceededException>(
            () => imageClient.UploadAsync(new SizedSeekableStream(tenMegabytes), "image/jpeg", "file"));
        Assert.Equal(0, imageHandler.RequestCount);

        var (videoClient, videoHandler) = CreateClient(
            options,
            _ => JsonResponse(HttpStatusCode.OK, """{"id":"media-123"}"""));
        var result = await videoClient.UploadAsync(new SizedSeekableStream(tenMegabytes), "video/mp4", "file");

        Assert.True(result.IsSuccess);
        Assert.Equal(1, videoHandler.RequestCount);
    }

    // ---------- Size validation — non-seekable streams (buffered path) ----------

    [Fact]
    public async Task UploadAsync_Throws_MediaSizeExceededException_ForOversizedNonSeekableStream()
    {
        var options = CreateOptions();
        var (client, handler) = CreateClient(options);
        var oversizedContent = new byte[StickerMaxBytes + 1];
        var nonSeekableStream = new NonSeekableStream(oversizedContent);

        var exception = await Assert.ThrowsAsync<MediaSizeExceededException>(
            () => client.UploadAsync(nonSeekableStream, "image/webp", "sticker.webp"));

        Assert.Equal("image/webp", exception.MimeType);
        Assert.Equal(StickerMaxBytes, exception.MaxAllowedBytes);
        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public async Task UploadAsync_Succeeds_ForNonSeekableStream_WithinLimit()
    {
        var options = CreateOptions();
        var (client, handler) = CreateClient(
            options,
            _ => JsonResponse(HttpStatusCode.OK, """{"id":"media-123"}"""));
        var content = new byte[] { 1, 2, 3, 4, 5 };
        var nonSeekableStream = new NonSeekableStream(content);

        var result = await client.UploadAsync(nonSeekableStream, "image/jpeg", "photo.jpg");

        Assert.True(result.IsSuccess);
        Assert.Equal("media-123", result.Value!.Id);
        Assert.Equal(1, handler.RequestCount);
    }

    // ---------- byte[] overload ----------

    [Fact]
    public async Task UploadAsync_ByteArray_BehavesLikeStreamOverload_AndSucceeds()
    {
        var options = CreateOptions();
        var (client, handler) = CreateClient(
            options,
            _ => JsonResponse(HttpStatusCode.OK, """{"id":"media-123"}"""));

        var result = await client.UploadAsync(new byte[] { 1, 2, 3 }, "image/jpeg", "photo.jpg");

        Assert.True(result.IsSuccess);
        Assert.Equal("media-123", result.Value!.Id);
        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task UploadAsync_ByteArray_Throws_MediaSizeExceededException_WhenOversized()
    {
        var options = CreateOptions();
        var (client, handler) = CreateClient(options);
        var oversizedContent = new byte[StickerMaxBytes + 1];

        var exception = await Assert.ThrowsAsync<MediaSizeExceededException>(
            () => client.UploadAsync(oversizedContent, "image/webp", "sticker.webp"));

        Assert.Equal(StickerMaxBytes, exception.MaxAllowedBytes);
        Assert.Equal(0, handler.RequestCount);
    }

    // ---------- filePath overload ----------

    [Fact]
    public async Task UploadAsync_FilePath_SendsFileContent_AndSucceeds()
    {
        var options = CreateOptions();
        var (client, handler) = CreateClient(
            options,
            _ => JsonResponse(HttpStatusCode.OK, """{"id":"media-123"}"""));
        var tempFile = Path.Combine(Path.GetTempPath(), $"media-upload-test-{Guid.NewGuid():N}.jpg");
        await File.WriteAllBytesAsync(tempFile, [1, 2, 3]);

        try
        {
            var result = await client.UploadAsync(tempFile, "image/jpeg");

            Assert.True(result.IsSuccess);
            Assert.Equal("media-123", result.Value!.Id);
            Assert.Equal(1, handler.RequestCount);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task UploadAsync_FilePath_DefaultsFilename_FromFilePath_WhenOmitted()
    {
        var options = CreateOptions();
        string? fileName = null;
        var (client, _) = CreateClient(options, request =>
        {
            var multipart = Assert.IsType<MultipartFormDataContent>(request.Content);
            var filePart = multipart.Single(part => part.Headers.ContentDisposition?.FileName is not null);
            fileName = filePart.Headers.ContentDisposition?.FileName?.Trim('"');
            return JsonResponse(HttpStatusCode.OK, """{"id":"media-123"}""");
        });
        var tempFile = Path.Combine(Path.GetTempPath(), $"media-upload-test-{Guid.NewGuid():N}.jpg");
        await File.WriteAllBytesAsync(tempFile, [1, 2, 3]);

        try
        {
            await client.UploadAsync(tempFile, "image/jpeg");

            Assert.Equal(Path.GetFileName(tempFile), fileName);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task UploadAsync_FilePath_UsesExplicitFilename_WhenSupplied()
    {
        var options = CreateOptions();
        string? fileName = null;
        var (client, _) = CreateClient(options, request =>
        {
            var multipart = Assert.IsType<MultipartFormDataContent>(request.Content);
            var filePart = multipart.Single(part => part.Headers.ContentDisposition?.FileName is not null);
            fileName = filePart.Headers.ContentDisposition?.FileName?.Trim('"');
            return JsonResponse(HttpStatusCode.OK, """{"id":"media-123"}""");
        });
        var tempFile = Path.Combine(Path.GetTempPath(), $"media-upload-test-{Guid.NewGuid():N}.jpg");
        await File.WriteAllBytesAsync(tempFile, [1, 2, 3]);

        try
        {
            await client.UploadAsync(tempFile, "image/jpeg", filename: "custom-name.jpg");

            Assert.Equal("custom-name.jpg", fileName);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task UploadAsync_FilePath_ThrowsFileNotFoundException_AndSendsNoRequest_WhenFileDoesNotExist()
    {
        var options = CreateOptions();
        var (client, handler) = CreateClient(options);
        var missingPath = Path.Combine(Path.GetTempPath(), $"does-not-exist-{Guid.NewGuid():N}.jpg");

        await Assert.ThrowsAsync<FileNotFoundException>(
            () => client.UploadAsync(missingPath, "image/jpeg"));

        Assert.Equal(0, handler.RequestCount);
    }

    // ---------- GetMediaUrlAsync ----------

    [Fact]
    public async Task GetMediaUrlAsync_SendsGetToMediaIdPath_WithNoPhoneNumberIdSubstitution()
    {
        var options = CreateOptions();
        const string mediaJson = """{"url":"https://cdn.example.com/media/abc","mime_type":"image/jpeg","sha256":"deadbeef","file_size":123,"id":"media-id-1"}""";
        var (client, handler) = CreateClient(
            options,
            _ => JsonResponse(HttpStatusCode.OK, mediaJson));

        await client.GetMediaUrlAsync("media-id-1");

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Equal(ExpectedRequestUri(options, "media-id-1"), handler.LastRequest.RequestUri!.ToString());
    }

    [Fact]
    public async Task GetMediaUrlAsync_ReturnsSuccessResult_WithDeserializedFields_On2xxResponse()
    {
        var options = CreateOptions();
        const string mediaJson = """{"url":"https://cdn.example.com/media/abc","mime_type":"image/jpeg","sha256":"deadbeef","file_size":123,"id":"media-id-1"}""";
        var (client, _) = CreateClient(
            options,
            _ => JsonResponse(HttpStatusCode.OK, mediaJson));

        var result = await client.GetMediaUrlAsync("media-id-1");

        Assert.True(result.IsSuccess);
        Assert.Equal("https://cdn.example.com/media/abc", result.Value!.Url);
        Assert.Equal("image/jpeg", result.Value.MimeType);
        Assert.Equal("deadbeef", result.Value.Sha256);
        Assert.Equal(123, result.Value.FileSize);
        Assert.Equal("media-id-1", result.Value.Id);
    }

    [Fact]
    public async Task GetMediaUrlAsync_ReturnsFailureResult_WithDeserializedError_OnNon2xxResponse()
    {
        var options = CreateOptions();
        var (client, _) = CreateClient(
            options,
            _ => JsonResponse(HttpStatusCode.NotFound, """{"error":{"message":"Unsupported get request","type":"GraphMethodException","code":100}}"""));

        var result = await client.GetMediaUrlAsync("unknown-media-id");

        Assert.False(result.IsSuccess);
        Assert.Equal("Unsupported get request", result.Error!.Message);
        Assert.Equal("GraphMethodException", result.Error.Type);
        Assert.Equal(100, result.Error.Code);
    }

    // ---------- DownloadAsync / DownloadBytesAsync ----------

    private const string CdnUrl = "https://lookaside.fbsbx.com/whatsapp_business/attachments/?token=xyz";
    private static readonly byte[] DownloadBytes = [10, 20, 30, 40, 50];

    [Fact]
    public async Task DownloadAsync_SendsRequest_ToTheExactAbsoluteUrl_NotTheConfiguredBaseAddress()
    {
        var options = CreateOptions();
        var (client, handler) = CreateClient(
            options,
            _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(DownloadBytes) });

        await client.DownloadAsync(CdnUrl);

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Equal(CdnUrl, handler.LastRequest.RequestUri!.ToString());
        Assert.Equal("lookaside.fbsbx.com", handler.LastRequest.RequestUri.Host);
        Assert.NotEqual("graph.facebook.com", handler.LastRequest.RequestUri.Host);
    }

    [Fact]
    public async Task DownloadAsync_AttachesBearerAuthorizationHeader()
    {
        var options = CreateOptions();
        var (client, handler) = CreateClient(
            options,
            _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(DownloadBytes) });

        await client.DownloadAsync(CdnUrl);

        Assert.NotNull(handler.LastRequest!.Headers.Authorization);
        Assert.Equal("Bearer", handler.LastRequest.Headers.Authorization!.Scheme);
        Assert.Equal(options.AccessToken, handler.LastRequest.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task DownloadAsync_ReturnsSuccessResult_WithStream_MatchingResponseBody_On2xxResponse()
    {
        var options = CreateOptions();
        var (client, _) = CreateClient(
            options,
            _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(DownloadBytes) });

        var result = await client.DownloadAsync(CdnUrl);

        Assert.True(result.IsSuccess);
        using var stream = result.Value!;
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer);
        Assert.Equal(DownloadBytes, buffer.ToArray());
    }

    [Fact]
    public async Task DownloadAsync_ReturnsFailureResult_WithDeserializedError_OnNon2xxResponse()
    {
        var options = CreateOptions();
        var (client, _) = CreateClient(
            options,
            _ => JsonResponse(HttpStatusCode.Forbidden, """{"error":{"message":"Media is no longer available","type":"OAuthException","code":10}}"""));

        var result = await client.DownloadAsync(CdnUrl);

        Assert.False(result.IsSuccess);
        Assert.Equal("Media is no longer available", result.Error!.Message);
        Assert.Equal("OAuthException", result.Error.Type);
        Assert.Equal(10, result.Error.Code);
    }

    [Fact]
    public async Task DownloadBytesAsync_SendsRequest_ToTheExactAbsoluteUrl_NotTheConfiguredBaseAddress()
    {
        var options = CreateOptions();
        var (client, handler) = CreateClient(
            options,
            _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(DownloadBytes) });

        await client.DownloadBytesAsync(CdnUrl);

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Equal(CdnUrl, handler.LastRequest.RequestUri!.ToString());
        Assert.Equal("lookaside.fbsbx.com", handler.LastRequest.RequestUri.Host);
        Assert.NotEqual("graph.facebook.com", handler.LastRequest.RequestUri.Host);
    }

    [Fact]
    public async Task DownloadBytesAsync_AttachesBearerAuthorizationHeader()
    {
        var options = CreateOptions();
        var (client, handler) = CreateClient(
            options,
            _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(DownloadBytes) });

        await client.DownloadBytesAsync(CdnUrl);

        Assert.NotNull(handler.LastRequest!.Headers.Authorization);
        Assert.Equal("Bearer", handler.LastRequest.Headers.Authorization!.Scheme);
        Assert.Equal(options.AccessToken, handler.LastRequest.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task DownloadBytesAsync_ReturnsSuccessResult_WithBytes_MatchingResponseBody_On2xxResponse()
    {
        var options = CreateOptions();
        var (client, _) = CreateClient(
            options,
            _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(DownloadBytes) });

        var result = await client.DownloadBytesAsync(CdnUrl);

        Assert.True(result.IsSuccess);
        Assert.Equal(DownloadBytes, result.Value);
    }

    [Fact]
    public async Task DownloadBytesAsync_ReturnsFailureResult_WithDeserializedError_OnNon2xxResponse()
    {
        var options = CreateOptions();
        var (client, _) = CreateClient(
            options,
            _ => JsonResponse(HttpStatusCode.Forbidden, """{"error":{"message":"Media is no longer available","type":"OAuthException","code":10}}"""));

        var result = await client.DownloadBytesAsync(CdnUrl);

        Assert.False(result.IsSuccess);
        Assert.Equal("Media is no longer available", result.Error!.Message);
        Assert.Equal("OAuthException", result.Error.Type);
        Assert.Equal(10, result.Error.Code);
    }
}
