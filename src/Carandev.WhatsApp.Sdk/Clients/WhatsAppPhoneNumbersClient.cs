using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.Options;
using Carandev.WhatsApp.Sdk.Models.PhoneNumbers;
using Carandev.WhatsApp.Sdk.Models.Templates;
using Carandev.WhatsApp.Sdk.Serialization;

namespace Carandev.WhatsApp.Sdk.Clients;

/// <inheritdoc cref="IWhatsAppPhoneNumbersClient" />
public sealed class WhatsAppPhoneNumbersClient : IWhatsAppPhoneNumbersClient
{
    private readonly IWhatsAppClient _client;
    private readonly IOptions<WhatsAppOptions> _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="WhatsAppPhoneNumbersClient"/> class.
    /// </summary>
    /// <param name="client">The low-level HTTP client used to build and send requests.</param>
    /// <param name="options">
    /// The configured <see cref="WhatsAppOptions"/>, used to resolve the default WABA id when a
    /// per-call override is not supplied for <see cref="ListPhoneNumbersAsync"/>.
    /// </param>
    public WhatsAppPhoneNumbersClient(IWhatsAppClient client, IOptions<WhatsAppOptions> options)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(options);

        _client = client;
        _options = options;
    }

    /// <inheritdoc />
    public async Task<Result<TemplatesPage<PhoneNumber>>> ListPhoneNumbersAsync(string? wabaIdOverride = null, CancellationToken ct = default)
    {
        var wabaId = wabaIdOverride ?? _options.Value.WabaId;
        if (wabaId is null)
        {
            return Result<TemplatesPage<PhoneNumber>>.Failure(MissingWabaIdError());
        }

        var httpRequest = _client.CreateWabaRequest(HttpMethod.Get, "{waba_id}/phone_numbers", wabaId);

        var response = await _client.SendAsync(httpRequest, ct).ConfigureAwait(false);
        return await ReadResultAsync(response, WhatsAppJsonSerializerContext.Default.TemplatesPagePhoneNumber, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result<PhoneNumber>> GetPhoneNumberAsync(string? phoneNumberIdOverride = null, CancellationToken ct = default)
    {
        var httpRequest = _client.CreateRequest(HttpMethod.Get, "{phone_number_id}", phoneNumberIdOverride);

        var response = await _client.SendAsync(httpRequest, ct).ConfigureAwait(false);
        return await ReadResultAsync(response, WhatsAppJsonSerializerContext.Default.PhoneNumber, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result> RequestVerificationCodeAsync(VerificationCodeMethod method, string languageCode, string? phoneNumberIdOverride = null, CancellationToken ct = default)
    {
        var request = new RequestVerificationCodeRequest(method, languageCode);

        var httpRequest = _client.CreateRequest(HttpMethod.Post, "{phone_number_id}/request_code", phoneNumberIdOverride);
        httpRequest.Content = CreateJsonContent(request, WhatsAppJsonSerializerContext.Default.RequestVerificationCodeRequest);

        var response = await _client.SendAsync(httpRequest, ct).ConfigureAwait(false);
        return await ReadResultAsync(response, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result> VerifyCodeAsync(string code, string? phoneNumberIdOverride = null, CancellationToken ct = default)
    {
        var request = new VerifyCodeRequest(code);

        var httpRequest = _client.CreateRequest(HttpMethod.Post, "{phone_number_id}/verify_code", phoneNumberIdOverride);
        httpRequest.Content = CreateJsonContent(request, WhatsAppJsonSerializerContext.Default.VerifyCodeRequest);

        var response = await _client.SendAsync(httpRequest, ct).ConfigureAwait(false);
        return await ReadResultAsync(response, ct).ConfigureAwait(false);
    }

    private static HttpContent CreateJsonContent<T>(T value, JsonTypeInfo<T> typeInfo)
    {
        var json = JsonSerializer.Serialize(value, typeInfo);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    private static async Task<Result<T>> ReadResultAsync<T>(HttpResponseMessage response, JsonTypeInfo<T> typeInfo, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
        {
            var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            var value = await JsonSerializer.DeserializeAsync(stream, typeInfo, ct).ConfigureAwait(false);
            return Result<T>.Success(value!);
        }

        return Result<T>.Failure(await ReadErrorAsync(response, ct).ConfigureAwait(false));
    }

    private static async Task<Result> ReadResultAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
        {
            return Result.Success();
        }

        return Result.Failure(await ReadErrorAsync(response, ct).ConfigureAwait(false));
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

    private static WhatsAppError MissingWabaIdError() => new()
    {
        Message = "WabaId is not configured; set WhatsAppOptions.WabaId or pass wabaIdOverride.",
    };
}
