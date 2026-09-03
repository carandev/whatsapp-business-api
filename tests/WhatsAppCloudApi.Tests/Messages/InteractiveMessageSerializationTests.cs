using System.Text.Json;
using WhatsAppCloudApi.Models.Messages;
using WhatsAppCloudApi.Models.Messages.Interactive;
using WhatsAppCloudApi.Serialization;

namespace WhatsAppCloudApi.Tests.Messages;

/// <summary>
/// Golden-JSON serialization tests and construction-validation tests for the interactive reply-
/// buttons message hierarchy, mirroring <see cref="SendMessageRequestSerializationTests"/>.
/// </summary>
public class InteractiveMessageSerializationTests
{
    // ---------- Reply buttons wire shape ----------

    [Fact]
    public void SendReplyButtonsMessageRequest_Serializes_ThroughGeneratedSerializerContext()
    {
        var request = new SendReplyButtonsMessageRequest(
            "16505551234",
            "Pick one",
            [new ReplyButton("id-1", "Yes"), new ReplyButton("id-2", "No")]);

        var json = JsonSerializer.Serialize(request, WhatsAppJsonSerializerContext.Default.SendReplyButtonsMessageRequest);

        Assert.Contains("\"type\":\"interactive\"", json, StringComparison.Ordinal);
        Assert.Contains("\"interactive\":{\"type\":\"button\"", json, StringComparison.Ordinal);
        Assert.Contains("\"body\":{\"text\":\"Pick one\"}", json, StringComparison.Ordinal);
        Assert.Contains(
            "\"action\":{\"buttons\":[{\"type\":\"reply\",\"reply\":{\"id\":\"id-1\",\"title\":\"Yes\"}},{\"type\":\"reply\",\"reply\":{\"id\":\"id-2\",\"title\":\"No\"}}]}",
            json,
            StringComparison.Ordinal);
    }

    [Fact]
    public void SendReplyButtonsMessageRequest_OmitsHeaderAndFooter_WhenNotSupplied()
    {
        var request = new SendReplyButtonsMessageRequest(
            "16505551234",
            "Pick one",
            [new ReplyButton("id-1", "Yes")]);

        var json = JsonSerializer.Serialize(request, WhatsAppJsonSerializerContext.Default.SendReplyButtonsMessageRequest);

        Assert.DoesNotContain("header", json, StringComparison.Ordinal);
        Assert.DoesNotContain("footer", json, StringComparison.Ordinal);
    }

    [Fact]
    public void SendReplyButtonsMessageRequest_IncludesFooter_WhenSupplied()
    {
        var request = new SendReplyButtonsMessageRequest(
            "16505551234",
            "Pick one",
            [new ReplyButton("id-1", "Yes")],
            footerText: "Choose wisely");

        var json = JsonSerializer.Serialize(request, WhatsAppJsonSerializerContext.Default.SendReplyButtonsMessageRequest);

        Assert.Contains("\"footer\":{\"text\":\"Choose wisely\"}", json, StringComparison.Ordinal);
    }

    // ---------- Header kinds ----------

    [Fact]
    public void SendReplyButtonsMessageRequest_IncludesTextHeader_WhenSupplied()
    {
        var request = new SendReplyButtonsMessageRequest(
            "16505551234",
            "Pick one",
            [new ReplyButton("id-1", "Yes")],
            header: new TextInteractiveHeader("Header text"));

        var json = JsonSerializer.Serialize(request, WhatsAppJsonSerializerContext.Default.SendReplyButtonsMessageRequest);

        Assert.Contains("\"header\":{\"type\":\"text\",\"text\":\"Header text\"}", json, StringComparison.Ordinal);
    }

    [Fact]
    public void SendReplyButtonsMessageRequest_IncludesImageHeader_WhenSupplied()
    {
        var request = new SendReplyButtonsMessageRequest(
            "16505551234",
            "Pick one",
            [new ReplyButton("id-1", "Yes")],
            header: new ImageInteractiveHeader(mediaId: "media-1"));

        var json = JsonSerializer.Serialize(request, WhatsAppJsonSerializerContext.Default.SendReplyButtonsMessageRequest);

        Assert.Contains("\"header\":{\"type\":\"image\",\"image\":{\"id\":\"media-1\"}}", json, StringComparison.Ordinal);
    }

