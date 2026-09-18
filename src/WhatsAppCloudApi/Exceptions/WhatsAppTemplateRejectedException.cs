using WhatsAppCloudApi.Serialization;

namespace WhatsAppCloudApi.Exceptions;

/// <summary>
/// Thrown by the resilience pipeline on retry exhaustion for a Graph API template-rejection error
/// (the <c>132000</c>-range codes covering template pacing, status, structure, and parameter
/// mismatches).
/// </summary>
public sealed class WhatsAppTemplateRejectedException : WhatsAppException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WhatsAppTemplateRejectedException"/> class.
    /// </summary>
    /// <param name="error">The Graph API template-rejection error this exception represents.</param>
    /// <param name="innerException">The exception that triggered retry exhaustion, if any.</param>
    public WhatsAppTemplateRejectedException(WhatsAppError error, Exception? innerException = null)
        : base(error, innerException)
    {
    }
}
