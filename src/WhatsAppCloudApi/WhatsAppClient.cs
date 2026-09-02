using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WhatsAppCloudApi.Logging;

namespace WhatsAppCloudApi;

/// <summary>
/// Default <see cref="IWhatsAppClient"/> implementation backed by a typed <see cref="HttpClient"/>
/// resolved via <c>IHttpClientFactory</c>.
/// </summary>
public sealed class WhatsAppClient : IWhatsAppClient
{
    private readonly HttpClient _httpClient;
    private readonly WhatsAppOptions _options;
    private readonly ILogger<WhatsAppClient> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WhatsAppClient"/> class.
    /// </summary>
    public WhatsAppClient(HttpClient httpClient, IOptions<WhatsAppOptions> options, ILogger<WhatsAppClient> logger)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public HttpRequestMessage CreateRequest(HttpMethod method, string relativePathTemplate, string? phoneNumberIdOverride = null)
    {
        ArgumentNullException.ThrowIfNull(method);
        ArgumentException.ThrowIfNullOrEmpty(relativePathTemplate);

        var phoneNumberId = phoneNumberIdOverride ?? _options.PhoneNumberId;
        var relativePath = relativePathTemplate.Replace("{phone_number_id}", phoneNumberId, StringComparison.Ordinal);

        var request = new HttpRequestMessage(method, relativePath);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.AccessToken);
        return request;
    }

    /// <inheritdoc />
    public HttpRequestMessage CreateWabaRequest(HttpMethod method, string relativePathTemplate, string? wabaIdOverride = null)
    {
        ArgumentNullException.ThrowIfNull(method);
        ArgumentException.ThrowIfNullOrEmpty(relativePathTemplate);

        var wabaId = wabaIdOverride ?? _options.WabaId;
        if (wabaId is null)
        {
            throw new InvalidOperationException(
                "No WABA ID is available. Pass a wabaIdOverride or set WhatsAppOptions.WabaId.");
        }

        var relativePath = relativePathTemplate.Replace("{waba_id}", wabaId, StringComparison.Ordinal);

        var request = new HttpRequestMessage(method, relativePath);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.AccessToken);
        return request;
    }

    /// <inheritdoc />
    public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogDebug("Sending {Method} {Uri}", request.Method, request.RequestUri);

        if (_logger.IsEnabled(LogLevel.Trace))
        {
            var headerFields = request.Headers.ToDictionary(
                header => header.Key,
                header => (string?)string.Join(",", header.Value));
            var redactedHeaders = SensitiveDataRedactor.Redact(headerFields);
            _logger.LogTrace("Request headers: {Headers}", redactedHeaders);
        }

        return await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
