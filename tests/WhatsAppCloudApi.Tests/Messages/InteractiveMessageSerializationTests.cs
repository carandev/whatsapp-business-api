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

    // ---------- List wire shape ----------

    [Fact]
    public void SendListMessageRequest_Serializes_ThroughGeneratedSerializerContext()
    {
        var request = new SendListMessageRequest(
            "16505551234",
            "Pick one",
            "Open list",
            [new ListSection("Section 1", [new ListRow("row-1", "Row one", "First row")])]);

        var json = JsonSerializer.Serialize(request, WhatsAppJsonSerializerContext.Default.SendListMessageRequest);

        Assert.Contains("\"type\":\"interactive\"", json, StringComparison.Ordinal);
        Assert.Contains("\"interactive\":{\"type\":\"list\"", json, StringComparison.Ordinal);
        Assert.Contains("\"body\":{\"text\":\"Pick one\"}", json, StringComparison.Ordinal);
        Assert.Contains(
            "\"action\":{\"button\":\"Open list\",\"sections\":[{\"title\":\"Section 1\",\"rows\":[{\"id\":\"row-1\",\"title\":\"Row one\",\"description\":\"First row\"}]}]}",
            json,
            StringComparison.Ordinal);
    }

    [Fact]
    public void SendListMessageRequest_OmitsHeaderAndFooter_WhenNotSupplied()
    {
        var request = new SendListMessageRequest(
            "16505551234",
            "Pick one",
            "Open list",
            [new ListSection("Section 1", [new ListRow("row-1", "Row one", "First row")])]);

        var json = JsonSerializer.Serialize(request, WhatsAppJsonSerializerContext.Default.SendListMessageRequest);

        Assert.DoesNotContain("header", json, StringComparison.Ordinal);
        Assert.DoesNotContain("footer", json, StringComparison.Ordinal);
    }

    [Fact]
    public void SendListMessageRequest_IncludesFooter_WhenSupplied()
    {
        var request = new SendListMessageRequest(
            "16505551234",
            "Pick one",
            "Open list",
            [new ListSection("Section 1", [new ListRow("row-1", "Row one", "First row")])],
            footerText: "Choose wisely");

        var json = JsonSerializer.Serialize(request, WhatsAppJsonSerializerContext.Default.SendListMessageRequest);

        Assert.Contains("\"footer\":{\"text\":\"Choose wisely\"}", json, StringComparison.Ordinal);
    }

    [Fact]
    public void SendListMessageRequest_IncludesTextHeader_WhenSupplied()
    {
        var request = new SendListMessageRequest(
            "16505551234",
            "Pick one",
            "Open list",
            [new ListSection("Section 1", [new ListRow("row-1", "Row one", "First row")])],
            headerText: "Header text");

        var json = JsonSerializer.Serialize(request, WhatsAppJsonSerializerContext.Default.SendListMessageRequest);

        Assert.Contains("\"header\":{\"type\":\"text\",\"text\":\"Header text\"}", json, StringComparison.Ordinal);
    }

    [Fact]
    public void SendListMessageRequest_IncludesContext_WhenSupplied()
    {
        var request = new SendListMessageRequest(
            "16505551234",
            "Pick one",
            "Open list",
            [new ListSection("Section 1", [new ListRow("row-1", "Row one", "First row")])],
            context: new Context { MessageId = "wamid.prior" });

        var json = JsonSerializer.Serialize(request, WhatsAppJsonSerializerContext.Default.SendListMessageRequest);

        Assert.Contains("\"context\":{\"message_id\":\"wamid.prior\"}", json, StringComparison.Ordinal);
    }

    [Fact]
    public void ListInteractive_HeaderConstructorParameter_IsTextInteractiveHeaderType_NotTheWiderInteractiveHeaderBase()
    {
        // Compile-time-shape proof: ListInteractive's constructor accepts only TextInteractiveHeader,
        // not the wider InteractiveHeader base used by reply-buttons/CTA-URL. This makes passing an
        // Image/Video/Document header to a list message unrepresentable — there is no thrown-
        // exception runtime path to test because none exists. (The Header property itself is
        // declared as the polymorphic base solely so serialization emits the "type":"text"
        // discriminator — see remarks on ListInteractive.Header.)
        var headerParameter = typeof(ListInteractive).GetConstructors().Single().GetParameters().Single(p => p.Name == "header");

        Assert.Equal(typeof(TextInteractiveHeader), headerParameter.ParameterType);
    }

    // ---------- ListRow / ListSection / ListAction construction validation ----------

    [Fact]
    public void ListRow_Throws_WhenIdHasLeadingOrTrailingWhitespace()
    {
        var ex = Assert.Throws<ArgumentException>(() => new ListRow(" row-1 ", "Title", "Description"));
        Assert.Equal("id", ex.ParamName);
    }

    [Fact]
    public void ListRow_Throws_WhenIdIsNullOrWhiteSpace()
    {
        Assert.Throws<ArgumentException>(() => new ListRow("   ", "Title", "Description"));
    }

    [Fact]
    public void ListRow_Throws_WhenTitleIsNullOrWhiteSpace()
    {
        Assert.Throws<ArgumentException>(() => new ListRow("row-1", "   ", "Description"));
    }

    [Fact]
    public void ListRow_Throws_WhenDescriptionIsNullOrWhiteSpace()
    {
        Assert.Throws<ArgumentException>(() => new ListRow("row-1", "Title", "   "));
    }

    [Fact]
    public void ListSection_Throws_WhenTitleIsNullOrWhiteSpace()
    {
        Assert.Throws<ArgumentException>(() => new ListSection("   ", [new ListRow("row-1", "Title", "Description")]));
    }

    [Fact]
    public void ListSection_Throws_WhenRowsIsEmpty()
    {
        Assert.Throws<ArgumentException>(() => new ListSection("Section 1", []));
    }

    [Fact]
    public void ListAction_Throws_WhenButtonIsNullOrWhiteSpace()
    {
        Assert.Throws<ArgumentException>(() => new ListAction("   ", [new ListSection("Section 1", [new ListRow("row-1", "Title", "Description")])]));
    }

    [Fact]
    public void ListAction_Throws_WhenSectionsIsEmpty()
    {
        Assert.Throws<ArgumentException>(() => new ListAction("Open list", []));
    }

    [Fact]
    public void SendListMessageRequest_Succeeds_WithOneSectionAndOneRow()
    {
        var request = new SendListMessageRequest(
            "16505551234",
            "Pick one",
            "Open list",
            [new ListSection("Section 1", [new ListRow("row-1", "Row one", "First row")])]);

        Assert.Single(request.Interactive.Action.Sections);
        Assert.Single(request.Interactive.Action.Sections[0].Rows);
    }

    // ---------- CTA URL wire shape ----------

    [Fact]
    public void SendCtaUrlMessageRequest_Serializes_ThroughGeneratedSerializerContext()
    {
        var request = new SendCtaUrlMessageRequest(
            "16505551234",
            "Check this out",
            "Visit site",
            "https://example.test/");

        var json = JsonSerializer.Serialize(request, WhatsAppJsonSerializerContext.Default.SendCtaUrlMessageRequest);

        Assert.Contains("\"type\":\"interactive\"", json, StringComparison.Ordinal);
        Assert.Contains("\"interactive\":{\"type\":\"cta_url\"", json, StringComparison.Ordinal);
        Assert.Contains("\"body\":{\"text\":\"Check this out\"}", json, StringComparison.Ordinal);
        Assert.Contains(
            "\"action\":{\"name\":\"cta_url\",\"parameters\":{\"display_text\":\"Visit site\",\"url\":\"https://example.test/\"}}",
            json,
            StringComparison.Ordinal);
    }

    [Fact]
    public void SendCtaUrlMessageRequest_OmitsHeaderAndFooter_WhenNotSupplied()
    {
        var request = new SendCtaUrlMessageRequest(
            "16505551234",
            "Check this out",
            "Visit site",
            "https://example.test/");

        var json = JsonSerializer.Serialize(request, WhatsAppJsonSerializerContext.Default.SendCtaUrlMessageRequest);

        Assert.DoesNotContain("header", json, StringComparison.Ordinal);
        Assert.DoesNotContain("footer", json, StringComparison.Ordinal);
    }

    [Fact]
    public void SendCtaUrlMessageRequest_IncludesFooter_WhenSupplied()
    {
        var request = new SendCtaUrlMessageRequest(
            "16505551234",
            "Check this out",
            "Visit site",
            "https://example.test/",
            footerText: "Limited time");

        var json = JsonSerializer.Serialize(request, WhatsAppJsonSerializerContext.Default.SendCtaUrlMessageRequest);

        Assert.Contains("\"footer\":{\"text\":\"Limited time\"}", json, StringComparison.Ordinal);
    }

    [Fact]
    public void SendCtaUrlMessageRequest_IncludesTextHeader_WhenSupplied()
    {
        var request = new SendCtaUrlMessageRequest(
            "16505551234",
            "Check this out",
            "Visit site",
            "https://example.test/",
            header: new TextInteractiveHeader("Header text"));

        var json = JsonSerializer.Serialize(request, WhatsAppJsonSerializerContext.Default.SendCtaUrlMessageRequest);

        Assert.Contains("\"header\":{\"type\":\"text\",\"text\":\"Header text\"}", json, StringComparison.Ordinal);
    }

    [Fact]
    public void SendCtaUrlMessageRequest_IncludesImageHeader_WhenSupplied()
    {
        var request = new SendCtaUrlMessageRequest(
            "16505551234",
            "Check this out",
            "Visit site",
            "https://example.test/",
            header: new ImageInteractiveHeader(mediaId: "media-1"));

        var json = JsonSerializer.Serialize(request, WhatsAppJsonSerializerContext.Default.SendCtaUrlMessageRequest);

        Assert.Contains("\"header\":{\"type\":\"image\",\"image\":{\"id\":\"media-1\"}}", json, StringComparison.Ordinal);
    }

    [Fact]
    public void SendCtaUrlMessageRequest_IncludesVideoHeader_WhenSupplied()
    {
        var request = new SendCtaUrlMessageRequest(
            "16505551234",
            "Check this out",
            "Visit site",
            "https://example.test/",
            header: new VideoInteractiveHeader(link: "https://example.test/video.mp4"));

        var json = JsonSerializer.Serialize(request, WhatsAppJsonSerializerContext.Default.SendCtaUrlMessageRequest);

        Assert.Contains("\"header\":{\"type\":\"video\",\"video\":{\"link\":\"https://example.test/video.mp4\"}}", json, StringComparison.Ordinal);
    }

    [Fact]
    public void SendCtaUrlMessageRequest_IncludesDocumentHeader_WhenSupplied()
    {
        var request = new SendCtaUrlMessageRequest(
            "16505551234",
            "Check this out",
            "Visit site",
            "https://example.test/",
            header: new DocumentInteractiveHeader(mediaId: "doc-1"));

        var json = JsonSerializer.Serialize(request, WhatsAppJsonSerializerContext.Default.SendCtaUrlMessageRequest);

        Assert.Contains("\"header\":{\"type\":\"document\",\"document\":{\"id\":\"doc-1\"}}", json, StringComparison.Ordinal);
    }

    [Fact]
    public void SendCtaUrlMessageRequest_IncludesContext_WhenSupplied()
    {
        var request = new SendCtaUrlMessageRequest(
            "16505551234",
            "Check this out",
            "Visit site",
            "https://example.test/",
            context: new Context { MessageId = "wamid.prior" });

        var json = JsonSerializer.Serialize(request, WhatsAppJsonSerializerContext.Default.SendCtaUrlMessageRequest);

        Assert.Contains("\"context\":{\"message_id\":\"wamid.prior\"}", json, StringComparison.Ordinal);
    }

    // ---------- CtaUrlParameters construction validation ----------

    [Fact]
    public void CtaUrlParameters_Throws_WhenDisplayTextIsNullOrWhiteSpace()
    {
        Assert.Throws<ArgumentException>(() => new CtaUrlParameters("   ", "https://example.test/"));
    }

    [Fact]
    public void CtaUrlParameters_Throws_WhenUrlIsNullOrWhiteSpace()
    {
        Assert.Throws<ArgumentException>(() => new CtaUrlParameters("Visit site", "   "));
    }

    [Fact]
    public void SendCtaUrlMessageRequest_Succeeds_WithValidDisplayTextAndUrl()
    {
        var request = new SendCtaUrlMessageRequest(
            "16505551234",
            "Check this out",
            "Visit site",
            "https://example.test/");

        Assert.Equal("Visit site", request.Interactive.Action.Parameters.DisplayText);
        Assert.Equal("https://example.test/", request.Interactive.Action.Parameters.Url);
    }
}
