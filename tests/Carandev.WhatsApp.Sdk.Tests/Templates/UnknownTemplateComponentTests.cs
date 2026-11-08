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
/// #63: reading templates whose components or buttons use a type the SDK does not model.
/// </summary>
public class UnknownTemplateComponentTests
{
    private const string CarouselComponent =
        """{"type":"CAROUSEL","cards":[{"components":[{"type":"HEADER","format":"IMAGE","example":{"header_handle":["https://example.com/a.png"]}},{"type":"BODY","text":"Card 1"},{"type":"BUTTONS","buttons":[{"type":"QUICK_REPLY","text":"Buy"}]}]}]}""";

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

    private static string TemplateJson(string id, string components) =>
        $$"""{"id":"{{id}}","name":"t{{id}}","status":"APPROVED","category":"MARKETING","language":"en_US","components":[{{components}}]}""";

    [Fact]
    public async Task ListAsync_WithCarouselAndUnknownComponents_KeepsEveryTemplateIntact()
    {
        var page = "{\"data\":["
            + string.Join(
                ",",
                TemplateJson("1", """{"type":"BODY","text":"Hello"}"""),
                TemplateJson("2", """{"type":"BODY","text":"Pick one"},""" + CarouselComponent),
                TemplateJson("3", """{"type":"LIMITED_TIME_OFFER","limited_time_offer":{"text":"Ends soon","has_expiration":true}},{"type":"FOOTER","text":"Bye"}"""))
            + "]}";
        var client = CreateClient(page);

        var result = await client.ListAsync();

        Assert.True(result.IsSuccess);
        var templates = result.Value!.Data!;
        Assert.Equal(3, templates.Count);

        Assert.Equal("Hello", Assert.IsType<BodyComponent>(Assert.Single(templates[0].Components!)).Text);

        Assert.Equal(2, templates[1].Components!.Count);
        Assert.Equal("Pick one", Assert.IsType<BodyComponent>(templates[1].Components![0]).Text);
        var carousel = Assert.IsType<UnknownTemplateComponent>(templates[1].Components![1]);
        Assert.Equal("CAROUSEL", carousel.Type);
        Assert.Equal(CarouselComponent, carousel.RawJson.GetRawText());

        var offer = Assert.IsType<UnknownTemplateComponent>(templates[2].Components![0]);
        Assert.Equal("LIMITED_TIME_OFFER", offer.Type);
        Assert.Equal("Bye", Assert.IsType<FooterComponent>(templates[2].Components![1]).Text);
    }

    [Fact]
    public async Task GetAsync_WithCarousel_ReturnsUnknownComponent()
    {
        var client = CreateClient(TemplateJson("2", CarouselComponent));

        var result = await client.GetAsync("2");

        Assert.True(result.IsSuccess);
        var carousel = Assert.IsType<UnknownTemplateComponent>(Assert.Single(result.Value!.Components!));
        Assert.Equal("CAROUSEL", carousel.Type);
    }

    [Fact]
    public async Task GetAsync_WithUnknownButtonType_KeepsOtherButtons()
    {
        var client = CreateClient(TemplateJson(
            "4",
            """{"type":"BUTTONS","buttons":[{"type":"QUICK_REPLY","text":"Yes"},{"type":"VOICE_CALL","text":"Call us"},{"type":"URL","text":"Open","url":"https://example.com"}]}"""));

        var result = await client.GetAsync("4");

        Assert.True(result.IsSuccess);
        var buttons = Assert.IsType<ButtonsComponent>(Assert.Single(result.Value!.Components!)).Buttons;
        Assert.Equal(3, buttons.Count);
        Assert.Equal("Yes", Assert.IsType<QuickReplyButton>(buttons[0]).Text);
        var unknown = Assert.IsType<UnknownTemplateButton>(buttons[1]);
        Assert.Equal("VOICE_CALL", unknown.Type);
        Assert.Equal("""{"type":"VOICE_CALL","text":"Call us"}""", unknown.RawJson.GetRawText());
        Assert.Equal("https://example.com", Assert.IsType<UrlButton>(buttons[2]).Url);
    }

    [Fact]
    public async Task GetAsync_WithComponentMissingType_ReturnsUnknownComponentWithEmptyType()
    {
        var client = CreateClient(TemplateJson("5", """{"text":"no type"}"""));

        var result = await client.GetAsync("5");

        Assert.True(result.IsSuccess);
        var unknown = Assert.IsType<UnknownTemplateComponent>(Assert.Single(result.Value!.Components!));
        Assert.Equal(string.Empty, unknown.Type);
    }

    [Fact]
    public void UnknownComponentsAndButtons_SerializeBackToTheirRawJson()
    {
        var buttons = """{"type":"BUTTONS","buttons":[{"type":"VOICE_CALL","text":"Call us"},{"type":"QUICK_REPLY","text":"Yes"}]}""";
        var template = JsonSerializer.Deserialize(
            TemplateJson("2", """{"type":"BODY","text":"Hi"},""" + CarouselComponent + "," + buttons),
            WhatsAppJsonSerializerContext.Default.Template)!;
        var request = new CreateTemplateRequest("name", TemplateCategory.Marketing, "en_US", template.Components!);

        var json = JsonSerializer.Serialize(request, WhatsAppJsonSerializerContext.Default.CreateTemplateRequest);

        Assert.Equal(
            """{"name":"name","category":"MARKETING","language":"en_US","components":[{"type":"BODY","text":"Hi"},""" + CarouselComponent + "," + buttons + "]}",
            json);
    }
}
