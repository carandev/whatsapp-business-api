using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using WhatsAppCloudApi.Models.BusinessProfile;
using WhatsAppCloudApi.Serialization;

namespace WhatsAppCloudApi.Clients;

/// <inheritdoc cref="IWhatsAppBusinessProfileClient" />
public sealed class WhatsAppBusinessProfileClient : IWhatsAppBusinessProfileClient
{
    private const string BusinessProfileFields = "about,address,description,email,websites,vertical,profile_picture_handle";

    private readonly IWhatsAppClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="WhatsAppBusinessProfileClient"/> class.
    /// </summary>
    /// <param name="client">The low-level HTTP client used to build and send requests.</param>
    public WhatsAppBusinessProfileClient(IWhatsAppClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
        _client = client;
    }

    /// <inheritdoc />
    public async Task<Result<global::WhatsAppCloudApi.Models.BusinessProfile.BusinessProfile>> GetBusinessProfileAsync(string? phoneNumberIdOverride = null, CancellationToken ct = default)
    {
        var httpRequest = _client.CreateRequest(HttpMethod.Get, $"{{phone_number_id}}/whatsapp_business_profile?fields={BusinessProfileFields}", phoneNumberIdOverride);

        var response = await _client.SendAsync(httpRequest, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return Result<global::WhatsAppCloudApi.Models.BusinessProfile.BusinessProfile>.Failure(await ReadErrorAsync(response, ct).ConfigureAwait(false));
        }

        var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        var envelope = await JsonSerializer.DeserializeAsync(stream, WhatsAppJsonSerializerContext.Default.BusinessProfileResponse, ct).ConfigureAwait(false);
        var profile = envelope?.Data?.Count > 0 ? envelope.Data[0] : null;
        if (profile is null)
        {
            return Result<global::WhatsAppCloudApi.Models.BusinessProfile.BusinessProfile>.Failure(EmptyBusinessProfileError());
        }

        return Result<global::WhatsAppCloudApi.Models.BusinessProfile.BusinessProfile>.Success(profile);
    }

    /// <inheritdoc />
    public async Task<Result> UpdateBusinessProfileAsync(UpdateBusinessProfileRequest request, string? phoneNumberIdOverride = null, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var httpRequest = _client.CreateRequest(HttpMethod.Post, "{phone_number_id}/whatsapp_business_profile", phoneNumberIdOverride);
        httpRequest.Content = CreateJsonContent(request, WhatsAppJsonSerializerContext.Default.UpdateBusinessProfileRequest);

        var response = await _client.SendAsync(httpRequest, ct).ConfigureAwait(false);
        if (response.IsSuccessStatusCode)
        {
            return Result.Success();
        }

        return Result.Failure(await ReadErrorAsync(response, ct).ConfigureAwait(false));
    }

    private static HttpContent CreateJsonContent<T>(T value, JsonTypeInfo<T> typeInfo)
    {
        var json = JsonSerializer.Serialize(value, typeInfo);
        return new StringContent(json, Encoding.UTF8, "application/json");
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

    private static WhatsAppError EmptyBusinessProfileError() => new()
    {
        Message = "The business profile response did not contain a profile entry.",
    };
}
