# WhatsAppCloudApi

A .NET SDK for [Meta's WhatsApp Cloud API](https://developers.facebook.com/docs/whatsapp/cloud-api). Send messages (text, media, templates, interactive), manage business profile/phone number/template resources, and receive inbound webhooks — all through a typed, dependency-injection-friendly client with built-in retry/resilience and structured error handling.

Targets `net8.0` and `net9.0`.

## Packages

| Package | Purpose |
|---|---|
| `WhatsAppCloudApi` | Core client, resource clients, models, and the `Result<T>`/`WhatsAppException` error surface. |
| `WhatsAppCloudApi.Extensions.DependencyInjection` | `AddWhatsAppCloudApi(...)` — registers every resource client plus the retry/resilience pipeline. |
| `WhatsAppCloudApi.Extensions.AspNetCore` | `UseWhatsAppWebhooks(...)` middleware for receiving inbound webhook events. |

## Quickstart

The fastest way to see the SDK work end-to-end is to run the included [`samples/SendHelloWorld`](samples/SendHelloWorld) console app, which sends a real WhatsApp text message using your own Meta credentials.

### 1. Install

```bash
dotnet add package WhatsAppCloudApi
dotnet add package WhatsAppCloudApi.Extensions.DependencyInjection
```

Add `WhatsAppCloudApi.Extensions.AspNetCore` too if you also need to receive inbound webhooks (see [Receiving webhooks](#receiving-webhooks) below).

### 2. Configure

The SDK binds `WhatsAppOptions` from a `WhatsApp` configuration section:

```json
{
  "WhatsApp": {
    "AccessToken": "YOUR_ACCESS_TOKEN_HERE",
    "PhoneNumberId": "YOUR_PHONE_NUMBER_ID_HERE"
  }
}
```

- **`AccessToken`** — a [System User access token](https://developers.facebook.com/docs/whatsapp/business-management-api/get-started) (or temporary token while testing) with `whatsapp_business_messaging` permission.
- **`PhoneNumberId`** — the phone number ID from your [WhatsApp Business Platform app setup](https://developers.facebook.com/docs/whatsapp/cloud-api/get-started).

Never commit real credentials. Keep the committed `appsettings.json` filled with placeholders and supply real values via a gitignored `appsettings.Local.json` (or `appsettings.Development.json`/environment variables, e.g. `WhatsApp__AccessToken`, or [`dotnet user-secrets`](https://learn.microsoft.com/aspnet/core/security/app-secrets)) — whichever your project already uses for local secrets.

### 3. Run the sample

```bash
cd samples/SendHelloWorld
cp appsettings.json appsettings.Local.json   # then edit appsettings.Local.json with real values
dotnet run
```

Run it from inside `samples/SendHelloWorld` (not the repo root) so `Host.CreateApplicationBuilder` finds `appsettings.json` relative to the current directory. Without real credentials configured, the sample prints a clear "Credentials are not configured" message and exits — it never sends a request or crashes with an unhandled exception. With real credentials, it sends `"Hello from WhatsAppCloudApi SDK!"` to the phone number in `SendHelloWorld:RecipientPhoneNumber` and prints the outcome:

- **Success** — the sent message's `MessageId`.
- **Business failure** (`Result<MessageResponse>.Failure`, e.g. invalid recipient) — the full `WhatsAppError` (`Code`/`Subcode`/`Title`/`Message`/`UserTitle`/`UserMessage`/`FbTraceId`).
- **Transport failure** (`WhatsAppException`, thrown only after the retry pipeline exhausts `WhatsAppOptions.MaxRetries` on a transient error) — the same fields plus `IsTransient`.

See [`samples/SendHelloWorld/Program.cs`](samples/SendHelloWorld/Program.cs) for the full, runnable example of both error-handling paths.

## Sending messages

### DI registration

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WhatsAppCloudApi.Clients;
using WhatsAppCloudApi.Extensions.DependencyInjection;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWhatsAppCloudApi(builder.Configuration);

using var host = builder.Build();
```

`AddWhatsAppCloudApi` binds `WhatsAppOptions` from configuration and registers `IWhatsAppClient` plus every resource client: `IWhatsAppMessagesClient`, `IWhatsAppMediaClient`, `IWhatsAppTemplatesClient`, `IWhatsAppPhoneNumbersClient`, `IWhatsAppBusinessProfileClient`, and `IWhatsAppWebhooksClient`. It also wires a retry/resilience pipeline that automatically retries transient failures (5xx responses, rate limits, recognized transient Graph error codes) with exponential backoff + jitter, honoring `Retry-After` when present, up to `WhatsAppOptions.MaxRetries` attempts.

### Sending a text message

```csharp
var messagesClient = host.Services.GetRequiredService<IWhatsAppMessagesClient>();

var result = await messagesClient.SendTextAsync(to: "15551234567", body: "Hello from WhatsAppCloudApi SDK!");

if (result.IsSuccess)
{
    Console.WriteLine($"Sent. MessageId={result.Value!.MessageId}");
}
else
{
    Console.WriteLine($"Failed: {result.Error!.Code} {result.Error!.Message}");
}
```

### Error handling: two paths, on purpose

- **`Result<T>.Failure`** — the Graph API call completed but was rejected as a business error (invalid recipient, unapproved template, permission issue, etc.). Every resource-client method is non-throwing for this case; inspect `result.Error` (a `WhatsAppError`).
- **`WhatsAppException`** (and its subtypes `WhatsAppRateLimitException`, `WhatsAppAuthenticationException`, `WhatsAppTemplateRejectedException`) — thrown *only* when the retry pipeline exhausts all attempts against a transient failure. This signals a transport/availability problem, not a rejected request, so it surfaces as an exception rather than a `Result<T>.Failure`.

```csharp
try
{
    var result = await messagesClient.SendTextAsync(to, body);
    // handle result.IsSuccess / result.Error as above
}
catch (WhatsAppException ex)
{
    Console.WriteLine($"Gave up after retries: {ex.Code} {ex.Message} (IsTransient={ex.IsTransient})");
}
```

## Receiving webhooks

Add `WhatsAppCloudApi.Extensions.AspNetCore` to an ASP.NET Core app, configure `WhatsApp:AppSecret` and `WhatsApp:WebhookVerifyToken` alongside `AccessToken`/`PhoneNumberId`, then wire the middleware:

```csharp
using WhatsAppCloudApi.Extensions.AspNetCore;
using WhatsAppCloudApi.Models.Webhooks;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddWhatsAppCloudApi(builder.Configuration);
builder.Services.Configure<WhatsAppOptions>(builder.Configuration.GetSection(WhatsAppOptions.SectionName));

var app = builder.Build();

app.UseWhatsAppWebhooks("/webhooks/whatsapp", new MyWebhookHandlers());

app.Run();

sealed class MyWebhookHandlers : WhatsAppWebhookHandlers
{
    public override Task OnMessageAsync(WebhookMessage message, CancellationToken cancellationToken)
    {
        if (message is TextWebhookMessage text)
        {
            Console.WriteLine($"From {text.From}: {text.Text.Body}");
        }

        return Task.CompletedTask;
    }

    public override Task OnStatusAsync(WebhookStatus status, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Message {status.Id} is now {status.Status}");
        return Task.CompletedTask;
    }

    public override Task OnErrorAsync(Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Webhook error: {exception.Message}");
        return Task.CompletedTask;
    }
}
```

`UseWhatsAppWebhooks` handles both the subscription GET handshake (echoing `hub.challenge` when `hub.verify_token` matches `WhatsAppOptions.WebhookVerifyToken`) and POST delivery (verifying the `X-Hub-Signature-256` header against `WhatsAppOptions.AppSecret` before parsing). `WhatsAppWebhookHandlers` methods are all `virtual` with no-op defaults, so override only what you need; a per-item exception from `OnMessageAsync`/`OnStatusAsync` is routed to `OnErrorAsync` instead of aborting the rest of the batch. See the [webhook setup guide](https://developers.facebook.com/docs/whatsapp/cloud-api/guides/set-up-webhooks) for configuring the callback URL and verify token in the Meta App Dashboard.

## Meta documentation references

- [WhatsApp Cloud API overview](https://developers.facebook.com/docs/whatsapp/cloud-api)
- [Get started / phone number setup](https://developers.facebook.com/docs/whatsapp/cloud-api/get-started)
- [Business Management API / access tokens](https://developers.facebook.com/docs/whatsapp/business-management-api/get-started)
- [Sending messages](https://developers.facebook.com/docs/whatsapp/cloud-api/guides/send-messages)
- [Message templates](https://developers.facebook.com/docs/whatsapp/business-management-api/guides/create-message-templates)
- [Media](https://developers.facebook.com/docs/whatsapp/cloud-api/reference/media)
- [Webhooks setup](https://developers.facebook.com/docs/whatsapp/cloud-api/guides/set-up-webhooks)
- [Error codes reference](https://developers.facebook.com/docs/whatsapp/cloud-api/support/error-codes)

## Development

```bash
dotnet build
dotnet test
```

`src/` projects build with `TreatWarningsAsErrors` and require XML documentation on every public member; `tests/` and `samples/` are not held to that standard.
