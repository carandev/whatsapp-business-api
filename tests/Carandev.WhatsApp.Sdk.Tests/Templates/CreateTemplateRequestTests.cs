using Carandev.WhatsApp.Sdk.Models.Templates;

namespace Carandev.WhatsApp.Sdk.Tests.Templates;

public class CreateTemplateRequestTests
{
    private static BodyComponent ValidBody() => new("Hello {{1}}, your order shipped.");

    private static IReadOnlyList<TemplateComponent> ValidComponents() =>
    [
        ValidBody(),
    ];

    // ---- CreateTemplateRequest: valid shape ----

    [Fact]
    public void CreateTemplateRequest_ConstructsSuccessfully_WithValidShape()
    {
        var request = new CreateTemplateRequest(
            "order_confirmation",
            TemplateCategory.Utility,
            "en_US",
            ValidComponents());

        Assert.Equal("order_confirmation", request.Name);
        Assert.Equal(TemplateCategory.Utility, request.Category);
        Assert.Equal("en_US", request.Language);
        Assert.Single(request.Components);
        Assert.IsType<BodyComponent>(request.Components[0]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateTemplateRequest_Throws_WhenNameIsEmpty(string? name)
    {
        Assert.ThrowsAny<ArgumentException>(() => new CreateTemplateRequest(
            name!,
            TemplateCategory.Utility,
            "en_US",
            ValidComponents()));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateTemplateRequest_Throws_WhenLanguageIsEmpty(string? language)
    {
        Assert.ThrowsAny<ArgumentException>(() => new CreateTemplateRequest(
            "order_confirmation",
            TemplateCategory.Utility,
            language!,
            ValidComponents()));
    }

    // ---- CreateTemplateRequest: structural validation (TemplateComponentValidation coverage) ----

    [Fact]
    public void CreateTemplateRequest_Throws_WhenComponentsIsEmpty()
    {
        Assert.Throws<ArgumentException>(() => new CreateTemplateRequest(
            "order_confirmation",
            TemplateCategory.Utility,
            "en_US",
            []));
    }

    [Fact]
    public void CreateTemplateRequest_Throws_WhenComponentsHasNoBody()
    {
        var components = new List<TemplateComponent> { new FooterComponent("Thanks for shopping with us") };

        Assert.Throws<ArgumentException>(() => new CreateTemplateRequest(
            "order_confirmation",
            TemplateCategory.Utility,
            "en_US",
            components));
    }

    [Fact]
    public void CreateTemplateRequest_Throws_WhenComponentsHasTwoBodyComponents()
    {
        var components = new List<TemplateComponent> { ValidBody(), ValidBody() };

        Assert.Throws<ArgumentException>(() => new CreateTemplateRequest(
            "order_confirmation",
            TemplateCategory.Utility,
            "en_US",
            components));
    }

    [Fact]
    public void CreateTemplateRequest_Throws_WhenComponentsHasTwoHeaderComponents()
    {
        var components = new List<TemplateComponent>
        {
            new TextHeaderComponent("Header One"),
            new TextHeaderComponent("Header Two"),
            ValidBody(),
        };

        Assert.Throws<ArgumentException>(() => new CreateTemplateRequest(
            "order_confirmation",
            TemplateCategory.Utility,
            "en_US",
            components));
    }

    [Fact]
    public void CreateTemplateRequest_Throws_WhenComponentsHasTwoFooterComponents()
    {
        var components = new List<TemplateComponent>
        {
            ValidBody(),
            new FooterComponent("Footer One"),
            new FooterComponent("Footer Two"),
        };

        Assert.Throws<ArgumentException>(() => new CreateTemplateRequest(
            "order_confirmation",
            TemplateCategory.Utility,
            "en_US",
            components));
    }

    [Fact]
    public void CreateTemplateRequest_ConstructsSuccessfully_WithHeaderBodyFooterAndButtons()
    {
        var components = new List<TemplateComponent>
        {
            new TextHeaderComponent("Order Confirmation"),
            ValidBody(),
            new FooterComponent("Thanks for shopping with us"),
            new ButtonsComponent([new QuickReplyButton("Track order")]),
        };

        var request = new CreateTemplateRequest(
            "order_confirmation",
            TemplateCategory.Utility,
            "en_US",
            components);

        Assert.Equal(4, request.Components.Count);
    }

    // ---- UpdateTemplateRequest ----

    [Fact]
    public void UpdateTemplateRequest_Throws_WhenNeitherCategoryNorComponentsIsProvided()
    {
        Assert.Throws<ArgumentException>(() => new UpdateTemplateRequest());
    }

    [Fact]
    public void UpdateTemplateRequest_ConstructsSuccessfully_WithOnlyCategory()
    {
        var request = new UpdateTemplateRequest(category: TemplateCategory.Marketing);

        Assert.Equal(TemplateCategory.Marketing, request.Category);
        Assert.Null(request.Components);
    }

    [Fact]
    public void UpdateTemplateRequest_ConstructsSuccessfully_WithOnlyComponents()
    {
        var request = new UpdateTemplateRequest(components: ValidComponents());

        Assert.Null(request.Category);
        Assert.NotNull(request.Components);
    }

    [Fact]
    public void UpdateTemplateRequest_ConstructsSuccessfully_WithBothCategoryAndComponents()
    {
        var request = new UpdateTemplateRequest(TemplateCategory.Marketing, ValidComponents());

        Assert.Equal(TemplateCategory.Marketing, request.Category);
        Assert.NotNull(request.Components);
    }

    [Fact]
    public void UpdateTemplateRequest_Throws_WhenComponentsIsEmpty()
    {
        Assert.Throws<ArgumentException>(() => new UpdateTemplateRequest(components: []));
    }

    [Fact]
    public void UpdateTemplateRequest_Throws_WhenComponentsHasTwoBodyComponents()
    {
        var components = new List<TemplateComponent> { ValidBody(), ValidBody() };

        Assert.Throws<ArgumentException>(() => new UpdateTemplateRequest(components: components));
    }

    [Fact]
    public void UpdateTemplateRequest_Throws_WhenComponentsHasTwoHeaderComponents()
    {
        var components = new List<TemplateComponent>
        {
            new TextHeaderComponent("Header One"),
            new TextHeaderComponent("Header Two"),
            ValidBody(),
        };

        Assert.Throws<ArgumentException>(() => new UpdateTemplateRequest(components: components));
    }

    [Fact]
    public void UpdateTemplateRequest_Throws_WhenComponentsHasTwoFooterComponents()
    {
        var components = new List<TemplateComponent>
        {
            ValidBody(),
            new FooterComponent("Footer One"),
            new FooterComponent("Footer Two"),
        };

        Assert.Throws<ArgumentException>(() => new UpdateTemplateRequest(components: components));
    }
}
