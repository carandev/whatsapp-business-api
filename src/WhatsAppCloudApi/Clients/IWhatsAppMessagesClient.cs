using WhatsAppCloudApi.Models.Messages;
using WhatsAppCloudApi.Models.Messages.Interactive;

namespace WhatsAppCloudApi.Clients;

/// <summary>
/// Client for the WhatsApp Cloud API messages resource: send text, template, media, location, and
/// contacts messages via <c>POST /{phone_number_id}/messages</c>. Every method resolves its target
/// phone number id from a per-call <c>phoneNumberIdOverride</c> or, when omitted,
/// <see cref="WhatsAppOptions.PhoneNumberId"/>. All methods are non-throwing for Graph API-level
/// (HTTP 4xx/5xx) failures — outcomes are reported via <see cref="Result{T}"/>, never an exception.
/// Local validation failures (e.g. mutually exclusive media reference) throw synchronously, before
/// any HTTP request is sent.
/// </summary>
public interface IWhatsAppMessagesClient
{
    /// <summary>
    /// Sends a text message via <c>POST /{phone_number_id}/messages</c>.
    /// </summary>
    /// <param name="to">The recipient's phone number, in E.164 format.</param>
    /// <param name="body">The message body.</param>
    /// <param name="previewUrl">Whether Meta should render a link preview for a URL found in <paramref name="body"/>.</param>
    /// <param name="context">An optional reference to a prior inbound message being replied to.</param>
    /// <param name="phoneNumberIdOverride">
    /// An optional phone number id that takes precedence over
    /// <see cref="WhatsAppOptions.PhoneNumberId"/> when resolving the sending identity.
    /// </param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> wrapping the sent <see cref="MessageResponse"/> on
    /// success; a failed <see cref="Result{T}"/> when the Graph API call fails.
    /// </returns>
    Task<Result<MessageResponse>> SendTextAsync(string to, string body, bool previewUrl = false, Context? context = null, string? phoneNumberIdOverride = null, CancellationToken ct = default);

    /// <summary>
    /// Sends an approved template message with optional send-time parameter values via
    /// <c>POST /{phone_number_id}/messages</c>.
    /// </summary>
    /// <param name="to">The recipient's phone number, in E.164 format.</param>
    /// <param name="templateName">The approved template's name.</param>
    /// <param name="languageCode">The template's language/locale code, e.g. <c>en_US</c>.</param>
    /// <param name="components">
    /// The send-time parameter values for the template's placeholders, or <see langword="null"/>
    /// when the template has no placeholders.
    /// </param>
    /// <param name="context">An optional reference to a prior inbound message being replied to.</param>
    /// <param name="phoneNumberIdOverride">
    /// An optional phone number id that takes precedence over
    /// <see cref="WhatsAppOptions.PhoneNumberId"/> when resolving the sending identity.
    /// </param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> wrapping the sent <see cref="MessageResponse"/> on
    /// success; a failed <see cref="Result{T}"/> when the Graph API call fails.
    /// </returns>
    Task<Result<MessageResponse>> SendTemplateAsync(string to, string templateName, string languageCode, IReadOnlyList<TemplateParameterComponent>? components = null, Context? context = null, string? phoneNumberIdOverride = null, CancellationToken ct = default);

    /// <summary>
    /// Sends an image message via <c>POST /{phone_number_id}/messages</c>. Exactly one of
    /// <paramref name="mediaId"/> or <paramref name="link"/> must be supplied.
    /// </summary>
    /// <param name="to">The recipient's phone number, in E.164 format.</param>
    /// <param name="mediaId">The uploaded media's opaque id. Mutually exclusive with <paramref name="link"/>.</param>
    /// <param name="link">A public URL Meta will fetch the media from. Mutually exclusive with <paramref name="mediaId"/>.</param>
    /// <param name="caption">An optional caption shown alongside the image.</param>
    /// <param name="context">An optional reference to a prior inbound message being replied to.</param>
    /// <param name="phoneNumberIdOverride">
    /// An optional phone number id that takes precedence over
    /// <see cref="WhatsAppOptions.PhoneNumberId"/> when resolving the sending identity.
    /// </param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> wrapping the sent <see cref="MessageResponse"/> on
    /// success; a failed <see cref="Result{T}"/> when the Graph API call fails.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when both or neither of <paramref name="mediaId"/> and <paramref name="link"/> are
    /// supplied, before any HTTP request is sent.
    /// </exception>
    Task<Result<MessageResponse>> SendImageAsync(string to, string? mediaId = null, string? link = null, string? caption = null, Context? context = null, string? phoneNumberIdOverride = null, CancellationToken ct = default);

