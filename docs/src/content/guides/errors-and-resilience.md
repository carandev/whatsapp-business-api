---
title: Errors and retries
description: "Result<T> versus WhatsAppException, the retry policy, and logging."
order: 8
---

The SDK separates **rejected requests** from **unavailable service** on purpose.

| Situation | What you get |
|---|---|
| The Graph API answered and rejected the request (invalid recipient, unapproved template, missing permission) | `Result<T>` with `IsSuccess == false` and a `WhatsAppError` |
| A transient failure (5xx, rate limit, transient Graph error codes) persisted after every retry | A thrown `WhatsAppException` |
| Invalid arguments detected locally (for example four reply buttons or an oversized upload) | A thrown `ArgumentException` before any HTTP call |

## Business errors: `Result<T>`

Resource-client methods never throw for business errors:

```csharp
var result = await messages.SendTextAsync(to, "Hi!");

if (!result.IsSuccess)
{
    var error = result.Error!;
    Console.WriteLine($"Code={error.Code} Subcode={error.ErrorSubcode} TraceId={error.FbtraceId}");
    Console.WriteLine(error.ErrorUserMsg ?? error.Message);
}
```

`WhatsAppError` exposes `Code`, `ErrorSubcode`, `Type`, `Message`, `ErrorUserTitle`, `ErrorUserMsg`, `FbtraceId`, and `IsTransient`. `ErrorUserTitle` and `ErrorUserMsg` are Meta's end-user-safe texts when available. `WhatsAppException` carries the same data as `Code`, `Subcode`, `Title`, `Message`, `UserTitle`, `UserMessage`, and `FbTraceId`. Look codes up in Meta's [error code reference](https://developers.facebook.com/docs/whatsapp/cloud-api/support/error-codes).

## Transport errors: `WhatsAppException`

```csharp
try
{
    await messages.SendTextAsync(to, "Hi!");
}
catch (WhatsAppRateLimitException ex)
{
    Console.WriteLine($"Rate limited, try later: {ex.Message}");
}
catch (WhatsAppAuthenticationException)
{
    Console.WriteLine("The access token is invalid or expired.");
}
catch (WhatsAppException ex)
{
    Console.WriteLine($"Gave up after retries: {ex.Code} {ex.Message} (IsTransient={ex.IsTransient})");
}
```

The exception type is chosen from the Graph error code:

| Exception | Graph error codes |
|---|---|
| `WhatsAppRateLimitException` | `80007` |
| `WhatsAppAuthenticationException` | `190`, `200` |
| `WhatsAppTemplateRejectedException` | `132000`, `132001`, `132005`, `132007`, `132012`, `132015`, `132016` |
| `WhatsAppException` | Anything else |

## Retry policy

`AddWhatsAppCloudApi` wires a retry pipeline on the HTTP client:

- Retries 5xx responses, rate limits, and Graph error codes classified as transient.
- Exponential backoff with jitter, starting at 250 ms.
- Honors the `Retry-After` header when Meta sends one.
- Stops after `WhatsAppOptions.MaxRetries` retries (default `3`), then throws `WhatsAppException`.

Non-transient errors are never retried: they return a `Result` failure immediately.

## Logging

The HTTP client logs a summary of each request and response through `ILogger`. Access tokens and other secrets are redacted from logged headers and bodies.
