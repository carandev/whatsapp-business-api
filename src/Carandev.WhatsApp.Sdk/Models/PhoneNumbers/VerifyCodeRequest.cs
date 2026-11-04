using System.Text.Json.Serialization;

namespace Carandev.WhatsApp.Sdk.Models.PhoneNumbers;

/// <summary>
/// An immutable, construction-validated request to verify a phone number verification code via
/// <c>IWhatsAppPhoneNumbersClient.VerifyCodeAsync</c>. Meta's
/// <c>POST /{phone-number-id}/verify_code</c> endpoint only requires the code itself — there is
/// no <c>code_id</c> parameter.
/// </summary>
public sealed record VerifyCodeRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VerifyCodeRequest"/> class.
    /// </summary>
    /// <param name="code">The verification code to submit. Must not be null, empty, or whitespace.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="code"/> is null, empty, or whitespace.
    /// </exception>
    public VerifyCodeRequest(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        Code = code;
    }

    /// <summary>
    /// The verification code to submit.
    /// </summary>
    [JsonPropertyName("code")]
    public string Code { get; }
}
