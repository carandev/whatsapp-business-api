using WhatsAppCloudApi.Resilience;

namespace WhatsAppCloudApi.Extensions.DependencyInjection.Resilience;

/// <summary>
/// The retry-eligibility predicate used by the <c>"whatsapp-retry"</c> resilience pipeline's
/// <c>ShouldHandle</c> delegate. Extracted into its own reusable, directly testable method so its
/// classification decisions can be exercised in isolation — without building a full Polly
/// pipeline — and kept in lockstep with
/// <see cref="WhatsAppCloudApi.Serialization.WhatsAppError.IsTransient"/>, which the pipeline must
/// never contradict.
/// </summary>
public static class WhatsAppRetryPredicate
{
    /// <summary>
    /// Determines whether an HTTP outcome should trigger a retry attempt: any transport-level
    /// exception, any HTTP 5xx response (regardless of body), or a non-5xx response whose body
    /// peeks (via <see cref="GraphErrorPeek"/>) as a Meta Graph error classified transient by
    /// <see cref="TransientErrorClassifier"/>. Every other outcome — including a successful
    /// response or a non-transient 4xx business error — is left alone so it can flow, unmodified,
    /// to the caller's existing <c>Result&lt;T&gt;.Failure</c> path.
    /// </summary>
    /// <param name="response">The response received for this attempt, or <see langword="null"/> when <paramref name="exception"/> is set.</param>
    /// <param name="exception">The exception thrown for this attempt, if any.</param>
    /// <param name="cancellationToken">A token to cancel the body peek.</param>
    public static async ValueTask<bool> ShouldRetryAsync(HttpResponseMessage? response, Exception? exception, CancellationToken cancellationToken = default)
    {
        if (exception is not null)
        {
            return true;
        }

        if (response is null || response.IsSuccessStatusCode)
        {
            return false;
        }

        if ((int)response.StatusCode >= 500)
        {
            return true;
        }

        var peek = await GraphErrorPeek.TryReadAsync(response, cancellationToken).ConfigureAwait(false);
        return peek is not null && TransientErrorClassifier.IsTransientGraphError(peek.Code, peek.ErrorSubcode);
    }
}
