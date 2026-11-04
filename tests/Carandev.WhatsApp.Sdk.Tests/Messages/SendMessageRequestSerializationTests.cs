using System.Text.Json;
using Carandev.WhatsApp.Sdk.Models.Messages;
using Carandev.WhatsApp.Sdk.Serialization;

namespace Carandev.WhatsApp.Sdk.Tests.Messages;

/// <summary>
/// Smoke tests confirming every per-message-kind <see cref="SendMessageRequest"/> derived record
/// is actually wired into the source-generated <see cref="WhatsAppJsonSerializerContext"/> — not
/// merely that the types compile. Catches a missing <c>[JsonSerializable]</c> registration before
/// PR4's <c>WhatsAppMessagesClient</c> tries to serialize through this context.
/// </summary>
public class SendMessageRequestSerializationTests
{
    [Fact]
    public void SendTextMessageRequest_Serializes_ThroughGeneratedSerializerContext()
    {
        var request = new SendTextMessageRequest("16505551234", new TextContent("Hello", previewUrl: true));

        var json = JsonSerializer.Serialize(request, WhatsAppJsonSerializerContext.Default.SendTextMessageRequest);

        Assert.Contains("\"type\":\"text\"", json, StringComparison.Ordinal);
        Assert.Contains("\"body\":\"Hello\"", json, StringComparison.Ordinal);
        Assert.Contains("\"preview_url\":true", json, StringComparison.Ordinal);
    }

    [Fact]
    public void SendTemplateMessageRequest_Serializes_ThroughGeneratedSerializerContext()
    {
        var request = new SendTemplateMessageRequest(
            "16505551234",
            new TemplateReference(
                "order_confirmation",
                new LanguageCode("en_US"),
                [new BodyParameterComponent([new TextParameter("John")])]));

        var json = JsonSerializer.Serialize(request, WhatsAppJsonSerializerContext.Default.SendTemplateMessageRequest);

        Assert.Contains("\"type\":\"template\"", json, StringComparison.Ordinal);
        Assert.Contains("\"name\":\"order_confirmation\"", json, StringComparison.Ordinal);
        Assert.Contains("\"code\":\"en_US\"", json, StringComparison.Ordinal);
        Assert.Contains("\"parameters\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void SendTemplateMessageRequest_OmitsComponents_WhenNull()
    {
        var request = new SendTemplateMessageRequest(
            "16505551234",
            new TemplateReference("no_placeholders", new LanguageCode("en_US")));

        var json = JsonSerializer.Serialize(request, WhatsAppJsonSerializerContext.Default.SendTemplateMessageRequest);

        Assert.DoesNotContain("components", json, StringComparison.Ordinal);
    }

    [Fact]
    public void SendImageMessageRequest_Serializes_ThroughGeneratedSerializerContext()
    {
        var request = new SendImageMessageRequest("16505551234", mediaId: "2:c2FtcGxl", caption: "A photo");

        var json = JsonSerializer.Serialize(request, WhatsAppJsonSerializerContext.Default.SendImageMessageRequest);

        Assert.Contains("\"type\":\"image\"", json, StringComparison.Ordinal);
        Assert.Contains("\"id\":\"2:c2FtcGxl\"", json, StringComparison.Ordinal);
        Assert.Contains("\"caption\":\"A photo\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void SendDocumentMessageRequest_Serializes_ThroughGeneratedSerializerContext()
    {
        var request = new SendDocumentMessageRequest("16505551234", link: "https://example.test/doc.pdf");

        var json = JsonSerializer.Serialize(request, WhatsAppJsonSerializerContext.Default.SendDocumentMessageRequest);

        Assert.Contains("\"type\":\"document\"", json, StringComparison.Ordinal);
        Assert.Contains("\"link\":\"https://example.test/doc.pdf\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("caption", json, StringComparison.Ordinal);
    }

    [Fact]
    public void SendAudioMessageRequest_Serializes_ThroughGeneratedSerializerContext_WithoutCaption()
    {
        var request = new SendAudioMessageRequest("16505551234", mediaId: "audio-id");

        var json = JsonSerializer.Serialize(request, WhatsAppJsonSerializerContext.Default.SendAudioMessageRequest);

        Assert.Contains("\"type\":\"audio\"", json, StringComparison.Ordinal);
        Assert.Contains("\"id\":\"audio-id\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("caption", json, StringComparison.Ordinal);
    }

    [Fact]
    public void SendVideoMessageRequest_Serializes_ThroughGeneratedSerializerContext()
    {
        var request = new SendVideoMessageRequest("16505551234", link: "https://example.test/video.mp4", caption: "A clip");

        var json = JsonSerializer.Serialize(request, WhatsAppJsonSerializerContext.Default.SendVideoMessageRequest);

        Assert.Contains("\"type\":\"video\"", json, StringComparison.Ordinal);
        Assert.Contains("\"caption\":\"A clip\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void SendStickerMessageRequest_Serializes_ThroughGeneratedSerializerContext_WithoutCaption()
    {
        var request = new SendStickerMessageRequest("16505551234", mediaId: "sticker-id");

        var json = JsonSerializer.Serialize(request, WhatsAppJsonSerializerContext.Default.SendStickerMessageRequest);

        Assert.Contains("\"type\":\"sticker\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("caption", json, StringComparison.Ordinal);
    }

    [Fact]
    public void SendLocationMessageRequest_Serializes_ThroughGeneratedSerializerContext()
    {
        var request = new SendLocationMessageRequest(
            "16505551234",
            new Location(37.4847, -122.1477, name: "Meta HQ", address: "1 Hacker Way"));

        var json = JsonSerializer.Serialize(request, WhatsAppJsonSerializerContext.Default.SendLocationMessageRequest);

        Assert.Contains("\"type\":\"location\"", json, StringComparison.Ordinal);
        Assert.Contains("\"latitude\":37.4847", json, StringComparison.Ordinal);
        Assert.Contains("\"longitude\":-122.1477", json, StringComparison.Ordinal);
        Assert.Contains("\"name\":\"Meta HQ\"", json, StringComparison.Ordinal);
        Assert.Contains("\"address\":\"1 Hacker Way\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void SendContactsMessageRequest_Serializes_ThroughGeneratedSerializerContext()
    {
        var request = new SendContactsMessageRequest("16505551234", [new Contact { Birthday = "1990-01-01" }]);

        var json = JsonSerializer.Serialize(request, WhatsAppJsonSerializerContext.Default.SendContactsMessageRequest);

        Assert.Contains("\"type\":\"contacts\"", json, StringComparison.Ordinal);
        Assert.Contains("\"birthday\":\"1990-01-01\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void SendXMessageRequest_IncludesContext_WhenSupplied()
    {
        var request = new SendTextMessageRequest(
            "16505551234", new TextContent("Hi"), new Context { MessageId = "wamid.abc" });

        var json = JsonSerializer.Serialize(request, WhatsAppJsonSerializerContext.Default.SendTextMessageRequest);

        Assert.Contains("\"context\":{\"message_id\":\"wamid.abc\"}", json, StringComparison.Ordinal);
    }

    [Fact]
    public void MessageResponse_Serializes_ThroughGeneratedSerializerContext()
    {
        var response = new MessageResponse { MessageId = "wamid.abc" };

        var json = JsonSerializer.Serialize(response, WhatsAppJsonSerializerContext.Default.MessageResponse);

        Assert.Contains("wamid.abc", json, StringComparison.Ordinal);
    }
}
