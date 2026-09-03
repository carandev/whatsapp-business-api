using WhatsAppCloudApi.Models.Media;

namespace WhatsAppCloudApi.Clients;

/// <summary>
/// Client for the WhatsApp Cloud API media resource (upload/download/delete media assets).
/// Upload methods resolve their target phone number id from a per-call
/// <c>phoneNumberIdOverride</c> or, when omitted, <see cref="WhatsAppOptions.PhoneNumberId"/>.
/// Graph API-level (HTTP 4xx/5xx) failures are reported via <see cref="Result{T}"/>, never an
/// exception. Local size validation failures throw <see cref="MediaSizeExceededException"/>
/// synchronously, before any HTTP request is sent. Additional members (get URL, download) will be
/// added in a future release.
/// </summary>
public interface IWhatsAppMediaClient
{
    /// <summary>
    /// Uploads media content from a <see cref="Stream"/> via <c>POST /{phone_number_id}/media</c>.
    /// </summary>
    /// <param name="content">
    /// The media content. When <see cref="Stream.CanSeek"/> is <see langword="true"/>, its
    /// <see cref="Stream.Length"/> is validated directly. Otherwise the stream is buffered into
    /// memory (capped at the applicable size ceiling plus one byte) before validation, so
    /// <see cref="MediaSizeExceededException"/> is still thrown before any HTTP request in either
    /// case.
    /// </param>
    /// <param name="mimeType">
    /// The content's MIME type (e.g. <c>image/jpeg</c>), used both as the multipart file part's
    /// <c>Content-Type</c> and to resolve the applicable MIME-category size ceiling.
    /// </param>
    /// <param name="filename">The filename to report in the multipart file part.</param>
    /// <param name="phoneNumberIdOverride">
    /// An optional phone number id that takes precedence over
    /// <see cref="WhatsAppOptions.PhoneNumberId"/> when resolving the target phone number.
    /// </param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> wrapping the uploaded media's
    /// <see cref="MediaUploadResponse"/> on success; a failed <see cref="Result{T}"/> when the API
    /// call fails.
    /// </returns>
    /// <exception cref="MediaSizeExceededException">
    /// Thrown when <paramref name="content"/> exceeds the size ceiling for
    /// <paramref name="mimeType"/>'s category. Thrown before any HTTP request is sent.
    /// </exception>
    Task<Result<MediaUploadResponse>> UploadAsync(Stream content, string mimeType, string filename, string? phoneNumberIdOverride = null, CancellationToken ct = default);

    /// <summary>
    /// Uploads media content from a byte array via <c>POST /{phone_number_id}/media</c>. Behaves
    /// identically to the <see cref="Stream"/> overload, wrapping <paramref name="content"/> in a
    /// seekable <see cref="MemoryStream"/>.
    /// </summary>
    /// <param name="content">The media content.</param>
    /// <param name="mimeType">
    /// The content's MIME type (e.g. <c>image/jpeg</c>), used both as the multipart file part's
    /// <c>Content-Type</c> and to resolve the applicable MIME-category size ceiling.
    /// </param>
    /// <param name="filename">The filename to report in the multipart file part.</param>
    /// <param name="phoneNumberIdOverride">
    /// An optional phone number id that takes precedence over
    /// <see cref="WhatsAppOptions.PhoneNumberId"/> when resolving the target phone number.
    /// </param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> wrapping the uploaded media's
    /// <see cref="MediaUploadResponse"/> on success; a failed <see cref="Result{T}"/> when the API
    /// call fails.
    /// </returns>
    /// <exception cref="MediaSizeExceededException">
    /// Thrown when <paramref name="content"/> exceeds the size ceiling for
    /// <paramref name="mimeType"/>'s category. Thrown before any HTTP request is sent.
    /// </exception>
    Task<Result<MediaUploadResponse>> UploadAsync(byte[] content, string mimeType, string filename, string? phoneNumberIdOverride = null, CancellationToken ct = default);

    /// <summary>
    /// Uploads media content read from a file on disk via <c>POST /{phone_number_id}/media</c>.
    /// Behaves identically to the <see cref="Stream"/> overload, wrapping the opened file in a
    /// seekable <see cref="FileStream"/>.
    /// </summary>
    /// <param name="filePath">The path to the file to upload.</param>
    /// <param name="mimeType">
    /// The content's MIME type (e.g. <c>image/jpeg</c>), used both as the multipart file part's
    /// <c>Content-Type</c> and to resolve the applicable MIME-category size ceiling.
    /// </param>
    /// <param name="filename">
    /// The filename to report in the multipart file part. When omitted, defaults to the file name
    /// portion of <paramref name="filePath"/>.
    /// </param>
    /// <param name="phoneNumberIdOverride">
    /// An optional phone number id that takes precedence over
    /// <see cref="WhatsAppOptions.PhoneNumberId"/> when resolving the target phone number.
    /// </param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> wrapping the uploaded media's
    /// <see cref="MediaUploadResponse"/> on success; a failed <see cref="Result{T}"/> when the API
    /// call fails.
    /// </returns>
    /// <exception cref="FileNotFoundException">
    /// Thrown when <paramref name="filePath"/> does not exist on disk. Thrown before any HTTP
    /// request is sent.
    /// </exception>
    /// <exception cref="MediaSizeExceededException">
    /// Thrown when the file's content exceeds the size ceiling for <paramref name="mimeType"/>'s
    /// category. Thrown before any HTTP request is sent.
    /// </exception>
    Task<Result<MediaUploadResponse>> UploadAsync(string filePath, string mimeType, string? filename = null, string? phoneNumberIdOverride = null, CancellationToken ct = default);
}
