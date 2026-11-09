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

### Variable examples

Meta reviews templates with variables against example values. Set them with the optional `Examples` (positional) or `NamedExamples` (named) init properties; they are omitted from the request when you leave them out:

```csharp
new TextHeaderComponent("Order {{1}}") { Examples = ["4521"] };
new BodyComponent("Hi {{1}}, your order arrives on {{2}}.") { Examples = ["Ana", "May 3"] };
new BodyComponent("Hi {{first_name}}!") { NamedExamples = [new NamedParameterExample("first_name", "Ana")] };
new UrlButton("Track order", "https://example.com/track/{{1}}") { Examples = ["4521"] };
```

Examples must match the placeholders: one value per distinct positional placeholder, in order, or exactly one entry per named placeholder. A mismatch throws an `ArgumentException`.

Named placeholders must be lowercase, as Meta requires: letters `a-z`, digits, and underscores, starting with a letter or underscore (`{{first_name}}`, `{{address_line_2}}`). `BodyComponent` and `TextHeaderComponent` throw an `ArgumentException` for a name such as `{{FirstName}}`. Templates you read back from Meta are not re-validated.

### Parameter format

`CreateTemplateRequest.ParameterFormat` sets Meta's `parameter_format` (`positional` or `named`). When you leave it null, the request sends `named` if the body or a text header uses named placeholders, and omits the field otherwise; Meta then treats the template as positional.

```csharp
var request = new CreateTemplateRequest(
    name: "order_named",
    category: TemplateCategory.Utility,
    language: "en_US",
    components: [new BodyComponent("Hi {{first_name}}!") { NamedExamples = [new("first_name", "Ana")] }])
{
    ParameterFormat = TemplateParameterFormat.Named,
};
```

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

Returned headers read as the matching type by their `format`, with their examples: `TextHeaderComponent` (`Examples` or `NamedExamples`), or `ImageHeaderComponent`, `VideoHeaderComponent`, and `DocumentHeaderComponent` (`ExampleHandle`). A header the SDK does not model, such as a `LOCATION` header, reads as `UnknownHeaderComponent` with its `Format`, so one unusual template never fails the whole page:

```csharp
var header = template.Components?.OfType<HeaderComponent>().FirstOrDefault();
if (header is UnknownHeaderComponent unknown)
{
    Console.WriteLine($"Unsupported header format: {unknown.Format}");
}
```

The same holds for components and buttons. A component with a `type` the SDK does not model, such as `CAROUSEL` or `LIMITED_TIME_OFFER`, reads as `UnknownTemplateComponent`, and a button such as `VOICE_CALL` reads as `UnknownTemplateButton`. Both keep `Type` and the original object in `RawJson`, and serialize back to that JSON unchanged, so a template read from Meta can be sent again:

```csharp
foreach (var component in template.Components?.OfType<UnknownTemplateComponent>() ?? [])
{
    Console.WriteLine($"Unsupported component {component.Type}: {component.RawJson}");
}
```

## Get, update, delete

```csharp
var template = await templates.GetAsync("1234567890");

await templates.UpdateAsync("1234567890", new UpdateTemplateRequest(
    components: [new BodyComponent("Hi {{1}}, your order arrives on {{2}}. Thanks!")]));

await templates.DeleteAsync("1234567890", name: "order_update");
```
