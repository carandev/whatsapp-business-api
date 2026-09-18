using WhatsAppCloudApi.Exceptions;
using WhatsAppCloudApi.Resilience;
using WhatsAppCloudApi.Serialization;

namespace WhatsAppCloudApi.Extensions.DependencyInjection.Resilience;

/// <summary>
/// Outer <see cref="DelegatingHandler"/> registered BEFORE (i.e. outside) the
/// <c>"whatsapp-retry"</c> resilience pipeline on the <see cref="IWhatsAppClient"/> typed-client
/// handler chain. Polly's <c>AddRetry</c> strategy does not throw when retries are exhausted — it
/// simply returns the last failed <see cref="HttpResponseMessage"/> to its caller. This handler
/// inspects that FINAL, post-retry outcome and, only when it is STILL classified transient
/// (meaning every retry attempt failed for the same retry-worthy reason), throws the matching
/// <see cref="WhatsAppException"/> subtype via <see cref="WhatsAppExceptionFactory"/>. A
/// non-transient (permanent business) 4xx response — which the retry pipeline never retried in the
/// first place — passes through completely unchanged, so <see cref="WhatsAppCloudApi.WhatsAppClient.SendAsync"/>
/// keeps returning it for the resource clients' existing <c>Result&lt;T&gt;.Failure</c> mapping.
/// </summary>
public sealed class WhatsAppRetryExhaustionHandler : DelegatingHandler
{
    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
        {
            return response;
        }

        // Same classification as WhatsAppRetryPredicate.ShouldRetryAsync (the pipeline's own
        // ShouldHandle delegate) — 5xx is always transient regardless of body; otherwise peek the
        // Graph body and check its code/subcode. Evaluated directly here (rather than by calling
        // ShouldRetryAsync itself) so GraphErrorPeek only buffers the body once: this method also
        // needs the peeked WhatsAppError to build the exception, not just a transient/permanent
        // boolean.
        var peek = await GraphErrorPeek.TryReadAsync(response, cancellationToken).ConfigureAwait(false);
        var isTransient = (int)response.StatusCode >= 500
            || (peek is not null && TransientErrorClassifier.IsTransientGraphError(peek.Code, peek.ErrorSubcode));

        if (!isTransient)
        {
            // Permanent business error: the pipeline never retried it, and this handler never
            // throws for it either. Flows through unchanged to WhatsAppClient.SendAsync's normal
            // return path so Result<T>.Failure mapping keeps working exactly as before.
            return response;
        }

        // Retries were genuinely exhausted on a transient failure. Build the WhatsAppError the same
        // way the resource clients' own ReadErrorAsync fallback does when the body isn't structured
        // JSON (echo the HTTP status into Code), then map to the matching subtype and throw.
        var error = peek ?? new WhatsAppError
        {
            Message = $"Request failed with status code {(int)response.StatusCode} ({response.StatusCode}) after retries were exhausted.",
            Code = (int)response.StatusCode,
        };

        throw WhatsAppExceptionFactory.Create(error);
    }
}
