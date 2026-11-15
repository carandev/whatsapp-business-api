namespace Carandev.WhatsApp.Sdk;

/// <summary>
/// Resolves the Graph API access token for each outgoing request, so one process can call the
/// API on behalf of several businesses (for example with per-business tokens obtained through
/// Embedded Signup). Register an implementation in dependency injection and
/// <see cref="WhatsAppClient"/> uses it; it may be scoped, for example to read the current
/// tenant from the incoming HTTP request.
/// </summary>
/// <remarks>
/// Precedence, per request: a token set with <see cref="WhatsAppCredentials.UseAccessToken"/>,
/// then this provider, then <see cref="WhatsAppOptions.AccessToken"/>. The token is resolved in
/// <see cref="IWhatsAppClient.SendAsync"/>, and only replaces the default token that
/// <see cref="IWhatsAppClient.CreateRequest"/> attached; an <c>Authorization</c> header the caller
/// set to another value is sent unchanged.
/// </remarks>
public interface IWhatsAppCredentialProvider
{
    /// <summary>
    /// Returns the access token for the current request, or <see langword="null"/> to use
    /// <see cref="WhatsAppOptions.AccessToken"/>.
    /// </summary>
    /// <param name="cancellationToken">The token of the request being sent.</param>
    ValueTask<string?> GetAccessTokenAsync(CancellationToken cancellationToken);
}
