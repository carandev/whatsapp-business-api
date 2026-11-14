using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Carandev.WhatsApp.Sdk.Clients;
using Carandev.WhatsApp.Sdk.Models.Messages;
using Carandev.WhatsApp.Sdk.Models.Messages.Interactive;
using Carandev.WhatsApp.Sdk.Serialization;
using Carandev.WhatsApp.Sdk.Tests.Fakes;

namespace Carandev.WhatsApp.Sdk.Tests.Messages;

/// <summary>
/// Interactive Flow messages (<c>interactive.type: "flow"</c>): wire shape, local validation, and
/// the <see cref="IWhatsAppMessagesClient.SendFlowAsync"/> round trip.
/// </summary>
public class FlowMessageTests
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

    private static (WhatsAppMessagesClient Client, RecordingHandler Handler) CreateClient(HttpStatusCode status = HttpStatusCode.OK, string body = SuccessBody)
    {
        var options = CreateOptions();
        var handler = new RecordingHandler(_ => new HttpResponseMessage(status)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        });
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(string.Format(options.BaseUrl, options.ApiVersion)),
        };
        var whatsAppClient = new WhatsAppClient(httpClient, Options.Create(options), NullLogger<WhatsAppClient>.Instance);
        return (new WhatsAppMessagesClient(whatsAppClient), handler);
    }

    private static string Serialize(SendFlowMessageRequest request) =>
        JsonSerializer.Serialize(request, WhatsAppJsonSerializerContext.Default.SendFlowMessageRequest);

    private static JsonElement Json(string json) => JsonDocument.Parse(json).RootElement.Clone();

    // ---------- wire shape ----------

    [Fact]
    public void Serializes_MinimalFlowById_WithFixedVersionAndNavigateAction()
    {
        var request = new SendFlowMessageRequest(
            "15550001111",
            "Book your appointment",
            new FlowMessageParameters("Book!", flowId: "123456"));

        var json = Serialize(request);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        Assert.Equal("whatsapp", root.GetProperty("messaging_product").GetString());
        Assert.Equal("15550001111", root.GetProperty("to").GetString());
        Assert.Equal("interactive", root.GetProperty("type").GetString());
        Assert.Equal(
            """{"type":"flow","body":{"text":"Book your appointment"},"action":{"name":"flow","parameters":{"flow_message_version":"3","flow_id":"123456","flow_cta":"Book!","flow_action":"navigate"}}}""",
            root.GetProperty("interactive").GetRawText());
    }

    [Fact]
    public void Serializes_AllNavigateParameters_HeaderFooterAndContext()
    {
        var request = new SendFlowMessageRequest(
            "15550001111",
            "Flow message body",
            new FlowMessageParameters(
                "Book!",
                flowName: "appointment_booking_v1",
                flowToken: "AQAAAAACS5FpgQ_cAAAAAD0QI3s.",
                action: FlowAction.Navigate,
                screen: "WELCOME",
                data: Json("""{"product_name":"name","product_price":100}"""),
                mode: FlowMode.Draft),
            headerText: "Flow message header",
            footerText: "Flow message footer",
            context: new Context { MessageId = "wamid.prior" });

        var json = Serialize(request);

        Assert.Contains("\"header\":{\"type\":\"text\",\"text\":\"Flow message header\"}", json, StringComparison.Ordinal);
        Assert.Contains("\"footer\":{\"text\":\"Flow message footer\"}", json, StringComparison.Ordinal);
        Assert.Contains("\"context\":{\"message_id\":\"wamid.prior\"}", json, StringComparison.Ordinal);
        Assert.Contains(
            """"parameters":{"flow_message_version":"3","flow_name":"appointment_booking_v1","flow_token":"AQAAAAACS5FpgQ_cAAAAAD0QI3s.","flow_cta":"Book!","flow_action":"navigate","flow_action_payload":{"screen":"WELCOME","data":{"product_name":"name","product_price":100}},"mode":"draft"}"""",
            json,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Serializes_DataExchange_WithoutPayload_AndPublishedMode()
    {
        var request = new SendFlowMessageRequest(
            "15550001111",
            "Body",
            new FlowMessageParameters("Open", flowId: "1", flowToken: "tok", action: FlowAction.DataExchange, mode: FlowMode.Published));

        Assert.Contains(
            """"parameters":{"flow_message_version":"3","flow_id":"1","flow_token":"tok","flow_cta":"Open","flow_action":"data_exchange","mode":"published"}"""",
            Serialize(request),
            StringComparison.Ordinal);
    }

    // ---------- local validation ----------

    [Fact]
    public void Rejects_BothFlowIdAndFlowName()
    {
        var ex = Assert.Throws<ArgumentException>(() => new FlowMessageParameters("Open", flowId: "1", flowName: "name"));
        Assert.Contains("Exactly one of flowId or flowName", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Rejects_NeitherFlowIdNorFlowName()
    {
        var ex = Assert.Throws<ArgumentException>(() => new FlowMessageParameters("Open"));
        Assert.Contains("Exactly one of flowId or flowName", ex.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Rejects_MissingFlowCta(string? flowCta)
    {
        Assert.ThrowsAny<ArgumentException>(() => new FlowMessageParameters(flowCta!, flowId: "1"));
    }

    [Fact]
    public void Rejects_ScreenWithDataExchange()
    {
        var ex = Assert.Throws<ArgumentException>(() => new FlowMessageParameters("Open", flowId: "1", action: FlowAction.DataExchange, screen: "WELCOME"));
        Assert.Contains("navigate", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Rejects_DataWithDataExchange()
    {
        Assert.Throws<ArgumentException>(() => new FlowMessageParameters("Open", flowId: "1", action: FlowAction.DataExchange, data: Json("""{"a":1}""")));
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("[1]")]
    [InlineData("\"text\"")]
    public void Rejects_DataThatIsNotANonEmptyObject(string data)
    {
        var ex = Assert.Throws<ArgumentException>(() => new FlowMessageParameters("Open", flowId: "1", data: Json(data)));
        Assert.Contains("non-empty JSON object", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Rejects_MissingBodyText()
    {
        Assert.ThrowsAny<ArgumentException>(() => new SendFlowMessageRequest("15550001111", " ", new FlowMessageParameters("Open", flowId: "1")));
    }

    // ---------- client ----------

    [Fact]
    public async Task SendFlowAsync_PostsFlowPayload_ToMessagesEndpoint_AndReturnsMessageId()
    {
        var (client, handler) = CreateClient();

        var result = await client.SendFlowAsync("15550001111", "Body", new FlowMessageParameters("Open", flowId: "1"));

        Assert.True(result.IsSuccess);
        Assert.Equal("wamid.HBg", result.Value!.MessageId);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal("https://graph.facebook.com/v21.0/default-phone-id/messages", handler.LastRequest.RequestUri!.ToString());
        var body = await handler.LastRequest.Content!.ReadAsStringAsync();
        Assert.Contains("\"interactive\":{\"type\":\"flow\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendFlowAsync_UsesPhoneNumberIdOverride()
    {
        var (client, handler) = CreateClient();

        await client.SendFlowAsync("15550001111", "Body", new FlowMessageParameters("Open", flowId: "1"), phoneNumberIdOverride: "other-phone");

        Assert.Equal("https://graph.facebook.com/v21.0/other-phone/messages", handler.LastRequest!.RequestUri!.ToString());
    }

    [Fact]
    public async Task SendFlowAsync_ReturnsFailure_OnGraphError()
    {
        var (client, _) = CreateClient(HttpStatusCode.BadRequest, ErrorBody);

        var result = await client.SendFlowAsync("15550001111", "Body", new FlowMessageParameters("Open", flowId: "1"));

        Assert.False(result.IsSuccess);
        Assert.Equal(100, result.Error!.Code);
    }

    [Fact]
    public async Task SendFlowAsync_ThrowsBeforeSending_WhenFlowIsNull()
    {
        var (client, handler) = CreateClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.SendFlowAsync("15550001111", "Body", null!));
        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public async Task SendFlowAsync_DefaultInterfaceMember_ThrowsNotSupported_ForThirdPartyImplementations()
    {
        IWhatsAppMessagesClient legacy = new LegacyMessagesClient();

        await Assert.ThrowsAsync<NotSupportedException>(() => legacy.SendFlowAsync("1", "Body", new FlowMessageParameters("Open", flowId: "1")));
    }

    private sealed class LegacyMessagesClient : IWhatsAppMessagesClient
    {
        public Task<Result<MessageResponse>> SendTextAsync(string to, string body, bool previewUrl = false, Context? context = null, string? phoneNumberIdOverride = null, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Result<MessageResponse>> SendTemplateAsync(string to, string templateName, string languageCode, IReadOnlyList<TemplateParameterComponent>? components = null, Context? context = null, string? phoneNumberIdOverride = null, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Result<MessageResponse>> SendImageAsync(string to, string? mediaId = null, string? link = null, string? caption = null, Context? context = null, string? phoneNumberIdOverride = null, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Result<MessageResponse>> SendDocumentAsync(string to, string? mediaId = null, string? link = null, string? caption = null, Context? context = null, string? phoneNumberIdOverride = null, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Result<MessageResponse>> SendVideoAsync(string to, string? mediaId = null, string? link = null, string? caption = null, Context? context = null, string? phoneNumberIdOverride = null, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Result<MessageResponse>> SendAudioAsync(string to, string? mediaId = null, string? link = null, Context? context = null, string? phoneNumberIdOverride = null, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Result<MessageResponse>> SendStickerAsync(string to, string? mediaId = null, string? link = null, Context? context = null, string? phoneNumberIdOverride = null, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Result<MessageResponse>> SendLocationAsync(string to, double latitude, double longitude, string? name = null, string? address = null, Context? context = null, string? phoneNumberIdOverride = null, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Result<MessageResponse>> SendContactsAsync(string to, IReadOnlyList<Contact> contacts, Context? context = null, string? phoneNumberIdOverride = null, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Result<MessageResponse>> SendReplyButtonsAsync(string to, string bodyText, IReadOnlyList<ReplyButton> buttons, InteractiveHeader? header = null, string? footerText = null, Context? context = null, string? phoneNumberIdOverride = null, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Result<MessageResponse>> SendListMessageAsync(string to, string bodyText, string buttonLabel, IReadOnlyList<ListSection> sections, string? headerText = null, string? footerText = null, Context? context = null, string? phoneNumberIdOverride = null, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Result<MessageResponse>> SendCtaUrlButtonAsync(string to, string bodyText, string displayText, string url, InteractiveHeader? header = null, string? footerText = null, Context? context = null, string? phoneNumberIdOverride = null, CancellationToken ct = default) => throw new NotImplementedException();
    }
}
