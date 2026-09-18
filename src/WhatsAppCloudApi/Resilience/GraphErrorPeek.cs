using System.Text.Json;
using WhatsAppCloudApi.Serialization;

namespace WhatsAppCloudApi.Resilience;

/// <summary>
/// Peeks at a Meta Graph API error body carried by an <see cref="HttpResponseMessage"/> without
/// consuming its content stream, so both the resilience retry predicate and any downstream reader
/// (e.g. a resource client's own error deserialization) can independently read the same body.
/// </summary>
public static class GraphErrorPeek
{
    /// <summary>
    /// Buffers <paramref name="response"/>'s content into memory, replaces
    /// <see cref="HttpResponseMessage.Content"/> with a fresh, equivalent, re-readable
    /// <see cref="ByteArrayContent"/> wrapping the same bytes, and attempts to deserialize the
    /// buffered bytes as a <see cref="WhatsAppErrorResponse"/>.
    /// </summary>
    /// <param name="response">The response whose body should be peeked.</param>
    /// <param name="cancellationToken">A token to cancel the read.</param>
    /// <returns>
    /// The parsed <see cref="WhatsAppError"/> when the body is valid JSON matching the Graph error
    /// envelope shape; otherwise <see langword="null"/>. Never throws on malformed or non-JSON
    /// bodies.
    /// </returns>
    public static async Task<WhatsAppError?> TryReadAsync(HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);

        var contentType = response.Content.Headers.ContentType;
        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);

        // Replace the (now-consumed) content with a fresh copy of the same bytes so downstream
        // readers — the resilience exhaustion handler in a later PR, or a resource client's own
        // ReadErrorAsync — can still read the full body.
        var replacement = new ByteArrayContent(bytes);
        replacement.Headers.ContentType = contentType;
        response.Content = replacement;

        if (bytes.Length == 0)
        {
            return null;
        }

        try
        {
            var errorResponse = JsonSerializer.Deserialize(bytes, WhatsAppJsonSerializerContext.Default.WhatsAppErrorResponse);
            return errorResponse?.Error;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
