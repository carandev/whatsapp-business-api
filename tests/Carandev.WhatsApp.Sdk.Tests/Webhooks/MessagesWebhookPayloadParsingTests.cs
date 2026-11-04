using System.Text.Json;
using Carandev.WhatsApp.Sdk.Models.Webhooks;
using Carandev.WhatsApp.Sdk.Serialization;

namespace Carandev.WhatsApp.Sdk.Tests.Webhooks;

/// <summary>
/// Golden-JSON parsing tests for every documented inbound message kind, the unrecognized-type
/// fallback, and the full <see cref="MessagesWebhookPayload"/> envelope
/// (<c>entry[].changes[].value</c>) nesting. Fixtures live under
/// <c>Webhooks/TestData/*.json</c> and are copied to the test output directory.
/// </summary>
public class MessagesWebhookPayloadParsingTests
{
    [Fact]
    public void Text_ParsesToTypedModel()
    {
        var message = Deserialize("text.json");

        var text = Assert.IsType<TextWebhookMessage>(message);
        Assert.Equal("16315551234", text.From);
        Assert.Equal("1603059201", text.Timestamp);
        Assert.Equal("Hello, this is a text message", text.Text.Body);
    }

    [Fact]
    public void Image_ParsesToTypedModel()
    {
        var message = Deserialize("image.json");

        var image = Assert.IsType<ImageWebhookMessage>(message);
        Assert.Equal("image/jpeg", image.Image.MimeType);
        Assert.Equal("1234567890", image.Image.Id);
        Assert.Equal("Check this out", image.Image.Caption);
    }

    [Fact]
    public void Document_ParsesToTypedModel()
    {
        var message = Deserialize("document.json");

        var document = Assert.IsType<DocumentWebhookMessage>(message);
        Assert.Equal("application/pdf", document.Document.MimeType);
        Assert.Equal("invoice.pdf", document.Document.Filename);
        Assert.Equal("Your invoice", document.Document.Caption);
    }

    [Fact]
    public void Audio_ParsesToTypedModel()
    {
        var message = Deserialize("audio.json");

        var audio = Assert.IsType<AudioWebhookMessage>(message);
        Assert.Equal("audio/ogg; codecs=opus", audio.Audio.MimeType);
        Assert.True(audio.Audio.Voice);
    }

    [Fact]
    public void Video_ParsesToTypedModel()
    {
        var message = Deserialize("video.json");

        var video = Assert.IsType<VideoWebhookMessage>(message);
        Assert.Equal("video/mp4", video.Video.MimeType);
        Assert.Equal("Watch this", video.Video.Caption);
    }

    [Fact]
    public void Sticker_ParsesToTypedModel()
    {
        var message = Deserialize("sticker.json");

        var sticker = Assert.IsType<StickerWebhookMessage>(message);
        Assert.Equal("image/webp", sticker.Sticker.MimeType);
        Assert.False(sticker.Sticker.Animated);
    }

    [Fact]
    public void Location_ParsesToTypedModel()
    {
        var message = Deserialize("location.json");

        var location = Assert.IsType<LocationWebhookMessage>(message);
        Assert.Equal(37.483499, location.Location.Latitude);
        Assert.Equal(-122.148499, location.Location.Longitude);
        Assert.Equal("Facebook HQ", location.Location.Name);
        Assert.Equal("1 Hacker Way, Menlo Park, CA", location.Location.Address);
    }

    [Fact]
    public void Contacts_ParsesToTypedModel()
    {
        var message = Deserialize("contacts.json");

        var contacts = Assert.IsType<ContactsWebhookMessage>(message);
        Assert.Single(contacts.Contacts);
        Assert.Equal("Jane Doe", contacts.Contacts[0].Name?.FormattedName);
        Assert.Equal("+16315559876", contacts.Contacts[0].Phones?[0].Phone);
        Assert.Equal("16315559876", contacts.Contacts[0].Phones?[0].WaId);
    }

    [Fact]
    public void Button_ParsesToTypedModel()
    {
        var message = Deserialize("button.json");

        var button = Assert.IsType<ButtonWebhookMessage>(message);
        Assert.Equal("Yes-Button-Payload", button.Button.Payload);
        Assert.Equal("Yes", button.Button.Text);
        Assert.Equal("wamid.ORIGINAL_TEMPLATE_MESSAGE", button.Context?.Id);
    }

