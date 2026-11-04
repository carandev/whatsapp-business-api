using Carandev.WhatsApp.Sdk.Resilience;
using Carandev.WhatsApp.Sdk.Serialization;

namespace Carandev.WhatsApp.Sdk.Exceptions;

/// <summary>
/// Maps a <see cref="WhatsAppError"/> to the most specific <see cref="WhatsAppException"/> subtype
/// for its <see cref="WhatsAppError.Code"/>, falling back to the base <see cref="WhatsAppException"/>
/// for codes that do not match any specialized subtype. Used exclusively by the resilience pipeline
/// on retry exhaustion.
/// </summary>
public static class WhatsAppExceptionFactory
{
    /// <summary>
    /// Authentication-related Graph API error codes: <c>190</c> (invalid/expired access token) and
    /// <c>200</c> (permission error), per Meta's documented authentication-error range.
    /// </summary>
    private static readonly IReadOnlySet<int> AuthenticationCodes = new HashSet<int> { 190, 200 };

    /// <summary>
    /// Template-rejection Graph API error codes in the <c>132000</c> range, covering template
    /// pacing, status, structure, and parameter-mismatch failures.
    /// </summary>
    private static readonly IReadOnlySet<int> TemplateRejectionCodes = new HashSet<int>
    {
        132000, 132001, 132005, 132007, 132012, 132015, 132016,
    };

    /// <summary>
    /// Creates the <see cref="WhatsAppException"/> subtype matching <paramref name="error"/>'s
    /// <see cref="WhatsAppError.Code"/>.
    /// </summary>
    /// <param name="error">The Graph API error to map.</param>
    /// <param name="innerException">The exception that triggered retry exhaustion, if any.</param>
    public static WhatsAppException Create(WhatsAppError error, Exception? innerException = null)
    {
        ArgumentNullException.ThrowIfNull(error);

        if (error.Code == TransientErrorClassifier.RateLimitCode)
        {
            return new WhatsAppRateLimitException(error, innerException);
        }

        if (AuthenticationCodes.Contains(error.Code))
        {
            return new WhatsAppAuthenticationException(error, innerException);
        }

        if (TemplateRejectionCodes.Contains(error.Code))
        {
            return new WhatsAppTemplateRejectedException(error, innerException);
        }

        return new WhatsAppException(error, innerException);
    }
}
