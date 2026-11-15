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

## Multiple businesses

`AccessToken` is the default token. To call the API for several businesses from one process, for example with per-business tokens from [Embedded Signup](https://developers.facebook.com/docs/whatsapp/embedded-signup), choose the token per request. Combine it with `phoneNumberIdOverride` or `wabaIdOverride`, since a token only grants access to its own business's numbers and WABA.

Wrap the calls in a scope:

```csharp
using (WhatsAppCredentials.UseAccessToken(tenant.AccessToken))
{
    await messages.SendTextAsync("15551234567", "Hello", phoneNumberIdOverride: tenant.PhoneNumberId);
}
```

Or register an `IWhatsAppCredentialProvider`, which can be scoped and look the token up asynchronously:

```csharp
sealed class TenantCredentialProvider(ITenantContext tenant, ITokenStore tokens) : IWhatsAppCredentialProvider
{
    public async ValueTask<string?> GetAccessTokenAsync(CancellationToken cancellationToken) =>
        await tokens.GetAccessTokenAsync(tenant.Id, cancellationToken); // null falls back to AccessToken
}

builder.Services.AddWhatsAppCloudApi(builder.Configuration);
builder.Services.AddScoped<IWhatsAppCredentialProvider, TenantCredentialProvider>();
```

Each request uses the first token found: an active `UseAccessToken` scope, then the provider, then `WhatsAppOptions.AccessToken`. Scopes nest and flow with `async` code, so concurrent requests for different tenants do not share a token. An `Authorization` header you set yourself on a request is sent unchanged.

Keep a valid default `AccessToken` configured: it is still required, and it is the token used when neither a scope nor the provider supplies one. A scoped provider requires resolving the WhatsApp clients from a scope (as ASP.NET Core does per request), not from a singleton or the root provider. A task started inside a scope and left running keeps that scope's token.