    [Fact]
    public void SendReplyButtonsMessageRequest_IncludesVideoHeader_WhenSupplied()
    {
        var request = new SendReplyButtonsMessageRequest(
            "16505551234",
            "Pick one",
            [new ReplyButton("id-1", "Yes")],
            header: new VideoInteractiveHeader(link: "https://example.test/video.mp4"));

        var json = JsonSerializer.Serialize(request, WhatsAppJsonSerializerContext.Default.SendReplyButtonsMessageRequest);

        Assert.Contains("\"header\":{\"type\":\"video\",\"video\":{\"link\":\"https://example.test/video.mp4\"}}", json, StringComparison.Ordinal);
    }

    [Fact]
    public void SendReplyButtonsMessageRequest_IncludesDocumentHeader_WhenSupplied()
    {
        var request = new SendReplyButtonsMessageRequest(
            "16505551234",
            "Pick one",
            [new ReplyButton("id-1", "Yes")],
            header: new DocumentInteractiveHeader(mediaId: "doc-1"));

        var json = JsonSerializer.Serialize(request, WhatsAppJsonSerializerContext.Default.SendReplyButtonsMessageRequest);

        Assert.Contains("\"header\":{\"type\":\"document\",\"document\":{\"id\":\"doc-1\"}}", json, StringComparison.Ordinal);
    }

    [Fact]
    public void SendReplyButtonsMessageRequest_IncludesContext_WhenSupplied()
    {
        var request = new SendReplyButtonsMessageRequest(
            "16505551234",
            "Pick one",
            [new ReplyButton("id-1", "Yes")],
            context: new Context { MessageId = "wamid.prior" });

        var json = JsonSerializer.Serialize(request, WhatsAppJsonSerializerContext.Default.SendReplyButtonsMessageRequest);

        Assert.Contains("\"context\":{\"message_id\":\"wamid.prior\"}", json, StringComparison.Ordinal);
    }

    // ---------- ReplyButton construction validation ----------

    [Fact]
    public void ReplyButton_Throws_WhenTitleExceeds20Characters()
    {
        var ex = Assert.Throws<ArgumentException>(() => new ReplyButton("id-1", "This title is way too long"));
        Assert.Equal("title", ex.ParamName);
    }

    [Fact]
    public void ReplyButton_Throws_WhenIdHasLeadingOrTrailingWhitespace()
    {
        var ex = Assert.Throws<ArgumentException>(() => new ReplyButton(" id-1 ", "Yes"));
        Assert.Equal("id", ex.ParamName);
    }

    [Fact]
    public void ReplyButton_Throws_WhenIdIsNullOrWhiteSpace()
    {
        Assert.Throws<ArgumentException>(() => new ReplyButton("   ", "Yes"));
    }

    [Fact]
    public void ReplyButton_Throws_WhenTitleIsNullOrWhiteSpace()
    {
        Assert.Throws<ArgumentException>(() => new ReplyButton("id-1", "   "));
    }

    [Fact]
    public void ReplyButton_Succeeds_WhenTitleIsExactly20Characters()
    {
        var button = new ReplyButton("id-1", "12345678901234567890");
        Assert.Equal("12345678901234567890", button.Title);
    }

    // ---------- ReplyButtonsAction / SendReplyButtonsMessageRequest construction validation ----------

    [Fact]
    public void SendReplyButtonsMessageRequest_Throws_WhenMoreThanThreeButtonsSupplied()
    {
        Assert.Throws<ArgumentException>(() => new SendReplyButtonsMessageRequest(
            "16505551234",
            "Pick one",
            [
                new ReplyButton("id-1", "One"),
                new ReplyButton("id-2", "Two"),
                new ReplyButton("id-3", "Three"),
                new ReplyButton("id-4", "Four"),
            ]));
    }

    [Fact]
    public void SendReplyButtonsMessageRequest_Throws_WhenNoButtonsSupplied()
    {
        Assert.Throws<ArgumentException>(() => new SendReplyButtonsMessageRequest(
            "16505551234",
            "Pick one",
            []));
    }

    [Fact]
    public void SendReplyButtonsMessageRequest_Succeeds_WithExactlyThreeButtons()
    {
        var request = new SendReplyButtonsMessageRequest(
            "16505551234",
            "Pick one",
            [
                new ReplyButton("id-1", "One"),
                new ReplyButton("id-2", "Two"),
                new ReplyButton("id-3", "Three"),
            ]);

        Assert.Equal(3, request.Interactive.Action.Buttons.Count);
    }
}
