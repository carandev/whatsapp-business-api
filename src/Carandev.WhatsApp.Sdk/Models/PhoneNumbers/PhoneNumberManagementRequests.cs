using System.Text.Json.Serialization;

namespace Carandev.WhatsApp.Sdk.Models.PhoneNumbers;

/// <summary>
/// Wire body of <c>POST /{phone-number-id}/register</c>, built by
/// <c>IWhatsAppPhoneNumbersClient.RegisterAsync</c>.
/// </summary>
public sealed record RegisterPhoneNumberRequest
{
    internal RegisterPhoneNumberRequest(string pin, string? dataLocalizationRegion)
    {
        Pin = pin;
        DataLocalizationRegion = dataLocalizationRegion;
    }

    /// <summary>
    /// Always <c>whatsapp</c>.
    /// </summary>
    [JsonPropertyName("messaging_product")]
    public string MessagingProduct => "whatsapp";

    /// <summary>
    /// The 6-digit two-step verification PIN.
    /// </summary>
    [JsonPropertyName("pin")]
    public string Pin { get; }

    /// <summary>
    /// The 2-letter ISO 3166 country code for local data storage, omitted when null.
    /// </summary>
    [JsonPropertyName("data_localization_region")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DataLocalizationRegion { get; }
}

/// <summary>
/// Wire body of <c>POST /{phone-number-id}</c> that sets the two-step verification PIN, built by
/// <c>IWhatsAppPhoneNumbersClient.SetTwoStepVerificationPinAsync</c>.
/// </summary>
public sealed record TwoStepVerificationPinRequest
{
    internal TwoStepVerificationPinRequest(string pin)
    {
        Pin = pin;
    }

    /// <summary>
    /// The 6-digit two-step verification PIN.
    /// </summary>
    [JsonPropertyName("pin")]
    public string Pin { get; }
}

/// <summary>
/// Wire body of <c>POST /{phone-number-id}</c> that sets or clears the phone number's webhook
/// callback override, built by <c>IWhatsAppPhoneNumbersClient.SetWebhookOverrideAsync</c> and
/// <c>ClearWebhookOverrideAsync</c>.
/// </summary>
public sealed record PhoneNumberWebhookOverrideRequest
{
    internal PhoneNumberWebhookOverrideRequest(PhoneNumberWebhookOverride webhookConfiguration)
    {
        WebhookConfiguration = webhookConfiguration;
    }

    /// <summary>
    /// The override to apply.
    /// </summary>
    [JsonPropertyName("webhook_configuration")]
    public PhoneNumberWebhookOverride WebhookConfiguration { get; }
}

/// <summary>
/// The <c>webhook_configuration</c> object of <see cref="PhoneNumberWebhookOverrideRequest"/>. An
/// empty <see cref="OverrideCallbackUri"/> removes the override.
/// </summary>
public sealed record PhoneNumberWebhookOverride
{
    internal PhoneNumberWebhookOverride(string overrideCallbackUri, string? verifyToken)
    {
        OverrideCallbackUri = overrideCallbackUri;
        VerifyToken = verifyToken;
    }

    /// <summary>
    /// The callback URL, or empty to remove the override.
    /// </summary>
    [JsonPropertyName("override_callback_uri")]
    public string OverrideCallbackUri { get; }

    /// <summary>
    /// The token Meta sends to the callback to verify it, omitted when null.
    /// </summary>
    [JsonPropertyName("verify_token")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? VerifyToken { get; }
}

/// <summary>
/// Validation shared by the phone number management calls.
/// </summary>
internal static class PhoneNumberArguments
{
    internal static void ThrowIfInvalidPin(string pin, string paramName)
    {
        ArgumentNullException.ThrowIfNull(pin, paramName);
        if (pin.Length != 6 || !pin.All(char.IsAsciiDigit))
        {
            throw new ArgumentException("The PIN must be exactly 6 digits (0-9).", paramName);
        }
    }

    internal static void ThrowIfInvalidRegion(string? region, string paramName)
    {
        if (region is not null && (region.Length != 2 || !region.All(char.IsAsciiLetter)))
        {
            throw new ArgumentException(
                "The data localization region must be a 2-letter ISO 3166 country code, for example CH.",
                paramName);
        }
    }

    internal static void ThrowIfInvalidCallback(Uri callbackUri, string paramName)
    {
        ArgumentNullException.ThrowIfNull(callbackUri, paramName);
        if (!callbackUri.IsAbsoluteUri || callbackUri.Scheme != Uri.UriSchemeHttps)
        {
            throw new ArgumentException("The callback must be an absolute HTTPS URL.", paramName);
        }
    }
}
