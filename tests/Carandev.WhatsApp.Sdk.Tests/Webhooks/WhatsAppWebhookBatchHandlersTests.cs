using Microsoft.Extensions.Options;
using Carandev.WhatsApp.Sdk.Models.Webhooks;
using Carandev.WhatsApp.Sdk.Webhooks;

namespace Carandev.WhatsApp.Sdk.Tests.Webhooks;

/// <summary>
/// Covers <see cref="WhatsAppWebhookHandlers.OnMessagesBatchAsync"/> and
/// <see cref="WhatsAppWebhookHandlers.OnStatusesBatchAsync"/>: one call per webhook change, and the
/// default implementations that fan out to the per-item handlers.
/// </summary>
public class WhatsAppWebhookBatchHandlersTests
{
    /// <summary>Records every call; optionally forwards the batch to <see langword="base"/>.</summary>
    private sealed class BatchRecordingHandlers : WhatsAppWebhookHandlers
    {
        public bool CallBase { get; init; }

        public Func<WebhookBatchContext, Exception?>? ThrowOnMessagesBatch { get; init; }

        public Func<WebhookBatchContext, Exception?>? ThrowOnStatusesBatch { get; init; }

        public List<(IReadOnlyList<WebhookMessage> Messages, WebhookBatchContext Context)> MessageBatches { get; } = [];

        public List<(IReadOnlyList<WebhookStatus> Statuses, WebhookBatchContext Context)> StatusBatches { get; } = [];

        public List<(WebhookMessage Message, WebhookContext Context)> Messages { get; } = [];

        public List<(WebhookStatus Status, WebhookContext Context)> Statuses { get; } = [];

        public List<(Exception Exception, WebhookContext? Context)> Errors { get; } = [];

        public override async Task OnMessagesBatchAsync(IReadOnlyList<WebhookMessage> messages, WebhookBatchContext context, CancellationToken cancellationToken)
        {
            MessageBatches.Add((messages, context));
            if (ThrowOnMessagesBatch?.Invoke(context) is { } failure)
            {
                throw failure;
            }

            if (CallBase)
            {
                await base.OnMessagesBatchAsync(messages, context, cancellationToken);
            }
        }

        public override async Task OnStatusesBatchAsync(IReadOnlyList<WebhookStatus> statuses, WebhookBatchContext context, CancellationToken cancellationToken)
        {
            StatusBatches.Add((statuses, context));
            if (ThrowOnStatusesBatch?.Invoke(context) is { } failure)
            {
                throw failure;
            }

            if (CallBase)
            {
                await base.OnStatusesBatchAsync(statuses, context, cancellationToken);
            }
        }

        public override Task OnMessageAsync(WebhookMessage message, WebhookContext context, CancellationToken cancellationToken)
        {
            Messages.Add((message, context));
            return Task.CompletedTask;
        }

        public override Task OnStatusAsync(WebhookStatus status, WebhookContext context, CancellationToken cancellationToken)
        {
            Statuses.Add((status, context));
            return Task.CompletedTask;
        }

        public override Task OnErrorAsync(Exception exception, WebhookContext? context, CancellationToken cancellationToken)
        {
            Errors.Add((exception, context));
            return Task.CompletedTask;
        }
    }

    /// <summary>Overrides only the per-item context handlers, like a 0.2.0 per-item consumer.</summary>
    private sealed class PerItemHandlers : WhatsAppWebhookHandlers
    {
        public Func<WebhookMessage, Task>? OnMessage { get; init; }

        public Func<WebhookStatus, Task>? OnStatus { get; init; }

        public List<(WebhookMessage Message, WebhookContext Context)> Messages { get; } = [];

        public List<(WebhookStatus Status, WebhookContext Context)> Statuses { get; } = [];

        public List<(Exception Exception, WebhookContext? Context)> Errors { get; } = [];

        public override async Task OnMessageAsync(WebhookMessage message, WebhookContext context, CancellationToken cancellationToken)
        {
            Messages.Add((message, context));
            if (OnMessage is not null)
            {
                await OnMessage(message);
            }
        }

        public override async Task OnStatusAsync(WebhookStatus status, WebhookContext context, CancellationToken cancellationToken)
        {
            Statuses.Add((status, context));
            if (OnStatus is not null)
            {
                await OnStatus(status);
            }
        }

        public override Task OnErrorAsync(Exception exception, WebhookContext? context, CancellationToken cancellationToken)
        {
            Errors.Add((exception, context));
            return Task.CompletedTask;
        }
    }

    private static WhatsAppWebhookProcessor CreateProcessor() =>
        new(Options.Create(new WhatsAppOptions
        {
            AccessToken = "test-access-token",
            PhoneNumberId = "123456123",
        }));

