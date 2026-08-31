# WhatsApp Business API

A stateless ASP.NET Core Web API wrapper around Meta's WhatsApp Cloud API (Graph API). No database, no background services, no per-request state beyond the HTTP call to Meta.

## Status

The MVP milestone is complete, built incrementally as a chain of pull requests off the `feature/whatsapp-mvp` tracker branch:

| PR | Scope | Status |
|----|-------|--------|
| 1 | Solution scaffolding + configuration (fail-fast options validation) | Implemented |
| 2 | Graph API error mapping (`ProblemDetails`, 24h-window handling) | Implemented |
| 3 | Outbound messaging client (`POST /messages`) | Implemented |
| 4 | Inbound webhook (verification handshake, signature validation, payload parsing) | Implemented |

All four PRs are implemented and committed on their respective branches, pending review and merge into `main`.

## Solution Layout

```
WhatsAppBusinessApi.sln
src/
  WhatsAppBusinessApi.Api/
    Clients/         # Typed HttpClient for Graph API + POST /messages endpoint
    Configuration/    # IOptions<T> classes + fail-fast IValidateOptions validators
    ErrorMapping/     # GraphApiException -> ProblemDetails (incl. 24h-window)
    Webhooks/         # GET/POST /webhook: verify handshake, signature check, payload parsing
tests/
  WhatsAppBusinessApi.Api.Tests/  # xUnit + Moq unit/integration tests
```

## Requirements

- .NET 10 SDK

## Endpoints

| Method | Path | Purpose |
|--------|------|---------|
| `POST` | `/messages` | Send a free-form text message to a WhatsApp number via Meta Graph API. Returns `202 Accepted` with the Graph message ID, or a `ProblemDetails` error (incl. `403` for the 24h customer-service-window rejection). |
| `GET` | `/webhook` | Meta's verification handshake. Echoes `hub.challenge` as plain text (`200`) if `hub.verify_token` matches configuration; otherwise `403`. |
| `POST` | `/webhook` | Inbound event delivery. Validates `X-Hub-Signature-256` (HMAC-SHA256 over the raw body, `401` if invalid/missing), then parses the payload, logs the event, and reports the parsed message type (`text`, `image`, `status`, ...). No reply/echo is ever sent back. |

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

Run a focused slice, e.g.:

```bash
dotnet test --filter "FullyQualifiedName~Configuration"
dotnet test --filter "FullyQualifiedName~ErrorMapping"
dotnet test --filter "FullyQualifiedName~Clients"
dotnet test --filter "FullyQualifiedName~Webhooks"
```

## Exposing a local webhook to Meta (dev tunnel required)

Meta's servers must reach your local machine over **public HTTPS** to deliver webhook events and perform the `GET /webhook` verification handshake — plain `localhost` is not reachable from Meta's infrastructure. This is a documented risk from the proposal (mitigated by using a dev tunnel for local testing; no additional hosting decision is required for the MVP).

Use a tunneling tool such as [ngrok](https://ngrok.com/) or [Visual Studio Dev Tunnels](https://learn.microsoft.com/aspnet/core/test/dev-tunnels):

```bash
ngrok http 5000
```

Then, in the Meta App Dashboard's WhatsApp > Configuration > Webhook settings:

1. Set the **Callback URL** to the resulting `https://*.ngrok-free.app/webhook` URL.
2. Set the **Verify Token** to the same value configured as `Webhook:VerifyToken` above.
3. Click **Verify and Save** — Meta calls `GET /webhook`; a successful handshake confirms the token matches and the tunnel is reachable.
4. Subscribe to the `messages` webhook field to start receiving inbound events at `POST /webhook`.

## Manual smoke-test checklist (against real Meta Cloud API)

Unit and integration tests cover all logic with mocked/fake HTTP handlers (no real network calls). To validate against the real Graph API and a real Meta webhook delivery, with valid credentials and a dev tunnel running:

- [ ] `POST /messages` with a real recipient number inside the 24h window returns `202` with a real Graph message ID
- [ ] `POST /messages` to a number outside the 24h window returns `403` with the `customer-service-window-expired` `ProblemDetails` type
- [ ] Meta Dashboard's webhook verification (`GET /webhook`) succeeds through the dev tunnel
- [ ] Sending a real WhatsApp message to the configured test number triggers `POST /webhook`, and the message type is visible in the application logs
- [ ] Tampering with or omitting `X-Hub-Signature-256` (e.g. via `curl`) results in `401` and no payload processing

## Success Criteria (from the MVP proposal)

- [x] Automated tests cover the send-message success and failure code paths against a mocked/faked Graph API — `POST /messages` implemented and integration-tested (PR 3)
- [ ] Service sends a text message to a **real** WhatsApp number via Cloud API and gets a success response — no automated test calls the real Graph API; pending the manual smoke-test checklist above
- [x] `GET /webhook` verification handshake returns the challenge when verify token matches — implemented and integration-tested (PR 4)
- [x] `POST /webhook` validates `X-Hub-Signature-256` and rejects invalid signatures — implemented and unit/integration-tested (PR 4)
- [x] Inbound message payloads are parsed, logged, and their message type reported — implemented and unit-tested against text/image/status fixtures (PR 4)
- [x] Graph API errors (including 24h-window rejections) are mapped to the service's own `ProblemDetails` error format — implemented and unit-tested (PR 2), exercised end-to-end via `POST /messages` (PR 3)
- [x] All credentials are externalized, never hardcoded — `IOptions<T>` + fail-fast `ValidateOnStart()` (PR 1); no secrets committed to `appsettings*.json`

Six of the seven items above are implemented and fully covered by automated tests. The "real WhatsApp number" item is intentionally left unchecked: automated tests only exercise the code paths against a mocked/faked Graph API, not Meta's live endpoint. That item — along with "real Meta webhook delivery" — is tracked in the manual smoke-test checklist above, since both require live Meta credentials and a reachable dev tunnel that automated tests intentionally do not depend on.