    /// <summary>
    /// Sends a document message via <c>POST /{phone_number_id}/messages</c>. Exactly one of
    /// <paramref name="mediaId"/> or <paramref name="link"/> must be supplied.
    /// </summary>
    /// <param name="to">The recipient's phone number, in E.164 format.</param>
    /// <param name="mediaId">The uploaded media's opaque id. Mutually exclusive with <paramref name="link"/>.</param>
    /// <param name="link">A public URL Meta will fetch the media from. Mutually exclusive with <paramref name="mediaId"/>.</param>
    /// <param name="caption">An optional caption shown alongside the document.</param>
    /// <param name="context">An optional reference to a prior inbound message being replied to.</param>
    /// <param name="phoneNumberIdOverride">
    /// An optional phone number id that takes precedence over
    /// <see cref="WhatsAppOptions.PhoneNumberId"/> when resolving the sending identity.
    /// </param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> wrapping the sent <see cref="MessageResponse"/> on
    /// success; a failed <see cref="Result{T}"/> when the Graph API call fails.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when both or neither of <paramref name="mediaId"/> and <paramref name="link"/> are
    /// supplied, before any HTTP request is sent.
    /// </exception>
    Task<Result<MessageResponse>> SendDocumentAsync(string to, string? mediaId = null, string? link = null, string? caption = null, Context? context = null, string? phoneNumberIdOverride = null, CancellationToken ct = default);

    /// <summary>
    /// Sends a video message via <c>POST /{phone_number_id}/messages</c>. Exactly one of
    /// <paramref name="mediaId"/> or <paramref name="link"/> must be supplied.
    /// </summary>
    /// <param name="to">The recipient's phone number, in E.164 format.</param>
    /// <param name="mediaId">The uploaded media's opaque id. Mutually exclusive with <paramref name="link"/>.</param>
    /// <param name="link">A public URL Meta will fetch the media from. Mutually exclusive with <paramref name="mediaId"/>.</param>
    /// <param name="caption">An optional caption shown alongside the video.</param>
    /// <param name="context">An optional reference to a prior inbound message being replied to.</param>
    /// <param name="phoneNumberIdOverride">
    /// An optional phone number id that takes precedence over
    /// <see cref="WhatsAppOptions.PhoneNumberId"/> when resolving the sending identity.
    /// </param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> wrapping the sent <see cref="MessageResponse"/> on
    /// success; a failed <see cref="Result{T}"/> when the Graph API call fails.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when both or neither of <paramref name="mediaId"/> and <paramref name="link"/> are
    /// supplied, before any HTTP request is sent.
    /// </exception>
    Task<Result<MessageResponse>> SendVideoAsync(string to, string? mediaId = null, string? link = null, string? caption = null, Context? context = null, string? phoneNumberIdOverride = null, CancellationToken ct = default);

    /// <summary>
    /// Sends an audio message via <c>POST /{phone_number_id}/messages</c>. Exactly one of
    /// <paramref name="mediaId"/> or <paramref name="link"/> must be supplied. Meta does not
    /// support a caption on audio messages.
    /// </summary>
    /// <param name="to">The recipient's phone number, in E.164 format.</param>
    /// <param name="mediaId">The uploaded media's opaque id. Mutually exclusive with <paramref name="link"/>.</param>
    /// <param name="link">A public URL Meta will fetch the media from. Mutually exclusive with <paramref name="mediaId"/>.</param>
    /// <param name="context">An optional reference to a prior inbound message being replied to.</param>
    /// <param name="phoneNumberIdOverride">
    /// An optional phone number id that takes precedence over
    /// <see cref="WhatsAppOptions.PhoneNumberId"/> when resolving the sending identity.
    /// </param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> wrapping the sent <see cref="MessageResponse"/> on
    /// success; a failed <see cref="Result{T}"/> when the Graph API call fails.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when both or neither of <paramref name="mediaId"/> and <paramref name="link"/> are
    /// supplied, before any HTTP request is sent.
    /// </exception>
    Task<Result<MessageResponse>> SendAudioAsync(string to, string? mediaId = null, string? link = null, Context? context = null, string? phoneNumberIdOverride = null, CancellationToken ct = default);

