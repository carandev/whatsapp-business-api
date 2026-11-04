# Carandev.WhatsApp.Sdk

A .NET SDK for [Meta's WhatsApp Cloud API](https://developers.facebook.com/docs/whatsapp/cloud-api). Send messages (text, media, templates, interactive), manage business profile/phone number/template resources, and receive inbound webhooks — all through a typed, dependency-injection-friendly client with built-in retry/resilience and structured error handling.

Targets `net8.0` and `net9.0`.

## Package

Everything ships in a single NuGet package, `Carandev.WhatsApp.Sdk`: the core client, resource clients, models, the `Result<T>`/`WhatsAppException` error surface, `AddWhatsAppCloudApi(...)` (registers every resource client plus the retry/resilience pipeline), and host-agnostic webhook processing.

## Quickstart

The fastest way to see the SDK work end-to-end is to run the included [`samples/SendHelloWorld`](samples/SendHelloWorld) console app, which sends a real WhatsApp text message using your own Meta credentials.

### 1. Install

```bash
dotnet add package Carandev.WhatsApp.Sdk
```

The same package covers receiving inbound webhooks (see [Receiving webhooks](#receiving-webhooks) below).

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

Run it from inside `samples/SendHelloWorld` (not the repo root) so `Host.CreateApplicationBuilder` finds `appsettings.json` relative to the current directory. Without real credentials configured, the sample prints a clear "Credentials are not configured" message and exits — it never sends a request or crashes with an unhandled exception. With real credentials, it sends `"Hello from Carandev.WhatsApp.Sdk!"` to the phone number in `SendHelloWorld:RecipientPhoneNumber` and prints the outcome:

- **Success** — the sent message's `MessageId`.
- **Business failure** (`Result<MessageResponse>.Failure`, e.g. invalid recipient) — the full `WhatsAppError` (`Code`/`ErrorSubcode`/`Type`/`Message`/`ErrorUserTitle`/`ErrorUserMsg`/`FbtraceId`).
- **Transport failure** (`WhatsAppException`, thrown only after the retry pipeline exhausts `WhatsAppOptions.MaxRetries` on a transient error) — the same fields plus `IsTransient`.

See [`samples/SendHelloWorld/Program.cs`](samples/SendHelloWorld/Program.cs) for the full, runnable example of both error-handling paths.

## Sending messages

### DI registration

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Carandev.WhatsApp.Sdk.Clients;
using Carandev.WhatsApp.Sdk.DependencyInjection;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWhatsAppCloudApi(builder.Configuration);

using var host = builder.Build();
```

`AddWhatsAppCloudApi` binds `WhatsAppOptions` from configuration and registers `IWhatsAppClient` plus every resource client: `IWhatsAppMessagesClient`, `IWhatsAppMediaClient`, `IWhatsAppTemplatesClient`, `IWhatsAppPhoneNumbersClient`, `IWhatsAppBusinessProfileClient`, and `IWhatsAppWebhooksClient`. It also wires a retry/resilience pipeline that automatically retries transient failures (5xx responses, rate limits, recognized transient Graph error codes) with exponential backoff + jitter, honoring `Retry-After` when present, up to `WhatsAppOptions.MaxRetries` attempts.

### Sending a text message

```csharp
var messagesClient = host.Services.GetRequiredService<IWhatsAppMessagesClient>();

var result = await messagesClient.SendTextAsync(to: "15551234567", body: "Hello from Carandev.WhatsApp.Sdk!");

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

Webhook support ships in the same `Carandev.WhatsApp.Sdk` package and does not depend on ASP.NET Core. `IWhatsAppWebhookProcessor` (registered by `AddWhatsAppCloudApi`) does the protocol work: the subscription handshake, `X-Hub-Signature-256` validation against `WhatsAppOptions.AppSecret`, payload parsing, and dispatch to your `WhatsAppWebhookHandlers`. You own the endpoint, so routing, authentication, DI scopes, and inline-vs-queued processing stay under your control.

Configure `WhatsApp:AppSecret` and `WhatsApp:WebhookVerifyToken` alongside `AccessToken`/`PhoneNumberId`, then map two endpoints. With ASP.NET Core Minimal APIs:

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

`WhatsAppWebhookHandlers` methods are all `virtual` with no-op defaults, so override only what you need. A per-item exception from `OnMessageAsync`/`OnStatusAsync` is routed to `OnErrorAsync` instead of aborting the rest of the batch.

`ProcessAsync` returns a `WebhookProcessingResult`:

| Result | Meaning | Suggested response |
|---|---|---|
| `Processed` | Every message/status item was dispatched. | `200 OK` |
| `InvalidSignature` | Missing or wrong `X-Hub-Signature-256`, or no `AppSecret` configured. No handler ran. | `401 Unauthorized` |
| `InvalidPayload` | The body is not a webhook envelope; `OnErrorAsync` received the parsing exception. | `200 OK`, so Meta does not retry a payload that will never parse |

### Queued processing

Meta expects a fast `200 OK`, and slow handlers lead to redelivery. To acknowledge first and handle later, split the two steps: call `processor.IsValidSignature(rawBody, signatureHeader)` in the endpoint, enqueue `rawBody`, return `200 OK`, and have your worker call `processor.DispatchAsync(rawBody, handlers, cancellationToken)`. The same calls work from Azure Functions, AWS Lambda, or MVC controllers.

See the [webhook setup guide](https://developers.facebook.com/docs/whatsapp/cloud-api/guides/set-up-webhooks) for configuring the callback URL and verify token in the Meta App Dashboard.

## Meta documentation references

- [WhatsApp Cloud API overview](https://developers.facebook.com/docs/whatsapp/cloud-api)
- [Get started / phone number setup](https://developers.facebook.com/docs/whatsapp/cloud-api/get-started)
- [Business Management API / access tokens](https://developers.facebook.com/docs/whatsapp/business-management-api/get-started)
- [Sending messages](https://developers.facebook.com/docs/whatsapp/cloud-api/guides/send-messages)
- [Message templates](https://developers.facebook.com/docs/whatsapp/business-management-api/guides/create-message-templates)
- [Media](https://developers.facebook.com/docs/whatsapp/cloud-api/reference/media)
- [Webhooks setup](https://developers.facebook.com/docs/whatsapp/cloud-api/guides/set-up-webhooks)
- [Error codes reference](https://developers.facebook.com/docs/whatsapp/cloud-api/support/error-codes)

## Documentation

The documentation site (guides plus a generated API reference) lives in [`docs/`](docs) and is built with [DocFX](https://dotnet.github.io/docfx/):

```bash
dotnet tool restore
dotnet docfx docs/docfx.json --serve
```

Then open http://localhost:8080.

## Development

```bash
dotnet build
dotnet test
```

`src/` projects build with `TreatWarningsAsErrors` and require XML documentation on every public member; `tests/` and `samples/` are not held to that standard.

## Releasing

CI (`.github/workflows/ci.yml`) builds, tests, and packs every push and pull request to `main`.

Publishing to nuget.org is tag-driven (`.github/workflows/release.yml`). The package version comes from the tag:

```bash
git tag v0.1.0
git push origin v0.1.0
```

Tags with a suffix (for example `v0.2.0-beta.1`) are published as NuGet prereleases. The workflow runs in the `nuget` GitHub environment and needs a `NUGET_API_KEY` secret: a nuget.org API key with push scope for the `Carandev.*` glob.

## License

[MIT](LICENSE)
