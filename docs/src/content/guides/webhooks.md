---
title: Receiving webhooks
description: "Verify subscriptions, validate signatures, and dispatch inbound events on any host."
order: 7
---

Webhook support ships in the same `Carandev.WhatsApp.Sdk` package and does not depend on ASP.NET Core. `IWhatsAppWebhookProcessor` (registered by `AddWhatsAppCloudApi`) does the protocol work: the subscription handshake, `X-Hub-Signature-256` validation against `WhatsAppOptions.AppSecret`, payload parsing, and dispatch to your `WhatsAppWebhookHandlers`. You own the endpoint, so routing, authentication, DI scopes, and inline-vs-queued processing stay under your control.

## Set up the subscription in Meta

1. In the [Meta App Dashboard](https://developers.facebook.com/apps), open **WhatsApp > Configuration**.
2. Set the **Callback URL** to your public endpoint, for example `https://api.example.com/webhooks/whatsapp`.
3. Set the **Verify token** to the same value as `WhatsApp:WebhookVerifyToken`.
4. Subscribe to the `messages` field.

Configure `WhatsApp:AppSecret` (the app secret from **App settings > Basic**) and `WhatsApp:WebhookVerifyToken`, then map two endpoints.

## Subscribe the app to a WABA

A WABA delivers webhooks to your app only after the app subscribes to it. `IWhatsAppWebhooksClient` does this from code, so connecting a new WABA needs no dashboard step. Every call uses `WhatsAppOptions.WabaId` unless you pass `wabaIdOverride`:

```csharp
// Subscribe the app that owns the access token. Webhooks go to the app's callback.
await webhooks.SubscribeAppAsync();

// Or send this WABA's webhooks to another callback, for example a staging environment.
await webhooks.SubscribeAppAsync(new Uri("https://staging.example.com/webhooks/whatsapp"), "staging-verify-token");

var apps = await webhooks.ListSubscribedAppsAsync();
foreach (var app in apps.Value?.Data ?? [])
{
    Console.WriteLine($"{app.App?.Name} ({app.App?.Id}) -> {app.OverrideCallbackUri ?? "app callback"}");
}

await webhooks.UnsubscribeAppAsync();
```

The callback and verify token go together: passing only one, a callback that is not an absolute HTTPS URL, or an empty token throws `ArgumentException` before any request is sent. Calling `SubscribeAppAsync()` without them removes an existing WABA override. A single phone number can also override both callbacks with `IWhatsAppPhoneNumbersClient.SetWebhookOverrideAsync`, described in the phone numbers guide.

## ASP.NET Core Minimal APIs

```csharp
using Carandev.WhatsApp.Sdk.DependencyInjection;
using Carandev.WhatsApp.Sdk.Models.Webhooks;
using Carandev.WhatsApp.Sdk.Webhooks;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddWhatsAppCloudApi(builder.Configuration);
builder.Services.AddScoped<MyWebhookHandlers>();

var app = builder.Build();

// Subscription handshake: echo hub.challenge when hub.verify_token matches.
app.MapGet("/webhooks/whatsapp", (HttpRequest request, IWhatsAppWebhookProcessor processor) =>
    processor.VerifySubscription(request.Query["hub.verify_token"], request.Query["hub.challenge"]) is { } challenge
        ? Results.Text(challenge)
        : Results.StatusCode(StatusCodes.Status403Forbidden));

// Event delivery: read the RAW body (the signature covers its exact bytes), then process it.
app.MapPost("/webhooks/whatsapp", async (HttpRequest request, IWhatsAppWebhookProcessor processor, MyWebhookHandlers handlers, CancellationToken cancellationToken) =>
{
    using var reader = new StreamReader(request.Body);
    var rawBody = await reader.ReadToEndAsync(cancellationToken);

    var result = await processor.ProcessAsync(rawBody, request.Headers["X-Hub-Signature-256"], handlers, cancellationToken);

    return result == WebhookProcessingResult.InvalidSignature ? Results.Unauthorized() : Results.Ok();
});

app.Run();

sealed class MyWebhookHandlers : WhatsAppWebhookHandlers
{
    public override Task OnMessageAsync(WebhookMessage message, WebhookContext context, CancellationToken cancellationToken)
    {
        if (message is TextWebhookMessage text)
        {
            var sender = context.Contact?.Profile?.Name ?? text.From;
            Console.WriteLine($"{sender} -> {context.Metadata?.DisplayPhoneNumber}: {text.Text.Body}");
        }

        return Task.CompletedTask;
    }

    public override Task OnStatusAsync(WebhookStatus status, WebhookContext context, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Message {status.Id} sent from {context.Metadata?.PhoneNumberId} is now {status.Status}");
        return Task.CompletedTask;
    }

    public override Task OnErrorAsync(Exception exception, WebhookContext? context, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Webhook error on {context?.Metadata?.PhoneNumberId ?? "unknown number"}: {exception.Message}");
        return Task.CompletedTask;
    }
}
```

`WhatsAppWebhookHandlers` methods are all `virtual` with no-op defaults, so override only what you need. A per-item exception from `OnMessageAsync`/`OnStatusAsync` is routed to `OnErrorAsync` instead of aborting the rest of the batch.

Each handler receives a `WebhookContext` with the WABA id (`WabaId`), the business number the event belongs to (`Metadata`, as Meta sends it), and, for inbound messages, the sender's profile (`Contact`). The handlers without the `context` parameter from 0.1.0 still work: the context overloads call them by default. Override one overload per event kind; if you override both, only the context overload runs unless it calls `base`.

`ProcessAsync` returns a `WebhookProcessingResult`:

| Result | Meaning | Suggested response |
|---|---|---|
| `Processed` | Every message/status item was dispatched. | `200 OK` |
| `InvalidSignature` | Missing or wrong `X-Hub-Signature-256`, or no `AppSecret` configured. No handler ran. | `401 Unauthorized` |
| `InvalidPayload` | The body is not a webhook envelope; `OnErrorAsync` received the parsing exception. | `200 OK`, so Meta does not retry a payload that will never parse |

## Multiple business numbers

When one Meta app serves several business numbers, every event says which number it belongs to. Route on `context.Metadata?.PhoneNumberId`, not on `message.From` (that is the customer):

```csharp
sealed class InboxHandlers(IInboxRouter inboxes) : WhatsAppWebhookHandlers
{
    public override Task OnMessageAsync(WebhookMessage message, WebhookContext context, CancellationToken cancellationToken)
    {
        var inbox = inboxes.ForPhoneNumberId(context.Metadata?.PhoneNumberId);
        var senderName = context.Contact?.Profile?.Name;
        return inbox.ReceiveAsync(message, senderName, cancellationToken);
    }

    public override Task OnStatusAsync(WebhookStatus status, WebhookContext context, CancellationToken cancellationToken) =>
        inboxes.ForPhoneNumberId(context.Metadata?.PhoneNumberId).UpdateStatusAsync(status, cancellationToken);
}
```

| `WebhookContext` member | Source in the payload | Messages | Statuses |
|---|---|---|---|
| `WabaId` | `entry[].id` | ✓ | ✓ |
| `Metadata.PhoneNumberId` / `Metadata.DisplayPhoneNumber` | `value.metadata` | ✓ | ✓ |
| `Contact` (`WaId`, `Profile.Name`) | `value.contacts[]` | ✓ | always `null` |

`Contact` is the contact whose `wa_id` equals the message's `from`. If none matches and the change carries exactly one contact, that contact is used; otherwise it is `null`.

`OnErrorAsync` receives a `null` context when the whole body cannot be parsed, a context with only `WabaId` when one change cannot be parsed, the failing item's full context when a per-item handler throws, and the change's context (no `Contact`) when a batch handler throws.

## Per-item vs batch handlers

Meta groups items: one webhook change can carry several messages or statuses. Choose, per event kind, how you receive them:

- **Per item** (default): override `OnMessageAsync` / `OnStatusAsync`. Each item arrives in its own call with its own `WebhookContext`, and an exception from one item goes to `OnErrorAsync` without stopping the others. Choose this for event-by-event logic such as replying or routing.
- **Per batch**: override `OnMessagesBatchAsync` / `OnStatusesBatchAsync`. You get one call per change with the whole list and a `WebhookBatchContext` (`WabaId`, `Metadata`, and every `Contacts` entry of the change; `Contacts` is never `null` and always empty for statuses). Choose this to persist in bulk, for example one `INSERT` or one transaction per change.

```csharp
sealed class BulkInsertHandlers(IMessageStore store) : WhatsAppWebhookHandlers
{
    public override async Task OnMessagesBatchAsync(IReadOnlyList<WebhookMessage> messages, WebhookBatchContext context, CancellationToken cancellationToken)
    {
        var rows = messages.Select(message => new InboundMessageRow(
            PhoneNumberId: context.Metadata?.PhoneNumberId,
            MessageId: message.Id,
            From: message.From,
            SenderName: context.Contacts.FirstOrDefault(c => c.WaId == message.From)?.Profile?.Name));

        await store.InsertManyAsync(rows, cancellationToken); // one round-trip per change
    }
}
```

The default batch implementations are what call the per-item handlers. If you override a batch method, the per-item handlers of that kind are no longer called unless your override calls `base.OnMessagesBatchAsync(...)` / `base.OnStatusesBatchAsync(...)`.

**Warning:** a batch override owns its own per-item error handling. Without `base`, nothing isolates one item's failure from the rest of its batch. An exception that escapes the override goes to `OnErrorAsync` with the change's context (`WabaId` and `Metadata`; `Contact` is `null`), and the remaining changes of the delivery are still dispatched.

## Queued processing

Meta expects a fast `200 OK`, and slow handlers lead to redelivery. To acknowledge first and handle later, split the two steps: call `processor.IsValidSignature(rawBody, signatureHeader)` in the endpoint, enqueue `rawBody`, return `200 OK`, and have your worker call `processor.DispatchAsync(rawBody, handlers, cancellationToken)`. The same calls work from Azure Functions, AWS Lambda, or MVC controllers.

## Inbound message types

`OnMessageAsync` receives a `WebhookMessage`; pattern-match on the subtype:

```csharp
sealed class SupportHandlers : WhatsAppWebhookHandlers
{
    public override Task OnMessageAsync(WebhookMessage message, CancellationToken cancellationToken)
    {
        var description = message switch
        {
            TextWebhookMessage text => $"text: {text.Text.Body}",
            ImageWebhookMessage image => $"image: {image.Image.Id}",
            DocumentWebhookMessage document => $"document: {document.Document.Id}",
            LocationWebhookMessage location => $"location: {location.Location.Latitude},{location.Location.Longitude}",
            _ => message.GetType().Name,
        };

        Console.WriteLine($"{message.From}: {description}");
        return Task.CompletedTask;
    }
}
```

Media messages carry an ID; download the file with [`IWhatsAppMediaClient`](../media/#download-inbound-media).

## Without dependency injection

```csharp
var processor = new WhatsAppWebhookProcessor(Options.Create(new WhatsAppOptions
{
    AccessToken = "unused-for-webhooks",
    PhoneNumberId = "unused-for-webhooks",
    AppSecret = Environment.GetEnvironmentVariable("WHATSAPP_APP_SECRET"),
    WebhookVerifyToken = Environment.GetEnvironmentVariable("WHATSAPP_VERIFY_TOKEN"),
}));
```

See Meta's [webhook setup guide](https://developers.facebook.com/docs/whatsapp/cloud-api/guides/set-up-webhooks) and [payload reference](https://developers.facebook.com/docs/whatsapp/cloud-api/webhooks/payload-examples).
