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
}
