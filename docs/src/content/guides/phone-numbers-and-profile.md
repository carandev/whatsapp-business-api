---
title: Phone numbers and business profile
description: "Read phone numbers, verify codes, and manage the business profile."
order: 6
---

The examples assume `phoneNumbers` is an `IWhatsAppPhoneNumbersClient` and `businessProfile` is an `IWhatsAppBusinessProfileClient`.

## Phone numbers

```csharp
// All numbers in the WABA (requires WhatsAppOptions.WabaId or wabaIdOverride).
var numbers = await phoneNumbers.ListPhoneNumbersAsync();

foreach (var number in numbers.Value?.Data ?? [])
{
    Console.WriteLine($"{number.DisplayPhoneNumber} {number.VerifiedName} quality={number.QualityRating}");
}

// The configured number (or phoneNumberIdOverride).
var current = await phoneNumbers.GetPhoneNumberAsync();
```

### Paging through numbers

A WABA with many numbers returns them in pages. Pass `PhoneNumbersListOptions` and follow `Paging.Cursors.After` until it is `null`:

```csharp
string? after = null;
do
{
    var page = await phoneNumbers.ListPhoneNumbersAsync(new PhoneNumbersListOptions { Limit = 50, After = after });
    if (!page.IsSuccess)
    {
        break;
    }

    foreach (var number in page.Value!.Data ?? [])
    {
        Console.WriteLine(number.DisplayPhoneNumber);
    }

    after = page.Value.Paging?.Cursors?.After;
}
while (after is not null);
```

### Code verification

```csharp
await phoneNumbers.RequestVerificationCodeAsync(VerificationCodeMethod.Sms, languageCode: "en_US");

// After the code arrives:
await phoneNumbers.VerifyCodeAsync("123456");
```

### Registration and two-step verification

A verified number must be registered before it can send or receive. The PIN is the 6-digit two-step verification PIN: the current one if two-step verification is already on, otherwise it becomes the new PIN. `dataLocalizationRegion` is optional and stores the number's data in that country (a 2-letter ISO 3166 code):

```csharp
await phoneNumbers.RegisterAsync("123456");
await phoneNumbers.RegisterAsync("123456", dataLocalizationRegion: "CH");

// Change the two-step verification PIN.
await phoneNumbers.SetTwoStepVerificationPinAsync("654321");

// Stop using the number with Cloud API. Register it again to resume.
await phoneNumbers.DeregisterAsync();
```

A PIN that is not exactly 6 digits, or a region that is not 2 letters, throws `ArgumentException` before any request is sent. Meta allows 10 register or deregister calls per number every 72 hours; past that, the result fails with error code `133016`.

### Per number webhook callback

A phone number can send its webhooks to its own callback, overriding the WABA and app callbacks. This is handy for pointing one number at a local tunnel or staging. Meta verifies the callback with the token before it accepts it:

```csharp
await phoneNumbers.SetWebhookOverrideAsync(new Uri("https://my-tunnel.example.com/webhook"), "my-verify-token");

var configuration = await phoneNumbers.GetWebhookConfigurationAsync();
Console.WriteLine(configuration.Value?.PhoneNumber);             // this number's override
Console.WriteLine(configuration.Value?.WhatsAppBusinessAccount); // the WABA override, if any
Console.WriteLine(configuration.Value?.Application);             // the app callback

// Send webhooks back to the WABA or app callback.
await phoneNumbers.ClearWebhookOverrideAsync();
```

The callback must be an absolute HTTPS URL, and the verify token must not be empty; otherwise `ArgumentException` is thrown before any request is sent. A plain `GetPhoneNumberAsync` call does not include `webhook_configuration`, so use `GetWebhookConfigurationAsync` to read it.

## Business profile

```csharp
var profile = await businessProfile.GetBusinessProfileAsync();
Console.WriteLine(profile.Value?.About);

await businessProfile.UpdateBusinessProfileAsync(new UpdateBusinessProfileRequest
{
    About = "Open Monday to Friday, 8:00 to 18:00",
    Email = "support@example.com",
    Websites = ["https://example.com"],
});
```

Only the properties you set are sent; omitted properties keep their current value.
