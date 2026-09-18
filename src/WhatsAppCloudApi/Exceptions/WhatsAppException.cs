using WhatsAppCloudApi.Serialization;

namespace WhatsAppCloudApi.Exceptions;

/// <summary>
/// Base exception for the WhatsApp Cloud API client library. Thrown ONLY by the resilience
/// pipeline on retry exhaustion for a transient failure — never thrown directly by a resource
/// client, whose non-transient failures continue to surface as <see cref="Result{T}.Failure"/>.
/// Constructed FROM a <see cref="WhatsAppError"/> so field-mapping logic is written exactly once.
/// </summary>
public class WhatsAppException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WhatsAppException"/> class, copying every
    /// user-facing field from <paramref name="error"/>.
    /// </summary>
    /// <param name="error">The Graph API error this exception represents.</param>
    /// <param name="innerException">The exception that triggered retry exhaustion, if any.</param>
    public WhatsAppException(WhatsAppError error, Exception? innerException = null)
        : base(error?.Message ?? "The WhatsApp Cloud API returned an unspecified error.", innerException)
    {
        ArgumentNullException.ThrowIfNull(error);

        Code = error.Code;
        Subcode = error.ErrorSubcode;
        Title = error.Type;
        UserTitle = error.ErrorUserTitle;
        UserMessage = error.ErrorUserMsg;
        FbTraceId = error.FbtraceId;
        IsTransient = error.IsTransient;
    }

    /// <summary>
    /// The Graph API numeric error code.
    /// </summary>
    public int Code { get; }

    /// <summary>
    /// An optional, more specific error subcode.
    /// </summary>
    public int? Subcode { get; }

    /// <summary>
    /// The Graph API error type, e.g. <c>OAuthException</c>.
    /// </summary>
    public string? Title { get; }

    /// <summary>
    /// A short, user-facing error title suitable for display, when provided by Meta.
    /// </summary>
    public string? UserTitle { get; }

    /// <summary>
    /// A user-facing error message suitable for display, when provided by Meta.
    /// </summary>
    public string? UserMessage { get; }

    /// <summary>
    /// A Facebook trace identifier useful when contacting Meta support.
    /// </summary>
    public string? FbTraceId { get; }

    /// <summary>
    /// Whether the source <see cref="WhatsAppError"/> was considered transient (safe to retry).
    /// Always <see langword="true"/> in practice, since this hierarchy is thrown only on retry
    /// exhaustion of a transient failure.
    /// </summary>
    public bool IsTransient { get; }
}
