---
title: Message templates
description: "Create, list, update, and delete message templates."
order: 5
---

`IWhatsAppTemplatesClient` manages the templates of your WhatsApp Business Account. These calls are WABA-scoped: configure `WhatsAppOptions.WabaId` or pass `wabaIdOverride`. The examples assume `templates` is an `IWhatsAppTemplatesClient`.

To **send** an approved template, use `IWhatsAppMessagesClient.SendTemplateAsync`; see [Sending messages](../sending-messages/#templates).

## Create

```csharp
var created = await templates.CreateAsync(new CreateTemplateRequest(
    name: "order_update",
    category: TemplateCategory.Utility,
    language: "en_US",
    components:
    [
        new TextHeaderComponent("Order {{1}}"),
        new BodyComponent("Hi {{1}}, your order arrives on {{2}}."),
        new FooterComponent("Reply STOP to opt out"),
        new ButtonsComponent([new UrlButton("Track order", "https://example.com/track/{{1}}")]),
    ]));
```

The component list is validated locally before any request: it needs exactly one body and at most one header and one footer, otherwise the constructor throws an `ArgumentException`. New templates start in review; check `Template.Status` until Meta approves them.

Button types: `UrlButton`, `PhoneNumberButton`, `QuickReplyButton`, `CopyCodeButton`, `OtpButton`, `CatalogButton`, and `FlowButton`. Headers: `TextHeaderComponent`, `ImageHeaderComponent`, `VideoHeaderComponent`, and `DocumentHeaderComponent`; media headers take an example handle from a resumable upload.

## List

```csharp
var page = await templates.ListAsync(new TemplatesListOptions { Status = "APPROVED", Limit = 50 });

foreach (var template in page.Value?.Data ?? [])
{
    Console.WriteLine($"{template.Name} ({template.Language}): {template.Status}");
}

var next = page.Value?.Paging?.Cursors?.After;
```

Pass `After` (or `Before`) in a new `TemplatesListOptions` to fetch the next page.

## Get, update, delete

```csharp
var template = await templates.GetAsync("1234567890");

await templates.UpdateAsync("1234567890", new UpdateTemplateRequest(
    components: [new BodyComponent("Hi {{1}}, your order arrives on {{2}}. Thanks!")]));

await templates.DeleteAsync("1234567890", name: "order_update");
```
