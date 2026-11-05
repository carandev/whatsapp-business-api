using Microsoft.Extensions.Options;
using Carandev.WhatsApp.Sdk.Models.Webhooks;
using Carandev.WhatsApp.Sdk.Webhooks;

namespace Carandev.WhatsApp.Sdk.Tests.Webhooks;

/// <summary>
/// Covers the <see cref="WebhookContext"/> passed to the context overloads of
/// <see cref="WhatsAppWebhookHandlers"/>. Handlers that override only the 0.1.0 overloads are
/// covered by <see cref="WhatsAppWebhookProcessorTests"/>.
/// </summary>
public class WhatsAppWebhookProcessorContextTests
{
    private sealed class ContextRecordingHandlers : WhatsAppWebhookHandlers
    {
        public List<(WebhookMessage Message, WebhookContext Context)> Messages { get; } = [];

        public List<(WebhookStatus Status, WebhookContext Context)> Statuses { get; } = [];

        public List<(Exception Exception, WebhookContext? Context)> Errors { get; } = [];

        public Func<WebhookMessage, Task>? OnMessage { get; init; }

        public bool LegacyOverloadCalled { get; private set; }

        public override async Task OnMessageAsync(WebhookMessage message, WebhookContext context, CancellationToken cancellationToken)
        {
            Messages.Add((message, context));
            if (OnMessage is not null)
            {
                await OnMessage(message);
            }
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

        public override Task OnMessageAsync(WebhookMessage message, CancellationToken cancellationToken)
        {
            LegacyOverloadCalled = true;
            return Task.CompletedTask;
        }

        public override Task OnStatusAsync(WebhookStatus status, CancellationToken cancellationToken)
        {
            LegacyOverloadCalled = true;
            return Task.CompletedTask;
        }

        public override Task OnErrorAsync(Exception exception, CancellationToken cancellationToken)
        {
            LegacyOverloadCalled = true;
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

    private static string MessagesValue(string metadata, string[] contacts, params string[] messages)
    {
        var contactsJson = contacts.Length == 0 ? string.Empty : $""" "contacts": [ {string.Join(",", contacts)} ], """;
        return $$"""
            { "messaging_product": "whatsapp", "metadata": {{metadata}}, {{contactsJson}} "messages": [ {{string.Join(",", messages)}} ] }
            """;
    }

    private static string StatusesValue(string metadata, string statusId) => $$"""
        { "messaging_product": "whatsapp", "metadata": {{metadata}},
          "statuses": [ { "id": "{{statusId}}", "status": "delivered", "timestamp": "1603059215", "recipient_id": "16315558151" } ] }
        """;

    [Fact]
    public async Task DispatchAsync_PassesWabaIdAndMetadata_ToMessageContext()
    {
        var handlers = new ContextRecordingHandlers();
        var body = Envelope(("102290129340398", MessagesValue(
            Metadata("16505551111", "123456123"),
            [Contact("16315558151", "Kerry Fisher")],
            TextMessage("16315558151", "wamid.AAA"))));

        var result = await CreateProcessor().DispatchAsync(body, handlers);

        Assert.Equal(WebhookProcessingResult.Processed, result);
        var (message, context) = Assert.Single(handlers.Messages);
        Assert.Equal("wamid.AAA", message.Id);
        Assert.Equal("102290129340398", context.WabaId);
        Assert.Equal(new WebhookMetadata { DisplayPhoneNumber = "16505551111", PhoneNumberId = "123456123" }, context.Metadata);
        Assert.Equal("Kerry Fisher", context.Contact?.Profile?.Name);
        Assert.False(handlers.LegacyOverloadCalled);
        Assert.Empty(handlers.Errors);
    }

    [Fact]
    public async Task DispatchAsync_MatchesContactByWaId_ToMessageContext()
    {
        var handlers = new ContextRecordingHandlers();
        var body = Envelope(("waba-1", MessagesValue(
            Metadata("16505551111", "123456123"),
            [Contact("222", "Bea"), Contact("111", "Ana")],
            TextMessage("111", "wamid.FROM-ANA"),
            TextMessage("222", "wamid.FROM-BEA"))));

        await CreateProcessor().DispatchAsync(body, handlers);

        Assert.Collection(
            handlers.Messages,
            m =>
            {
                Assert.Equal("wamid.FROM-ANA", m.Message.Id);
                Assert.Equal("111", m.Context.Contact?.WaId);
                Assert.Equal("Ana", m.Context.Contact?.Profile?.Name);
            },
            m =>
            {
                Assert.Equal("wamid.FROM-BEA", m.Message.Id);
                Assert.Equal("222", m.Context.Contact?.WaId);
                Assert.Equal("Bea", m.Context.Contact?.Profile?.Name);
            });
    }

    [Fact]
    public async Task DispatchAsync_FallsBackToSingleContact_WhenWaIdDoesNotMatch()
    {
        var handlers = new ContextRecordingHandlers();
        var body = Envelope(("waba-1", MessagesValue(
            Metadata("16505551111", "123456123"),
            [Contact("5215512345678", "Carla")],
            TextMessage("525512345678", "wamid.AAA"))));

        await CreateProcessor().DispatchAsync(body, handlers);

        var (_, context) = Assert.Single(handlers.Messages);
        Assert.Equal("5215512345678", context.Contact?.WaId);
        Assert.Equal("Carla", context.Contact?.Profile?.Name);
    }

    [Fact]
    public async Task DispatchAsync_LeavesContactNull_WhenContactsAreMissing()
    {
        var handlers = new ContextRecordingHandlers();
        var body = Envelope(("waba-1", MessagesValue(
            Metadata("16505551111", "123456123"),
            [],
            TextMessage("111", "wamid.AAA"))));

        await CreateProcessor().DispatchAsync(body, handlers);

        var (_, context) = Assert.Single(handlers.Messages);
        Assert.Null(context.Contact);
        Assert.Equal("123456123", context.Metadata?.PhoneNumberId);
    }

    [Fact]
    public async Task DispatchAsync_LeavesContactNull_WhenSeveralContactsDoNotMatch()
    {
        var handlers = new ContextRecordingHandlers();
        var body = Envelope(("waba-1", MessagesValue(
            Metadata("16505551111", "123456123"),
            [Contact("222", "Bea"), Contact("333", "Cris")],
            TextMessage("111", "wamid.AAA"))));

        await CreateProcessor().DispatchAsync(body, handlers);

        var (_, context) = Assert.Single(handlers.Messages);
        Assert.Null(context.Contact);
    }

    [Fact]
    public async Task DispatchAsync_PassesMetadataAndNullContact_ToStatusContext()
    {
        var handlers = new ContextRecordingHandlers();
        var body = Envelope(("102290129340398", StatusesValue(Metadata("16505551111", "123456123"), "wamid.AAA")));

        var result = await CreateProcessor().DispatchAsync(body, handlers);

        Assert.Equal(WebhookProcessingResult.Processed, result);
        var (status, context) = Assert.Single(handlers.Statuses);
        Assert.Equal("wamid.AAA", status.Id);
        Assert.Equal("102290129340398", context.WabaId);
        Assert.Equal("16505551111", context.Metadata?.DisplayPhoneNumber);
        Assert.Equal("123456123", context.Metadata?.PhoneNumberId);
        Assert.Null(context.Contact);
        Assert.False(handlers.LegacyOverloadCalled);
    }

    [Fact]
    public async Task DispatchAsync_UsesPerChangeContext_ForMultiNumberBatch()
    {
        var handlers = new ContextRecordingHandlers();
        var body = Envelope(
            ("waba-A", MessagesValue(Metadata("15550000001", "pn-1"), [Contact("111", "Ana")], TextMessage("111", "wamid.TO-PN1"))),
            ("waba-B", MessagesValue(Metadata("15550000002", "pn-2"), [Contact("222", "Bea")], TextMessage("222", "wamid.TO-PN2"))),
            ("waba-B", StatusesValue(Metadata("15550000002", "pn-2"), "wamid.STATUS-PN2")));

        await CreateProcessor().DispatchAsync(body, handlers);

        Assert.Collection(
            handlers.Messages,
            m =>
            {
                Assert.Equal("wamid.TO-PN1", m.Message.Id);
                Assert.Equal("waba-A", m.Context.WabaId);
                Assert.Equal("pn-1", m.Context.Metadata?.PhoneNumberId);
                Assert.Equal("Ana", m.Context.Contact?.Profile?.Name);
            },
            m =>
            {
                Assert.Equal("wamid.TO-PN2", m.Message.Id);
                Assert.Equal("waba-B", m.Context.WabaId);
                Assert.Equal("pn-2", m.Context.Metadata?.PhoneNumberId);
                Assert.Equal("Bea", m.Context.Contact?.Profile?.Name);
            });
        var (status, statusContext) = Assert.Single(handlers.Statuses);
        Assert.Equal("wamid.STATUS-PN2", status.Id);
        Assert.Equal("pn-2", statusContext.Metadata?.PhoneNumberId);
    }

    [Fact]
    public async Task DispatchAsync_RoutesContextHandlerException_ToOnErrorWithItemContext_AndContinues()
    {
        var failure = new InvalidOperationException("handler failed");
        var handlers = new ContextRecordingHandlers
        {
            OnMessage = m => m.Id == "wamid.AAA" ? Task.FromException(failure) : Task.CompletedTask,
        };
        var body = Envelope(("waba-1", MessagesValue(
            Metadata("16505551111", "123456123"),
            [Contact("111", "Ana")],
            TextMessage("111", "wamid.AAA"),
            TextMessage("111", "wamid.BBB"))));

        var result = await CreateProcessor().DispatchAsync(body, handlers);

        Assert.Equal(WebhookProcessingResult.Processed, result);
        Assert.Equal(["wamid.AAA", "wamid.BBB"], handlers.Messages.Select(m => m.Message.Id));
        var (exception, context) = Assert.Single(handlers.Errors);
        Assert.Same(failure, exception);
        Assert.NotNull(context);
        Assert.Equal("waba-1", context.WabaId);
        Assert.Equal("123456123", context.Metadata?.PhoneNumberId);
        Assert.Equal("Ana", context.Contact?.Profile?.Name);
        Assert.False(handlers.LegacyOverloadCalled);
    }

    [Fact]
    public async Task DispatchAsync_PassesNullContextToOnError_ForMalformedEnvelope()
    {
        var handlers = new ContextRecordingHandlers();

        var result = await CreateProcessor().DispatchAsync("{not-valid-json", handlers);

        Assert.Equal(WebhookProcessingResult.InvalidPayload, result);
        var (_, context) = Assert.Single(handlers.Errors);
        Assert.Null(context);
        Assert.Empty(handlers.Messages);
    }

    [Fact]
    public async Task DispatchAsync_PassesWabaIdOnlyContextToOnError_ForMalformedChangeValue()
    {
        var handlers = new ContextRecordingHandlers();
        var body = Envelope(
            ("waba-bad", """{ "metadata": { "phone_number_id": "pn-1" }, "messages": "not-an-array" }"""),
            ("waba-ok", MessagesValue(Metadata("15550000002", "pn-2"), [], TextMessage("222", "wamid.OK"))));

        var result = await CreateProcessor().DispatchAsync(body, handlers);

        Assert.Equal(WebhookProcessingResult.Processed, result);
        var (_, context) = Assert.Single(handlers.Errors);
        Assert.Equal(new WebhookContext { WabaId = "waba-bad" }, context);
        Assert.Equal("wamid.OK", Assert.Single(handlers.Messages).Message.Id);
    }
}
