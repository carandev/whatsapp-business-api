using System.Text.Json;
using Carandev.WhatsApp.Sdk.Models.Templates;
using Carandev.WhatsApp.Sdk.Serialization;

namespace Carandev.WhatsApp.Sdk.Tests.Templates;

/// <summary>
/// Pins the exact wire JSON of template components and their examples, serialized through the
/// same <see cref="WhatsAppJsonSerializerContext.CreateTemplateRequest"/> path the client uses.
/// </summary>
public class TemplateComponentExampleSerializationTests
{
    private static string SerializeComponent(TemplateComponent component)
    {
        TemplateComponent[] components = component is BodyComponent
            ? [component]
            : [component, new BodyComponent("Body")];
        var request = new CreateTemplateRequest("name", TemplateCategory.Utility, "en_US", components);

        var json = JsonSerializer.Serialize(request, WhatsAppJsonSerializerContext.Default.CreateTemplateRequest);

        using var document = JsonDocument.Parse(json);
        return document.RootElement.GetProperty("components")[0].GetRawText();
    }

    // ---- #47: media header example ----

    [Fact]
    public void ImageHeaderComponent_SerializesExampleAsHeaderHandle()
    {
        var json = SerializeComponent(new ImageHeaderComponent("4::image=="));

        Assert.Equal(
            """{"type":"HEADER","format":"IMAGE","example":{"header_handle":["4::image=="]}}""",
            json);
    }

    [Fact]
    public void VideoHeaderComponent_SerializesExampleAsHeaderHandle()
    {
        var json = SerializeComponent(new VideoHeaderComponent("4::video=="));

        Assert.Equal(
            """{"type":"HEADER","format":"VIDEO","example":{"header_handle":["4::video=="]}}""",
            json);
    }

    [Fact]
    public void DocumentHeaderComponent_SerializesExampleAsHeaderHandle()
    {
        var json = SerializeComponent(new DocumentHeaderComponent("4::doc=="));

        Assert.Equal(
            """{"type":"HEADER","format":"DOCUMENT","example":{"header_handle":["4::doc=="]}}""",
            json);
    }

    [Fact]
    public void TextHeaderComponent_SerializesFormatAndText()
    {
        var json = SerializeComponent(new TextHeaderComponent("Order update"));

        Assert.Equal("""{"type":"HEADER","format":"TEXT","text":"Order update"}""", json);
    }

    [Fact]
    public void ImageHeaderComponent_KeepsExampleHandleProperty()
    {
        var header = new ImageHeaderComponent("4::image==");

        Assert.Equal("4::image==", header.ExampleHandle);
    }

    // ---- #50: body examples ----

    [Fact]
    public void BodyComponent_WithoutExamples_SerializesAsBefore()
    {
        var json = SerializeComponent(new BodyComponent("Hi {{1}}"));

        Assert.Equal("""{"type":"BODY","text":"Hi {{1}}"}""", json);
    }

    [Fact]
    public void BodyComponent_WithPositionalExamples_SerializesBodyText()
    {
        var body = new BodyComponent("Hi {{1}}, order {{2}} ships {{1}}") { Examples = ["Ana", "42"] };

        var json = SerializeComponent(body);

        Assert.Equal(
            """{"type":"BODY","text":"Hi {{1}}, order {{2}} ships {{1}}","example":{"body_text":[["Ana","42"]]}}""",
            json);
    }

    [Fact]
    public void BodyComponent_WithNamedExamples_SerializesBodyTextNamedParams()
    {
        var body = new BodyComponent("Hi {{first_name}}, order {{order_number}}")
        {
            NamedExamples = [new("order_number", "42"), new("first_name", "Ana")],
        };

        var json = SerializeComponent(body);

        Assert.Equal(
            """{"type":"BODY","text":"Hi {{first_name}}, order {{order_number}}","example":{"body_text_named_params":[{"param_name":"order_number","example":"42"},{"param_name":"first_name","example":"Ana"}]}}""",
            json);
    }

    [Fact]
    public void BodyComponent_WithEmptyExamplesAndNoPlaceholders_OmitsExample()
    {
        var json = SerializeComponent(new BodyComponent("Hello") { Examples = [] });

        Assert.Equal("""{"type":"BODY","text":"Hello"}""", json);
    }

    [Fact]
    public void BodyComponent_Throws_WhenPositionalExampleCountDoesNotMatch()
    {
        var ex = Assert.Throws<ArgumentException>(
            () => new BodyComponent("Hi {{1}}, order {{2}}") { Examples = ["Ana"] });

        Assert.Equal(
            "Body text has 2 positional placeholder(s) but 1 example(s) were provided. (Parameter 'Examples')",
            ex.Message);
    }

    [Fact]
    public void BodyComponent_Throws_WhenPositionalExamplesGivenForNamedText()
    {
        var ex = Assert.Throws<ArgumentException>(
            () => new BodyComponent("Hi {{first_name}}") { Examples = ["Ana"] });

        Assert.Equal(
            "Body text uses named placeholders; provide NamedExamples instead. (Parameter 'Examples')",
            ex.Message);
    }

    [Fact]
    public void BodyComponent_Throws_WhenNamedExampleKeysDoNotMatch()
    {
        var ex = Assert.Throws<ArgumentException>(
            () => new BodyComponent("Hi {{first_name}}, order {{order_number}}")
            {
                NamedExamples = [new("first_name", "Ana"), new("last_name", "Diaz")],
            });

        Assert.Equal(
            "Body text named examples must match its placeholders. Expected: first_name, order_number. Provided: first_name, last_name. (Parameter 'NamedExamples')",
            ex.Message);
    }

