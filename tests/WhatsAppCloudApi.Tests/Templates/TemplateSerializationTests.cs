using System.Text.Json;
using WhatsAppCloudApi.Models.Templates;
using WhatsAppCloudApi.Serialization;

namespace WhatsAppCloudApi.Tests.Templates;

/// <summary>
/// Smoke tests confirming <see cref="Template"/> and <see cref="TemplatesPage{T}"/> are actually
/// wired into the source-generated <see cref="WhatsAppJsonSerializerContext"/> — not merely that
/// the types compile, but that the generated context can serialize and deserialize them.
/// </summary>
public class TemplateSerializationTests
{
    [Fact]
    public void Template_RoundTrips_ThroughGeneratedSerializerContext()
    {
        var template = new Template
        {
            Id = "123456",
            Name = "order_confirmation",
            Category = TemplateCategory.Utility,
            Language = "en_US",
            Status = "APPROVED",
        };

        var json = JsonSerializer.Serialize(template, WhatsAppJsonSerializerContext.Default.Template);
        var deserialized = JsonSerializer.Deserialize(json, WhatsAppJsonSerializerContext.Default.Template);

        Assert.Contains("\"id\":\"123456\"", json, StringComparison.Ordinal);
        Assert.Contains("\"category\":\"UTILITY\"", json, StringComparison.Ordinal);
        Assert.NotNull(deserialized);
        Assert.Equal("123456", deserialized!.Id);
        Assert.Equal("order_confirmation", deserialized.Name);
        Assert.Equal(TemplateCategory.Utility, deserialized.Category);
        Assert.Equal("en_US", deserialized.Language);
        Assert.Equal("APPROVED", deserialized.Status);
    }

    [Fact]
    public void TemplatesPageOfTemplate_RoundTrips_ThroughGeneratedSerializerContext()
    {
        var page = new TemplatesPage<Template>
        {
            Data =
            [
                new Template { Id = "1", Name = "one", Category = TemplateCategory.Marketing, Status = "APPROVED" },
                new Template { Id = "2", Name = "two", Category = TemplateCategory.Authentication, Status = "PENDING" },
            ],
            Paging = new TemplatesPaging
            {
                Cursors = new TemplatesPagingCursors { Before = "before-cursor", After = "after-cursor" },
            },
        };

        var json = JsonSerializer.Serialize(page, WhatsAppJsonSerializerContext.Default.TemplatesPageTemplate);
        var deserialized = JsonSerializer.Deserialize(json, WhatsAppJsonSerializerContext.Default.TemplatesPageTemplate);

        Assert.NotNull(deserialized);
        Assert.NotNull(deserialized!.Data);
        Assert.Equal(2, deserialized.Data!.Count);
        Assert.Equal("1", deserialized.Data[0].Id);
        Assert.Equal("2", deserialized.Data[1].Id);
        Assert.NotNull(deserialized.Paging);
        Assert.Equal("before-cursor", deserialized.Paging!.Cursors?.Before);
        Assert.Equal("after-cursor", deserialized.Paging.Cursors?.After);
    }

    [Fact]
    public void CreateTemplateRequest_SerializesComponents_ThroughGeneratedSerializerContext()
    {
        var request = new CreateTemplateRequest(
            "order_confirmation",
            TemplateCategory.Utility,
            "en_US",
            [new BodyComponent("Hello {{1}}")]);

        var json = JsonSerializer.Serialize(request, WhatsAppJsonSerializerContext.Default.CreateTemplateRequest);

        Assert.Contains("\"name\":\"order_confirmation\"", json, StringComparison.Ordinal);
        Assert.Contains("\"category\":\"UTILITY\"", json, StringComparison.Ordinal);
        Assert.Contains("\"type\":\"BODY\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void UpdateTemplateRequest_OmitsNullFields_ThroughGeneratedSerializerContext()
    {
        var request = new UpdateTemplateRequest(category: TemplateCategory.Marketing);

        var json = JsonSerializer.Serialize(request, WhatsAppJsonSerializerContext.Default.UpdateTemplateRequest);

        Assert.Contains("\"category\":\"MARKETING\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("components", json, StringComparison.Ordinal);
    }
}
