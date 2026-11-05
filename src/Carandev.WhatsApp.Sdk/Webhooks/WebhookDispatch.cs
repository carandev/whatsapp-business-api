using Carandev.WhatsApp.Sdk.Models.Webhooks;

namespace Carandev.WhatsApp.Sdk.Webhooks;

/// <summary>
/// Dispatch rules shared by <see cref="WhatsAppWebhookProcessor"/> and the default batch
/// implementations of <see cref="WhatsAppWebhookHandlers"/>.
/// </summary>
internal static class WebhookDispatch
{
    /// <summary>
    /// Picks the sender's contact for a message: the one whose <c>wa_id</c> equals the message's
    /// <c>from</c>; otherwise the change's only contact, if it has exactly one; otherwise none.
    /// </summary>
    internal static WebhookContact? ResolveContact(IReadOnlyList<WebhookContact>? contacts, string? from)
    {
        if (contacts is null || contacts.Count == 0)
        {
            return null;
        }

        foreach (var contact in contacts)
        {
            if (string.Equals(contact.WaId, from, StringComparison.Ordinal))
            {
                return contact;
            }
        }

        return contacts.Count == 1 ? contacts[0] : null;
    }

    /// <summary>
    /// Runs one handler call and routes any exception it raises to
    /// <see cref="WhatsAppWebhookHandlers.OnErrorAsync(Exception, WebhookContext, CancellationToken)"/>
    /// with <paramref name="context"/>, so the caller can continue with the next item or change.
    /// </summary>
    internal static async Task InvokeSafelyAsync(Func<Task> handlerCall, WebhookContext context, WhatsAppWebhookHandlers handlers, CancellationToken cancellationToken)
    {
        try
        {
            await handlerCall().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            try
            {
                await handlers.OnErrorAsync(ex, context, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception errorHandlerException)
            {
                // An exception thrown by OnErrorAsync itself must escape processing unchanged, as in
                // 0.1.0. Mark it so the change-level wrapper does not route it back to OnErrorAsync.
                errorHandlerException.Data[RaisedByErrorHandlerKey] = true;
                throw;
            }
        }
    }

    /// <summary>
    /// Invokes a batch handler for one change, routing an exception it throws to
    /// <see cref="WhatsAppWebhookHandlers.OnErrorAsync(Exception, WebhookContext, CancellationToken)"/>
    /// with the change <paramref name="context"/>. Exceptions raised by <c>OnErrorAsync</c> while
    /// isolating an item propagate unchanged instead of being reported a second time.
    /// </summary>
    internal static async Task InvokeBatchSafelyAsync(Func<Task> batchCall, WebhookContext context, WhatsAppWebhookHandlers handlers, CancellationToken cancellationToken)
    {
        try
        {
            await batchCall().ConfigureAwait(false);
        }
        catch (Exception ex) when (!ex.Data.Contains(RaisedByErrorHandlerKey))
        {
            await handlers.OnErrorAsync(ex, context, cancellationToken).ConfigureAwait(false);
        }
    }

    private const string RaisedByErrorHandlerKey = "Carandev.WhatsApp.Sdk.Webhooks.RaisedByOnErrorAsync";
}