    [Fact]
    public void InteractiveButtonReply_ParsesToTypedModel()
    {
        var message = Deserialize("interactive-button-reply.json");

        var interactive = Assert.IsType<InteractiveWebhookMessage>(message);
        var buttonReply = Assert.IsType<WebhookButtonReply>(interactive.Interactive);
        Assert.Equal("unique-button-id", buttonReply.ButtonReply.Id);
        Assert.Equal("Confirm", buttonReply.ButtonReply.Title);
    }

    [Fact]
    public void InteractiveListReply_ParsesToTypedModel()
    {
        var message = Deserialize("interactive-list-reply.json");

        var interactive = Assert.IsType<InteractiveWebhookMessage>(message);
        var listReply = Assert.IsType<WebhookListReply>(interactive.Interactive);
        Assert.Equal("unique-row-id", listReply.ListReply.Id);
        Assert.Equal("Option A", listReply.ListReply.Title);
        Assert.Equal("The first option", listReply.ListReply.Description);
    }

    [Fact]
    public void Reaction_ParsesToTypedModel()
    {
        var message = Deserialize("reaction.json");

        var reaction = Assert.IsType<ReactionWebhookMessage>(message);
        Assert.Equal("wamid.REACTED_TO_MESSAGE", reaction.Reaction.MessageId);
        Assert.Equal("👍", reaction.Reaction.Emoji);
    }

    [Fact]
    public void RequestWelcome_ParsesToTypedModel()
    {
        var message = Deserialize("request-welcome.json");

        var requestWelcome = Assert.IsType<RequestWelcomeWebhookMessage>(message);
        Assert.Equal("16315551234", requestWelcome.From);
        Assert.Equal("wamid.ID_REQUEST_WELCOME", requestWelcome.Id);
    }

    [Fact]
    public void System_ParsesToTypedModel()
    {
        var message = Deserialize("system.json");

        var system = Assert.IsType<SystemWebhookMessage>(message);
        Assert.Equal("customer_changed_number", system.System.Type);
        Assert.Equal("16315551234", system.System.WaId);
        Assert.Equal("16315551234", system.System.NewWaId);
    }

    [Fact]
    public void UnrecognizedType_FallsBackToUnknown_WithoutThrowing()
    {
        var message = Deserialize("unknown-type.json");

        Assert.IsType<WebhookMessage>(message, exactMatch: true);
        Assert.Equal("16315551234", message.From);
        Assert.NotNull(message.Extra);
        Assert.Contains("order", message.Extra!.Keys);
    }

    [Fact]
    public void FullEnvelope_ParsesNestedEntryChangesValueStructure()
    {
        var json = LoadFixture("full-envelope.json");

        var envelope = JsonSerializer.Deserialize(json, WhatsAppJsonSerializerContext.Default.WebhookEnvelope);

        Assert.NotNull(envelope);
        Assert.Equal("whatsapp_business_account", envelope!.Object);
        Assert.NotNull(envelope.Entry);
        var entry = Assert.Single(envelope.Entry!);
        Assert.Equal("102290129340398", entry.Id);
        var change = Assert.Single(entry.Changes!);
        Assert.Equal("messages", change.Field);

        var payload = JsonSerializer.Deserialize(change.Value, WhatsAppJsonSerializerContext.Default.MessagesWebhookPayload);

        Assert.NotNull(payload);
        Assert.Equal("whatsapp", payload!.MessagingProduct);
        Assert.Equal("16505551111", payload.Metadata?.DisplayPhoneNumber);
        Assert.Equal("123456123", payload.Metadata?.PhoneNumberId);
        Assert.NotNull(payload.Contacts);
        Assert.Equal("Kerry Fisher", payload.Contacts![0].Profile?.Name);
        Assert.Equal("16315558151", payload.Contacts[0].WaId);
        Assert.NotNull(payload.Messages);
        var text = Assert.IsType<TextWebhookMessage>(Assert.Single(payload.Messages!));
        Assert.Equal("Hello, this is a text message", text.Text.Body);
    }

    private static WebhookMessage Deserialize(string fixtureFileName)
    {
        var json = LoadFixture(fixtureFileName);
        var message = JsonSerializer.Deserialize(json, WhatsAppJsonSerializerContext.Default.WebhookMessage);
        Assert.NotNull(message);
        return message!;
    }

    private static string LoadFixture(string fixtureFileName) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Webhooks", "TestData", fixtureFileName));
}
