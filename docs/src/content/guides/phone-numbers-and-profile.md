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
