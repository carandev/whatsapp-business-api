using System.Text.Json;
using System.Text.Json.Serialization;
using Carandev.WhatsApp.Sdk.Models.Webhooks;
using Carandev.WhatsApp.Sdk.Serialization;

namespace Carandev.WhatsApp.Sdk.Tests.Webhooks;

/// <summary>
/// Flow replies: an <c>interactive</c> webhook message whose nested type is <c>nfm_reply</c>,
/// carrying the Flow's answers as a JSON string in <c>response_json</c>.
/// </summary>
public class FlowReplyParsingTests
{
    [Fact]
    public void NfmReply_ParsesToTypedModel_WithRawFields()
    {
        var message = Deserialize(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Webhooks", "TestData", "interactive-nfm-reply.json")));

        var interactive = Assert.IsType<InteractiveWebhookMessage>(message);
        var reply = Assert.IsType<WebhookFlowReply>(interactive.Interactive);
        Assert.Equal("flow", reply.NfmReply.Name);
        Assert.Equal("Sent", reply.NfmReply.Body);
        Assert.Equal("""{"flow_token": "booking-42", "date": "2026-11-20", "guests": 2}""", reply.NfmReply.ResponseJson);
        Assert.Equal("gBGGEiRVVgBPAgm7FUgc73noXjo", interactive.Context!.Id);
    }

    [Fact]
    public void NfmReply_ExposesFlowToken_AndParsedResponse()
    {
        var reply = ParseReply("""{\"flow_token\": \"booking-42\", \"guests\": 2}""");

        Assert.Equal("booking-42", reply.NfmReply.FlowToken);
        var response = reply.NfmReply.GetResponse();
        Assert.Equal(JsonValueKind.Object, response.ValueKind);
        Assert.Equal(2, response.GetProperty("guests").GetInt32());
    }

    [Fact]
    public void NfmReply_DeserializesResponse_IntoCallerType()
    {
        var reply = ParseReply("""{\"flow_token\": \"booking-42\", \"date\": \"2026-11-20\", \"guests\": 2}""");

        var booking = reply.NfmReply.GetResponse(FlowReplyTestContext.Default.BookingAnswers);

        Assert.NotNull(booking);
        Assert.Equal("booking-42", booking!.FlowToken);
        Assert.Equal("2026-11-20", booking.Date);
        Assert.Equal(2, booking.Guests);
    }

    [Fact]
    public void NfmReply_FlowTokenIsNull_WhenResponseJsonIsMalformedOrHasNoToken()
    {
        Assert.Null(ParseReply("not json").NfmReply.FlowToken);
        Assert.Null(ParseReply("""{\"guests\": 2}""").NfmReply.FlowToken);
        Assert.Null(ParseReply("""{\"flow_token\": 7}""").NfmReply.FlowToken);
    }

    [Fact]
    public void NfmReply_GetResponse_ThrowsJsonException_WhenResponseJsonIsMalformed()
    {
        var reply = ParseReply("not json");

        Assert.ThrowsAny<JsonException>(() => reply.NfmReply.GetResponse());
    }

    [Fact]
    public void NfmReply_RoundTrips_ThroughGeneratedSerializerContext()
    {
        var reply = ParseReply("""{\"flow_token\": \"t\"}""");

        var json = JsonSerializer.Serialize<WebhookInteractiveReply>(reply, WhatsAppJsonSerializerContext.Default.WebhookInteractiveReply);

        using var document = JsonDocument.Parse(json);
        Assert.Equal("nfm_reply", document.RootElement.GetProperty("type").GetString());
        var reparsed = Assert.IsType<WebhookFlowReply>(JsonSerializer.Deserialize(json, WhatsAppJsonSerializerContext.Default.WebhookInteractiveReply));
        Assert.Equal(reply.NfmReply, reparsed.NfmReply);
    }

    [Fact]
    public void ButtonReply_StillParses_AfterAddingNfmReply()
    {
        var message = Deserialize("""{"from":"1","id":"w","timestamp":"1","type":"interactive","interactive":{"type":"button_reply","button_reply":{"id":"b1","title":"Yes"}}}""");

        var reply = Assert.IsType<WebhookButtonReply>(Assert.IsType<InteractiveWebhookMessage>(message).Interactive);
        Assert.Equal("b1", reply.ButtonReply.Id);
    }

    private static WebhookFlowReply ParseReply(string escapedResponseJson)
    {
        var json = "{\"from\":\"1\",\"id\":\"w\",\"timestamp\":\"1\",\"type\":\"interactive\",\"interactive\":{\"type\":\"nfm_reply\",\"nfm_reply\":{\"name\":\"flow\",\"body\":\"Sent\",\"response_json\":\"" + escapedResponseJson + "\"}}}";
        return Assert.IsType<WebhookFlowReply>(Assert.IsType<InteractiveWebhookMessage>(Deserialize(json)).Interactive);
    }

    private static WebhookMessage Deserialize(string json) =>
        JsonSerializer.Deserialize(json, WhatsAppJsonSerializerContext.Default.WebhookMessage)!;
}

public sealed record BookingAnswers
{
    [JsonPropertyName("flow_token")]
    public string? FlowToken { get; init; }

    [JsonPropertyName("date")]
    public string? Date { get; init; }

    [JsonPropertyName("guests")]
    public int Guests { get; init; }
}

[JsonSerializable(typeof(BookingAnswers))]
internal sealed partial class FlowReplyTestContext : JsonSerializerContext
{
}