    /// <summary>
    /// Sends a sticker message via <c>POST /{phone_number_id}/messages</c>. Exactly one of
    /// <paramref name="mediaId"/> or <paramref name="link"/> must be supplied. Meta does not
    /// support a caption on sticker messages.
    /// </summary>
    /// <param name="to">The recipient's phone number, in E.164 format.</param>
    /// <param name="mediaId">The uploaded media's opaque id. Mutually exclusive with <paramref name="link"/>.</param>
    /// <param name="link">A public URL Meta will fetch the media from. Mutually exclusive with <paramref name="mediaId"/>.</param>
    /// <param name="context">An optional reference to a prior inbound message being replied to.</param>
    /// <param name="phoneNumberIdOverride">
    /// An optional phone number id that takes precedence over
    /// <see cref="WhatsAppOptions.PhoneNumberId"/> when resolving the sending identity.
    /// </param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> wrapping the sent <see cref="MessageResponse"/> on
    /// success; a failed <see cref="Result{T}"/> when the Graph API call fails.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when both or neither of <paramref name="mediaId"/> and <paramref name="link"/> are
    /// supplied, before any HTTP request is sent.
    /// </exception>
    Task<Result<MessageResponse>> SendStickerAsync(string to, string? mediaId = null, string? link = null, Context? context = null, string? phoneNumberIdOverride = null, CancellationToken ct = default);

    /// <summary>
    /// Sends a location message via <c>POST /{phone_number_id}/messages</c>.
    /// </summary>
    /// <param name="to">The recipient's phone number, in E.164 format.</param>
    /// <param name="latitude">The location's latitude.</param>
    /// <param name="longitude">The location's longitude.</param>
    /// <param name="name">An optional name for the location.</param>
    /// <param name="address">An optional address for the location.</param>
    /// <param name="context">An optional reference to a prior inbound message being replied to.</param>
    /// <param name="phoneNumberIdOverride">
    /// An optional phone number id that takes precedence over
    /// <see cref="WhatsAppOptions.PhoneNumberId"/> when resolving the sending identity.
    /// </param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> wrapping the sent <see cref="MessageResponse"/> on
    /// success; a failed <see cref="Result{T}"/> when the Graph API call fails.
    /// </returns>
    Task<Result<MessageResponse>> SendLocationAsync(string to, double latitude, double longitude, string? name = null, string? address = null, Context? context = null, string? phoneNumberIdOverride = null, CancellationToken ct = default);

    /// <summary>
    /// Sends one or more contact cards via <c>POST /{phone_number_id}/messages</c>.
    /// </summary>
    /// <param name="to">The recipient's phone number, in E.164 format.</param>
    /// <param name="contacts">The contact cards to send.</param>
    /// <param name="context">An optional reference to a prior inbound message being replied to.</param>
    /// <param name="phoneNumberIdOverride">
    /// An optional phone number id that takes precedence over
    /// <see cref="WhatsAppOptions.PhoneNumberId"/> when resolving the sending identity.
    /// </param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> wrapping the sent <see cref="MessageResponse"/> on
    /// success; a failed <see cref="Result{T}"/> when the Graph API call fails.
    /// </returns>
    Task<Result<MessageResponse>> SendContactsAsync(string to, IReadOnlyList<Contact> contacts, Context? context = null, string? phoneNumberIdOverride = null, CancellationToken ct = default);

    /// <summary>
    /// Sends a reply-buttons interactive message via <c>POST /{phone_number_id}/messages</c>.
    /// <paramref name="buttons"/> must contain 1-3 items. <paramref name="header"/> may be any
    /// header kind (text, image, video, document) or omitted.
    /// </summary>
    /// <param name="to">The recipient's phone number, in E.164 format.</param>
    /// <param name="bodyText">The message body text.</param>
    /// <param name="buttons">The 1-3 reply buttons offered to the recipient.</param>
    /// <param name="header">An optional header of any kind (text, image, video, document).</param>
    /// <param name="footerText">Optional footer text.</param>
    /// <param name="context">An optional reference to a prior inbound message being replied to.</param>
    /// <param name="phoneNumberIdOverride">
    /// An optional phone number id that takes precedence over
    /// <see cref="WhatsAppOptions.PhoneNumberId"/> when resolving the sending identity.
    /// </param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> wrapping the sent <see cref="MessageResponse"/> on
    /// success; a failed <see cref="Result{T}"/> when the Graph API call fails.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="bodyText"/> is null, empty, or whitespace; or when
    /// <paramref name="buttons"/> is null, empty, or contains more than 3 items; or when any
    /// button's title exceeds 20 characters or id has leading/trailing whitespace. Thrown
    /// synchronously, before any HTTP request is sent.
    /// </exception>
    Task<Result<MessageResponse>> SendReplyButtonsAsync(string to, string bodyText, IReadOnlyList<ReplyButton> buttons, InteractiveHeader? header = null, string? footerText = null, Context? context = null, string? phoneNumberIdOverride = null, CancellationToken ct = default);
}
