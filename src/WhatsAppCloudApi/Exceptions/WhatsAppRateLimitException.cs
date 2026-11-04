using WhatsAppCloudApi.Serialization;

namespace WhatsAppCloudApi.Exceptions;

/// <summary>
/// Thrown by the resilience pipeline on retry exhaustion for a Graph API rate-limit error
/// (<see cref="Resilience.TransientErrorClassifier.RateLimitCode"/>, code <c>80007</c>).
/// </summary>
public sealed class WhatsAppRateLimitException : WhatsAppException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WhatsAppRateLimitException"/> class.
    /// </summary>
    /// <param name="error">The Graph API rate-limit error this exception represents.</param>
    /// <param name="innerException">The exception that triggered retry exhaustion, if any.</param>
    public WhatsAppRateLimitException(WhatsAppError error, Exception? innerException = null)
        : base(error, innerException)
    {
    }
}
