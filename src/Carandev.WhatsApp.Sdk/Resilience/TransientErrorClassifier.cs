namespace Carandev.WhatsApp.Sdk.Resilience;

/// <summary>
/// Classifies a Meta Graph API error as transient (safe to retry) or permanent, based on the
/// error's numeric <c>code</c> and <c>error_subcode</c>. Shared by <see cref="Carandev.WhatsApp.Sdk.Serialization.WhatsAppError.IsTransient"/>
/// and, in later PRs, the Polly retry pipeline's <c>ShouldHandle</c> predicate, so classification
/// logic is written exactly once.
/// </summary>
public static class TransientErrorClassifier
{
    /// <summary>
    /// The Graph API rate-limit error code (<c>80007</c>). Always transient.
    /// </summary>
    public const int RateLimitCode = 80007;

    /// <summary>
    /// A small, extensible set of Meta <c>error_subcode</c> values known to indicate a transient
    /// condition (throttling/rate limiting) rather than a permanent business error. Seeded from
    /// Meta's documented WhatsApp Cloud API rate-limit subcodes; extend as new transient subcodes
    /// are identified.
    /// </summary>
    public static readonly IReadOnlySet<int> TransientSubcodes = new HashSet<int>
    {
        131048, // Spam rate limit hit
        131056, // Too many requests due to a phone-number pair rate limit
        133016, // Message throughput exceeded
    };

    /// <summary>
    /// Determines whether a Graph API error identified by <paramref name="code"/> and
    /// <paramref name="subcode"/> is transient (safe to retry).
    /// </summary>
    /// <param name="code">The Graph API top-level error code.</param>
    /// <param name="subcode">The Graph API error subcode, when present.</param>
    public static bool IsTransientGraphError(int code, int? subcode) =>
        code == RateLimitCode || (subcode is { } s && TransientSubcodes.Contains(s));
}
