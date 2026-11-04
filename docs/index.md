---
_layout: landing
---

# Carandev.WhatsApp.Sdk

A .NET SDK for [Meta's WhatsApp Cloud API](https://developers.facebook.com/docs/whatsapp/cloud-api). Send text, media, template, and interactive messages, manage templates, media, phone numbers, and the business profile, and receive inbound webhooks, all through typed clients with built-in retries and structured error handling.

```bash
dotnet add package Carandev.WhatsApp.Sdk
```

Targets `net8.0` and `net9.0`. One package, no ASP.NET Core dependency.

## Start here

- [Getting started](guides/getting-started.md): install, configure, and send your first message.
- [Configuration](guides/configuration.md): every `WhatsAppOptions` setting.
- [Sending messages](guides/sending-messages.md): text, media, location, contacts, templates, and interactive messages.
- [Receiving webhooks](guides/webhooks.md): verification, signature validation, and dispatch on any host.
- [Errors and retries](guides/errors-and-resilience.md): `Result<T>` versus `WhatsAppException`.
- [API reference](api/Carandev.WhatsApp.Sdk.yml): every public type, generated from the XML documentation.
