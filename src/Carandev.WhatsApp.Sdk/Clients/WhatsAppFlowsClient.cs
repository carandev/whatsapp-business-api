using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Carandev.WhatsApp.Sdk.Models.Flows;
using Carandev.WhatsApp.Sdk.Models.Templates;
using Carandev.WhatsApp.Sdk.Serialization;

namespace Carandev.WhatsApp.Sdk.Clients;

/// <inheritdoc cref="IWhatsAppFlowsClient" />
public sealed class WhatsAppFlowsClient : IWhatsAppFlowsClient
{
    private const string FlowsPath = "{waba_id}/flows";

    private readonly IWhatsAppClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="WhatsAppFlowsClient"/> class.
    /// </summary>
    /// <param name="client">
    /// The low-level HTTP client used to build and send requests. It resolves the WABA id from
    /// <see cref="WhatsAppOptions.WabaId"/> when a per-call override is not supplied.
    /// </param>
    public WhatsAppFlowsClient(IWhatsAppClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
        _client = client;
    }

    /// <inheritdoc />
    public Task<Result<CreateFlowResponse>> CreateFlowAsync(CreateFlowRequest request, string? wabaIdOverride = null, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!TryCreateWabaRequest(HttpMethod.Post, FlowsPath, wabaIdOverride, out var httpRequest, out var error))
        {
            return Task.FromResult(Result<CreateFlowResponse>.Failure(error));
        }

        httpRequest.Content = JsonContent(request, WhatsAppJsonSerializerContext.Default.CreateFlowRequest);
        return SendAsync(httpRequest, WhatsAppJsonSerializerContext.Default.CreateFlowResponse, ct);
    }

    /// <inheritdoc />
    public Task<Result<TemplatesPage<Flow>>> ListFlowsAsync(FlowsListOptions? options = null, string? wabaIdOverride = null, CancellationToken ct = default)
    {
        var query = GraphQueryString.Build(
        [
            ("limit", options?.Limit?.ToString(CultureInfo.InvariantCulture)),
            ("after", options?.After),
            ("before", options?.Before),
        ]);

        if (!TryCreateWabaRequest(HttpMethod.Get, FlowsPath + query, wabaIdOverride, out var httpRequest, out var error))
        {
            return Task.FromResult(Result<TemplatesPage<Flow>>.Failure(error));
        }

        return SendAsync(httpRequest, WhatsAppJsonSerializerContext.Default.TemplatesPageFlow, ct);
    }

    /// <inheritdoc />
    public Task<Result<Flow>> GetFlowAsync(string flowId, IReadOnlyList<string>? fields = null, CancellationToken ct = default)
    {
        var path = FlowPath(flowId);
        var query = GraphQueryString.Build([("fields", fields is null || fields.Count == 0 ? null : string.Join(',', fields))]);
        return SendAsync(_client.CreateRequest(HttpMethod.Get, path + query), WhatsAppJsonSerializerContext.Default.Flow, ct);
    }

    /// <inheritdoc />
    public Task<Result> UpdateFlowMetadataAsync(string flowId, UpdateFlowMetadataRequest request, CancellationToken ct = default)
    {
        var path = FlowPath(flowId);
        ArgumentNullException.ThrowIfNull(request);
        request.Validate(nameof(request));

        var httpRequest = _client.CreateRequest(HttpMethod.Post, path);
        httpRequest.Content = JsonContent(request, WhatsAppJsonSerializerContext.Default.UpdateFlowMetadataRequest);
        return SendAsync(httpRequest, ct);
    }

    /// <inheritdoc />
    public Task<Result<FlowJsonUploadResponse>> UploadFlowJsonAsync(string flowId, string flowJson, CancellationToken ct = default)
    {
        var path = FlowPath(flowId);
        ArgumentException.ThrowIfNullOrWhiteSpace(flowJson);

        var file = new ByteArrayContent(Encoding.UTF8.GetBytes(flowJson));
        file.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        var multipart = new MultipartFormDataContent
        {
            { file, "file", "flow.json" },
            { new StringContent("flow.json"), "name" },
            { new StringContent("FLOW_JSON"), "asset_type" },
        };

        var httpRequest = _client.CreateRequest(HttpMethod.Post, path + "/assets");
        httpRequest.Content = multipart;
        return SendAsync(httpRequest, WhatsAppJsonSerializerContext.Default.FlowJsonUploadResponse, ct);
    }

    /// <inheritdoc />
    public Task<Result> PublishFlowAsync(string flowId, CancellationToken ct = default) =>
        SendAsync(_client.CreateRequest(HttpMethod.Post, FlowPath(flowId) + "/publish"), ct);

    /// <inheritdoc />
    public Task<Result> DeprecateFlowAsync(string flowId, CancellationToken ct = default) =>
        SendAsync(_client.CreateRequest(HttpMethod.Post, FlowPath(flowId) + "/deprecate"), ct);

    /// <inheritdoc />
    public Task<Result> DeleteFlowAsync(string flowId, CancellationToken ct = default) =>
        SendAsync(_client.CreateRequest(HttpMethod.Delete, FlowPath(flowId)), ct);

    /// <inheritdoc />
    public async Task<Result<FlowPreview>> GetFlowPreviewAsync(string flowId, bool invalidate = false, CancellationToken ct = default)
    {
        var path = FlowPath(flowId);
        var field = invalidate ? "preview.invalidate(true)" : "preview.invalidate(false)";
        var httpRequest = _client.CreateRequest(HttpMethod.Get, path + GraphQueryString.Build([("fields", field)]));

        var result = await SendAsync(httpRequest, WhatsAppJsonSerializerContext.Default.Flow, ct).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return Result<FlowPreview>.Failure(result.Error!);
        }

        return result.Value?.Preview is { } preview
            ? Result<FlowPreview>.Success(preview)
            : Result<FlowPreview>.Failure(new WhatsAppError { Message = "The response did not include a preview." });
    }

    private static string FlowPath(string flowId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(flowId);
        return Uri.EscapeDataString(flowId);
    }

    private static StringContent JsonContent<T>(T value, JsonTypeInfo<T> typeInfo) =>
        new(JsonSerializer.Serialize(value, typeInfo), Encoding.UTF8, "application/json");

    private bool TryCreateWabaRequest(HttpMethod method, string path, string? wabaIdOverride, out HttpRequestMessage request, out WhatsAppError error)
    {
        try
        {
            // The low-level client falls back to WhatsAppOptions.WabaId and throws
            // InvalidOperationException when neither is set.
            request = _client.CreateWabaRequest(method, path, wabaIdOverride);
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

    private async Task<Result> SendAsync(HttpRequestMessage httpRequest, CancellationToken ct)
    {
        using var response = await _client.SendAsync(httpRequest, ct).ConfigureAwait(false);
        return response.IsSuccessStatusCode
            ? Result.Success()
            : Result.Failure(await ReadErrorAsync(response, ct).ConfigureAwait(false));
    }

    private async Task<Result<T>> SendAsync<T>(HttpRequestMessage httpRequest, JsonTypeInfo<T> typeInfo, CancellationToken ct)
    {
        using var response = await _client.SendAsync(httpRequest, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return Result<T>.Failure(await ReadErrorAsync(response, ct).ConfigureAwait(false));
        }

        var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        var value = await JsonSerializer.DeserializeAsync(stream, typeInfo, ct).ConfigureAwait(false);
        return Result<T>.Success(value!);
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
