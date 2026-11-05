using Carandev.WhatsApp.Sdk.Models.Webhooks;

namespace Carandev.WhatsApp.Sdk.Webhooks;

/// <summary>
/// Base class for handling WhatsApp Cloud API inbound webhook events.
/// </summary>
/// <remarks>
/// <para>
/// All members are <see langword="virtual"/> with no-op default implementations, so a consumer
/// only needs to override the handlers relevant to their use case. Consumed by
/// <see cref="IWhatsAppWebhookProcessor"/>, which invokes one call per batched item and isolates exceptions
/// raised by the message/status handlers by routing them to
/// <see cref="OnErrorAsync(Exception, WebhookContext, CancellationToken)"/>.
/// </para>
/// <para>
/// Each event kind has two overloads. The processor always calls the one taking a
/// <see cref="WebhookContext"/> (receiving business number, WABA id, sender profile), whose default
/// implementation calls the overload without it. Override one overload per event kind: the
/// context overload to receive the context, or the original overload if you do not need it. If
/// you override both, only the context overload runs unless it calls <see langword="base"/>.
/// </para>
/// </remarks>
public abstract class WhatsAppWebhookHandlers
{
    /// <summary>
    /// Invoked once per inbound message item in a webhook batch, unless
    /// <see cref="OnMessageAsync(WebhookMessage, WebhookContext, CancellationToken)"/> is overridden
    /// without calling <see langword="base"/>.
    /// </summary>
    /// <param name="message">The strongly-typed parsed message.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    public virtual Task OnMessageAsync(WebhookMessage message, CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Invoked once per inbound message item in a webhook batch, with the envelope data of the
    /// change the message arrived in. The default implementation calls
    /// <see cref="OnMessageAsync(WebhookMessage, CancellationToken)"/>.
    /// </summary>
    /// <param name="message">The strongly-typed parsed message.</param>
    /// <param name="context">The WABA id, receiving business number, and sender profile.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    public virtual Task OnMessageAsync(WebhookMessage message, WebhookContext context, CancellationToken cancellationToken)
        => OnMessageAsync(message, cancellationToken);

    /// <summary>
    /// Invoked once per inbound status item in a webhook batch, unless
    /// <see cref="OnStatusAsync(WebhookStatus, WebhookContext, CancellationToken)"/> is overridden
    /// without calling <see langword="base"/>.
    /// </summary>
    /// <param name="status">The strongly-typed parsed status update.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    public virtual Task OnStatusAsync(WebhookStatus status, CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Invoked once per inbound status item in a webhook batch, with the envelope data of the change
    /// the status arrived in (<see cref="WebhookContext.Contact"/> is always <see langword="null"/>).
    /// The default implementation calls <see cref="OnStatusAsync(WebhookStatus, CancellationToken)"/>.
    /// </summary>
    /// <param name="status">The strongly-typed parsed status update.</param>
    /// <param name="context">The WABA id and the business number that sent the message.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    public virtual Task OnStatusAsync(WebhookStatus status, WebhookContext context, CancellationToken cancellationToken)
        => OnStatusAsync(status, cancellationToken);

    /// <summary>
    /// Invoked when webhook processing fails for the whole payload (e.g. malformed JSON) or for
    /// an individual item (e.g. an exception thrown by a message or status handler), unless
    /// <see cref="OnErrorAsync(Exception, WebhookContext, CancellationToken)"/> is overridden
    /// without calling <see langword="base"/>.
    /// </summary>
    /// <param name="exception">The exception that was raised.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    public virtual Task OnErrorAsync(Exception exception, CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Invoked when webhook processing fails, with as much context as was known at that point.
    /// The default implementation calls <see cref="OnErrorAsync(Exception, CancellationToken)"/>.
    /// </summary>
    /// <param name="exception">The exception that was raised.</param>
    /// <param name="context">
    /// <see langword="null"/> when the whole body could not be parsed; only
    /// <see cref="WebhookContext.WabaId"/> when one change's value could not be parsed; the
    /// failing item's full context when a message or status handler threw.
    /// </param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    public virtual Task OnErrorAsync(Exception exception, WebhookContext? context, CancellationToken cancellationToken)
        => OnErrorAsync(exception, cancellationToken);
}
