using Microsoft.AspNetCore.Builder;

namespace WhatsAppCloudApi.Extensions.AspNetCore;

/// <summary>
/// Registers the WhatsApp Cloud API inbound webhook endpoint into an ASP.NET Core pipeline.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Adds middleware that handles the WhatsApp webhook subscription GET handshake and POST
    /// delivery at <paramref name="path"/>. Requests to any other path pass through unchanged.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <param name="path">The request path to handle, e.g. <c>/webhooks/whatsapp</c>.</param>
    /// <param name="handlers">The handlers invoked once per parsed message/status item.</param>
    /// <returns><paramref name="app"/>, for chaining.</returns>
    /// <remarks>
    /// Requires <see cref="Microsoft.Extensions.Options.IOptions{TOptions}"/> of
    /// <see cref="WhatsAppOptions"/> to be resolvable from the application's
    /// <see cref="IServiceProvider"/> (e.g. via <c>services.Configure&lt;WhatsAppOptions&gt;(...)</c>).
    /// </remarks>
    public static IApplicationBuilder UseWhatsAppWebhooks(this IApplicationBuilder app, string path, WhatsAppWebhookHandlers handlers)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentException.ThrowIfNullOrEmpty(path);
        ArgumentNullException.ThrowIfNull(handlers);

        return app.Use(next => new WhatsAppWebhookMiddleware(next, path, handlers).InvokeAsync);
    }
}
