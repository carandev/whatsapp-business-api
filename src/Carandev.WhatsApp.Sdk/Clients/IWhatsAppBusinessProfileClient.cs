using Carandev.WhatsApp.Sdk.Models.BusinessProfile;

namespace Carandev.WhatsApp.Sdk.Clients;

/// <summary>
/// Client for the WhatsApp Cloud API business profile resource: read and partially update the
/// WhatsApp Business Profile for a phone number. All methods are non-throwing for expected
/// failure modes — outcomes are reported via <see cref="Result"/>/<see cref="Result{T}"/>, never
/// an exception.
/// </summary>
public interface IWhatsAppBusinessProfileClient
{
    /// <summary>
    /// Fetches the business profile for a phone number via
    /// <c>GET /{phone_number_id}/whatsapp_business_profile</c>, requesting Meta's full documented
    /// <c>fields=</c> list explicitly. Meta wraps the response in a <c>{"data": [...]}</c>
    /// envelope containing a single-element array, which is unwrapped before returning.
    /// </summary>
    /// <param name="phoneNumberIdOverride">
    /// An optional phone number id that takes precedence over
    /// <see cref="WhatsAppOptions.PhoneNumberId"/> when resolving the target phone number. When
    /// omitted, <see cref="WhatsAppOptions.PhoneNumberId"/> is used.
    /// </param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> wrapping the business profile on success; a failed
    /// <see cref="Result{T}"/> when the API call fails or the response envelope is empty.
    /// </returns>
    Task<Result<global::Carandev.WhatsApp.Sdk.Models.BusinessProfile.BusinessProfile>> GetBusinessProfileAsync(string? phoneNumberIdOverride = null, CancellationToken ct = default);

    /// <summary>
    /// Partially updates the business profile for a phone number via
    /// <c>POST /{phone_number_id}/whatsapp_business_profile</c>. Only the fields explicitly set on
    /// <paramref name="request"/> are sent — unset fields are omitted from the outgoing JSON body
    /// and left unchanged by Meta.
    /// </summary>
    /// <param name="request">The partial update request.</param>
    /// <param name="phoneNumberIdOverride">
    /// An optional phone number id that takes precedence over
    /// <see cref="WhatsAppOptions.PhoneNumberId"/> when resolving the target phone number. When
    /// omitted, <see cref="WhatsAppOptions.PhoneNumberId"/> is used.
    /// </param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A successful <see cref="Result"/> on success; a failed <see cref="Result"/> otherwise.</returns>
    Task<Result> UpdateBusinessProfileAsync(UpdateBusinessProfileRequest request, string? phoneNumberIdOverride = null, CancellationToken ct = default);
}
