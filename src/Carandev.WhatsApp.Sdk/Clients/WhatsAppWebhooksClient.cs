using System.Text;
using System.Text.Json;
using Carandev.WhatsApp.Sdk.Models.PhoneNumbers;
using Carandev.WhatsApp.Sdk.Models.Templates;
using Carandev.WhatsApp.Sdk.Models.Webhooks;
using Carandev.WhatsApp.Sdk.Serialization;

namespace Carandev.WhatsApp.Sdk.Clients;

/// <inheritdoc cref="IWhatsAppWebhooksClient" />
public sealed class WhatsAppWebhooksClient : IWhatsAppWebhooksClient
{
    private const string SubscribedAppsPath = "{waba_id}/subscribed_apps";

    private readonly IWhatsAppClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="WhatsAppWebhooksClient"/> class.
    /// </summary>
    /// <param name="client">
    /// The low-level HTTP client used to build and send requests. It resolves the WABA id from
    /// <see cref="WhatsAppOptions.WabaId"/> when a per-call override is not supplied.
    /// </param>
    public WhatsAppWebhooksClient(IWhatsAppClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
        _client = client;
    }

    /// <inheritdoc />
    public Task<Result> SubscribeAppAsync(Uri? overrideCallbackUri = null, string? verifyToken = null, string? wabaIdOverride = null, CancellationToken ct = default)
    {
        HttpContent? content = null;
        if (overrideCallbackUri is not null || verifyToken is not null)
        {
            if (overrideCallbackUri is null)
            {
                throw new ArgumentException("A verify token requires an override callback URI.", nameof(verifyToken));
            }

            if (string.IsNullOrWhiteSpace(verifyToken))
            {
                throw new ArgumentException("An override callback URI requires a non-empty verify token.", nameof(verifyToken));
            }

            PhoneNumberArguments.ThrowIfInvalidCallback(overrideCallbackUri, nameof(overrideCallbackUri));

            var json = JsonSerializer.Serialize(
                new SubscribeAppRequest(overrideCallbackUri.AbsoluteUri, verifyToken),
                WhatsAppJsonSerializerContext.Default.SubscribeAppRequest);
            content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        return SendAsync(HttpMethod.Post, wabaIdOverride, content, ct);
    }

    /// <inheritdoc />
    public async Task<Result<TemplatesPage<SubscribedApp>>> ListSubscribedAppsAsync(string? wabaIdOverride = null, CancellationToken ct = default)
    {
        if (!TryCreateRequest(HttpMethod.Get, wabaIdOverride, out var httpRequest, out var error))
        {
            return Result<TemplatesPage<SubscribedApp>>.Failure(error);
        }

        var response = await _client.SendAsync(httpRequest, ct).ConfigureAwait(false);
        if (response.IsSuccessStatusCode)
        {
            var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            var page = await JsonSerializer.DeserializeAsync(stream, WhatsAppJsonSerializerContext.Default.TemplatesPageSubscribedApp, ct).ConfigureAwait(false);
            return Result<TemplatesPage<SubscribedApp>>.Success(page!);
        }

        return Result<TemplatesPage<SubscribedApp>>.Failure(await ReadErrorAsync(response, ct).ConfigureAwait(false));
    }

    /// <inheritdoc />
    public Task<Result> UnsubscribeAppAsync(string? wabaIdOverride = null, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Delete, wabaIdOverride, content: null, ct);

    private async Task<Result> SendAsync(HttpMethod method, string? wabaIdOverride, HttpContent? content, CancellationToken ct)
    {
        if (!TryCreateRequest(method, wabaIdOverride, out var httpRequest, out var error))
        {
            content?.Dispose();
            return Result.Failure(error);
        }

        httpRequest.Content = content;
        var response = await _client.SendAsync(httpRequest, ct).ConfigureAwait(false);
        return response.IsSuccessStatusCode
            ? Result.Success()
            : Result.Failure(await ReadErrorAsync(response, ct).ConfigureAwait(false));
    }

    private bool TryCreateRequest(HttpMethod method, string? wabaIdOverride, out HttpRequestMessage request, out WhatsAppError error)
    {
        try
        {
            // The low-level client falls back to WhatsAppOptions.WabaId and throws
            // InvalidOperationException when neither is set.
            request = _client.CreateWabaRequest(method, SubscribedAppsPath, wabaIdOverride);
            error = null!;
            return true;
        }
        catch (InvalidOperationException)
        {
            request = null!;
            error = new WhatsAppError
            {
                Message = "WabaId is not configured; set WhatsAppOptions.WabaId or pass wabaIdOverride.",
            };
            return false;
        }
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
}
