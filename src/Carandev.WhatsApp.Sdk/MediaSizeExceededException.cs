using System.Globalization;

namespace Carandev.WhatsApp.Sdk;

/// <summary>
/// Thrown when content passed to <c>IWhatsAppMediaClient.UploadAsync</c> exceeds the size ceiling
/// for its MIME-type category (image, video, audio, document, or sticker). Thrown synchronously,
/// before any HTTP request is sent — same precedent as <c>MediaReference.Create</c>'s
/// <see cref="ArgumentException"/>.
/// </summary>
public sealed class MediaSizeExceededException : ArgumentException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MediaSizeExceededException"/> class.
    /// </summary>
    /// <param name="mimeType">The content's MIME type, used to resolve the applicable size category.</param>
    /// <param name="contentLength">The content's actual size, in bytes.</param>
    /// <param name="maxAllowedBytes">The maximum size allowed for <paramref name="mimeType"/>'s category, in bytes.</param>
    public MediaSizeExceededException(string mimeType, long contentLength, long maxAllowedBytes)
        : base(BuildMessage(mimeType, contentLength, maxAllowedBytes))
    {
        MimeType = mimeType;
        ContentLength = contentLength;
        MaxAllowedBytes = maxAllowedBytes;
    }

    /// <summary>
    /// The content's MIME type, used to resolve the applicable size category.
    /// </summary>
    public string MimeType { get; }

    /// <summary>
    /// The content's actual size, in bytes. For a non-seekable stream that was buffered and found
    /// to exceed the ceiling, this reflects the buffered byte count observed at the point the
    /// ceiling was crossed.
    /// </summary>
    public long ContentLength { get; }

    /// <summary>
    /// The maximum size allowed for <see cref="MimeType"/>'s category, in bytes.
    /// </summary>
    public long MaxAllowedBytes { get; }

    private static string BuildMessage(string mimeType, long contentLength, long maxAllowedBytes) =>
        string.Format(
            CultureInfo.InvariantCulture,
            "Content of type '{0}' is {1} bytes, which exceeds the {2}-byte size limit for its MIME category.",
            mimeType,
            contentLength,
            maxAllowedBytes);
}
