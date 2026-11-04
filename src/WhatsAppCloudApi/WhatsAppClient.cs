using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WhatsAppCloudApi.Logging;
using WhatsAppCloudApi.Resilience;

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
    public HttpRequestMessage CreateAbsoluteRequest(HttpMethod method, Uri absoluteUri)
    {
        ArgumentNullException.ThrowIfNull(method);
        ArgumentNullException.ThrowIfNull(absoluteUri);

        var request = new HttpRequestMessage(method, absoluteUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.AccessToken);
        return request;
    }

    /// <inheritdoc />
    public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var method = request.Method;
        var uri = request.RequestUri;

        _logger.LogDebug("Sending {Method} {Uri}", method, uri);

        if (_logger.IsEnabled(LogLevel.Trace))
        {
            var headerFields = request.Headers.ToDictionary(
                header => header.Key,
                header => (string?)string.Join(",", header.Value));
            var redactedHeaders = SensitiveDataRedactor.Redact(headerFields);
            _logger.LogTrace("Request headers: {Headers}", redactedHeaders);
        }

        if (_logger.IsEnabled(LogLevel.Debug) && request.Content is not null)
        {
            var requestBody = await ReadBodyForLoggingAsync(request.Content, cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("Request body: {Body}", RedactBody(requestBody));
        }

        var stopwatch = Stopwatch.StartNew();
        HttpResponseMessage? response = null;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            return response;
        }
        finally
        {
            stopwatch.Stop();

            _logger.LogInformation(
                "{Method} {Uri} responded {StatusCode} in {ElapsedMilliseconds}ms",
                method,
                RedactUri(uri),
                response is not null ? response.StatusCode.ToString() : "(no response)",
                stopwatch.ElapsedMilliseconds);

            if (response is not null && _logger.IsEnabled(LogLevel.Debug))
            {
                // Buffer-and-replace the response body via the same primitive the resilience
                // pipeline uses (GraphErrorPeek), so the body stays fully readable downstream by
                // both the retry predicate/exhaustion handler and each resource client's
                // ReadErrorAsync/success-deserialization path. Calling it here is idempotent even
                // when the pipeline already peeked the body earlier for a failure response.
                await GraphErrorPeek.TryReadAsync(response, cancellationToken).ConfigureAwait(false);
                var responseBody = await ReadBodyForLoggingAsync(response.Content, cancellationToken).ConfigureAwait(false);
                _logger.LogDebug("Response body: {Body}", RedactBody(responseBody));
            }
        }
    }

    private static async Task<string?> ReadBodyForLoggingAsync(HttpContent content, CancellationToken cancellationToken)
    {
        if (!IsLoggableTextContent(content.Headers.ContentType?.MediaType))
        {
            return "(binary content omitted)";
        }

        return await content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
    }

    private static bool IsLoggableTextContent(string? mediaType) =>
        mediaType is null
        || mediaType.Contains("json", StringComparison.OrdinalIgnoreCase)
        || mediaType.StartsWith("text/", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Redacts sensitive top-level JSON fields (<c>token</c>/<c>access_token</c>/<c>Authorization</c>)
    /// from a request/response body before it is logged at Debug level, reusing the same
    /// <see cref="SensitiveDataRedactor"/> used for headers. Non-JSON or non-object bodies are
    /// logged verbatim since there is no known key shape to redact.
    /// </summary>
    private static string RedactBody(string? body)
    {
        if (string.IsNullOrEmpty(body))
        {
            return string.Empty;
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return body;
            }

            var fields = new Dictionary<string, string?>(StringComparer.Ordinal);
            foreach (var property in document.RootElement.EnumerateObject())
            {
                fields[property.Name] = property.Value.ValueKind == JsonValueKind.String
                    ? property.Value.GetString()
                    : property.Value.GetRawText();
            }

            var redacted = SensitiveDataRedactor.Redact(fields);
            return JsonSerializer.Serialize(redacted);
        }
        catch (JsonException)
        {
            return body;
        }
    }

    /// <summary>
    /// Redacts sensitive query-string parameters from <paramref name="uri"/> before it is logged
    /// at Information level, reusing <see cref="SensitiveDataRedactor.IsSensitiveKey"/> for the
    /// same key set as headers/bodies.
    /// </summary>
    private static string RedactUri(Uri? uri)
    {
        if (uri is null)
        {
            return "(none)";
        }

        // Requests built via CreateRequest/CreateWabaRequest carry a *relative* URI until
        // HttpClient combines it with BaseAddress at send time — Uri.Query throws on those, so
        // operate on the string form instead, which works for both relative and absolute URIs.
        var uriText = uri.IsAbsoluteUri ? uri.ToString() : uri.OriginalString;
        var queryIndex = uriText.IndexOf('?', StringComparison.Ordinal);
        if (queryIndex < 0)
        {
            return uriText;
        }

        var path = uriText[..queryIndex];
        var query = uriText[(queryIndex + 1)..];

        var pairs = query.Split('&', StringSplitOptions.RemoveEmptyEntries);
        var redactedPairs = new string[pairs.Length];
        for (var i = 0; i < pairs.Length; i++)
        {
            var pair = pairs[i];
            var separatorIndex = pair.IndexOf('=');
            if (separatorIndex < 0)
            {
                redactedPairs[i] = pair;
                continue;
            }

            var key = Uri.UnescapeDataString(pair[..separatorIndex]);
            redactedPairs[i] = SensitiveDataRedactor.IsSensitiveKey(key)
                ? $"{pair[..separatorIndex]}={SensitiveDataRedactor.Mask}"
                : pair;
        }

        return $"{path}?{string.Join("&", redactedPairs)}";
    }
}