    private static string Envelope(params (string WabaId, string ValueJson)[] changes)
    {
        var entries = changes.Select(c => $$"""
            { "id": "{{c.WabaId}}", "changes": [ { "field": "messages", "value": {{c.ValueJson}} } ] }
            """);
        return $$"""{ "object": "whatsapp_business_account", "entry": [ {{string.Join(",", entries)}} ] }""";
    }

    private static string Metadata(string displayPhoneNumber, string phoneNumberId) =>
        $$"""{ "display_phone_number": "{{displayPhoneNumber}}", "phone_number_id": "{{phoneNumberId}}" }""";

    private static string Contact(string waId, string name) =>
        $$"""{ "profile": { "name": "{{name}}" }, "wa_id": "{{waId}}" }""";

    private static string TextMessage(string from, string id) =>
        $$"""{ "from": "{{from}}", "id": "{{id}}", "timestamp": "1603059201", "type": "text", "text": { "body": "Hi" } }""";

    private static string Status(string id) =>
        $$"""{ "id": "{{id}}", "status": "delivered", "timestamp": "1603059215", "recipient_id": "16315558151" }""";

    private static string MessagesValue(string metadata, string[] contacts, params string[] messages)
    {
        var contactsJson = contacts.Length == 0 ? string.Empty : $""" "contacts": [ {string.Join(",", contacts)} ], """;
        return $$"""
            { "messaging_product": "whatsapp", "metadata": {{metadata}}, {{contactsJson}} "messages": [ {{string.Join(",", messages)}} ] }
            """;
    }

    private static string StatusesValue(string metadata, params string[] statuses) => $$"""
        { "messaging_product": "whatsapp", "metadata": {{metadata}}, "statuses": [ {{string.Join(",", statuses)}} ] }
        """;

    [Fact]
    public async Task DispatchAsync_CallsMessagesBatchOncePerChange_WithAllMessagesAndContacts()
    {
        var handlers = new BatchRecordingHandlers();
        var body = Envelope(
            ("waba-A", MessagesValue(
                Metadata("15550000001", "pn-1"),
                [Contact("111", "Ana"), Contact("222", "Bea")],
                TextMessage("111", "wamid.A1"),
                TextMessage("222", "wamid.A2"))),
            ("waba-B", MessagesValue(
                Metadata("15550000002", "pn-2"),
                [],
                TextMessage("333", "wamid.B1"))));

        var result = await CreateProcessor().DispatchAsync(body, handlers);

        Assert.Equal(WebhookProcessingResult.Processed, result);
        Assert.Collection(
            handlers.MessageBatches,
            batch =>
            {
                Assert.Equal(["wamid.A1", "wamid.A2"], batch.Messages.Select(m => m.Id));
                Assert.Equal("waba-A", batch.Context.WabaId);
                Assert.Equal(new WebhookMetadata { DisplayPhoneNumber = "15550000001", PhoneNumberId = "pn-1" }, batch.Context.Metadata);
                Assert.Equal(["111", "222"], batch.Context.Contacts.Select(c => c.WaId));
                Assert.Equal(["Ana", "Bea"], batch.Context.Contacts.Select(c => c.Profile?.Name));
            },
            batch =>
            {
                Assert.Equal(["wamid.B1"], batch.Messages.Select(m => m.Id));
                Assert.Equal("waba-B", batch.Context.WabaId);
                Assert.Equal("pn-2", batch.Context.Metadata?.PhoneNumberId);
                Assert.NotNull(batch.Context.Contacts);
                Assert.Empty(batch.Context.Contacts);
            });
        Assert.Empty(handlers.StatusBatches);
        Assert.Empty(handlers.Messages);
        Assert.Empty(handlers.Errors);
    }

    [Fact]
    public async Task DispatchAsync_CallsStatusesBatchOncePerChange_WithEmptyContacts()
    {
        var handlers = new BatchRecordingHandlers();
        var body = Envelope(
            ("waba-A", StatusesValue(Metadata("15550000001", "pn-1"), Status("wamid.S1"), Status("wamid.S2"))),
            ("waba-B", StatusesValue(Metadata("15550000002", "pn-2"), Status("wamid.S3"))));

        var result = await CreateProcessor().DispatchAsync(body, handlers);

        Assert.Equal(WebhookProcessingResult.Processed, result);
        Assert.Collection(
            handlers.StatusBatches,
            batch =>
            {
                Assert.Equal(["wamid.S1", "wamid.S2"], batch.Statuses.Select(s => s.Id));
                Assert.Equal("waba-A", batch.Context.WabaId);
                Assert.Equal(new WebhookMetadata { DisplayPhoneNumber = "15550000001", PhoneNumberId = "pn-1" }, batch.Context.Metadata);
                Assert.NotNull(batch.Context.Contacts);
                Assert.Empty(batch.Context.Contacts);
            },
            batch =>
            {
                Assert.Equal(["wamid.S3"], batch.Statuses.Select(s => s.Id));
                Assert.Equal("waba-B", batch.Context.WabaId);
                Assert.Equal("pn-2", batch.Context.Metadata?.PhoneNumberId);
                Assert.Empty(batch.Context.Contacts);
            });
        Assert.Empty(handlers.MessageBatches);
        Assert.Empty(handlers.Statuses);
        Assert.Empty(handlers.Errors);
    }

