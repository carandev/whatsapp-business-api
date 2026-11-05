---
title: Sending messages
description: "Text, media, location, contacts, templates, and interactive messages."
order: 3
---

All send methods live on `IWhatsAppMessagesClient` and return `Task<Result<MessageResponse>>`. The examples assume `messages` is an `IWhatsAppMessagesClient` resolved from dependency injection and `to` is the recipient's number in international format without `+`.

Every method also accepts:

- `context`: reply to a previous message (`new Context { MessageId = "wamid..." }`).
- `phoneNumberIdOverride`: send from a number other than `WhatsAppOptions.PhoneNumberId`.
- `ct`: a `CancellationToken`.

## Text

```csharp
var result = await messages.SendTextAsync(to, "Your order has shipped: https://example.com/track/123", previewUrl: true);
```

## Reply to a message

```csharp
await messages.SendTextAsync(to, "Thanks, we got it!", context: new Context { MessageId = "wamid.HBgLMTY1MDUwNzY1MjAVAgARGBI5QTNDQTVCM0Q0Q0Q2RTY3RTcA" });
```

## Media

Send images, documents, videos, audio, and stickers by **either** a `mediaId` from an [upload](../media/) **or** a public HTTPS `link`:

```csharp
await messages.SendImageAsync(to, link: "https://example.com/product.jpg", caption: "New arrival");
await messages.SendDocumentAsync(to, mediaId: "1037543291543636", caption: "Invoice #42");
await messages.SendVideoAsync(to, link: "https://example.com/demo.mp4");
await messages.SendAudioAsync(to, mediaId: "1037543291543637");
await messages.SendStickerAsync(to, mediaId: "1037543291543638");
```

## Location

```csharp
await messages.SendLocationAsync(to, latitude: 4.6097, longitude: -74.0817, name: "Main office", address: "Bogotá, Colombia");
```

## Contacts

```csharp
await messages.SendContactsAsync(to,
[
    new Contact
    {
        Name = new ContactName { FormattedName = "Ana Pérez", FirstName = "Ana", LastName = "Pérez" },
        Phones = [new ContactPhone { Phone = "+15551234567", Type = "WORK" }],
    },
]);
```

## Templates

Template messages are the only messages you can send outside the 24-hour customer service window. The template must already be [approved](../templates/).

```csharp
await messages.SendTemplateAsync(to, templateName: "hello_world", languageCode: "en_US");
```

Fill template variables with parameter components:

```csharp
await messages.SendTemplateAsync(to, "order_update", "en_US",
[
    new HeaderParameterComponent([new TextParameter("#1234")]),
    new BodyParameterComponent([new TextParameter("Ana"), new TextParameter("Friday")]),
    new ButtonParameterComponent(ButtonParameterSubType.Url, index: "0", [new TextParameter("1234")]),
]);
```

Available parameter types: `TextParameter`, `CurrencyParameter`, and `DateTimeParameter` for text variables; `ImageParameter`, `DocumentParameter`, and `VideoParameter` for media headers; and `PayloadParameter` for quick-reply buttons.

## Interactive messages

### Reply buttons

One to three buttons (more throw an `ArgumentException`). The selected button arrives as an inbound webhook message.

```csharp
await messages.SendReplyButtonsAsync(to, "Do you confirm your appointment for tomorrow at 10:00?",
[
    new ReplyButton("confirm", "Confirm"),
    new ReplyButton("reschedule", "Reschedule"),
],
header: new TextInteractiveHeader("Appointment"),
footerText: "Reply STOP to opt out");
```

### List

```csharp
await messages.SendListMessageAsync(to,
    bodyText: "Pick a delivery slot",
    buttonLabel: "View slots",
    sections:
    [
        new ListSection("Tomorrow",
        [
            new ListRow("slot-am", "Morning", "08:00 to 12:00"),
            new ListRow("slot-pm", "Afternoon", "14:00 to 18:00"),
            new ListRow("slot-any", "Any time"), // the description is optional
        ]),
    ],
    headerText: "Delivery");
```

### Call-to-action URL button

```csharp
await messages.SendCtaUrlButtonAsync(to,
    bodyText: "Your invoice is ready.",
    displayText: "View invoice",
    url: "https://example.com/invoices/42");
```

## Handling the result

```csharp
var sent = await messages.SendTextAsync(to, "Hi!");

if (sent.IsSuccess)
{
    Console.WriteLine(sent.Value!.MessageId);
}
else
{
    Console.WriteLine($"{sent.Error!.Code}: {sent.Error.ErrorUserMsg ?? sent.Error.Message}");
}
```

See [Errors and retries](../errors-and-resilience/) for when a call returns a failure versus throws.
