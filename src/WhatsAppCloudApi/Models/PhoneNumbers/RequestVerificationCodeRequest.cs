using System.Text.Json.Serialization;

namespace WhatsAppCloudApi.Models.PhoneNumbers;

/// <summary>
/// An immutable, construction-validated request to send a phone number verification code via
/// <c>IWhatsAppPhoneNumbersClient.RequestVerificationCodeAsync</c>. Meta's
/// <c>POST /{phone-number-id}/request_code</c> endpoint responds with only
/// <c>{"success": boolean}</c> — no response payload type exists for this request.
/// </summary>
public sealed record RequestVerificationCodeRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RequestVerificationCodeRequest"/> class.
    /// </summary>
    /// <param name="codeMethod">The delivery method for the verification code.</param>
    /// <param name="language">
    /// The language code the verification message/call should be delivered in, e.g. <c>en_US</c>.
    /// Must not be null, empty, or whitespace.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="language"/> is null, empty, or whitespace.
    /// </exception>
    public RequestVerificationCodeRequest(VerificationCodeMethod codeMethod, string language)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(language);

        CodeMethod = codeMethod;
        Language = language;
    }

    /// <summary>
    /// The delivery method for the verification code.
    /// </summary>
    [JsonPropertyName("code_method")]
    public VerificationCodeMethod CodeMethod { get; }

    /// <summary>
    /// The language code the verification message/call should be delivered in.
    /// </summary>
    [JsonPropertyName("language")]
    public string Language { get; }
}
