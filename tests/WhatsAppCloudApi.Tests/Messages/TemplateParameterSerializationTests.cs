using System.Text.Json;
using System.Text.Json.Nodes;
using WhatsAppCloudApi.Models.Messages;
using WhatsAppCloudApi.Serialization;

namespace WhatsAppCloudApi.Tests.Messages;

/// <summary>
/// Verifies the send-time <see cref="TemplateParameterComponent"/>/<see cref="TemplateParameter"/>
/// hierarchy serializes to the exact nested JSON shape Meta documents for
/// <c>SendTemplateAsync</c> — distinct from <c>Models.Templates.TemplateComponent</c>'s
/// definition-time shape.
/// </summary>
/// <remarks>
/// Most cases below use plain reflection-based <see cref="JsonSerializer"/> — established in PR2
/// before this hierarchy was registered on <see cref="WhatsAppJsonSerializerContext"/>.
/// <c>[JsonPolymorphic]</c>/<c>[JsonDerivedType]</c> serialize identically under both the
/// reflection-based serializer and the source-generated context; see
/// <see cref="HeaderParameterComponent_ThroughGeneratedSerializerContext_MatchesReflectionOutput"/>
/// for confirmation now that Phase 6 has registered this hierarchy.
/// </remarks>
public class TemplateParameterSerializationTests
{
    [Fact]
    public void HeaderParameterComponent_ThroughGeneratedSerializerContext_MatchesReflectionOutput()
    {
        var component = new HeaderParameterComponent([new ImageParameter(mediaId: "2:c2FtcGxl")]);

        var reflectionJson = JsonSerializer.Serialize<TemplateParameterComponent>(component);
        var sourceGenJson = JsonSerializer.Serialize<TemplateParameterComponent>(
            component, WhatsAppJsonSerializerContext.Default.TemplateParameterComponent);

        AssertJsonEquals(reflectionJson, sourceGenJson);
    }

    [Fact]
    public void HeaderParameterComponent_WithImageParameter_MatchesMetaDocumentedShape()
    {
        var component = new HeaderParameterComponent([new ImageParameter(mediaId: "2:c2FtcGxl")]);

        var json = JsonSerializer.Serialize<TemplateParameterComponent>(component);

        AssertJsonEquals(
            """
            {"type":"header","parameters":[{"type":"image","image":{"id":"2:c2FtcGxl"}}]}
            """,
            json);
    }

    [Fact]
    public void BodyParameterComponent_WithTextParameter_MatchesMetaDocumentedShape()
    {
        var component = new BodyParameterComponent([new TextParameter("John")]);

        var json = JsonSerializer.Serialize<TemplateParameterComponent>(component);

        AssertJsonEquals(
            """
            {"type":"body","parameters":[{"type":"text","text":"John"}]}
            """,
            json);
    }

    [Fact]
    public void BodyParameterComponent_WithCurrencyParameter_MatchesMetaDocumentedShape()
    {
        var component = new BodyParameterComponent(
            [new CurrencyParameter(new Currency { FallbackValue = "$100.99", Code = "USD", Amount1000 = 100990 })]);

        var json = JsonSerializer.Serialize<TemplateParameterComponent>(component);

        AssertJsonEquals(
            """
            {"type":"body","parameters":[{"type":"currency","currency":{"fallback_value":"$100.99","code":"USD","amount_1000":100990}}]}
            """,
            json);
    }

    [Fact]
    public void BodyParameterComponent_WithDateTimeParameter_MatchesMetaDocumentedShape()
    {
        var component = new BodyParameterComponent(
            [new DateTimeParameter(new TemplateDateTime { FallbackValue = "February 25, 1977" })]);

        var json = JsonSerializer.Serialize<TemplateParameterComponent>(component);

        AssertJsonEquals(
            """
            {"type":"body","parameters":[{"type":"date_time","date_time":{"fallback_value":"February 25, 1977"}}]}
            """,
            json);
    }

    [Fact]
    public void ButtonParameterComponent_QuickReplyWithPayloadParameter_MatchesMetaDocumentedShape()
    {
        var component = new ButtonParameterComponent(
            ButtonParameterSubType.QuickReply, "0", [new PayloadParameter("some-payload")]);

        var json = JsonSerializer.Serialize<TemplateParameterComponent>(component);

        AssertJsonEquals(
            """
            {"type":"button","sub_type":"quick_reply","index":"0","parameters":[{"type":"payload","payload":"some-payload"}]}
            """,
            json);
    }

    [Fact]
    public void ButtonParameterComponent_UrlWithTextParameter_MatchesMetaDocumentedShape()
    {
        var component = new ButtonParameterComponent(
            ButtonParameterSubType.Url, "0", [new TextParameter("customer_id_123")]);

        var json = JsonSerializer.Serialize<TemplateParameterComponent>(component);

        AssertJsonEquals(
            """
            {"type":"button","sub_type":"url","index":"0","parameters":[{"type":"text","text":"customer_id_123"}]}
            """,
            json);
    }

    [Fact]
    public void ImageParameter_Throws_WhenBothMediaIdAndLinkSupplied()
    {
        Assert.Throws<ArgumentException>(() => new ImageParameter(mediaId: "abc", link: "https://example.test/img.jpg"));
    }

    [Fact]
    public void ImageParameter_Throws_WhenNeitherMediaIdNorLinkSupplied()
    {
        Assert.Throws<ArgumentException>(() => new ImageParameter());
    }

    private static void AssertJsonEquals(string expectedJson, string actualJson)
    {
        var expected = JsonNode.Parse(expectedJson);
        var actual = JsonNode.Parse(actualJson);

        Assert.True(
            JsonNode.DeepEquals(expected, actual),
            $"Expected JSON:{Environment.NewLine}{expected}{Environment.NewLine}Actual JSON:{Environment.NewLine}{actual}");
    }
}