    [Fact]
    public void BodyComponent_Throws_WhenNamedExampleKeyIsDuplicated()
    {
        var ex = Assert.Throws<ArgumentException>(
            () => new BodyComponent("Hi {{first_name}}")
            {
                NamedExamples = [new("first_name", "Ana"), new("first_name", "Eva")],
            });

        Assert.Equal(
            "Body text named examples contain a duplicate parameter 'first_name'. (Parameter 'NamedExamples')",
            ex.Message);
    }

    [Fact]
    public void BodyComponent_Throws_WhenNamedExamplesGivenForPositionalText()
    {
        var ex = Assert.Throws<ArgumentException>(
            () => new BodyComponent("Hi {{1}}") { NamedExamples = [new("first_name", "Ana")] });

        Assert.Equal(
            "Body text uses positional placeholders; provide Examples instead. (Parameter 'NamedExamples')",
            ex.Message);
    }

    [Theory]
    [InlineData(null, "x")]
    [InlineData("", "x")]
    [InlineData("name", " ")]
    public void NamedParameterExample_Throws_WhenFieldIsEmpty(string? paramName, string? example)
    {
        Assert.ThrowsAny<ArgumentException>(() => new NamedParameterExample(paramName!, example!));
    }

    [Fact]
    public void BodyComponent_Throws_WhenPositionalExampleIsEmpty()
    {
        Assert.ThrowsAny<ArgumentException>(() => new BodyComponent("Hi {{1}}") { Examples = [" "] });
    }

    [Fact]
    public void Template_WithBodyExample_StillDeserializes()
    {
        const string json = """
            {"id":"1","components":[{"type":"BODY","text":"Hi {{1}}","example":{"body_text":[["Ana"]]}}]}
            """;

        var template = JsonSerializer.Deserialize(json, WhatsAppJsonSerializerContext.Default.Template);

        var body = Assert.IsType<BodyComponent>(Assert.Single(template!.Components!));
        Assert.Equal("Hi {{1}}", body.Text);
        Assert.Equal(["Ana"], body.Examples!);
    }

    [Fact]
    public void Template_WithUrlButtonExample_StillDeserializes()
    {
        const string json = """
            {"id":"1","components":[{"type":"BUTTONS","buttons":[{"type":"URL","text":"Track","url":"https://example.com/{{1}}","example":["abc"]}]}]}
            """;

        var template = JsonSerializer.Deserialize(json, WhatsAppJsonSerializerContext.Default.Template);

        var buttons = Assert.IsType<ButtonsComponent>(Assert.Single(template!.Components!));
        var button = Assert.IsType<UrlButton>(Assert.Single(buttons.Buttons));
        Assert.Equal(["abc"], button.Examples!);
    }

    // ---- #50: text header examples ----

    [Fact]
    public void TextHeaderComponent_WithPositionalExample_SerializesHeaderText()
    {
        var header = new TextHeaderComponent("Order {{1}}") { Examples = ["42"] };

        var json = SerializeComponent(header);

        Assert.Equal(
            """{"type":"HEADER","format":"TEXT","text":"Order {{1}}","example":{"header_text":["42"]}}""",
            json);
    }

    [Fact]
    public void TextHeaderComponent_WithNamedExample_SerializesHeaderTextNamedParams()
    {
        var header = new TextHeaderComponent("Order {{order_number}}")
        {
            NamedExamples = [new("order_number", "42")],
        };

        var json = SerializeComponent(header);

        Assert.Equal(
            """{"type":"HEADER","format":"TEXT","text":"Order {{order_number}}","example":{"header_text_named_params":[{"param_name":"order_number","example":"42"}]}}""",
            json);
    }

    [Fact]
    public void TextHeaderComponent_Throws_WhenPositionalExampleCountDoesNotMatch()
    {
        var ex = Assert.Throws<ArgumentException>(
            () => new TextHeaderComponent("Order {{1}}") { Examples = ["42", "43"] });

        Assert.Equal(
            "Header text has 1 positional placeholder(s) but 2 example(s) were provided. (Parameter 'Examples')",
            ex.Message);
    }

    [Fact]
    public void TextHeaderComponent_Throws_WhenNamedExampleKeysDoNotMatch()
    {
        var ex = Assert.Throws<ArgumentException>(
            () => new TextHeaderComponent("Order {{order_number}}") { NamedExamples = [new("order_id", "42")] });

        Assert.Equal(
            "Header text named examples must match its placeholders. Expected: order_number. Provided: order_id. (Parameter 'NamedExamples')",
            ex.Message);
    }

    // ---- #50: URL button examples ----

    [Fact]
    public void UrlButton_WithoutExample_SerializesAsBefore()
    {
        var json = SerializeComponent(new ButtonsComponent([new UrlButton("Track", "https://example.com/{{1}}")]));

        Assert.Equal(
            """{"type":"BUTTONS","buttons":[{"type":"URL","text":"Track","url":"https://example.com/{{1}}"}]}""",
            json);
    }

    [Fact]
    public void UrlButton_WithExample_SerializesExampleArray()
    {
        var button = new UrlButton("Track", "https://example.com/{{1}}") { Examples = ["summer2023"] };

        var json = SerializeComponent(new ButtonsComponent([button]));

        Assert.Equal(
            """{"type":"BUTTONS","buttons":[{"type":"URL","text":"Track","url":"https://example.com/{{1}}","example":["summer2023"]}]}""",
            json);
    }

    [Fact]
    public void UrlButton_Throws_WhenExampleCountDoesNotMatch()
    {
        var ex = Assert.Throws<ArgumentException>(
            () => new UrlButton("Track", "https://example.com/track") { Examples = ["summer2023"] });

        Assert.Equal(
            "Button URL has 0 positional placeholder(s) but 1 example(s) were provided. (Parameter 'Examples')",
            ex.Message);
    }
}
