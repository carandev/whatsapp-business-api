using System.Text.Json.Serialization;
using WhatsAppCloudApi.Models.Media;
using WhatsAppCloudApi.Models.Messages;
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
// Messages — foundation (PR1)
[JsonSerializable(typeof(Context))]
[JsonSerializable(typeof(Contact))]
[JsonSerializable(typeof(ContactName))]
[JsonSerializable(typeof(ContactAddress))]
[JsonSerializable(typeof(ContactEmail))]
[JsonSerializable(typeof(ContactPhone))]
[JsonSerializable(typeof(ContactOrg))]
[JsonSerializable(typeof(ContactUrl))]
[JsonSerializable(typeof(SendMessageRequest))]
[JsonSerializable(typeof(MediaReference))]
// Messages — send-time template parameter hierarchy (PR2)
[JsonSerializable(typeof(TemplateParameterComponent))]
[JsonSerializable(typeof(HeaderParameterComponent))]
[JsonSerializable(typeof(BodyParameterComponent))]
[JsonSerializable(typeof(ButtonParameterComponent))]
[JsonSerializable(typeof(TemplateParameter))]
[JsonSerializable(typeof(TextParameter))]
[JsonSerializable(typeof(Currency))]
[JsonSerializable(typeof(CurrencyParameter))]
[JsonSerializable(typeof(TemplateDateTime))]
[JsonSerializable(typeof(DateTimeParameter))]
[JsonSerializable(typeof(ImageParameter))]
[JsonSerializable(typeof(DocumentParameter))]
[JsonSerializable(typeof(VideoParameter))]
[JsonSerializable(typeof(PayloadParameter))]
// Messages — per-message-kind requests and response mapping (PR3)
[JsonSerializable(typeof(SendTextMessageRequest))]
[JsonSerializable(typeof(TextContent))]
[JsonSerializable(typeof(SendTemplateMessageRequest))]
[JsonSerializable(typeof(TemplateReference))]
[JsonSerializable(typeof(LanguageCode))]
[JsonSerializable(typeof(SendImageMessageRequest))]
[JsonSerializable(typeof(SendDocumentMessageRequest))]
[JsonSerializable(typeof(SendAudioMessageRequest))]
[JsonSerializable(typeof(SendVideoMessageRequest))]
[JsonSerializable(typeof(SendStickerMessageRequest))]
[JsonSerializable(typeof(SendLocationMessageRequest))]
[JsonSerializable(typeof(Location))]
[JsonSerializable(typeof(SendContactsMessageRequest))]
[JsonSerializable(typeof(MessageResponse))]
// Media — upload (PR1 of issue #6)
[JsonSerializable(typeof(MediaUploadResponse))]
public partial class WhatsAppJsonSerializerContext : JsonSerializerContext
{
}
