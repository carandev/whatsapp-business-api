using System.Text.Json;
using Carandev.WhatsApp.Sdk.Models.Templates;
using Carandev.WhatsApp.Sdk.Serialization;

namespace Carandev.WhatsApp.Sdk.Tests.Templates;

public class TemplateParameterFormatTests
{
    private static string Serialize(CreateTemplateRequest request) =>
        JsonSerializer.Serialize(request, WhatsAppJsonSerializerContext.Default.CreateTemplateRequest);

    private static CreateTemplateRequest Request(params TemplateComponent[] components) =>
        new("order_update", TemplateCategory.Utility, "en_US", components);

    [Fact]
    public void CreateTemplateRequest_WithPositionalBodyAndNoFormat_SerializesAsBefore()
    {
        var json = Serialize(Request(new BodyComponent("Hi {{1}}")));

        Assert.Equal(
            """{"name":"order_update","category":"UTILITY","language":"en_US","components":[{"type":"BODY","text":"Hi {{1}}"}]}""",
            json);
    }

    [Fact]
    public void CreateTemplateRequest_WithExplicitPositional_SerializesParameterFormat()
    {
        var request = Request(new BodyComponent("Hi {{1}}")) with { ParameterFormat = TemplateParameterFormat.Positional };

        var json = Serialize(request);

        Assert.Equal(
            """{"name":"order_update","category":"UTILITY","language":"en_US","components":[{"type":"BODY","text":"Hi {{1}}"}],"parameter_format":"positional"}""",
            json);
    }

    [Fact]
    public void CreateTemplateRequest_WithExplicitNamed_SerializesParameterFormat()
    {
        var request = Request(new BodyComponent("Hello")) with { ParameterFormat = TemplateParameterFormat.Named };

        var json = Serialize(request);

        Assert.EndsWith("""],"parameter_format":"named"}""", json, StringComparison.Ordinal);
        Assert.Equal(TemplateParameterFormat.Named, request.ParameterFormat);
    }

    [Fact]
    public void CreateTemplateRequest_WithNamedBodyAndNoFormat_InfersNamed()
    {
        var request = Request(new BodyComponent("Hi {{first_name}}"));

        var json = Serialize(request);

        Assert.EndsWith("""],"parameter_format":"named"}""", json, StringComparison.Ordinal);
        Assert.Null(request.ParameterFormat);
    }

    [Fact]
    public void CreateTemplateRequest_WithNamedTextHeaderAndNoFormat_InfersNamed()
    {
        var json = Serialize(Request(new TextHeaderComponent("Order {{order_number}}"), new BodyComponent("Thanks")));

        Assert.EndsWith("""],"parameter_format":"named"}""", json, StringComparison.Ordinal);
    }

    [Fact]
    public void CreateTemplateRequest_WithExplicitPositional_IsNotOverriddenByInference()
    {
        var request = Request(new BodyComponent("Hi {{first_name}}")) with { ParameterFormat = TemplateParameterFormat.Positional };

        var json = Serialize(request);

        Assert.EndsWith("""],"parameter_format":"positional"}""", json, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("\"named\"", TemplateParameterFormat.Named)]
    [InlineData("\"NAMED\"", TemplateParameterFormat.Named)]
    [InlineData("\"positional\"", TemplateParameterFormat.Positional)]
    [InlineData("\"POSITIONAL\"", TemplateParameterFormat.Positional)]
    public void TemplateParameterFormat_Deserializes_CaseInsensitively(string json, TemplateParameterFormat expected)
    {
        var value = JsonSerializer.Deserialize(json, WhatsAppJsonSerializerContext.Default.TemplateParameterFormat);

        Assert.Equal(expected, value);
    }

    [Fact]
    public void TemplateParameterFormat_Throws_OnUnknownValue()
    {
        Assert.Throws<JsonException>(
            () => JsonSerializer.Deserialize("\"mixed\"", WhatsAppJsonSerializerContext.Default.TemplateParameterFormat));
    }
}
