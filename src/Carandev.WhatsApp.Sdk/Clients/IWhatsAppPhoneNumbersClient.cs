using Carandev.WhatsApp.Sdk.Models.PhoneNumbers;
using Carandev.WhatsApp.Sdk.Models.Templates;

namespace Carandev.WhatsApp.Sdk.Clients;

/// <summary>
/// Client for the WhatsApp Cloud API phone numbers resource: list a WABA's registered phone
/// numbers, inspect a single number's state, and run the two-step verification code flow. All
/// methods are non-throwing for expected failure modes — outcomes are reported via
/// <see cref="Result"/>/<see cref="Result{T}"/>, never an exception.
/// </summary>
public interface IWhatsAppPhoneNumbersClient
{
    /// <summary>
    /// Lists the phone numbers registered to a WABA via <c>GET /{waba_id}/phone_numbers</c>.
    /// </summary>
    /// <param name="wabaIdOverride">
    /// An optional WABA id that takes precedence over <see cref="WhatsAppOptions.WabaId"/> when
    /// resolving the target WABA. When omitted, <see cref="WhatsAppOptions.WabaId"/> is used.
    /// </param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> wrapping a <see cref="TemplatesPage{T}"/> of
    /// <see cref="PhoneNumber"/> on success; a failed <see cref="Result{T}"/> when the WABA id
    /// cannot be resolved (no HTTP request is sent in that case) or the API call fails.
    /// </returns>
    Task<Result<TemplatesPage<PhoneNumber>>> ListPhoneNumbersAsync(string? wabaIdOverride = null, CancellationToken ct = default);

    /// <summary>
    /// Lists one page of the phone numbers registered to a WABA via
    /// <c>GET /{waba_id}/phone_numbers</c>, with <paramref name="options"/> sent as the
    /// <c>limit</c>, <c>after</c>, and <c>before</c> query parameters.
    /// </summary>
    /// <param name="options">The page size and cursor. Null fields are omitted from the request.</param>
    /// <param name="wabaIdOverride">
    /// An optional WABA id that takes precedence over <see cref="WhatsAppOptions.WabaId"/> when
    /// resolving the target WABA. When omitted, <see cref="WhatsAppOptions.WabaId"/> is used.
    /// </param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> wrapping a <see cref="TemplatesPage{T}"/> of
    /// <see cref="PhoneNumber"/>, whose <c>Paging.Cursors.After</c> is the cursor for the next page;
    /// a failed <see cref="Result{T}"/> when the WABA id cannot be resolved (no HTTP request is sent
    /// in that case) or the API call fails.
    /// </returns>
    /// <remarks>
    /// The default implementation throws <see cref="NotSupportedException"/> so that third-party
    /// implementations written against 0.1.0 keep compiling; <see cref="WhatsAppPhoneNumbersClient"/>
    /// implements it.
    /// </remarks>
    /// <exception cref="NotSupportedException">The implementation does not support paging.</exception>
    Task<Result<TemplatesPage<PhoneNumber>>> ListPhoneNumbersAsync(PhoneNumbersListOptions options, string? wabaIdOverride = null, CancellationToken ct = default) =>
        throw new NotSupportedException(
            $"{GetType().FullName} does not implement {nameof(ListPhoneNumbersAsync)} with {nameof(PhoneNumbersListOptions)}. Use WhatsAppPhoneNumbersClient or implement the member.");

    /// <summary>
    /// Fetches a single phone number's details via <c>GET /{phone_number_id}</c>.
    /// </summary>
    /// <param name="phoneNumberIdOverride">
    /// An optional phone number id that takes precedence over
    /// <see cref="WhatsAppOptions.PhoneNumberId"/> when resolving the target phone number. When
    /// omitted, <see cref="WhatsAppOptions.PhoneNumberId"/> is used.
    /// </param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> wrapping the full <see cref="PhoneNumber"/> on
    /// success; a failed <see cref="Result{T}"/> (including a not-found error) when the API call
    /// fails.
    /// </returns>
    Task<Result<PhoneNumber>> GetPhoneNumberAsync(string? phoneNumberIdOverride = null, CancellationToken ct = default);

    /// <summary>
    /// Requests a verification code for a phone number via
    /// <c>POST /{phone_number_id}/request_code</c>. Meta's response contains only a success flag —
    /// no code id or other payload is returned.
    /// </summary>
    /// <param name="method">The delivery method for the verification code.</param>
    /// <param name="languageCode">
    /// The language code the verification message/call should be delivered in, e.g. <c>en_US</c>.
    /// </param>
    /// <param name="phoneNumberIdOverride">
    /// An optional phone number id that takes precedence over
    /// <see cref="WhatsAppOptions.PhoneNumberId"/> when resolving the target phone number. When
    /// omitted, <see cref="WhatsAppOptions.PhoneNumberId"/> is used.
    /// </param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A successful <see cref="Result"/> on success; a failed <see cref="Result"/> otherwise.</returns>
    Task<Result> RequestVerificationCodeAsync(VerificationCodeMethod method, string languageCode, string? phoneNumberIdOverride = null, CancellationToken ct = default);

