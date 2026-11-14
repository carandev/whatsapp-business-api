using System.Text.Json;
using Carandev.WhatsApp.Sdk.Models.Webhooks;
using Carandev.WhatsApp.Sdk.Serialization;

namespace Carandev.WhatsApp.Sdk.Tests.Webhooks;

/// <summary>
/// Pins that serializing a <see cref="WebhookMessage"/> through <see cref="WebhookMessageConverter"/>
/// emits the same <c>type</c> discriminator it reads, so <c>Serialize</c> followed by
/// <c>Deserialize</c> returns the same concrete type (issue #72).
/// </summary>
public class WebhookMessageRoundTripTests
{
    public static TheoryData<string, string, Type> Fixtures => new()
    {
        { "text.json", "text", typeof(TextWebhookMessage) },
        { "image.json", "image", typeof(ImageWebhookMessage) },
        { "document.json", "document", typeof(DocumentWebhookMessage) },
        { "audio.json", "audio", typeof(AudioWebhookMessage) },
        { "video.json", "video", typeof(VideoWebhookMessage) },
        { "sticker.json", "sticker", typeof(StickerWebhookMessage) },
        { "location.json", "location", typeof(LocationWebhookMessage) },
        { "contacts.json", "contacts", typeof(ContactsWebhookMessage) },
        { "button.json", "button", typeof(ButtonWebhookMessage) },
        { "interactive-button-reply.json", "interactive", typeof(InteractiveWebhookMessage) },
        { "interactive-list-reply.json", "interactive", typeof(InteractiveWebhookMessage) },
        { "interactive-nfm-reply.json", "interactive", typeof(InteractiveWebhookMessage) },
        { "reaction.json", "reaction", typeof(ReactionWebhookMessage) },
        { "request-welcome.json", "request_welcome", typeof(RequestWelcomeWebhookMessage) },
        { "system.json", "system", typeof(SystemWebhookMessage) },
        { "unknown-type.json", "order", typeof(WebhookMessage) },
    };

    [Theory]
    [MemberData(nameof(Fixtures))]
    public void Serialize_ThenDeserialize_ReturnsSameConcreteType(string fixture, string expectedType, Type expectedClrType)
    {
        var original = Deserialize(LoadFixture(fixture));

        var json = Serialize(original);
        var roundTripped = Deserialize(json);

        using var document = JsonDocument.Parse(json);
        Assert.Equal(expectedType, document.RootElement.GetProperty("type").GetString());
        Assert.Equal(expectedClrType, roundTripped.GetType());
        Assert.Equal(expectedType, roundTripped.Type);
        Assert.Equal(original.From, roundTripped.From);
        Assert.Equal(original.Id, roundTripped.Id);
        Assert.Equal(original.Timestamp, roundTripped.Timestamp);
        Assert.Equal(json, Serialize(roundTripped));
    }

    [Theory]
    [MemberData(nameof(Fixtures))]
    public void Deserialize_ExposesWireTypeOnMessage(string fixture, string expectedType, Type expectedClrType)
    {
        _ = expectedClrType;

        var message = Deserialize(LoadFixture(fixture));

        Assert.Equal(expectedType, message.Type);
    }

    [Fact]
    public void Serialize_ConstructedImageMessage_EmitsTypeAndRoundTripsMediaId()
    {
        WebhookMessage message = new ImageWebhookMessage
        {
            From = "16315551234",
            Id = "wamid.ID_IMAGE",
            Timestamp = "1603059202",
            Image = new WebhookMediaContent { Id = "1234567890", MimeType = "image/jpeg", Sha256 = "abc" },
        };

        var json = Serialize(message);
        var roundTripped = Deserialize(json);

        Assert.Contains("\"type\":\"image\"", json, StringComparison.Ordinal);
        var image = Assert.IsType<ImageWebhookMessage>(roundTripped);
        Assert.Equal("1234567890", image.Image.Id);
    }

    [Fact]
    public void Serialize_UnknownType_PreservesTypeAndExtraFields()
    {
        var original = Deserialize(LoadFixture("unknown-type.json"));

        var roundTripped = Deserialize(Serialize(original));

        Assert.IsType<WebhookMessage>(roundTripped, exactMatch: true);
        Assert.Equal("order", roundTripped.Type);
        Assert.NotNull(roundTripped.Extra);
        Assert.Equal(["order"], roundTripped.Extra!.Keys);
        Assert.Equal("123456", roundTripped.Extra["order"].GetProperty("catalog_id").GetString());
    }

    [Fact]
    public void Serialize_PayloadWithMessages_RoundTripsConcreteTypes()
    {
        var envelope = JsonSerializer.Deserialize(LoadFixture("full-envelope.json"), WhatsAppJsonSerializerContext.Default.WebhookEnvelope)!;
        var payload = JsonSerializer.Deserialize(envelope.Entry![0].Changes![0].Value, WhatsAppJsonSerializerContext.Default.MessagesWebhookPayload)!;

        var json = JsonSerializer.Serialize(payload, WhatsAppJsonSerializerContext.Default.MessagesWebhookPayload);
        var roundTripped = JsonSerializer.Deserialize(json, WhatsAppJsonSerializerContext.Default.MessagesWebhookPayload)!;

        Assert.Equal(
            payload.Messages!.Select(m => m.GetType()),
            roundTripped.Messages!.Select(m => m.GetType()));
    }

    private static string Serialize(WebhookMessage message) =>
        JsonSerializer.Serialize(message, WhatsAppJsonSerializerContext.Default.WebhookMessage);

    private static WebhookMessage Deserialize(string json)
    {
        var message = JsonSerializer.Deserialize(json, WhatsAppJsonSerializerContext.Default.WebhookMessage);
        Assert.NotNull(message);
        return message!;
    }

    private static string LoadFixture(string fixtureFileName) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Webhooks", "TestData", fixtureFileName));
}
