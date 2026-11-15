namespace Carandev.WhatsApp.Sdk;

/// <summary>
/// Ambient, per-async-flow credential overrides. Use it to send requests with a specific
/// business's access token without changing any method signature:
/// <code>
/// using (WhatsAppCredentials.UseAccessToken(tenant.AccessToken))
/// {
///     await messages.SendTextAsync(request);
/// }
/// </code>
/// </summary>
/// <remarks>
/// The override flows with the current <see cref="ExecutionContext"/> (it is backed by
/// <see cref="AsyncLocal{T}"/>), so concurrent requests for different tenants do not see each
/// other's token. It wins over <see cref="IWhatsAppCredentialProvider"/> and
/// <see cref="WhatsAppOptions.AccessToken"/>.
/// </remarks>
public static class WhatsAppCredentials
{
    private static readonly AsyncLocal<string?> CurrentToken = new();

    /// <summary>
    /// The access token set by the innermost active <see cref="UseAccessToken"/> scope, or
    /// <see langword="null"/> when none is active.
    /// </summary>
    internal static string? CurrentAccessToken => CurrentToken.Value;

    /// <summary>
    /// Uses <paramref name="accessToken"/> for every request sent in the current async flow until
    /// the returned scope is disposed. Scopes nest; disposing one restores the token that was active
    /// when it was created.
    /// </summary>
    /// <param name="accessToken">The access token to send.</param>
    /// <returns>A scope that restores the previous token when disposed.</returns>
    /// <exception cref="ArgumentException"><paramref name="accessToken"/> is null, empty, or whitespace.</exception>
    public static IDisposable UseAccessToken(string accessToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        var previous = CurrentToken.Value;
        CurrentToken.Value = accessToken;
        return new Scope(previous);
    }

    private sealed class Scope(string? previous) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            CurrentToken.Value = previous;
        }
    }
}