    [Fact]
    public async Task DefaultBatch_DelegatesToPerItemHandlers_WithResolvedContext()
    {
        var handlers = new PerItemHandlers();
        var body = Envelope(
            ("waba-A", MessagesValue(
                Metadata("15550000001", "pn-1"),
                [Contact("222", "Bea"), Contact("111", "Ana")],
                TextMessage("111", "wamid.FROM-ANA"),
                TextMessage("222", "wamid.FROM-BEA"),
                TextMessage("999", "wamid.UNKNOWN"))),
            ("waba-B", MessagesValue(
                Metadata("15550000002", "pn-2"),
                [Contact("5215512345678", "Carla")],
                TextMessage("525512345678", "wamid.SINGLE"))),
            ("waba-B", StatusesValue(Metadata("15550000002", "pn-2"), Status("wamid.S1"))));

        var result = await CreateProcessor().DispatchAsync(body, handlers);

        Assert.Equal(WebhookProcessingResult.Processed, result);
        Assert.Collection(
            handlers.Messages,
            m =>
            {
                Assert.Equal("wamid.FROM-ANA", m.Message.Id);
                Assert.Equal("waba-A", m.Context.WabaId);
                Assert.Equal("pn-1", m.Context.Metadata?.PhoneNumberId);
                Assert.Equal("Ana", m.Context.Contact?.Profile?.Name);
            },
            m =>
            {
                Assert.Equal("wamid.FROM-BEA", m.Message.Id);
                Assert.Equal("Bea", m.Context.Contact?.Profile?.Name);
            },
            m =>
            {
                Assert.Equal("wamid.UNKNOWN", m.Message.Id);
                Assert.Equal("waba-A", m.Context.WabaId);
                Assert.Null(m.Context.Contact);
            },
            m =>
            {
                Assert.Equal("wamid.SINGLE", m.Message.Id);
                Assert.Equal("waba-B", m.Context.WabaId);
                Assert.Equal("pn-2", m.Context.Metadata?.PhoneNumberId);
                Assert.Equal("Carla", m.Context.Contact?.Profile?.Name);
            });
        var (status, statusContext) = Assert.Single(handlers.Statuses);
        Assert.Equal("wamid.S1", status.Id);
        Assert.Equal(
            new WebhookContext { WabaId = "waba-B", Metadata = new WebhookMetadata { DisplayPhoneNumber = "15550000002", PhoneNumberId = "pn-2" } },
            statusContext);
        Assert.Empty(handlers.Errors);
    }

    [Fact]
    public async Task DefaultBatch_IsolatesPerItemExceptions_AndContinues()
    {
        var messageFailure = new InvalidOperationException("message handler failed");
        var statusFailure = new InvalidOperationException("status handler failed");
        var handlers = new PerItemHandlers
        {
            OnMessage = m => m.Id == "wamid.M1" ? Task.FromException(messageFailure) : Task.CompletedTask,
            OnStatus = s => s.Id == "wamid.S1" ? throw statusFailure : Task.CompletedTask,
        };
        var body = Envelope(
            ("waba-1", MessagesValue(
                Metadata("16505551111", "123456123"),
                [Contact("111", "Ana")],
                TextMessage("111", "wamid.M1"),
                TextMessage("111", "wamid.M2"))),
            ("waba-1", StatusesValue(Metadata("16505551111", "123456123"), Status("wamid.S1"), Status("wamid.S2"))));

        var result = await CreateProcessor().DispatchAsync(body, handlers);

        Assert.Equal(WebhookProcessingResult.Processed, result);
        Assert.Equal(["wamid.M1", "wamid.M2"], handlers.Messages.Select(m => m.Message.Id));
        Assert.Equal(["wamid.S1", "wamid.S2"], handlers.Statuses.Select(s => s.Status.Id));
        Assert.Collection(
            handlers.Errors,
            e =>
            {
                Assert.Same(messageFailure, e.Exception);
                Assert.NotNull(e.Context);
                Assert.Equal("waba-1", e.Context.WabaId);
                Assert.Equal("123456123", e.Context.Metadata?.PhoneNumberId);
                Assert.Equal("Ana", e.Context.Contact?.Profile?.Name);
            },
            e =>
            {
                Assert.Same(statusFailure, e.Exception);
                Assert.NotNull(e.Context);
                Assert.Equal("waba-1", e.Context.WabaId);
                Assert.Equal("123456123", e.Context.Metadata?.PhoneNumberId);
                Assert.Null(e.Context.Contact);
            });
    }

