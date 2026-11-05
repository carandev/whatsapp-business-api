using Carandev.WhatsApp.Sdk.Models.Webhooks;

namespace Carandev.WhatsApp.Sdk.Webhooks;

/// <summary>
/// Base class for handling WhatsApp Cloud API inbound webhook events.
/// </summary>
/// <remarks>
/// <para>
/// All members are <see langword="virtual"/> with no-op default implementations, so a consumer
/// only needs to override the handlers relevant to their use case. Consumed by
/// <see cref="IWhatsAppWebhookProcessor"/>, which calls one batch handler per webhook change.
/// </para>
/// <para>
/// Choose a dispatch mode per event kind:
/// </para>
/// <list type="bullet">
/// <item><description>
/// <b>Per item</b>: override
/// <see cref="OnMessageAsync(WebhookMessage, WebhookContext, CancellationToken)"/> or
/// <see cref="OnStatusAsync(WebhookStatus, WebhookContext, CancellationToken)"/>. The default
/// batch implementations call them once per item, with that item's <see cref="WebhookContext"/>,
/// and route an exception raised by one item to
/// <see cref="OnErrorAsync(Exception, WebhookContext, CancellationToken)"/> before continuing
/// with the next item.
/// </description></item>
/// <item><description>
/// <b>Per batch</b>: override
/// <see cref="OnMessagesBatchAsync(IReadOnlyList{WebhookMessage}, WebhookBatchContext, CancellationToken)"/>
/// or
/// <see cref="OnStatusesBatchAsync(IReadOnlyList{WebhookStatus}, WebhookBatchContext, CancellationToken)"/>
/// to receive all the items of a change in one call, for example to persist them in one
/// transaction. The per-item handlers of that kind are then not called unless the override calls
/// <see langword="base"/>.
/// </description></item>
/// </list>
/// <para>
/// A batch override owns per-item error handling: unless it calls <see langword="base"/>, nothing
/// isolates one item's failure from the others. An exception that escapes a batch override is
/// routed to <see cref="OnErrorAsync(Exception, WebhookContext, CancellationToken)"/> with the
/// change's context (<see cref="WebhookContext.Contact"/> is <see langword="null"/>), and the
/// remaining changes of the delivery are still dispatched.
/// </para>
/// <para>
/// Each per-item event kind has two overloads. The default batch implementations call the one
/// taking a <see cref="WebhookContext"/> (receiving business number, WABA id, sender profile),
/// whose default implementation calls the overload without it. Override one overload per event
/// kind: the context overload to receive the context, or the original overload if you do not
/// need it. If you override both, only the context overload runs unless it calls
/// <see langword="base"/>.
/// </para>
/// </remarks>
public abstract class WhatsAppWebhookHandlers
{
    /// <summary>
    /// Invoked once per webhook change that carries inbound messages, with all of them. The
    /// default implementation calls
    /// <see cref="OnMessageAsync(WebhookMessage, WebhookContext, CancellationToken)"/> once per
    /// message, with the sender's contact resolved from <see cref="WebhookBatchContext.Contacts"/>,
    /// and routes an exception raised for one message to
    /// <see cref="OnErrorAsync(Exception, WebhookContext, CancellationToken)"/> before continuing
    /// with the next one.
    /// </summary>
    /// <remarks>
    /// An override owns per-item error handling and skips the per-item message handlers unless it
    /// calls <see langword="base"/>. An exception it lets escape goes to
    /// <see cref="OnErrorAsync(Exception, WebhookContext, CancellationToken)"/> with the change's
    /// context.
    /// </remarks>
    /// <param name="messages">The messages of the change, in delivery order. Never empty.</param>
    /// <param name="context">The WABA id, receiving business number, and every contact of the change.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    public virtual async Task OnMessagesBatchAsync(IReadOnlyList<WebhookMessage> messages, WebhookBatchContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(messages);
        ArgumentNullException.ThrowIfNull(context);

        var changeContext = new WebhookContext { WabaId = context.WabaId, Metadata = context.Metadata };

        foreach (var message in messages)
        {
            var itemContext = changeContext with { Contact = WebhookDispatch.ResolveContact(context.Contacts, message.From) };
            await WebhookDispatch.InvokeSafelyAsync(() => OnMessageAsync(message, itemContext, cancellationToken), itemContext, this, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Invoked once per webhook change that carries status updates, with all of them. The default
    /// implementation calls
    /// <see cref="OnStatusAsync(WebhookStatus, WebhookContext, CancellationToken)"/> once per
    /// status and routes an exception raised for one status to
    /// <see cref="OnErrorAsync(Exception, WebhookContext, CancellationToken)"/> before continuing
    /// with the next one.
    /// </summary>
    /// <remarks>
    /// An override owns per-item error handling and skips the per-item status handlers unless it
    /// calls <see langword="base"/>. An exception it lets escape goes to
    /// <see cref="OnErrorAsync(Exception, WebhookContext, CancellationToken)"/> with the change's
    /// context.
    /// </remarks>
    /// <param name="statuses">The status updates of the change, in delivery order. Never empty.</param>
    /// <param name="context">
    /// The WABA id and the business number that sent the messages;
    /// <see cref="WebhookBatchContext.Contacts"/> is always empty.
    /// </param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    public virtual async Task OnStatusesBatchAsync(IReadOnlyList<WebhookStatus> statuses, WebhookBatchContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(statuses);
        ArgumentNullException.ThrowIfNull(context);

        var itemContext = new WebhookContext { WabaId = context.WabaId, Metadata = context.Metadata };

        foreach (var status in statuses)
        {
            await WebhookDispatch.InvokeSafelyAsync(() => OnStatusAsync(status, itemContext, cancellationToken), itemContext, this, cancellationToken).ConfigureAwait(false);
        }
    }

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
    /// failing item's full context when a message or status handler threw; the change's
    /// <see cref="WebhookContext.WabaId"/> and <see cref="WebhookContext.Metadata"/> (with a
    /// <see langword="null"/> <see cref="WebhookContext.Contact"/>) when a batch handler threw.
    /// </param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    public virtual Task OnErrorAsync(Exception exception, WebhookContext? context, CancellationToken cancellationToken)
        => OnErrorAsync(exception, cancellationToken);
}