    /// <summary>
    /// Verifies a previously requested code for a phone number via
    /// <c>POST /{phone_number_id}/verify_code</c>. Meta's request body only needs the code itself
    /// — there is no <c>code_id</c> parameter.
    /// </summary>
    /// <param name="code">The verification code to submit.</param>
    /// <param name="phoneNumberIdOverride">
    /// An optional phone number id that takes precedence over
    /// <see cref="WhatsAppOptions.PhoneNumberId"/> when resolving the target phone number. When
    /// omitted, <see cref="WhatsAppOptions.PhoneNumberId"/> is used.
    /// </param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A successful <see cref="Result"/> on success; a failed <see cref="Result"/> otherwise.</returns>
    Task<Result> VerifyCodeAsync(string code, string? phoneNumberIdOverride = null, CancellationToken ct = default);

    /// <summary>
    /// Registers a phone number for Cloud API use via <c>POST /{phone_number_id}/register</c>, so
    /// it can send and receive messages.
    /// </summary>
    /// <param name="pin">
    /// The 6-digit two-step verification PIN. If two-step verification is already enabled, this
    /// must be the current PIN; otherwise it becomes the new PIN.
    /// </param>
    /// <param name="dataLocalizationRegion">
    /// An optional 2-letter ISO 3166 country code, for example <c>CH</c>, to store the number's
    /// data at rest in that region. Omitted from the request when null.
    /// </param>
    /// <param name="phoneNumberIdOverride">
    /// An optional phone number id that takes precedence over
    /// <see cref="WhatsAppOptions.PhoneNumberId"/> when resolving the target phone number. When
    /// omitted, <see cref="WhatsAppOptions.PhoneNumberId"/> is used.
    /// </param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A successful <see cref="Result"/> on success; a failed <see cref="Result"/> otherwise.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="pin"/> is not exactly 6 digits, or <paramref name="dataLocalizationRegion"/>
    /// is not 2 letters. No request is sent.
    /// </exception>
    /// <remarks>
    /// The default implementation throws <see cref="NotSupportedException"/> so that third-party
    /// implementations written against earlier versions keep compiling;
    /// <see cref="WhatsAppPhoneNumbersClient"/> implements it.
    /// </remarks>
    Task<Result> RegisterAsync(string pin, string? dataLocalizationRegion = null, string? phoneNumberIdOverride = null, CancellationToken ct = default) =>
        throw new NotSupportedException(
            $"{GetType().FullName} does not implement {nameof(RegisterAsync)}. Use WhatsAppPhoneNumbersClient or implement the member.");

    /// <summary>
    /// Deregisters a phone number from Cloud API via <c>POST /{phone_number_id}/deregister</c>.
    /// The number cannot send or receive until it is registered again; its data is kept.
    /// </summary>
    /// <param name="phoneNumberIdOverride">
    /// An optional phone number id that takes precedence over
    /// <see cref="WhatsAppOptions.PhoneNumberId"/> when resolving the target phone number. When
    /// omitted, <see cref="WhatsAppOptions.PhoneNumberId"/> is used.
    /// </param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A successful <see cref="Result"/> on success; a failed <see cref="Result"/> otherwise.</returns>
    /// <remarks>
    /// The default implementation throws <see cref="NotSupportedException"/> so that third-party
    /// implementations written against earlier versions keep compiling;
    /// <see cref="WhatsAppPhoneNumbersClient"/> implements it.
    /// </remarks>
    Task<Result> DeregisterAsync(string? phoneNumberIdOverride = null, CancellationToken ct = default) =>
        throw new NotSupportedException(
            $"{GetType().FullName} does not implement {nameof(DeregisterAsync)}. Use WhatsAppPhoneNumbersClient or implement the member.");

    /// <summary>
    /// Sets or changes a phone number's two-step verification PIN via
    /// <c>POST /{phone_number_id}</c> with <c>pin</c>.
    /// </summary>
    /// <param name="pin">The new 6-digit PIN.</param>
    /// <param name="phoneNumberIdOverride">
    /// An optional phone number id that takes precedence over
    /// <see cref="WhatsAppOptions.PhoneNumberId"/> when resolving the target phone number. When
    /// omitted, <see cref="WhatsAppOptions.PhoneNumberId"/> is used.
    /// </param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A successful <see cref="Result"/> on success; a failed <see cref="Result"/> otherwise.</returns>
    /// <exception cref="ArgumentException"><paramref name="pin"/> is not exactly 6 digits. No request is sent.</exception>
    /// <remarks>
    /// The default implementation throws <see cref="NotSupportedException"/> so that third-party
    /// implementations written against earlier versions keep compiling;
    /// <see cref="WhatsAppPhoneNumbersClient"/> implements it.
    /// </remarks>
    Task<Result> SetTwoStepVerificationPinAsync(string pin, string? phoneNumberIdOverride = null, CancellationToken ct = default) =>
        throw new NotSupportedException(
            $"{GetType().FullName} does not implement {nameof(SetTwoStepVerificationPinAsync)}. Use WhatsAppPhoneNumbersClient or implement the member.");

