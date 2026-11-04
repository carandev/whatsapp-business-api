using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WhatsAppCloudApi.Clients;
using WhatsAppCloudApi.Models.Messages;
using WhatsAppCloudApi.Models.Messages.Interactive;
using WhatsAppCloudApi.Tests.Fakes;

namespace WhatsAppCloudApi.Tests.Messages;

public class WhatsAppMessagesClientTests
{
    private const string SuccessBody = """{"messaging_product":"whatsapp","contacts":[{"input":"+15550001111","wa_id":"15550001111"}],"messages":[{"id":"wamid.HBg"}]}""";
    private const string ErrorBody = """{"error":{"message":"Invalid parameter","type":"OAuthException","code":100}}""";

    private static WhatsAppOptions CreateOptions() => new()
    {
        AccessToken = "test-access-token",
        PhoneNumberId = "default-phone-id",
        ApiVersion = "v21.0",
        BaseUrl = "https://graph.facebook.com/{0}/",
    };

    private static (WhatsAppMessagesClient Client, RecordingHandler Handler) CreateClient(
        WhatsAppOptions options,
        Func<HttpRequestMessage, HttpResponseMessage>? respond = null)
    {
        var handler = respond is null ? new RecordingHandler() : new RecordingHandler(respond);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(string.Format(options.BaseUrl, options.ApiVersion)),
        };
        var whatsAppClient = new WhatsAppClient(httpClient, Options.Create(options), NullLogger<WhatsAppClient>.Instance);
        var client = new WhatsAppMessagesClient(whatsAppClient);
        return (client, handler);
    }

    private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string json) =>
        new(statusCode) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private static string ExpectedRequestUri(WhatsAppOptions options) =>
        string.Format(options.BaseUrl, options.ApiVersion) + $"{options.PhoneNumberId}/messages";

    private static async Task<string> ReadRequestBodyAsync(HttpRequestMessage request) =>
        await request.Content!.ReadAsStringAsync();

    private static void AssertSuccess(Result<MessageResponse> result)
    {
        Assert.True(result.IsSuccess);
        Assert.Equal("wamid.HBg", result.Value!.MessageId);
    }

    private static void AssertFailure(Result<MessageResponse> result)
    {
        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid parameter", result.Error!.Message);
        Assert.Equal("OAuthException", result.Error.Type);
        Assert.Equal(100, result.Error.Code);
    }

    // ---------- SendTextAsync ----------

    [Fact]
    public async Task SendTextAsync_SendsPostWithTextPayload_ToMessagesEndpoint()
    {
        var options = CreateOptions();
        var (client, handler) = CreateClient(options, _ => JsonResponse(HttpStatusCode.OK, SuccessBody));

        await client.SendTextAsync("15550001111", "Hello there", previewUrl: true);

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal(ExpectedRequestUri(options), handler.LastRequest.RequestUri!.ToString());
        var body = await ReadRequestBodyAsync(handler.LastRequest);
        Assert.Contains("\"type\":\"text\"", body, StringComparison.Ordinal);
        Assert.Contains("\"body\":\"Hello there\"", body, StringComparison.Ordinal);
        Assert.Contains("\"preview_url\":true", body, StringComparison.Ordinal);
        Assert.Contains("\"to\":\"15550001111\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendTextAsync_ReturnsSuccess_WithMessageId_On2xxResponse()
    {
        var options = CreateOptions();
        var (client, _) = CreateClient(options, _ => JsonResponse(HttpStatusCode.OK, SuccessBody));

        var result = await client.SendTextAsync("15550001111", "Hello there");

        AssertSuccess(result);
    }

    [Fact]
    public async Task SendTextAsync_ReturnsFailure_WithDeserializedError_OnNon2xxResponse()
    {
        var options = CreateOptions();
        var (client, _) = CreateClient(options, _ => JsonResponse(HttpStatusCode.BadRequest, ErrorBody));

        var result = await client.SendTextAsync("15550001111", "Hello there");

        AssertFailure(result);
    }

    [Fact]
    public async Task SendTextAsync_IncludesContext_WhenSupplied()
    {
        var options = CreateOptions();
        var (client, handler) = CreateClient(options, _ => JsonResponse(HttpStatusCode.OK, SuccessBody));

        await client.SendTextAsync("15550001111", "Hello there", context: new Context { MessageId = "wamid.prior" });

        var body = await ReadRequestBodyAsync(handler.LastRequest!);
        Assert.Contains("\"context\":{\"message_id\":\"wamid.prior\"}", body, StringComparison.Ordinal);
    }

    // ---------- SendTemplateAsync ----------

    [Fact]
    public async Task SendTemplateAsync_SendsPostWithTemplatePayload_ToMessagesEndpoint()
    {
        var options = CreateOptions();
        var (client, handler) = CreateClient(options, _ => JsonResponse(HttpStatusCode.OK, SuccessBody));

        await client.SendTemplateAsync(
            "15550001111",
            "order_confirmation",
            "en_US",
            [new BodyParameterComponent([new TextParameter("John")])]);

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal(ExpectedRequestUri(options), handler.LastRequest.RequestUri!.ToString());
        var body = await ReadRequestBodyAsync(handler.LastRequest);
        Assert.Equal(
            """{"type":"template","template":{"name":"order_confirmation","language":{"code":"en_US"},"components":[{"type":"body","parameters":[{"type":"text","text":"John"}]}]},"messaging_product":"whatsapp","to":"15550001111"}""",
            body);
    }

    [Fact]
    public async Task SendTemplateAsync_OmitsComponents_WhenNull()
    {
        var options = CreateOptions();
        var (client, handler) = CreateClient(options, _ => JsonResponse(HttpStatusCode.OK, SuccessBody));

        await client.SendTemplateAsync("15550001111", "order_confirmation", "en_US");

        var body = await ReadRequestBodyAsync(handler.LastRequest!);
        Assert.DoesNotContain("\"components\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendTemplateAsync_ReturnsSuccess_On2xxResponse()
    {
        var options = CreateOptions();
        var (client, _) = CreateClient(options, _ => JsonResponse(HttpStatusCode.OK, SuccessBody));

        var result = await client.SendTemplateAsync("15550001111", "order_confirmation", "en_US");

        AssertSuccess(result);
    }

    [Fact]
    public async Task SendTemplateAsync_ReturnsFailure_OnNon2xxResponse()
    {
        var options = CreateOptions();
        var (client, _) = CreateClient(options, _ => JsonResponse(HttpStatusCode.BadRequest, ErrorBody));

        var result = await client.SendTemplateAsync("15550001111", "unknown_template", "en_US");

        AssertFailure(result);
    }

    // ---------- SendImageAsync ----------

    [Fact]
    public async Task SendImageAsync_SendsPostWithMediaIdPayload()
    {
        var options = CreateOptions();
        var (client, handler) = CreateClient(options, _ => JsonResponse(HttpStatusCode.OK, SuccessBody));

        await client.SendImageAsync("15550001111", mediaId: "media-123");

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal(ExpectedRequestUri(options), handler.LastRequest.RequestUri!.ToString());
        var body = await ReadRequestBodyAsync(handler.LastRequest);
        Assert.Contains("\"type\":\"image\"", body, StringComparison.Ordinal);
        Assert.Contains("\"image\":{\"id\":\"media-123\"}", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendImageAsync_SendsPostWithLinkAndCaptionPayload()
    {
        var options = CreateOptions();
        var (client, handler) = CreateClient(options, _ => JsonResponse(HttpStatusCode.OK, SuccessBody));

        await client.SendImageAsync("15550001111", link: "https://example.com/pic.jpg", caption: "A picture");

        var body = await ReadRequestBodyAsync(handler.LastRequest!);
        Assert.Contains("\"image\":{\"link\":\"https://example.com/pic.jpg\",\"caption\":\"A picture\"}", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendImageAsync_ReturnsSuccess_On2xxResponse()
    {
        var options = CreateOptions();
        var (client, _) = CreateClient(options, _ => JsonResponse(HttpStatusCode.OK, SuccessBody));

        var result = await client.SendImageAsync("15550001111", mediaId: "media-123");

        AssertSuccess(result);
    }

    [Fact]
    public async Task SendImageAsync_ReturnsFailure_OnNon2xxResponse()
    {
        var options = CreateOptions();
        var (client, _) = CreateClient(options, _ => JsonResponse(HttpStatusCode.BadRequest, ErrorBody));

        var result = await client.SendImageAsync("15550001111", mediaId: "media-123");

        AssertFailure(result);
    }

    [Fact]
    public async Task SendImageAsync_ThrowsArgumentException_AndSendsNoRequest_WhenBothMediaIdAndLinkSupplied()
    {
        var options = CreateOptions();
        var (client, handler) = CreateClient(options);

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await client.SendImageAsync("15550001111", mediaId: "media-123", link: "https://example.com/pic.jpg"));

        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public async Task SendImageAsync_ThrowsArgumentException_AndSendsNoRequest_WhenNeitherMediaIdNorLinkSupplied()
    {
        var options = CreateOptions();
        var (client, handler) = CreateClient(options);

        await Assert.ThrowsAsync<ArgumentException>(async () => await client.SendImageAsync("15550001111"));

        Assert.Equal(0, handler.RequestCount);
    }

    // ---------- SendDocumentAsync ----------

    [Fact]
    public async Task SendDocumentAsync_SendsPostWithLinkPayload()
    {
        var options = CreateOptions();
        var (client, handler) = CreateClient(options, _ => JsonResponse(HttpStatusCode.OK, SuccessBody));

        await client.SendDocumentAsync("15550001111", link: "https://example.com/doc.pdf");

        var body = await ReadRequestBodyAsync(handler.LastRequest!);
        Assert.Contains("\"type\":\"document\"", body, StringComparison.Ordinal);
        Assert.Contains("\"document\":{\"link\":\"https://example.com/doc.pdf\"}", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendDocumentAsync_ReturnsSuccess_On2xxResponse()
    {
        var options = CreateOptions();
        var (client, _) = CreateClient(options, _ => JsonResponse(HttpStatusCode.OK, SuccessBody));

        var result = await client.SendDocumentAsync("15550001111", mediaId: "media-123");

        AssertSuccess(result);
    }

    [Fact]
    public async Task SendDocumentAsync_ReturnsFailure_OnNon2xxResponse()
    {
        var options = CreateOptions();
        var (client, _) = CreateClient(options, _ => JsonResponse(HttpStatusCode.BadRequest, ErrorBody));

        var result = await client.SendDocumentAsync("15550001111", mediaId: "media-123");

        AssertFailure(result);
    }

    [Fact]
    public async Task SendDocumentAsync_ThrowsArgumentException_AndSendsNoRequest_WhenBothMediaIdAndLinkSupplied()
    {
        var options = CreateOptions();
        var (client, handler) = CreateClient(options);

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await client.SendDocumentAsync("15550001111", mediaId: "media-123", link: "https://example.com/doc.pdf"));

        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public async Task SendDocumentAsync_ThrowsArgumentException_AndSendsNoRequest_WhenNeitherMediaIdNorLinkSupplied()
    {
        var options = CreateOptions();
        var (client, handler) = CreateClient(options);

        await Assert.ThrowsAsync<ArgumentException>(async () => await client.SendDocumentAsync("15550001111"));

        Assert.Equal(0, handler.RequestCount);
    }

    // ---------- SendVideoAsync ----------

    [Fact]
    public async Task SendVideoAsync_SendsPostWithMediaIdAndCaptionPayload()
    {
        var options = CreateOptions();
        var (client, handler) = CreateClient(options, _ => JsonResponse(HttpStatusCode.OK, SuccessBody));

        await client.SendVideoAsync("15550001111", mediaId: "media-123", caption: "A video");

        var body = await ReadRequestBodyAsync(handler.LastRequest!);
        Assert.Contains("\"type\":\"video\"", body, StringComparison.Ordinal);
        Assert.Contains("\"video\":{\"id\":\"media-123\",\"caption\":\"A video\"}", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendVideoAsync_ReturnsSuccess_On2xxResponse()
    {
        var options = CreateOptions();
        var (client, _) = CreateClient(options, _ => JsonResponse(HttpStatusCode.OK, SuccessBody));

        var result = await client.SendVideoAsync("15550001111", mediaId: "media-123");

        AssertSuccess(result);
    }

    [Fact]
    public async Task SendVideoAsync_ReturnsFailure_OnNon2xxResponse()
    {
        var options = CreateOptions();
        var (client, _) = CreateClient(options, _ => JsonResponse(HttpStatusCode.BadRequest, ErrorBody));

        var result = await client.SendVideoAsync("15550001111", mediaId: "media-123");

        AssertFailure(result);
    }

    [Fact]
    public async Task SendVideoAsync_ThrowsArgumentException_AndSendsNoRequest_WhenBothMediaIdAndLinkSupplied()
    {
        var options = CreateOptions();
        var (client, handler) = CreateClient(options);

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await client.SendVideoAsync("15550001111", mediaId: "media-123", link: "https://example.com/vid.mp4"));

        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public async Task SendVideoAsync_ThrowsArgumentException_AndSendsNoRequest_WhenNeitherMediaIdNorLinkSupplied()
    {
        var options = CreateOptions();
        var (client, handler) = CreateClient(options);

        await Assert.ThrowsAsync<ArgumentException>(async () => await client.SendVideoAsync("15550001111"));

        Assert.Equal(0, handler.RequestCount);
    }

    // ---------- SendAudioAsync ----------

    [Fact]
    public async Task SendAudioAsync_SendsPostWithMediaIdPayload_NoCaptionField()
    {
        var options = CreateOptions();
        var (client, handler) = CreateClient(options, _ => JsonResponse(HttpStatusCode.OK, SuccessBody));

        await client.SendAudioAsync("15550001111", mediaId: "media-123");

        var body = await ReadRequestBodyAsync(handler.LastRequest!);
        Assert.Contains("\"type\":\"audio\"", body, StringComparison.Ordinal);
        Assert.Contains("\"audio\":{\"id\":\"media-123\"}", body, StringComparison.Ordinal);
        Assert.DoesNotContain("caption", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendAudioAsync_ReturnsSuccess_On2xxResponse()
    {
        var options = CreateOptions();
        var (client, _) = CreateClient(options, _ => JsonResponse(HttpStatusCode.OK, SuccessBody));

        var result = await client.SendAudioAsync("15550001111", mediaId: "media-123");

        AssertSuccess(result);
    }

    [Fact]
    public async Task SendAudioAsync_ReturnsFailure_OnNon2xxResponse()
    {
        var options = CreateOptions();
        var (client, _) = CreateClient(options, _ => JsonResponse(HttpStatusCode.BadRequest, ErrorBody));

        var result = await client.SendAudioAsync("15550001111", mediaId: "media-123");

        AssertFailure(result);
    }

    [Fact]
    public async Task SendAudioAsync_ThrowsArgumentException_AndSendsNoRequest_WhenBothMediaIdAndLinkSupplied()
    {
        var options = CreateOptions();
        var (client, handler) = CreateClient(options);

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await client.SendAudioAsync("15550001111", mediaId: "media-123", link: "https://example.com/a.mp3"));

        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public async Task SendAudioAsync_ThrowsArgumentException_AndSendsNoRequest_WhenNeitherMediaIdNorLinkSupplied()
    {
        var options = CreateOptions();
        var (client, handler) = CreateClient(options);

        await Assert.ThrowsAsync<ArgumentException>(async () => await client.SendAudioAsync("15550001111"));

        Assert.Equal(0, handler.RequestCount);
    }

    // ---------- SendStickerAsync ----------

    [Fact]
    public async Task SendStickerAsync_SendsPostWithLinkPayload_NoCaptionField()
    {
        var options = CreateOptions();
        var (client, handler) = CreateClient(options, _ => JsonResponse(HttpStatusCode.OK, SuccessBody));

        await client.SendStickerAsync("15550001111", link: "https://example.com/s.webp");

        var body = await ReadRequestBodyAsync(handler.LastRequest!);
        Assert.Contains("\"type\":\"sticker\"", body, StringComparison.Ordinal);
        Assert.Contains("\"sticker\":{\"link\":\"https://example.com/s.webp\"}", body, StringComparison.Ordinal);
        Assert.DoesNotContain("caption", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendStickerAsync_ReturnsSuccess_On2xxResponse()
    {
        var options = CreateOptions();
        var (client, _) = CreateClient(options, _ => JsonResponse(HttpStatusCode.OK, SuccessBody));

        var result = await client.SendStickerAsync("15550001111", mediaId: "media-123");

        AssertSuccess(result);
    }

    [Fact]
    public async Task SendStickerAsync_ReturnsFailure_OnNon2xxResponse()
    {
        var options = CreateOptions();
        var (client, _) = CreateClient(options, _ => JsonResponse(HttpStatusCode.BadRequest, ErrorBody));

        var result = await client.SendStickerAsync("15550001111", mediaId: "media-123");

        AssertFailure(result);
    }

    [Fact]
    public async Task SendStickerAsync_ThrowsArgumentException_AndSendsNoRequest_WhenBothMediaIdAndLinkSupplied()
    {
        var options = CreateOptions();
        var (client, handler) = CreateClient(options);

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await client.SendStickerAsync("15550001111", mediaId: "media-123", link: "https://example.com/s.webp"));

        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public async Task SendStickerAsync_ThrowsArgumentException_AndSendsNoRequest_WhenNeitherMediaIdNorLinkSupplied()
    {
        var options = CreateOptions();
        var (client, handler) = CreateClient(options);

        await Assert.ThrowsAsync<ArgumentException>(async () => await client.SendStickerAsync("15550001111"));

        Assert.Equal(0, handler.RequestCount);
    }

    // ---------- SendLocationAsync ----------

    [Fact]
    public async Task SendLocationAsync_SendsPostWithLocationPayload()
    {
        var options = CreateOptions();
        var (client, handler) = CreateClient(options, _ => JsonResponse(HttpStatusCode.OK, SuccessBody));

        await client.SendLocationAsync("15550001111", 37.4847, -122.1477, name: "Meta HQ", address: "1 Hacker Way");

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal(ExpectedRequestUri(options), handler.LastRequest.RequestUri!.ToString());
        var body = await ReadRequestBodyAsync(handler.LastRequest);
        Assert.Contains("\"type\":\"location\"", body, StringComparison.Ordinal);
        Assert.Contains("\"latitude\":37.4847", body, StringComparison.Ordinal);
        Assert.Contains("\"longitude\":-122.1477", body, StringComparison.Ordinal);
        Assert.Contains("\"name\":\"Meta HQ\"", body, StringComparison.Ordinal);
        Assert.Contains("\"address\":\"1 Hacker Way\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendLocationAsync_ReturnsSuccess_On2xxResponse()
    {
        var options = CreateOptions();
        var (client, _) = CreateClient(options, _ => JsonResponse(HttpStatusCode.OK, SuccessBody));

        var result = await client.SendLocationAsync("15550001111", 37.4847, -122.1477);

        AssertSuccess(result);
    }

    [Fact]
    public async Task SendLocationAsync_ReturnsFailure_OnNon2xxResponse()
    {
        var options = CreateOptions();
        var (client, _) = CreateClient(options, _ => JsonResponse(HttpStatusCode.BadRequest, ErrorBody));

        var result = await client.SendLocationAsync("15550001111", 37.4847, -122.1477);

        AssertFailure(result);
    }

    // ---------- SendContactsAsync ----------

    [Fact]
    public async Task SendContactsAsync_SendsPostWithContactsPayload()
    {
        var options = CreateOptions();
        var (client, handler) = CreateClient(options, _ => JsonResponse(HttpStatusCode.OK, SuccessBody));

        IReadOnlyList<Contact> contacts =
        [
            new Contact { Name = new ContactName { FormattedName = "John Doe" } },
        ];

        await client.SendContactsAsync("15550001111", contacts);

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal(ExpectedRequestUri(options), handler.LastRequest.RequestUri!.ToString());
        var body = await ReadRequestBodyAsync(handler.LastRequest);
        Assert.Contains("\"type\":\"contacts\"", body, StringComparison.Ordinal);
        Assert.Contains("\"contacts\":[{\"name\":{\"formatted_name\":\"John Doe\"}}]", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendContactsAsync_ReturnsSuccess_On2xxResponse()
    {
        var options = CreateOptions();
        var (client, _) = CreateClient(options, _ => JsonResponse(HttpStatusCode.OK, SuccessBody));

        var result = await client.SendContactsAsync("15550001111", [new Contact()]);

        AssertSuccess(result);
    }

    [Fact]
    public async Task SendContactsAsync_ReturnsFailure_OnNon2xxResponse()
    {
        var options = CreateOptions();
        var (client, _) = CreateClient(options, _ => JsonResponse(HttpStatusCode.BadRequest, ErrorBody));

        var result = await client.SendContactsAsync("15550001111", [new Contact()]);

        AssertFailure(result);
    }

    // ---------- SendReplyButtonsAsync ----------

    [Fact]
    public async Task SendReplyButtonsAsync_SendsPostWithReplyButtonsPayload_ToMessagesEndpoint()
    {
        var options = CreateOptions();
        var (client, handler) = CreateClient(options, _ => JsonResponse(HttpStatusCode.OK, SuccessBody));

        await client.SendReplyButtonsAsync(
            "15550001111",
            "Pick one",
            [new ReplyButton("id-1", "Yes"), new ReplyButton("id-2", "No")]);

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal(ExpectedRequestUri(options), handler.LastRequest.RequestUri!.ToString());
        var body = await ReadRequestBodyAsync(handler.LastRequest);
        Assert.Contains("\"type\":\"interactive\"", body, StringComparison.Ordinal);
        Assert.Contains("\"interactive\":{\"type\":\"button\"", body, StringComparison.Ordinal);
        Assert.Contains(
            "\"action\":{\"buttons\":[{\"type\":\"reply\",\"reply\":{\"id\":\"id-1\",\"title\":\"Yes\"}},{\"type\":\"reply\",\"reply\":{\"id\":\"id-2\",\"title\":\"No\"}}]}",
            body,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendReplyButtonsAsync_ReturnsSuccess_WithMessageId_On2xxResponse()
    {
        var options = CreateOptions();
        var (client, _) = CreateClient(options, _ => JsonResponse(HttpStatusCode.OK, SuccessBody));

        var result = await client.SendReplyButtonsAsync("15550001111", "Pick one", [new ReplyButton("id-1", "Yes")]);

        AssertSuccess(result);
    }

    [Fact]
    public async Task SendReplyButtonsAsync_ReturnsFailure_WithDeserializedError_OnNon2xxResponse()
    {
        var options = CreateOptions();
        var (client, _) = CreateClient(options, _ => JsonResponse(HttpStatusCode.BadRequest, ErrorBody));

        var result = await client.SendReplyButtonsAsync("15550001111", "Pick one", [new ReplyButton("id-1", "Yes")]);

        AssertFailure(result);
    }

    [Fact]
    public async Task SendReplyButtonsAsync_IncludesContext_WhenSupplied()
    {
        var options = CreateOptions();
        var (client, handler) = CreateClient(options, _ => JsonResponse(HttpStatusCode.OK, SuccessBody));

        await client.SendReplyButtonsAsync(
            "15550001111",
            "Pick one",
            [new ReplyButton("id-1", "Yes")],
            context: new Context { MessageId = "wamid.prior" });

        var body = await ReadRequestBodyAsync(handler.LastRequest!);
        Assert.Contains("\"context\":{\"message_id\":\"wamid.prior\"}", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendReplyButtonsAsync_ThrowsArgumentException_AndSendsNoRequest_WhenMoreThanThreeButtonsSupplied()
    {
        var options = CreateOptions();
        var (client, handler) = CreateClient(options);

        await Assert.ThrowsAsync<ArgumentException>(async () => await client.SendReplyButtonsAsync(
            "15550001111",
            "Pick one",
            [
                new ReplyButton("id-1", "One"),
                new ReplyButton("id-2", "Two"),
                new ReplyButton("id-3", "Three"),
                new ReplyButton("id-4", "Four"),
            ]));

        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public async Task SendReplyButtonsAsync_ThrowsArgumentException_AndSendsNoRequest_WhenButtonTitleExceeds20Characters()
    {
        var options = CreateOptions();
        var (client, handler) = CreateClient(options);

        await Assert.ThrowsAsync<ArgumentException>(async () => await client.SendReplyButtonsAsync(
            "15550001111",
            "Pick one",
            [new ReplyButton("id-1", "This title is way too long")]));

        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public async Task SendReplyButtonsAsync_ThrowsArgumentException_AndSendsNoRequest_WhenButtonIdHasLeadingOrTrailingWhitespace()
    {
        var options = CreateOptions();
        var (client, handler) = CreateClient(options);

        await Assert.ThrowsAsync<ArgumentException>(async () => await client.SendReplyButtonsAsync(
            "15550001111",
            "Pick one",
            [new ReplyButton(" id-1 ", "Yes")]));

        Assert.Equal(0, handler.RequestCount);
    }

    // ---------- SendListMessageAsync ----------

    [Fact]
    public async Task SendListMessageAsync_SendsPostWithListPayload_ToMessagesEndpoint()
    {
        var options = CreateOptions();
        var (client, handler) = CreateClient(options, _ => JsonResponse(HttpStatusCode.OK, SuccessBody));

        await client.SendListMessageAsync(
            "15550001111",
            "Pick one",
            "Open list",
            [new ListSection("Section 1", [new ListRow("row-1", "Row one", "First row")])]);

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal(ExpectedRequestUri(options), handler.LastRequest.RequestUri!.ToString());
        var body = await ReadRequestBodyAsync(handler.LastRequest);
        Assert.Contains("\"type\":\"interactive\"", body, StringComparison.Ordinal);
        Assert.Contains("\"interactive\":{\"type\":\"list\"", body, StringComparison.Ordinal);
        Assert.Contains(
            "\"action\":{\"button\":\"Open list\",\"sections\":[{\"title\":\"Section 1\",\"rows\":[{\"id\":\"row-1\",\"title\":\"Row one\",\"description\":\"First row\"}]}]}",
            body,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendListMessageAsync_ReturnsSuccess_WithMessageId_On2xxResponse()
    {
        var options = CreateOptions();
        var (client, _) = CreateClient(options, _ => JsonResponse(HttpStatusCode.OK, SuccessBody));

        var result = await client.SendListMessageAsync(
            "15550001111",
            "Pick one",
            "Open list",
            [new ListSection("Section 1", [new ListRow("row-1", "Row one", "First row")])]);

        AssertSuccess(result);
    }

    [Fact]
    public async Task SendListMessageAsync_ReturnsFailure_WithDeserializedError_OnNon2xxResponse()
    {
        var options = CreateOptions();
        var (client, _) = CreateClient(options, _ => JsonResponse(HttpStatusCode.BadRequest, ErrorBody));

        var result = await client.SendListMessageAsync(
            "15550001111",
            "Pick one",
            "Open list",
            [new ListSection("Section 1", [new ListRow("row-1", "Row one", "First row")])]);

        AssertFailure(result);
    }

    [Fact]
    public async Task SendListMessageAsync_IncludesContext_WhenSupplied()
    {
        var options = CreateOptions();
        var (client, handler) = CreateClient(options, _ => JsonResponse(HttpStatusCode.OK, SuccessBody));

        await client.SendListMessageAsync(
            "15550001111",
            "Pick one",
            "Open list",
            [new ListSection("Section 1", [new ListRow("row-1", "Row one", "First row")])],
            context: new Context { MessageId = "wamid.prior" });

        var body = await ReadRequestBodyAsync(handler.LastRequest!);
        Assert.Contains("\"context\":{\"message_id\":\"wamid.prior\"}", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendListMessageAsync_ThrowsArgumentException_AndSendsNoRequest_WhenSectionsIsEmpty()
    {
        var options = CreateOptions();
        var (client, handler) = CreateClient(options);

        await Assert.ThrowsAsync<ArgumentException>(async () => await client.SendListMessageAsync(
            "15550001111",
            "Pick one",
            "Open list",
            []));

        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public async Task SendListMessageAsync_ThrowsArgumentException_AndSendsNoRequest_WhenButtonLabelIsWhitespace()
    {
        var options = CreateOptions();
        var (client, handler) = CreateClient(options);

        await Assert.ThrowsAsync<ArgumentException>(async () => await client.SendListMessageAsync(
            "15550001111",
            "Pick one",
            "   ",
            [new ListSection("Section 1", [new ListRow("row-1", "Row one", "First row")])]));

        Assert.Equal(0, handler.RequestCount);
    }

    // ---------- SendCtaUrlButtonAsync ----------

    [Fact]
    public async Task SendCtaUrlButtonAsync_SendsPostWithCtaUrlPayload_ToMessagesEndpoint()
    {
        var options = CreateOptions();
        var (client, handler) = CreateClient(options, _ => JsonResponse(HttpStatusCode.OK, SuccessBody));

        await client.SendCtaUrlButtonAsync(
            "15550001111",
            "Check this out",
            "Visit site",
            "https://example.test/");

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal(ExpectedRequestUri(options), handler.LastRequest.RequestUri!.ToString());
        var body = await ReadRequestBodyAsync(handler.LastRequest);
        Assert.Contains("\"type\":\"interactive\"", body, StringComparison.Ordinal);
        Assert.Contains("\"interactive\":{\"type\":\"cta_url\"", body, StringComparison.Ordinal);
        Assert.Contains(
            "\"action\":{\"name\":\"cta_url\",\"parameters\":{\"display_text\":\"Visit site\",\"url\":\"https://example.test/\"}}",
            body,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendCtaUrlButtonAsync_ReturnsSuccess_WithMessageId_On2xxResponse()
    {
        var options = CreateOptions();
        var (client, _) = CreateClient(options, _ => JsonResponse(HttpStatusCode.OK, SuccessBody));

        var result = await client.SendCtaUrlButtonAsync("15550001111", "Check this out", "Visit site", "https://example.test/");

        AssertSuccess(result);
    }

    [Fact]
    public async Task SendCtaUrlButtonAsync_ReturnsFailure_WithDeserializedError_OnNon2xxResponse()
    {
        var options = CreateOptions();
        var (client, _) = CreateClient(options, _ => JsonResponse(HttpStatusCode.BadRequest, ErrorBody));

        var result = await client.SendCtaUrlButtonAsync("15550001111", "Check this out", "Visit site", "https://example.test/");

        AssertFailure(result);
    }

    [Fact]
    public async Task SendCtaUrlButtonAsync_IncludesContext_WhenSupplied()
    {
        var options = CreateOptions();
        var (client, handler) = CreateClient(options, _ => JsonResponse(HttpStatusCode.OK, SuccessBody));

        await client.SendCtaUrlButtonAsync(
            "15550001111",
            "Check this out",
            "Visit site",
            "https://example.test/",
            context: new Context { MessageId = "wamid.prior" });

        var body = await ReadRequestBodyAsync(handler.LastRequest!);
        Assert.Contains("\"context\":{\"message_id\":\"wamid.prior\"}", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendCtaUrlButtonAsync_ThrowsArgumentException_AndSendsNoRequest_WhenDisplayTextIsWhitespace()
    {
        var options = CreateOptions();
        var (client, handler) = CreateClient(options);

        await Assert.ThrowsAsync<ArgumentException>(async () => await client.SendCtaUrlButtonAsync(
            "15550001111",
            "Check this out",
            "   ",
            "https://example.test/"));

        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public async Task SendCtaUrlButtonAsync_ThrowsArgumentException_AndSendsNoRequest_WhenUrlIsWhitespace()
    {
        var options = CreateOptions();
        var (client, handler) = CreateClient(options);

        await Assert.ThrowsAsync<ArgumentException>(async () => await client.SendCtaUrlButtonAsync(
            "15550001111",
            "Check this out",
            "Visit site",
            "   "));

        Assert.Equal(0, handler.RequestCount);
    }

    // ---------- phoneNumberIdOverride ----------

    [Fact]
    public async Task SendTextAsync_UsesPhoneNumberIdOverride_WhenSupplied()
    {
        var options = CreateOptions();
        var (client, handler) = CreateClient(options, _ => JsonResponse(HttpStatusCode.OK, SuccessBody));

        await client.SendTextAsync("15550001111", "Hello there", phoneNumberIdOverride: "override-phone-id");

        Assert.Equal(
            string.Format(options.BaseUrl, options.ApiVersion) + "override-phone-id/messages",
            handler.LastRequest!.RequestUri!.ToString());
    }
}
