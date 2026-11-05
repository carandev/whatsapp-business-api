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

`OnErrorAsync` receives a `null` context when the whole body cannot be parsed, a context with only `WabaId` when one change cannot be parsed, and the failing item's full context when your handler throws.

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
