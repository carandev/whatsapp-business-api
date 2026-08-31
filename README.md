# WhatsApp Business API

A stateless ASP.NET Core Web API wrapper around Meta's WhatsApp Cloud API (Graph API). No database, no background services, no per-request state beyond the HTTP call to Meta.

## Status

This project is being built incrementally as a chain of pull requests off the `feature/whatsapp-mvp` tracker branch:

| PR | Scope | Status |
|----|-------|--------|
| 1 | Solution scaffolding + configuration (fail-fast options validation) | Implemented |
| 2 | Graph API error mapping (`ProblemDetails`, 24h-window handling) | Pending |
| 3 | Outbound messaging client (`POST /messages`) | Pending |
| 4 | Inbound webhook (verification handshake, signature validation, payload parsing) | Pending |

## Solution Layout

```
WhatsAppBusinessApi.sln
src/
  WhatsAppBusinessApi.Api/        # ASP.NET Core minimal API project (net10.0)
tests/
  WhatsAppBusinessApi.Api.Tests/  # xUnit + Moq unit/integration tests
```

## Requirements

- .NET 10 SDK

## Configuration

Credentials and webhook settings are bound from configuration via `IOptions<T>` — never hardcoded. The host **fails fast at startup** (`OptionsValidationException`) if any required key below is missing or empty.

| Section | Key | Required | Notes |
|---------|-----|----------|-------|
| `GraphApi` | `PhoneNumberId` | Yes | WhatsApp Business phone number ID |
| `GraphApi` | `AccessToken` | Yes | Graph API bearer token |
| `GraphApi` | `ApiBaseUrl` | No | Defaults to `https://graph.facebook.com/v21.0` |
| `Webhook` | `VerifyToken` | Yes | Compared against Meta's `hub.verify_token` on the GET handshake |
| `Webhook` | `AppSecret` | Yes | Used to validate the `X-Hub-Signature-256` HMAC on inbound POSTs |

`appsettings.json` ships these keys as empty stubs so the schema is documented without committing secrets. Provide real values via **user-secrets** (local dev) or **environment variables** (any environment) — never commit them.

### Local development (user-secrets)

```bash
cd src/WhatsAppBusinessApi.Api
dotnet user-secrets init
dotnet user-secrets set "GraphApi:PhoneNumberId" "<your-phone-number-id>"
dotnet user-secrets set "GraphApi:AccessToken" "<your-access-token>"
dotnet user-secrets set "Webhook:VerifyToken" "<your-verify-token>"
dotnet user-secrets set "Webhook:AppSecret" "<your-app-secret>"
```

### Environment variables (alternative)

```bash
export GraphApi__PhoneNumberId="<your-phone-number-id>"
export GraphApi__AccessToken="<your-access-token>"
export Webhook__VerifyToken="<your-verify-token>"
export Webhook__AppSecret="<your-app-secret>"
```

## Run

```bash
dotnet build
dotnet run --project src/WhatsAppBusinessApi.Api
```

With no configuration provided, the host will refuse to start and print an `OptionsValidationException` naming every missing key. This is expected — it's the fail-fast contract described above.

## Test

```bash
dotnet test
```

Run a focused slice (e.g. only configuration tests):

```bash
dotnet test --filter "FullyQualifiedName~Configuration"
```

## Exposing a local webhook to Meta (future PR)

Once the webhook endpoints land (PR 4), Meta needs to reach your local machine over HTTPS to deliver events and perform the verification handshake. A tunneling tool such as [ngrok](https://ngrok.com/) is the simplest option:

```bash
ngrok http 5000
```

Use the resulting `https://*.ngrok-free.app` URL as the webhook callback URL in the Meta App Dashboard, with the same `Webhook:VerifyToken` value configured above.
