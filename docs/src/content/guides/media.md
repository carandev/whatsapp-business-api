# Media

`IWhatsAppMediaClient` uploads files to WhatsApp, resolves media IDs to download URLs, and downloads media. The examples assume `media` is an `IWhatsAppMediaClient` and `messages` is an `IWhatsAppMessagesClient`.

## Upload and send

```csharp
var upload = await media.UploadAsync("invoice-42.pdf", "application/pdf");

if (upload.IsSuccess)
{
    await messages.SendDocumentAsync(to, mediaId: upload.Value!.Id, caption: "Invoice #42");
}
```

`UploadAsync` has overloads for a file path, a `Stream`, and a `byte[]`. The stream and byte array overloads require a `filename`.

### Size limits

Uploads are checked before any HTTP call; content over the limit throws `MediaSizeExceededException` (a subclass of `ArgumentException`):

| Type | Limit |
|---|---|
| Sticker (`image/webp`) | 500 KB |
| Image | 5 MB |
| Audio | 16 MB |
| Video | 16 MB |
| Document | 100 MB |

## Download inbound media

Inbound webhook messages carry a media ID. Resolve it to a short-lived URL, then download it. The SDK sends the access token on the download request for you.

```csharp
var url = await media.GetMediaUrlAsync("1037543291543636");

if (url.IsSuccess)
{
    var file = await media.DownloadAsync(url.Value!.Url!);
    if (file.IsSuccess)
    {
        await using var content = file.Value!;
        await using var output = File.Create($"download-{url.Value.Id}");
        await content.CopyToAsync(output);
    }
}
```

`DownloadAsync` returns a `Stream` you must dispose. `DownloadBytesAsync` buffers the whole file into a `byte[]` instead.

> [!NOTE]
> Download URLs expire after a few minutes. Resolve a fresh URL with `GetMediaUrlAsync` right before downloading.