    /// <summary>
    /// Sends this phone number's webhooks to its own callback URL, overriding the WABA and app
    /// callbacks, via <c>POST /{phone_number_id}</c> with
    /// <c>webhook_configuration: { override_callback_uri, verify_token }</c>. Meta verifies the
    /// callback with <paramref name="verifyToken"/> before accepting it.
    /// </summary>
    /// <param name="callbackUri">The callback URL. Must be an absolute HTTPS URL.</param>
    /// <param name="verifyToken">The token Meta sends to the callback to verify it. Must not be empty.</param>
    /// <param name="phoneNumberIdOverride">
    /// An optional phone number id that takes precedence over
    /// <see cref="WhatsAppOptions.PhoneNumberId"/> when resolving the target phone number. When
    /// omitted, <see cref="WhatsAppOptions.PhoneNumberId"/> is used.
    /// </param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A successful <see cref="Result"/> on success; a failed <see cref="Result"/> otherwise.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="callbackUri"/> is not an absolute HTTPS URL, or
    /// <paramref name="verifyToken"/> is null, empty, or whitespace. No request is sent.
    /// </exception>
    /// <remarks>
    /// The default implementation throws <see cref="NotSupportedException"/> so that third-party
    /// implementations written against earlier versions keep compiling;
    /// <see cref="WhatsAppPhoneNumbersClient"/> implements it.
    /// </remarks>
    Task<Result> SetWebhookOverrideAsync(Uri callbackUri, string verifyToken, string? phoneNumberIdOverride = null, CancellationToken ct = default) =>
        throw new NotSupportedException(
            $"{GetType().FullName} does not implement {nameof(SetWebhookOverrideAsync)}. Use WhatsAppPhoneNumbersClient or implement the member.");

    /// <summary>
    /// Removes this phone number's webhook callback override via <c>POST /{phone_number_id}</c>
    /// with an empty <c>override_callback_uri</c>, so its webhooks go back to the WABA or app
    /// callback.
    /// </summary>
    /// <param name="phoneNumberIdOverride">
    /// An optional phone number id that takes precedence over
    /// <see cref="WhatsAppOptions.PhoneNumberId"/> when resolving the target phone number. When
    /// omitted, <see cref="WhatsAppOptions.PhoneNumberId"/> is used.
    /// </param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A successful <see cref="Result"/> on success; a failed <see cref="Result"/> otherwise.</returns>
    /// <remarks>
    /// The default implementation throws <see cref="NotSupportedException"/> so that third-party
    /// implementations written against earlier versions keep compiling;
    /// <see cref="WhatsAppPhoneNumbersClient"/> implements it.
    /// </remarks>
    Task<Result> ClearWebhookOverrideAsync(string? phoneNumberIdOverride = null, CancellationToken ct = default) =>
        throw new NotSupportedException(
            $"{GetType().FullName} does not implement {nameof(ClearWebhookOverrideAsync)}. Use WhatsAppPhoneNumbersClient or implement the member.");

    /// <summary>
    /// Reads a phone number's webhook callbacks via
    /// <c>GET /{phone_number_id}?fields=webhook_configuration</c>. Meta does not include this
    /// field in a plain <see cref="GetPhoneNumberAsync"/> response.
    /// </summary>
    /// <param name="phoneNumberIdOverride">
    /// An optional phone number id that takes precedence over
    /// <see cref="WhatsAppOptions.PhoneNumberId"/> when resolving the target phone number. When
    /// omitted, <see cref="WhatsAppOptions.PhoneNumberId"/> is used.
    /// </param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> wrapping the <see cref="PhoneNumberWebhookConfiguration"/>
    /// (with null callbacks when Meta returns none); a failed <see cref="Result{T}"/> otherwise.
    /// </returns>
    /// <remarks>
    /// The default implementation throws <see cref="NotSupportedException"/> so that third-party
    /// implementations written against earlier versions keep compiling;
    /// <see cref="WhatsAppPhoneNumbersClient"/> implements it.
    /// </remarks>
    Task<Result<PhoneNumberWebhookConfiguration>> GetWebhookConfigurationAsync(string? phoneNumberIdOverride = null, CancellationToken ct = default) =>
        throw new NotSupportedException(
            $"{GetType().FullName} does not implement {nameof(GetWebhookConfigurationAsync)}. Use WhatsAppPhoneNumbersClient or implement the member.");
}
