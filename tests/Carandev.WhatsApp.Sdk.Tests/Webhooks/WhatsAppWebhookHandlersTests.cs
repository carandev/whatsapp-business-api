using Carandev.WhatsApp.Sdk.Webhooks;
using Carandev.WhatsApp.Sdk.Models.Webhooks;

namespace Carandev.WhatsApp.Sdk.Tests.Webhooks;

public class WhatsAppWebhookHandlersTests
{
    private sealed class NoOverrideHandlers : WhatsAppWebhookHandlers
    {
    }

    private sealed class MessageOnlyHandlers : WhatsAppWebhookHandlers
    {
        public WebhookMessage? Received { get; private set; }

        public override Task OnMessageAsync(WebhookMessage message, CancellationToken cancellationToken)
        {
            Received = message;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task OnMessageAsync_default_is_a_synchronous_no_op()
    {
        var handlers = new NoOverrideHandlers();
        var message = new WebhookMessage { From = "15551234567", Id = "wamid.1", Timestamp = "1700000000" };

        var task = handlers.OnMessageAsync(message, CancellationToken.None);

        Assert.True(task.IsCompletedSuccessfully);
        await task;
    }

    [Fact]
    public async Task OnStatusAsync_default_is_a_synchronous_no_op()
    {
        var handlers = new NoOverrideHandlers();
        var status = new WebhookStatus
        {
            Id = "wamid.1",
            Status = WebhookStatusValue.Sent,
            Timestamp = "1700000000",
            RecipientId = "15551234567",
        };

        var task = handlers.OnStatusAsync(status, CancellationToken.None);

        Assert.True(task.IsCompletedSuccessfully);
        await task;
    }

    [Fact]
    public async Task OnErrorAsync_default_is_a_synchronous_no_op()
    {
        var handlers = new NoOverrideHandlers();

        var task = handlers.OnErrorAsync(new InvalidOperationException("boom"), CancellationToken.None);

        Assert.True(task.IsCompletedSuccessfully);
        await task;
    }

    [Fact]
    public async Task Derived_class_can_override_only_OnMessageAsync_without_implementing_the_others()
    {
        var handlers = new MessageOnlyHandlers();
        var message = new WebhookMessage { From = "15551234567", Id = "wamid.1", Timestamp = "1700000000" };
        var status = new WebhookStatus
        {
            Id = "wamid.1",
            Status = WebhookStatusValue.Delivered,
            Timestamp = "1700000000",
            RecipientId = "15551234567",
        };

        await handlers.OnMessageAsync(message, CancellationToken.None);
        await handlers.OnStatusAsync(status, CancellationToken.None);
        await handlers.OnErrorAsync(new InvalidOperationException("boom"), CancellationToken.None);

        Assert.Same(message, handlers.Received);
    }
}
