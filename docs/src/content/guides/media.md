---
title: Media
description: "Upload files, resolve media IDs, and download inbound media."
order: 4
---

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

## Upload template header media

Templates with an image, video, or document header need an example file handle, not a media ID. `UploadTemplateMediaAsync` gets one through the Graph Resumable Upload API and returns the handle string:

```csharp
await using var file = File.OpenRead("header.jpg");
var handle = await media.UploadTemplateMediaAsync(file, "image/jpeg", "header.jpg");

if (handle.IsSuccess)
{
    // Use handle.Value as the header example when you create the template.
}
```

The method needs your Meta App ID. Set `WhatsAppOptions.AppId` (see [Configuration](../configuration/)) or pass `appIdOverride` per call. Without either, it returns a `Result` failure and sends no request.

It makes two requests:

1. `POST /{app-id}/uploads?file_name=...&file_length=...&file_type=...` starts an upload session.
2. `POST /upload:<session>` sends the whole file with `Authorization: OAuth <token>` and `file_offset: 0`. The response's `h` is the handle.

The same size limits as `UploadAsync` apply, checked before any HTTP call. A non-seekable stream is buffered into memory first, because the session needs the file length up front.

<aside class="callout callout--note" data-label="Note">

If the upload fails, the SDK asks Meta how much of the file arrived and, when part of it did, sends only the remaining bytes, once. Transient failures also go through the normal retry policy. Any other failure returns a `Result` failure, and you can call `UploadTemplateMediaAsync` again to start a new session. The stream you pass in is not disposed.

</aside>

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

<aside class="callout callout--note" data-label="Note">

Download URLs expire after a few minutes. Resolve a fresh URL with `GetMediaUrlAsync` right before downloading.

</aside>
