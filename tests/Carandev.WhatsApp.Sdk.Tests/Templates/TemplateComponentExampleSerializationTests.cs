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
}
