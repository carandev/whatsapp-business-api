using System.Text.Json;
using Microsoft.Extensions.Options;
using Carandev.WhatsApp.Sdk.Models.Webhooks;
using Carandev.WhatsApp.Sdk.Serialization;

namespace Carandev.WhatsApp.Sdk.Webhooks;

/// <summary>
/// Default <see cref="IWhatsAppWebhookProcessor"/> implementation, registered as a singleton by
/// <c>AddWhatsAppCloudApi</c>. It can also be constructed directly with
/// <see cref="Options.Create{TOptions}(TOptions)"/> when dependency injection is not used.
/// </summary>
public sealed class WhatsAppWebhookProcessor : IWhatsAppWebhookProcessor
{
    private readonly IOptions<WhatsAppOptions> _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="WhatsAppWebhookProcessor"/> class.
    /// </summary>
    /// <param name="options">Options providing <see cref="WhatsAppOptions.AppSecret"/> and <see cref="WhatsAppOptions.WebhookVerifyToken"/>.</param>
    public WhatsAppWebhookProcessor(IOptions<WhatsAppOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
    }

    /// <inheritdoc />
    public string? VerifySubscription(string? verifyToken, string? challenge)
    {
        var expected = _options.Value.WebhookVerifyToken;

        return !string.IsNullOrEmpty(expected) && string.Equals(verifyToken, expected, StringComparison.Ordinal)
            ? challenge ?? string.Empty
            : null;
    }

    /// <inheritdoc />
    public bool IsValidSignature(string rawBody, string? signatureHeader)
    {
        ArgumentNullException.ThrowIfNull(rawBody);
        return new WhatsAppSignatureValidator(_options.Value.AppSecret).Verify(rawBody, signatureHeader);
    }

    /// <inheritdoc />
    public async Task<WebhookProcessingResult> ProcessAsync(string rawBody, string? signatureHeader, WhatsAppWebhookHandlers handlers, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rawBody);
        ArgumentNullException.ThrowIfNull(handlers);

        if (!IsValidSignature(rawBody, signatureHeader))
        {
            return WebhookProcessingResult.InvalidSignature;
        }

        return await DispatchAsync(rawBody, handlers, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<WebhookProcessingResult> DispatchAsync(string rawBody, WhatsAppWebhookHandlers handlers, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rawBody);
        ArgumentNullException.ThrowIfNull(handlers);

        WebhookEnvelope? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize(rawBody, WhatsAppJsonSerializerContext.Default.WebhookEnvelope);
        }
        catch (JsonException ex)
        {
            await handlers.OnErrorAsync(ex, cancellationToken).ConfigureAwait(false);
            return WebhookProcessingResult.InvalidPayload;
        }

        if (envelope?.Entry is not null)
        {
            foreach (var entry in envelope.Entry)
            {
                if (entry.Changes is null)
                {
                    continue;
                }

                foreach (var change in entry.Changes)
                {
                    await DispatchChangeAsync(change, handlers, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        return WebhookProcessingResult.Processed;
    }

    private static async Task DispatchChangeAsync(WebhookChange change, WhatsAppWebhookHandlers handlers, CancellationToken cancellationToken)
    {
        if (change.Value.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        if (change.Value.TryGetProperty("messages", out _))
        {
            await DispatchMessagesAsync(change.Value, handlers, cancellationToken).ConfigureAwait(false);
        }

        if (change.Value.TryGetProperty("statuses", out _))
        {
            await DispatchStatusesAsync(change.Value, handlers, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task DispatchMessagesAsync(JsonElement value, WhatsAppWebhookHandlers handlers, CancellationToken cancellationToken)
    {
        MessagesWebhookPayload? payload;
        try
        {
            payload = value.Deserialize(WhatsAppJsonSerializerContext.Default.MessagesWebhookPayload);
        }
        catch (JsonException ex)
        {
            await handlers.OnErrorAsync(ex, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (payload?.Messages is null)
        {
            return;
        }

        foreach (var message in payload.Messages)
        {
            await InvokeHandlerSafelyAsync(() => handlers.OnMessageAsync(message, cancellationToken), handlers, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task DispatchStatusesAsync(JsonElement value, WhatsAppWebhookHandlers handlers, CancellationToken cancellationToken)
    {
        StatusesWebhookPayload? payload;
        try
        {
            payload = value.Deserialize(WhatsAppJsonSerializerContext.Default.StatusesWebhookPayload);
        }
        catch (JsonException ex)
        {
            await handlers.OnErrorAsync(ex, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (payload?.Statuses is null)
        {
            return;
        }

        foreach (var status in payload.Statuses)
        {
            await InvokeHandlerSafelyAsync(() => handlers.OnStatusAsync(status, cancellationToken), handlers, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task InvokeHandlerSafelyAsync(Func<Task> handlerCall, WhatsAppWebhookHandlers handlers, CancellationToken cancellationToken)
    {
        try
        {
            await handlerCall().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await handlers.OnErrorAsync(ex, cancellationToken).ConfigureAwait(false);
        }
    }
}
