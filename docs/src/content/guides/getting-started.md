# Getting started

## Install

```bash
dotnet add package Carandev.WhatsApp.Sdk
```

The package targets `net8.0` and `net9.0` and includes everything: the API clients, dependency injection registration, the retry pipeline, and webhook processing.

## Get your Meta credentials

You need two values from your [WhatsApp Business Platform app](https://developers.facebook.com/docs/whatsapp/cloud-api/get-started):

- **Access token**: a [System User access token](https://developers.facebook.com/docs/whatsapp/business-management-api/get-started) with the `whatsapp_business_messaging` permission (a temporary token works while testing).
- **Phone number ID**: the ID of the sending number, shown in the app's WhatsApp setup page. This is not the phone number itself.

## Configure

The SDK binds `WhatsAppOptions` from the `WhatsApp` configuration section:

```json
{
  "WhatsApp": {
    "AccessToken": "YOUR_ACCESS_TOKEN_HERE",
    "PhoneNumberId": "YOUR_PHONE_NUMBER_ID_HERE"
  }
}
```

> [!WARNING]
> Never commit real credentials. Use [`dotnet user-secrets`](https://learn.microsoft.com/aspnet/core/security/app-secrets), environment variables (`WhatsApp__AccessToken`), or a gitignored settings file.

See [Configuration](configuration.md) for every option.

## Register the clients

```csharp
using Carandev.WhatsApp.Sdk.Clients;
using Carandev.WhatsApp.Sdk.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWhatsAppCloudApi(builder.Configuration);

using var host = builder.Build();
```

`AddWhatsAppCloudApi` binds and validates `WhatsAppOptions` and registers:

| Service | Purpose |
|---|---|
| `IWhatsAppMessagesClient` | Send every message type. |
| `IWhatsAppMediaClient` | Upload, resolve, and download media. |
| `IWhatsAppTemplatesClient` | Create, list, update, and delete message templates. |
| `IWhatsAppPhoneNumbersClient` | Read phone numbers and run code verification. |
| `IWhatsAppBusinessProfileClient` | Read and update the business profile. |
| `IWhatsAppWebhookProcessor` | Verify and dispatch inbound webhooks. |
| `IWhatsAppClient` | Low-level typed HTTP client used by the resource clients. |

## Send your first message

```csharp
var messages = host.Services.GetRequiredService<IWhatsAppMessagesClient>();

var result = await messages.SendTextAsync(to: "15551234567", body: "Hello from Carandev.WhatsApp.Sdk!");

if (result.IsSuccess)
{
    Console.WriteLine($"Sent. MessageId={result.Value!.MessageId}");
}
else
{
    Console.WriteLine($"Failed: {result.Error!.Code} {result.Error.Message}");
}
```

Phone numbers use international format with no `+`, spaces, or dashes.

> [!NOTE]
> Outside the 24-hour customer service window, WhatsApp only accepts [template messages](templates.md). To test free-form text, first send a message to your business number from the recipient's phone.

The repository includes a runnable console sample in [`samples/SendHelloWorld`](https://github.com/carandev/whatsapp-business-api/tree/main/samples/SendHelloWorld).

## Next steps

- [Sending messages](sending-messages.md)
- [Receiving webhooks](webhooks.md)
- [Errors and retries](errors-and-resilience.md)
