using WhatsAppCloudApi.Serialization;

namespace WhatsAppCloudApi.Exceptions;

/// <summary>
/// Thrown by the resilience pipeline on retry exhaustion for a Graph API authentication error
/// (e.g. code <c>190</c> — invalid/expired access token, or code <c>200</c> — permission error).
/// </summary>
public sealed class WhatsAppAuthenticationException : WhatsAppException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WhatsAppAuthenticationException"/> class.
    /// </summary>
    /// <param name="error">The Graph API authentication error this exception represents.</param>
    /// <param name="innerException">The exception that triggered retry exhaustion, if any.</param>
    public WhatsAppAuthenticationException(WhatsAppError error, Exception? innerException = null)
        : base(error, innerException)
    {
    }
}
