using WhatsAppCloudApi.Models.Templates;

namespace WhatsAppCloudApi.Tests.Templates;

public class TemplateComponentTests
{
    // ---- HeaderComponent variants ----

    [Fact]
    public void TextHeaderComponent_ConstructsSuccessfully_WhenTextProvided()
    {
        var header = new TextHeaderComponent("Order Confirmation");

        Assert.Equal("Order Confirmation", header.Text);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TextHeaderComponent_Throws_WhenTextIsEmpty(string? text)
    {
        Assert.ThrowsAny<ArgumentException>(() => new TextHeaderComponent(text!));
    }

    [Fact]
    public void ImageHeaderComponent_ConstructsSuccessfully_WhenExampleHandleProvided()
    {
        var header = new ImageHeaderComponent("4::image-handle==");

        Assert.Equal("4::image-handle==", header.ExampleHandle);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ImageHeaderComponent_Throws_WhenExampleHandleIsEmpty(string? handle)
    {
        Assert.ThrowsAny<ArgumentException>(() => new ImageHeaderComponent(handle!));
    }

    [Fact]
    public void VideoHeaderComponent_ConstructsSuccessfully_WhenExampleHandleProvided()
    {
        var header = new VideoHeaderComponent("4::video-handle==");

        Assert.Equal("4::video-handle==", header.ExampleHandle);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void VideoHeaderComponent_Throws_WhenExampleHandleIsEmpty(string? handle)
    {
        Assert.ThrowsAny<ArgumentException>(() => new VideoHeaderComponent(handle!));
    }

    [Fact]
    public void DocumentHeaderComponent_ConstructsSuccessfully_WhenExampleHandleProvided()
    {
        var header = new DocumentHeaderComponent("4::document-handle==");

        Assert.Equal("4::document-handle==", header.ExampleHandle);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void DocumentHeaderComponent_Throws_WhenExampleHandleIsEmpty(string? handle)
    {
        Assert.ThrowsAny<ArgumentException>(() => new DocumentHeaderComponent(handle!));
    }

    // ---- BodyComponent ----

    [Fact]
    public void BodyComponent_ConstructsSuccessfully_WhenTextHasNoPlaceholders()
    {
        var body = new BodyComponent("Thank you for your order.");

        Assert.Equal("Thank you for your order.", body.Text);
    }

    [Theory]
    [InlineData("Hello {{1}}, your order {{2}} has shipped.")]
    [InlineData("{{1}}")]
    public void BodyComponent_ConstructsSuccessfully_WithValidPositionalPlaceholders(string text)
    {
        var body = new BodyComponent(text);

        Assert.Equal(text, body.Text);
    }

    [Theory]
    [InlineData("Hello {{customer_name}}, your order {{order_id}} has shipped.")]
    [InlineData("{{customer_name}}")]
    public void BodyComponent_ConstructsSuccessfully_WithValidNamedPlaceholders(string text)
    {
        var body = new BodyComponent(text);

        Assert.Equal(text, body.Text);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BodyComponent_Throws_WhenTextIsEmpty(string? text)
    {
        Assert.ThrowsAny<ArgumentException>(() => new BodyComponent(text!));
    }

    [Fact]
    public void BodyComponent_Throws_WhenPlaceholdersMixPositionalAndNamed()
    {
        Assert.ThrowsAny<ArgumentException>(() => new BodyComponent("Hello {{1}}, order {{order_id}}."));
    }

    [Theory]
    [InlineData("Hello {{0}}")]
    [InlineData("Hello {{}}")]
    [InlineData("Hello {{ 1 }}")]
    [InlineData("Hello {{1")]
    [InlineData("Hello 1}}")]
    [InlineData("Hello {{customer name}}")]
    [InlineData("Hello {{01}}")]
    public void BodyComponent_Throws_WhenPlaceholderIsMalformed(string text)
    {
        Assert.ThrowsAny<ArgumentException>(() => new BodyComponent(text));
    }

    // ---- FooterComponent ----

    [Fact]
    public void FooterComponent_ConstructsSuccessfully_WhenTextProvided()
    {
        var footer = new FooterComponent("Reply STOP to unsubscribe.");

        Assert.Equal("Reply STOP to unsubscribe.", footer.Text);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FooterComponent_Throws_WhenTextIsEmpty(string? text)
    {
        Assert.ThrowsAny<ArgumentException>(() => new FooterComponent(text!));
    }

    // ---- ButtonsComponent ----

    [Fact]
    public void ButtonsComponent_ConstructsSuccessfully_WhenAtLeastOneButtonProvided()
    {
        var buttons = new ButtonsComponent(new TemplateButton[] { new QuickReplyButton("Yes") });

        Assert.Single(buttons.Buttons);
    }

    [Fact]
    public void ButtonsComponent_Throws_WhenButtonsIsEmpty()
    {
        Assert.ThrowsAny<ArgumentException>(() => new ButtonsComponent(Array.Empty<TemplateButton>()));
    }

    [Fact]
    public void ButtonsComponent_Throws_WhenButtonsIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new ButtonsComponent(null!));
    }

    // ---- TemplateButton variants ----

    [Fact]
    public void UrlButton_ConstructsSuccessfully_WhenTextAndUrlProvided()
    {
        var button = new UrlButton("Visit site", "https://example.test/{{1}}");

        Assert.Equal("Visit site", button.Text);
        Assert.Equal("https://example.test/{{1}}", button.Url);
    }

    [Theory]
    [InlineData(null, "https://example.test")]
    [InlineData("Visit site", null)]
    [InlineData("", "https://example.test")]
    [InlineData("Visit site", "")]
    public void UrlButton_Throws_WhenRequiredFieldIsEmpty(string? text, string? url)
    {
        Assert.ThrowsAny<ArgumentException>(() => new UrlButton(text!, url!));
    }

    [Fact]
    public void PhoneNumberButton_ConstructsSuccessfully_WhenTextAndPhoneNumberProvided()
    {
        var button = new PhoneNumberButton("Call us", "+15550001234");

        Assert.Equal("Call us", button.Text);
        Assert.Equal("+15550001234", button.PhoneNumber);
    }

    [Theory]
    [InlineData(null, "+15550001234")]
    [InlineData("Call us", null)]
    [InlineData("", "+15550001234")]
    [InlineData("Call us", "")]
    public void PhoneNumberButton_Throws_WhenRequiredFieldIsEmpty(string? text, string? phoneNumber)
    {
        Assert.ThrowsAny<ArgumentException>(() => new PhoneNumberButton(text!, phoneNumber!));
    }

    [Fact]
    public void QuickReplyButton_ConstructsSuccessfully_WhenTextProvided()
    {
        var button = new QuickReplyButton("Yes");

        Assert.Equal("Yes", button.Text);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void QuickReplyButton_Throws_WhenTextIsEmpty(string? text)
    {
        Assert.ThrowsAny<ArgumentException>(() => new QuickReplyButton(text!));
    }

    [Fact]
    public void OtpButton_ConstructsSuccessfully_WhenOtpTypeAndTextProvided()
    {
        var button = new OtpButton(OtpButtonType.CopyCode, "Copy Code");

        Assert.Equal(OtpButtonType.CopyCode, button.OtpType);
        Assert.Equal("Copy Code", button.Text);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void OtpButton_Throws_WhenTextIsEmpty(string? text)
    {
        Assert.ThrowsAny<ArgumentException>(() => new OtpButton(OtpButtonType.OneTap, text!));
    }

    [Fact]
    public void CopyCodeButton_ConstructsSuccessfully_WhenExampleProvided()
    {
        var button = new CopyCodeButton("250FF");

        Assert.Equal("250FF", button.Example);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CopyCodeButton_Throws_WhenExampleIsEmpty(string? example)
    {
        Assert.ThrowsAny<ArgumentException>(() => new CopyCodeButton(example!));
    }

    [Fact]
    public void CatalogButton_ConstructsSuccessfully_WhenTextProvided()
    {
        var button = new CatalogButton("View catalog");

        Assert.Equal("View catalog", button.Text);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CatalogButton_Throws_WhenTextIsEmpty(string? text)
    {
        Assert.ThrowsAny<ArgumentException>(() => new CatalogButton(text!));
    }

    [Fact]
    public void FlowButton_ConstructsSuccessfully_WhenTextAndFlowIdProvided()
    {
        var button = new FlowButton("Start", "123456789");

        Assert.Equal("Start", button.Text);
        Assert.Equal("123456789", button.FlowId);
    }

    [Theory]
    [InlineData(null, "123456789")]
    [InlineData("Start", null)]
    [InlineData("", "123456789")]
    [InlineData("Start", "")]
    public void FlowButton_Throws_WhenRequiredFieldIsEmpty(string? text, string? flowId)
    {
        Assert.ThrowsAny<ArgumentException>(() => new FlowButton(text!, flowId!));
    }
}
