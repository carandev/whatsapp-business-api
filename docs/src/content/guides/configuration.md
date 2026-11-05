---
title: Configuration
description: "Every WhatsAppOptions setting, environment variables, and multiple phone numbers."
order: 2
---

`WhatsAppOptions` is bound from the `WhatsApp` configuration section (`WhatsAppOptions.SectionName`) and validated with data annotations the first time the options are resolved (the first client call). Missing required values fail with an `OptionsValidationException`.

| Option | Required | Default | Description |
|---|---|---|---|
| `AccessToken` | Yes | | Meta access token sent as a bearer token. Redacted from logs. |
| `PhoneNumberId` | Yes | | Default sending phone number ID. Every phone-scoped method also accepts a `phoneNumberIdOverride`. |
| `WabaId` | For templates and phone number listing | | WhatsApp Business Account ID. WABA-scoped methods also accept a `wabaIdOverride`; without either, they return a `Result` failure instead of calling the API. |
| `AppId` | For template media uploads | | Meta App ID used by `UploadTemplateMediaAsync` (Resumable Upload API). The method also accepts an `appIdOverride`; without either, it returns a `Result` failure instead of calling the API. See [Media](../media/). |
| `ApiVersion` | No | `v21.0` | Graph API version. |
| `BaseUrl` | No | `https://graph.facebook.com/{0}/` | Graph API base URL; `{0}` is replaced with `ApiVersion`. |
| `MaxRetries` | No | `3` | Retry attempts for transient failures; `0` disables retries. See [Errors and retries](../errors-and-resilience/). |
| `Timeout` | No | `00:00:30` | Timeout for each HTTP attempt; a timed-out attempt is retried up to `MaxRetries`. |
| `AppSecret` | For webhooks | | Meta app secret used to validate `X-Hub-Signature-256`. |
| `WebhookVerifyToken` | For webhooks | | Token you choose and enter in the Meta App Dashboard for the subscription handshake. |

## Example

```json
{
  "WhatsApp": {
    "AccessToken": "YOUR_ACCESS_TOKEN_HERE",
    "PhoneNumberId": "YOUR_PHONE_NUMBER_ID_HERE",
    "WabaId": "YOUR_WABA_ID_HERE",
    "AppId": "YOUR_APP_ID_HERE",
    "ApiVersion": "v21.0",
    "MaxRetries": 3,
    "AppSecret": "YOUR_APP_SECRET_HERE",
    "WebhookVerifyToken": "A_RANDOM_STRING_YOU_CHOOSE"
  }
}
```

## Environment variables

Use a double underscore for the section separator:

```bash
export WhatsApp__AccessToken="..."
export WhatsApp__PhoneNumberId="..."
```

## Multiple phone numbers

Configure one default `PhoneNumberId` and pass `phoneNumberIdOverride` per call to send from another number in the same account:

```csharp
await messages.SendTextAsync("15551234567", "Sent from the support line", phoneNumberIdOverride: "109876543210");
```
