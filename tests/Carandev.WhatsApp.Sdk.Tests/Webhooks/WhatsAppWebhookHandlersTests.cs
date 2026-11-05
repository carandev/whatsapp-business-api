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

    private sealed class LegacyRecordingHandlers : WhatsAppWebhookHandlers
    {
        public WebhookMessage? Message { get; private set; }

        public WebhookStatus? Status { get; private set; }

        public Exception? Error { get; private set; }

        public override Task OnMessageAsync(WebhookMessage message, CancellationToken cancellationToken)
        {
            Message = message;
            return Task.CompletedTask;
        }

        public override Task OnStatusAsync(WebhookStatus status, CancellationToken cancellationToken)
        {
            Status = status;
            return Task.CompletedTask;
        }

        public override Task OnErrorAsync(Exception exception, CancellationToken cancellationToken)
        {
            Error = exception;
            return Task.CompletedTask;
        }
    }

    private static readonly WebhookContext SampleContext = new()
    {
        WabaId = "waba-1",
        Metadata = new WebhookMetadata { DisplayPhoneNumber = "16505551111", PhoneNumberId = "123456123" },
    };

    [Fact]
    public async Task OnMessageAsync_context_overload_default_delegates_to_legacy_overload()
    {
        var handlers = new LegacyRecordingHandlers();
        var message = new WebhookMessage { From = "15551234567", Id = "wamid.1", Timestamp = "1700000000" };

        await handlers.OnMessageAsync(message, SampleContext, CancellationToken.None);

        Assert.Same(message, handlers.Message);
    }

    [Fact]
    public async Task OnStatusAsync_context_overload_default_delegates_to_legacy_overload()
    {
        var handlers = new LegacyRecordingHandlers();
        var status = new WebhookStatus
        {
            Id = "wamid.1",
            Status = WebhookStatusValue.Read,
            Timestamp = "1700000000",
            RecipientId = "15551234567",
        };

        await handlers.OnStatusAsync(status, SampleContext, CancellationToken.None);

        Assert.Same(status, handlers.Status);
    }

    [Fact]
    public async Task OnErrorAsync_context_overload_default_delegates_to_legacy_overload()
    {
        var handlers = new LegacyRecordingHandlers();
        var withContext = new InvalidOperationException("with context");
        var withoutContext = new InvalidOperationException("without context");

        await handlers.OnErrorAsync(withContext, SampleContext, CancellationToken.None);
        Assert.Same(withContext, handlers.Error);

        await handlers.OnErrorAsync(withoutContext, null, CancellationToken.None);
        Assert.Same(withoutContext, handlers.Error);
    }

    [Fact]
    public async Task Context_overloads_default_are_synchronous_no_ops()
    {
        var handlers = new NoOverrideHandlers();
        var message = new WebhookMessage { From = "15551234567", Id = "wamid.1", Timestamp = "1700000000" };
        var status = new WebhookStatus
        {
            Id = "wamid.1",
            Status = WebhookStatusValue.Sent,
            Timestamp = "1700000000",
            RecipientId = "15551234567",
        };

        var tasks = new[]
        {
            handlers.OnMessageAsync(message, SampleContext, CancellationToken.None),
            handlers.OnStatusAsync(status, SampleContext, CancellationToken.None),
            handlers.OnErrorAsync(new InvalidOperationException("boom"), null, CancellationToken.None),
        };

        Assert.All(tasks, t => Assert.True(t.IsCompletedSuccessfully));
        await Task.WhenAll(tasks);
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
