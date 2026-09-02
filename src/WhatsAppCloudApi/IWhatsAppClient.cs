namespace WhatsAppCloudApi;

/// <summary>
/// Low-level typed HTTP client for the WhatsApp Cloud API. Builds authenticated requests
/// against the configured Graph API base address and sends them. Resource clients (e.g.
/// <c>IWhatsAppMessagesClient</c>) depend on this to perform their HTTP calls.
/// </summary>
public interface IWhatsAppClient
{
    /// <summary>
    /// Creates an <see cref="HttpRequestMessage"/> with the <c>Authorization</c> header set from
    /// <see cref="WhatsAppOptions.AccessToken"/>.
    /// </summary>
    /// <param name="method">The HTTP method to use.</param>
    /// <param name="relativePathTemplate">
    /// A relative path template, e.g. <c>"{phone_number_id}/messages"</c>. Any
    /// <c>{phone_number_id}</c> token is replaced with the effective phone number ID.
    /// </param>
    /// <param name="phoneNumberIdOverride">
    /// An optional phone number ID that takes precedence over
    /// <see cref="WhatsAppOptions.PhoneNumberId"/> when resolving the <c>{phone_number_id}</c>
    /// token. When omitted, <see cref="WhatsAppOptions.PhoneNumberId"/> is used.
    /// </param>
    /// <returns>A request message ready to be sent via <see cref="SendAsync"/>.</returns>
    HttpRequestMessage CreateRequest(HttpMethod method, string relativePathTemplate, string? phoneNumberIdOverride = null);

    /// <summary>
    /// Creates an <see cref="HttpRequestMessage"/> with the <c>Authorization</c> header set from
    /// <see cref="WhatsAppOptions.AccessToken"/>, for WABA-scoped resources (e.g. message
    /// templates).
    /// </summary>
    /// <param name="method">The HTTP method to use.</param>
    /// <param name="relativePathTemplate">
    /// A relative path template, e.g. <c>"{waba_id}/message_templates"</c>. Any
    /// <c>{waba_id}</c> token is replaced with the effective WABA ID.
    /// </param>
    /// <param name="wabaIdOverride">
    /// An optional WABA ID that takes precedence over <see cref="WhatsAppOptions.WabaId"/> when
    /// resolving the <c>{waba_id}</c> token. When omitted, <see cref="WhatsAppOptions.WabaId"/>
    /// is used.
    /// </param>
    /// <returns>A request message ready to be sent via <see cref="SendAsync"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// Neither <paramref name="wabaIdOverride"/> nor <see cref="WhatsAppOptions.WabaId"/> is set.
    /// </exception>
    HttpRequestMessage CreateWabaRequest(HttpMethod method, string relativePathTemplate, string? wabaIdOverride = null);

    /// <summary>
    /// Sends the given request using the underlying <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="request">The request to send, typically built via <see cref="CreateRequest"/>.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default);
}
