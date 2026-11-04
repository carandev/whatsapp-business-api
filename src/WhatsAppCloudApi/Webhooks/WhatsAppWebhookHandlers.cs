using WhatsAppCloudApi.Models.Webhooks;

namespace WhatsAppCloudApi.Webhooks;

/// <summary>
/// Base class for handling WhatsApp Cloud API inbound webhook events.
/// </summary>
/// <remarks>
/// All members are <see langword="virtual"/> with no-op default implementations, so a consumer
/// only needs to override the handlers relevant to their use case. Consumed by
/// <see cref="IWhatsAppWebhookProcessor"/>, which invokes one call per batched item and isolates exceptions
/// raised by <see cref="OnMessageAsync"/>/<see cref="OnStatusAsync"/> by routing them to
/// <see cref="OnErrorAsync"/>.
/// </remarks>
public abstract class WhatsAppWebhookHandlers
{
    /// <summary>
    /// Invoked once per inbound message item in a webhook batch.
    /// </summary>
    /// <param name="message">The strongly-typed parsed message.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    public virtual Task OnMessageAsync(WebhookMessage message, CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Invoked once per inbound status item in a webhook batch.
    /// </summary>
    /// <param name="status">The strongly-typed parsed status update.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    public virtual Task OnStatusAsync(WebhookStatus status, CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Invoked when webhook processing fails for the whole payload (e.g. malformed JSON) or for
    /// an individual item (e.g. an exception thrown by <see cref="OnMessageAsync"/> or
    /// <see cref="OnStatusAsync"/>).
    /// </summary>
    /// <param name="exception">The exception that was raised.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    public virtual Task OnErrorAsync(Exception exception, CancellationToken cancellationToken) => Task.CompletedTask;
}
