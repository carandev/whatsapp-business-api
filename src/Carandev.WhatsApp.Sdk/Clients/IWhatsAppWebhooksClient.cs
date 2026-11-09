using Carandev.WhatsApp.Sdk.Models.Templates;
using Carandev.WhatsApp.Sdk.Models.Webhooks;

namespace Carandev.WhatsApp.Sdk.Clients;

/// <summary>
/// Client for a WhatsApp Business Account's webhook subscriptions
/// (<c>/{waba_id}/subscribed_apps</c>). A WABA delivers webhooks to an app only after the app
/// subscribes to it. Failures from Meta are reported via <see cref="Result"/>/<see cref="Result{T}"/>,
/// never an exception.
/// </summary>
/// <remarks>
/// Every member has a default implementation that throws <see cref="NotSupportedException"/>, so
/// third-party implementations written when this interface had no members keep compiling;
/// <see cref="WhatsAppWebhooksClient"/> implements them.
/// </remarks>
public interface IWhatsAppWebhooksClient
{
    /// <summary>
    /// Subscribes the app that owns the access token to a WABA's webhooks via
    /// <c>POST /{waba_id}/subscribed_apps</c>. With <paramref name="overrideCallbackUri"/> and
    /// <paramref name="verifyToken"/>, this WABA's webhooks go to that URL instead of the app's
    /// callback; without them, the request has no body and any existing WABA override is removed.
    /// </summary>
    /// <param name="overrideCallbackUri">
    /// An optional callback URL for this WABA's webhooks. Must be an absolute HTTPS URL, and must be
    /// given together with <paramref name="verifyToken"/>.
    /// </param>
    /// <param name="verifyToken">
    /// The token Meta sends to <paramref name="overrideCallbackUri"/> to verify it. Required when
    /// <paramref name="overrideCallbackUri"/> is set, and not allowed without it.
    /// </param>
    /// <param name="wabaIdOverride">
    /// An optional WABA id that takes precedence over <see cref="WhatsAppOptions.WabaId"/>.
    /// </param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>
    /// A successful <see cref="Result"/> on success; a failed <see cref="Result"/> when the WABA id
    /// cannot be resolved (no HTTP request is sent in that case) or the API call fails.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Only one of <paramref name="overrideCallbackUri"/> and <paramref name="verifyToken"/> is set,
    /// <paramref name="overrideCallbackUri"/> is not an absolute HTTPS URL, or
    /// <paramref name="verifyToken"/> is empty or whitespace. No request is sent.
    /// </exception>
    Task<Result> SubscribeAppAsync(Uri? overrideCallbackUri = null, string? verifyToken = null, string? wabaIdOverride = null, CancellationToken ct = default) =>
        throw new NotSupportedException(
            $"{GetType().FullName} does not implement {nameof(SubscribeAppAsync)}. Use WhatsAppWebhooksClient or implement the member.");

    /// <summary>
    /// Lists the apps subscribed to a WABA's webhooks via <c>GET /{waba_id}/subscribed_apps</c>.
    /// </summary>
    /// <param name="wabaIdOverride">
    /// An optional WABA id that takes precedence over <see cref="WhatsAppOptions.WabaId"/>.
    /// </param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> wrapping a <see cref="TemplatesPage{T}"/> of
    /// <see cref="SubscribedApp"/>; a failed <see cref="Result{T}"/> when the WABA id cannot be
    /// resolved (no HTTP request is sent in that case) or the API call fails.
    /// </returns>
    Task<Result<TemplatesPage<SubscribedApp>>> ListSubscribedAppsAsync(string? wabaIdOverride = null, CancellationToken ct = default) =>
        throw new NotSupportedException(
            $"{GetType().FullName} does not implement {nameof(ListSubscribedAppsAsync)}. Use WhatsAppWebhooksClient or implement the member.");

    /// <summary>
    /// Unsubscribes the app that owns the access token from a WABA's webhooks via
    /// <c>DELETE /{waba_id}/subscribed_apps</c>.
    /// </summary>
    /// <param name="wabaIdOverride">
    /// An optional WABA id that takes precedence over <see cref="WhatsAppOptions.WabaId"/>.
    /// </param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>
    /// A successful <see cref="Result"/> on success; a failed <see cref="Result"/> when the WABA id
    /// cannot be resolved (no HTTP request is sent in that case) or the API call fails.
    /// </returns>
    Task<Result> UnsubscribeAppAsync(string? wabaIdOverride = null, CancellationToken ct = default) =>
        throw new NotSupportedException(
            $"{GetType().FullName} does not implement {nameof(UnsubscribeAppAsync)}. Use WhatsAppWebhooksClient or implement the member.");
}
