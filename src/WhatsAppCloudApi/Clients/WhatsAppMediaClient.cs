using System.Net.Http.Headers;
using System.Text.Json;
using WhatsAppCloudApi.Models.Media;
using WhatsAppCloudApi.Serialization;

namespace WhatsAppCloudApi.Clients;

/// <inheritdoc cref="IWhatsAppMediaClient" />
public sealed class WhatsAppMediaClient : IWhatsAppMediaClient
{
    private const string MediaPathTemplate = "{phone_number_id}/media";
    private const int BufferChunkSize = 81920;

    private readonly IWhatsAppClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="WhatsAppMediaClient"/> class.
    /// </summary>
    /// <param name="client">The low-level HTTP client used to build and send requests.</param>
    public WhatsAppMediaClient(IWhatsAppClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
        _client = client;
    }

    /// <inheritdoc />
    public async Task<Result<MediaUploadResponse>> UploadAsync(Stream content, string mimeType, string filename, string? phoneNumberIdOverride = null, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(mimeType);
        ArgumentException.ThrowIfNullOrWhiteSpace(filename);

        var validatedContent = await ValidateAndPrepareContentAsync(content, mimeType, ct).ConfigureAwait(false);

        using var multipart = new MultipartFormDataContent
        {
            { new StringContent("whatsapp"), "messaging_product" },
        };

        var filePart = new StreamContent(validatedContent);
        filePart.Headers.ContentType = new MediaTypeHeaderValue(mimeType);
        multipart.Add(filePart, "file", filename);

        var httpRequest = _client.CreateRequest(HttpMethod.Post, MediaPathTemplate, phoneNumberIdOverride);
        httpRequest.Content = multipart;

        var response = await _client.SendAsync(httpRequest, ct).ConfigureAwait(false);
        return await ReadResultAsync(response, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result<MediaUploadResponse>> UploadAsync(byte[] content, string mimeType, string filename, string? phoneNumberIdOverride = null, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        using var stream = new MemoryStream(content, writable: false);
        return await UploadAsync(stream, mimeType, filename, phoneNumberIdOverride, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result<MediaUploadResponse>> UploadAsync(string filePath, string mimeType, string? filename = null, string? phoneNumberIdOverride = null, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(mimeType);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"The file '{filePath}' was not found.", filePath);
        }

        var effectiveFilename = filename ?? Path.GetFileName(filePath);

        using var stream = File.OpenRead(filePath);
        return await UploadAsync(stream, mimeType, effectiveFilename, phoneNumberIdOverride, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Validates <paramref name="content"/> against the MIME-category size ceiling for
    /// <paramref name="mimeType"/>, before any HTTP request is built. Seekable streams are
    /// validated directly against <see cref="Stream.Length"/>. Non-seekable streams are buffered
    /// into a size-capped <see cref="MemoryStream"/> first (reading no more than the ceiling plus
    /// one byte), then the buffered length is validated — either way,
    /// <see cref="MediaSizeExceededException"/> is thrown before any HTTP call when oversized.
    /// </summary>
    private static async Task<Stream> ValidateAndPrepareContentAsync(Stream content, string mimeType, CancellationToken ct)
    {
        var maxAllowedBytes = MediaSizeLimits.GetMaxBytes(mimeType);

        if (content.CanSeek)
        {
            if (content.Length > maxAllowedBytes)
            {
                throw new MediaSizeExceededException(mimeType, content.Length, maxAllowedBytes);
            }

            return content;
        }

        var buffer = await BufferWithCapAsync(content, maxAllowedBytes, ct).ConfigureAwait(false);
        if (buffer.Length > maxAllowedBytes)
        {
            throw new MediaSizeExceededException(mimeType, buffer.Length, maxAllowedBytes);
        }

        buffer.Position = 0;
        return buffer;
    }

    /// <summary>
    /// Reads <paramref name="content"/> into a new <see cref="MemoryStream"/>, stopping as soon as
    /// <paramref name="maxAllowedBytes"/> + 1 bytes have been buffered (or the source stream ends,
    /// whichever comes first) — the caller only needs to know whether the content is over the
    /// ceiling, not its exact full length when it clearly exceeds it.
    /// </summary>
    private static async Task<MemoryStream> BufferWithCapAsync(Stream content, long maxAllowedBytes, CancellationToken ct)
    {
        var cap = maxAllowedBytes + 1;
        var buffer = new MemoryStream();
        var chunk = new byte[BufferChunkSize];

        while (buffer.Length < cap)
        {
            var toRead = (int)Math.Min(chunk.Length, cap - buffer.Length);
            var read = await content.ReadAsync(chunk.AsMemory(0, toRead), ct).ConfigureAwait(false);
            if (read == 0)
            {
                break;
            }

            buffer.Write(chunk, 0, read);
        }

        return buffer;
    }

    private static async Task<Result<MediaUploadResponse>> ReadResultAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
        {
            var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            var value = await JsonSerializer.DeserializeAsync(stream, WhatsAppJsonSerializerContext.Default.MediaUploadResponse, ct).ConfigureAwait(false);
            return Result<MediaUploadResponse>.Success(value!);
        }

        return Result<MediaUploadResponse>.Failure(await ReadErrorAsync(response, ct).ConfigureAwait(false));
    }

    private static async Task<WhatsAppError> ReadErrorAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            var errorResponse = await JsonSerializer.DeserializeAsync(stream, WhatsAppJsonSerializerContext.Default.WhatsAppErrorResponse, ct).ConfigureAwait(false);
            if (errorResponse?.Error is not null)
            {
                return errorResponse.Error;
            }
        }
        catch (JsonException)
        {
            // Response body was not a WhatsAppErrorResponse — fall through to the generic error below.
        }

        return new WhatsAppError
        {
            Message = $"Request failed with status code {(int)response.StatusCode} ({response.StatusCode}).",
            Code = (int)response.StatusCode,
        };
    }

    /// <summary>
    /// MIME-category-keyed upload size ceilings, per Meta's documented per-type limits.
    /// </summary>
    private static class MediaSizeLimits
    {
        private const long StickerMaxBytes = 500 * 1024L;
        private const long ImageMaxBytes = 5 * 1024 * 1024L;
        private const long AudioMaxBytes = 16 * 1024 * 1024L;
        private const long VideoMaxBytes = 16 * 1024 * 1024L;
        private const long DocumentMaxBytes = 100 * 1024 * 1024L;

        /// <summary>
        /// Resolves the size ceiling, in bytes, for the given MIME type's category.
        /// </summary>
        /// <param name="mimeType">The content's MIME type.</param>
        /// <returns>
        /// The applicable ceiling: 500 KB for <c>image/webp</c> (sticker), 5 MB for other
        /// <c>image/*</c>, 16 MB for <c>audio/*</c> or <c>video/*</c>, and 100 MB for everything
        /// else (document).
        /// </returns>
        public static long GetMaxBytes(string mimeType)
        {
            if (mimeType.Equals("image/webp", StringComparison.OrdinalIgnoreCase))
            {
                return StickerMaxBytes;
            }

            if (mimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                return ImageMaxBytes;
            }

            if (mimeType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase))
            {
                return AudioMaxBytes;
            }

            if (mimeType.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
            {
                return VideoMaxBytes;
            }

            return DocumentMaxBytes;
        }
    }
}
