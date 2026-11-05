using System.Text.Json;
using Carandev.WhatsApp.Sdk.Models.Templates;
using Carandev.WhatsApp.Sdk.Serialization;

namespace Carandev.WhatsApp.Sdk.Tests.Templates;

/// <summary>
/// #58: Meta requires named placeholders to be lowercase.
/// </summary>
public class NamedPlaceholderCaseTests
{
    [Theory]
    [InlineData("Hi {{FirstName}}", "FirstName")]
    [InlineData("Hi {{first_Name}}", "first_Name")]
    [InlineData("Hi {{ORDER}}", "ORDER")]
    public void BodyComponent_Throws_WhenNamedPlaceholderHasUppercase(string text, string name)
    {
        var ex = Assert.Throws<ArgumentException>(() => new BodyComponent(text));

        Assert.Equal(
            $"Body text contains the named placeholder '{{{{{name}}}}}' with uppercase letters; named placeholders must be lowercase (a-z, 0-9, and _, starting with a letter or _). (Parameter 'text')",
            ex.Message);
    }

    [Fact]
    public void TextHeaderComponent_Throws_WhenNamedPlaceholderHasUppercase()
    {
        var ex = Assert.Throws<ArgumentException>(() => new TextHeaderComponent("Order {{OrderNumber}}"));

        Assert.Equal(
            "Header text contains the named placeholder '{{OrderNumber}}' with uppercase letters; named placeholders must be lowercase (a-z, 0-9, and _, starting with a letter or _). (Parameter 'text')",
            ex.Message);
    }

    [Theory]
    [InlineData("Hi {{first_name}}")]
    [InlineData("Line {{address_line_2}}")]
    [InlineData("Code {{_code1}}")]
    public void LowercaseNamedPlaceholders_WithUnderscoresAndDigits_AreAccepted(string text)
    {
        var name = text[(text.IndexOf("{{", StringComparison.Ordinal) + 2)..text.IndexOf("}}", StringComparison.Ordinal)];

        var body = new BodyComponent(text) { NamedExamples = [new(name, "x")] };
        var header = new TextHeaderComponent(text) { NamedExamples = [new(name, "x")] };

        Assert.Equal(text, body.Text);
        Assert.Equal(text, header.Text);
    }

    [Fact]
    public void PositionalPlaceholders_AreUnaffected()
    {
        var body = new BodyComponent("Hi {{1}}, order {{2}}") { Examples = ["Ana", "42"] };
        var header = new TextHeaderComponent("Order {{1}}") { Examples = ["42"] };

        Assert.Equal(["Ana", "42"], body.Examples!);
        Assert.Equal(["42"], header.Examples!);
    }

    [Fact]
    public void Template_FromMeta_WithNamedParams_DeserializesWithoutValidating()
    {
        const string json = """
            {"id":"1","name":"legacy","status":"APPROVED","category":"MARKETING","language":"en_US","components":[
              {"type":"HEADER","format":"TEXT","text":"Hi {{FirstName}}","example":{"header_text_named_params":[{"param_name":"FirstName","example":"Ana"}]}},
              {"type":"BODY","text":"Order {{order_number}} for {{CustomerName}}","example":{"body_text_named_params":[{"param_name":"order_number","example":"42"},{"param_name":"CustomerName","example":"Ana"}]}}
            ]}
            """;

        var template = JsonSerializer.Deserialize(json, WhatsAppJsonSerializerContext.Default.Template)!;

        var header = Assert.IsType<TextHeaderComponent>(template.Components![0]);
        Assert.Equal("Hi {{FirstName}}", header.Text);
        Assert.Equal("FirstName", Assert.Single(header.NamedExamples!).ParamName);
        var body = Assert.IsType<BodyComponent>(template.Components[1]);
        Assert.Equal("Order {{order_number}} for {{CustomerName}}", body.Text);
        Assert.Equal(["order_number", "CustomerName"], body.NamedExamples!.Select(e => e.ParamName));
    }
}
