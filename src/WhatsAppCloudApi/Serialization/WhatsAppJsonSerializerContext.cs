using System.Text.Json.Serialization;
using WhatsAppCloudApi.Models.Templates;

namespace WhatsAppCloudApi.Serialization;

/// <summary>
/// Source-generated <see cref="JsonSerializerContext"/> for WhatsApp Cloud API DTOs. Avoids
/// reflection-based serialization so consumers can opt into trimming/AOT-friendly behavior.
/// </summary>
[JsonSerializable(typeof(WhatsAppErrorResponse))]
[JsonSerializable(typeof(WhatsAppError))]
[JsonSerializable(typeof(Template))]
[JsonSerializable(typeof(TemplatesPage<Template>))]
[JsonSerializable(typeof(TemplatesPaging))]
[JsonSerializable(typeof(TemplatesPagingCursors))]
[JsonSerializable(typeof(CreateTemplateRequest))]
[JsonSerializable(typeof(UpdateTemplateRequest))]
[JsonSerializable(typeof(TemplateCategory))]
[JsonSerializable(typeof(TemplateComponent))]
[JsonSerializable(typeof(HeaderComponent))]
[JsonSerializable(typeof(TextHeaderComponent))]
[JsonSerializable(typeof(ImageHeaderComponent))]
[JsonSerializable(typeof(VideoHeaderComponent))]
[JsonSerializable(typeof(DocumentHeaderComponent))]
[JsonSerializable(typeof(BodyComponent))]
[JsonSerializable(typeof(FooterComponent))]
[JsonSerializable(typeof(ButtonsComponent))]
[JsonSerializable(typeof(TemplateButton))]
[JsonSerializable(typeof(UrlButton))]
[JsonSerializable(typeof(PhoneNumberButton))]
[JsonSerializable(typeof(QuickReplyButton))]
[JsonSerializable(typeof(OtpButton))]
[JsonSerializable(typeof(CopyCodeButton))]
[JsonSerializable(typeof(CatalogButton))]
[JsonSerializable(typeof(FlowButton))]
public partial class WhatsAppJsonSerializerContext : JsonSerializerContext
{
}