    [Fact]
    public async Task BatchOverride_SkipsPerItemHandlers_UnlessBaseIsCalled()
    {
        var body = Envelope(
            ("waba-1", MessagesValue(Metadata("16505551111", "123456123"), [Contact("111", "Ana")], TextMessage("111", "wamid.M1"))),
            ("waba-1", StatusesValue(Metadata("16505551111", "123456123"), Status("wamid.S1"))));

        var withoutBase = new BatchRecordingHandlers();
        var withBase = new BatchRecordingHandlers { CallBase = true };

        var withoutBaseResult = await CreateProcessor().DispatchAsync(body, withoutBase);
        var withBaseResult = await CreateProcessor().DispatchAsync(body, withBase);

        Assert.Equal(WebhookProcessingResult.Processed, withoutBaseResult);
        Assert.Single(withoutBase.MessageBatches);
        Assert.Single(withoutBase.StatusBatches);
        Assert.Empty(withoutBase.Messages);
        Assert.Empty(withoutBase.Statuses);
        Assert.Empty(withoutBase.Errors);

        Assert.Equal(WebhookProcessingResult.Processed, withBaseResult);
        Assert.Single(withBase.MessageBatches);
        Assert.Single(withBase.StatusBatches);
        var (message, messageContext) = Assert.Single(withBase.Messages);
        Assert.Equal("wamid.M1", message.Id);
        Assert.Equal("Ana", messageContext.Contact?.Profile?.Name);
        Assert.Equal("123456123", messageContext.Metadata?.PhoneNumberId);
        var (status, statusContext) = Assert.Single(withBase.Statuses);
        Assert.Equal("wamid.S1", status.Id);
        Assert.Null(statusContext.Contact);
        Assert.Empty(withBase.Errors);
    }

    [Fact]
    public async Task BatchOverrideException_RoutesToOnErrorWithChangeContext_AndContinuesNextChange()
    {
        var messagesFailure = new InvalidOperationException("bulk insert failed");
        var statusesFailure = new InvalidOperationException("bulk update failed");
        var handlers = new BatchRecordingHandlers
        {
            CallBase = true,
            ThrowOnMessagesBatch = c => c.WabaId == "waba-bad" ? messagesFailure : null,
            ThrowOnStatusesBatch = c => c.WabaId == "waba-bad" ? statusesFailure : null,
        };
        var body = Envelope(
            ("waba-bad", MessagesValue(Metadata("15550000001", "pn-1"), [Contact("111", "Ana")], TextMessage("111", "wamid.BAD"))),
            ("waba-bad", StatusesValue(Metadata("15550000001", "pn-1"), Status("wamid.S-BAD"))),
            ("waba-ok", MessagesValue(Metadata("15550000002", "pn-2"), [Contact("222", "Bea")], TextMessage("222", "wamid.OK"))),
            ("waba-ok", StatusesValue(Metadata("15550000002", "pn-2"), Status("wamid.S-OK"))));

        var result = await CreateProcessor().DispatchAsync(body, handlers);

        Assert.Equal(WebhookProcessingResult.Processed, result);
        Assert.Equal(["waba-bad", "waba-ok"], handlers.MessageBatches.Select(b => b.Context.WabaId));
        Assert.Equal(["waba-bad", "waba-ok"], handlers.StatusBatches.Select(b => b.Context.WabaId));
        Assert.Equal(["wamid.OK"], handlers.Messages.Select(m => m.Message.Id));
        Assert.Equal(["wamid.S-OK"], handlers.Statuses.Select(s => s.Status.Id));

        var changeContext = new WebhookContext
        {
            WabaId = "waba-bad",
            Metadata = new WebhookMetadata { DisplayPhoneNumber = "15550000001", PhoneNumberId = "pn-1" },
        };
        Assert.Collection(
            handlers.Errors,
            e =>
            {
                Assert.Same(messagesFailure, e.Exception);
                Assert.Equal(changeContext, e.Context);
                Assert.Null(e.Context?.Contact);
            },
            e =>
            {
                Assert.Same(statusesFailure, e.Exception);
                Assert.Equal(changeContext, e.Context);
            });
    }
}
