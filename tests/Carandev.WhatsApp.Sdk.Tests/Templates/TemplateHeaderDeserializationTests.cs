using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Carandev.WhatsApp.Sdk.Clients;
using Carandev.WhatsApp.Sdk.Models.Templates;
using Carandev.WhatsApp.Sdk.Serialization;
using Carandev.WhatsApp.Sdk.Tests.Fakes;

namespace Carandev.WhatsApp.Sdk.Tests.Templates;

/// <summary>
/// #57: reading templates whose components contain a <c>HEADER</c> through
/// <see cref="WhatsAppTemplatesClient.ListAsync"/> and <see cref="WhatsAppTemplatesClient.GetAsync"/>.
/// </summary>
public class TemplateHeaderDeserializationTests
{
    private static WhatsAppTemplatesClient CreateClient(string responseJson)
    {
        var options = new WhatsAppOptions
        {
            AccessToken = "test-access-token",
            PhoneNumberId = "default-phone-id",
            WabaId = "default-waba-id",
            ApiVersion = "v21.0",
            BaseUrl = "https://graph.facebook.com/{0}/",
        };
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json"),
        });
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(string.Format(options.BaseUrl, options.ApiVersion)),
        };
        var whatsAppClient = new WhatsAppClient(httpClient, Options.Create(options), NullLogger<WhatsAppClient>.Instance);
        return new WhatsAppTemplatesClient(whatsAppClient, Options.Create(options));
    }

    private static string TemplateJson(string id, string header) =>
        $$"""{"id":"{{id}}","name":"t{{id}}","status":"APPROVED","category":"UTILITY","language":"en_US","components":[{{header}},{"type":"BODY","text":"Hello"}]}""";

    [Fact]
    public async Task ListAsync_DeserializesEachHeaderFormat_ToItsConcreteType()
    {
        var page = "{\"data\":["
            + string.Join(
                ",",
                TemplateJson("1", """{"type":"HEADER","format":"TEXT","text":"Order {{1}}","example":{"header_text":["42"]}}"""),
                TemplateJson("2", """{"type":"HEADER","format":"TEXT","text":"Hi {{first_name}}","example":{"header_text_named_params":[{"param_name":"first_name","example":"Ana"}]}}"""),
                TemplateJson("3", """{"type":"HEADER","format":"IMAGE","example":{"header_handle":["https://example.com/image.png"]}}"""),
                TemplateJson("4", """{"type":"HEADER","format":"VIDEO","example":{"header_handle":["https://example.com/video.mp4"]}}"""),
                TemplateJson("5", """{"type":"HEADER","format":"DOCUMENT","example":{"header_handle":["https://example.com/doc.pdf"]}}"""),
                TemplateJson("6", """{"type":"HEADER","format":"TEXT","text":"Plain header"}"""))
            + "],\"paging\":{\"cursors\":{\"before\":\"b\",\"after\":\"a\"}}}";
        var client = CreateClient(page);

        var result = await client.ListAsync();

        Assert.True(result.IsSuccess);
        var templates = result.Value!.Data!;
        Assert.Equal(6, templates.Count);

        var positional = Assert.IsType<TextHeaderComponent>(templates[0].Components![0]);
        Assert.Equal("Order {{1}}", positional.Text);
        Assert.Equal(["42"], positional.Examples!);
        Assert.Null(positional.NamedExamples);

        var named = Assert.IsType<TextHeaderComponent>(templates[1].Components![0]);
        Assert.Equal("Hi {{first_name}}", named.Text);
        Assert.Null(named.Examples);
        var namedExample = Assert.Single(named.NamedExamples!);
        Assert.Equal("first_name", namedExample.ParamName);
        Assert.Equal("Ana", namedExample.Example);

        var image = Assert.IsType<ImageHeaderComponent>(templates[2].Components![0]);
        Assert.Equal("https://example.com/image.png", image.ExampleHandle);

        var video = Assert.IsType<VideoHeaderComponent>(templates[3].Components![0]);
        Assert.Equal("https://example.com/video.mp4", video.ExampleHandle);

        var document = Assert.IsType<DocumentHeaderComponent>(templates[4].Components![0]);
        Assert.Equal("https://example.com/doc.pdf", document.ExampleHandle);

        var plain = Assert.IsType<TextHeaderComponent>(templates[5].Components![0]);
        Assert.Equal("Plain header", plain.Text);
        Assert.Null(plain.Examples);
        Assert.Null(plain.NamedExamples);

        foreach (var template in templates)
        {
            var body = Assert.IsType<BodyComponent>(template.Components![1]);
            Assert.Equal("Hello", body.Text);
        }
    }

    [Fact]
    public async Task GetAsync_DeserializesHeaderAlongsideBodyFooterAndButtons()
    {
        const string json = """
            {"id":"9","name":"order","status":"APPROVED","category":"UTILITY","language":"en_US","components":[
              {"type":"HEADER","format":"IMAGE","example":{"header_handle":["https://example.com/a.png"]}},
              {"type":"BODY","text":"Hi {{1}}","example":{"body_text":[["Ana"]]}},
              {"type":"FOOTER","text":"Thanks"},
              {"type":"BUTTONS","buttons":[{"type":"URL","text":"Track","url":"https://example.com/{{1}}","example":["abc"]},{"type":"QUICK_REPLY","text":"Stop"}]}
            ]}
            """;
        var client = CreateClient(json);

        var result = await client.GetAsync("9");

        Assert.True(result.IsSuccess);
        var components = result.Value!.Components!;
        Assert.Equal(4, components.Count);
        Assert.Equal("https://example.com/a.png", Assert.IsType<ImageHeaderComponent>(components[0]).ExampleHandle);
        var body = Assert.IsType<BodyComponent>(components[1]);
        Assert.Equal("Hi {{1}}", body.Text);
        Assert.Equal(["Ana"], body.Examples!);
        Assert.Equal("Thanks", Assert.IsType<FooterComponent>(components[2]).Text);
        var buttons = Assert.IsType<ButtonsComponent>(components[3]);
        Assert.Equal(["abc"], Assert.IsType<UrlButton>(buttons.Buttons[0]).Examples!);
        Assert.Equal("Stop", Assert.IsType<QuickReplyButton>(buttons.Buttons[1]).Text);
    }

    [Theory]
    [InlineData("LOCATION")]
    [InlineData("location")]
    [InlineData("GIF")]
    public async Task GetAsync_MapsUnknownHeaderFormat_ToUnknownHeaderComponent(string format)
    {
        var client = CreateClient(TemplateJson("7", $$"""{"type":"HEADER","format":"{{format}}"}"""));

        var result = await client.GetAsync("7");

        Assert.True(result.IsSuccess);
        var components = result.Value!.Components!;
        Assert.Equal(2, components.Count);
        var header = Assert.IsType<UnknownHeaderComponent>(components[0]);
        Assert.Equal(format, header.Format);
        Assert.IsType<BodyComponent>(components[1]);
    }

    [Fact]
    public void UnknownHeaderComponent_SerializesBackToItsFormat()
    {
        var template = JsonSerializer.Deserialize(
            TemplateJson("7", """{"type":"HEADER","format":"LOCATION"}"""),
            WhatsAppJsonSerializerContext.Default.Template)!;
        var request = new CreateTemplateRequest("name", TemplateCategory.Utility, "en_US", template.Components!);

        var json = JsonSerializer.Serialize(request, WhatsAppJsonSerializerContext.Default.CreateTemplateRequest);

        Assert.Equal(
            """{"name":"name","category":"UTILITY","language":"en_US","components":[{"type":"HEADER","format":"LOCATION"},{"type":"BODY","text":"Hello"}]}""",
            json);
    }

    public static TheoryData<TemplateComponent> HeaderKinds() => new()
    {
        new TextHeaderComponent("Order {{1}}") { Examples = ["42"] },
        new TextHeaderComponent("Order {{order_number}}") { NamedExamples = [new("order_number", "42")] },
        new ImageHeaderComponent("4::image=="),
        new VideoHeaderComponent("4::video=="),
        new DocumentHeaderComponent("4::doc=="),
    };

    [Theory]
    [MemberData(nameof(HeaderKinds))]
    public async Task CreatedRequest_RoundTripsThroughGetAsync_ToIdenticalJson(TemplateComponent header)
    {
        var body = header is TextHeaderComponent { NamedExamples: not null }
            ? new BodyComponent("Hi {{first_name}}") { NamedExamples = [new("first_name", "Ana")] }
            : new BodyComponent("Hi {{1}}") { Examples = ["Ana"] };
        var request = new CreateTemplateRequest(
            "order",
            TemplateCategory.Utility,
            "en_US",
            [
                header,
                body,
                new FooterComponent("Thanks"),
                new ButtonsComponent([new UrlButton("Track", "https://example.com/{{1}}") { Examples = ["abc"] }]),
            ]);
        var sent = JsonSerializer.Serialize(request, WhatsAppJsonSerializerContext.Default.CreateTemplateRequest);
        string sentComponents;
        using (var document = JsonDocument.Parse(sent))
        {
            sentComponents = document.RootElement.GetProperty("components").GetRawText();
        }

        var client = CreateClient($$"""{"id":"1","name":"order","status":"APPROVED","category":"UTILITY","language":"en_US","components":{{sentComponents}}}""");
        var read = (await client.GetAsync("1")).Value!;
        var resent = JsonSerializer.Serialize(
            new CreateTemplateRequest(read.Name!, read.Category, read.Language!, read.Components!),
            WhatsAppJsonSerializerContext.Default.CreateTemplateRequest);

        Assert.Equal(sent, resent);
    }

    [Fact]
    public void Template_WithHeader_RoundTripsThroughGeneratedSerializerContext()
    {
        const string json = """{"id":"1","name":"order","category":"UTILITY","language":"en_US","status":"APPROVED","components":[{"type":"HEADER","format":"TEXT","text":"Order {{1}}","example":{"header_text":["42"]}},{"type":"BODY","text":"Hi"}]}""";

        var template = JsonSerializer.Deserialize(json, WhatsAppJsonSerializerContext.Default.Template)!;
        var written = JsonSerializer.Serialize(template, WhatsAppJsonSerializerContext.Default.Template);

        Assert.Equal(json, written);
    }

    [Fact]
    public void CreateTemplateRequest_WithEveryComponentKind_SerializesExactJson()
    {
        var request = new CreateTemplateRequest(
            "order_update",
            TemplateCategory.Utility,
            "en_US",
            [
                new TextHeaderComponent("Order {{order_number}}") { NamedExamples = [new("order_number", "42")] },
                new BodyComponent("Hi {{first_name}}") { NamedExamples = [new("first_name", "Ana")] },
                new FooterComponent("Thanks"),
                new ButtonsComponent(
                [
                    new UrlButton("Track", "https://example.com/{{1}}") { Examples = ["abc"] },
                    new QuickReplyButton("Stop"),
                ]),
            ]);

        var json = JsonSerializer.Serialize(request, WhatsAppJsonSerializerContext.Default.CreateTemplateRequest);

        Assert.Equal(
            """{"name":"order_update","category":"UTILITY","language":"en_US","components":[{"type":"HEADER","format":"TEXT","text":"Order {{order_number}}","example":{"header_text_named_params":[{"param_name":"order_number","example":"42"}]}},{"type":"BODY","text":"Hi {{first_name}}","example":{"body_text_named_params":[{"param_name":"first_name","example":"Ana"}]}},{"type":"FOOTER","text":"Thanks"},{"type":"BUTTONS","buttons":[{"type":"URL","text":"Track","url":"https://example.com/{{1}}","example":["abc"]},{"type":"QUICK_REPLY","text":"Stop"}]}],"parameter_format":"named"}""",
            json);
    }
}
