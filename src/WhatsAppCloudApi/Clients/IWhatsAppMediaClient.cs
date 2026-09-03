using WhatsAppCloudApi.Models.Media;

namespace WhatsAppCloudApi.Clients;

/// <summary>
/// Client for the WhatsApp Cloud API media resource (upload/download/delete media assets).
/// Upload methods resolve their target phone number id from a per-call
/// <c>phoneNumberIdOverride</c> or, when omitted, <see cref="WhatsAppOptions.PhoneNumberId"/>.
/// Graph API-level (HTTP 4xx/5xx) failures are reported via <see cref="Result{T}"/>, never an
/// exception. Local size validation failures throw <see cref="MediaSizeExceededException"/>
/// synchronously, before any HTTP request is sent. <see cref="GetMediaUrlAsync"/> resolves a media
/// id to a short-lived CDN URL; <see cref="DownloadAsync"/> and <see cref="DownloadBytesAsync"/>
/// download that URL's content using the same underlying <see cref="HttpClient"/>, with an
/// absolute-URI request built via <see cref="IWhatsAppClient.CreateAbsoluteRequest"/>.
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

    /// <summary>
    /// Resolves a media id to a short-lived, downloadable CDN URL via <c>GET /{media-id}</c>. This
    /// endpoint is not phone-number-scoped.
    /// </summary>
    /// <param name="mediaId">The Meta-assigned media identifier to resolve.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> wrapping the resolved <see cref="MediaUrlResponse"/> on
    /// success; a failed <see cref="Result{T}"/> when the media id is unknown or the API call
    /// fails.
    /// </returns>
    Task<Result<MediaUrlResponse>> GetMediaUrlAsync(string mediaId, CancellationToken ct = default);

    /// <summary>
    /// Downloads media content from an absolute CDN <paramref name="url"/>, as returned by
    /// <see cref="GetMediaUrlAsync"/>. Builds the request via
    /// <see cref="IWhatsAppClient.CreateAbsoluteRequest"/>, which attaches the same
    /// <c>Authorization: Bearer</c> header used for other Graph API calls and overrides the
    /// client's relative-path base address behavior.
    /// </summary>
    /// <param name="url">The absolute CDN URL to download from.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> wrapping a <see cref="Stream"/> the caller is
    /// responsible for disposing; a failed <see cref="Result{T}"/> when the download fails.
    /// </returns>
    Task<Result<Stream>> DownloadAsync(string url, CancellationToken ct = default);

    /// <summary>
    /// Downloads media content from an absolute CDN <paramref name="url"/>, as returned by
    /// <see cref="GetMediaUrlAsync"/>, buffering the full response into memory. Uses the same
    /// absolute-URI, same-<see cref="HttpClient"/>, Bearer-auth mechanism as
    /// <see cref="DownloadAsync"/>.
    /// </summary>
    /// <param name="url">The absolute CDN URL to download from.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> wrapping the downloaded content as a <c>byte[]</c>; a
    /// failed <see cref="Result{T}"/> when the download fails.
    /// </returns>
    Task<Result<byte[]>> DownloadBytesAsync(string url, CancellationToken ct = default);
}
