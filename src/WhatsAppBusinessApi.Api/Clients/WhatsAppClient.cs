using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using WhatsAppBusinessApi.Api.ErrorMapping;

namespace WhatsAppBusinessApi.Api.Clients;

/// <summary>
/// Typed <see cref="HttpClient"/> implementation of <see cref="IWhatsAppClient"/>.
/// Registered via <c>AddHttpClient&lt;IWhatsAppClient, WhatsAppClient&gt;()</c> in
/// Program.cs, which configures <see cref="HttpClient.BaseAddress"/> and the
/// bearer authorization header from <see cref="Configuration.GraphApiOptions"/>.
/// Non-2xx Graph API responses are translated into <see cref="GraphApiException"/>
/// so the caller never needs to parse the raw Graph error payload itself.
/// </summary>
public sealed class WhatsAppClient(HttpClient httpClient) : IWhatsAppClient
{
    public async Task<SendMessageResult> SendTextMessageAsync(string to, string body, CancellationToken ct)
    {
        var requestBody = new GraphSendMessageRequest(
            MessagingProduct: "whatsapp",
            To: to,
            Type: "text",
            Text: new GraphSendMessageText(body));

        using var response = await httpClient.PostAsJsonAsync("messages", requestBody, ct).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw await BuildGraphApiExceptionAsync(response, ct).ConfigureAwait(false);
        }

        var result = await response.Content
            .ReadFromJsonAsync<GraphSendMessageResponse>(cancellationToken: ct)
            .ConfigureAwait(false);

        var messageId = result?.Messages?.FirstOrDefault()?.Id;

        if (messageId is null)
        {
            throw new GraphApiException(
                response.StatusCode,
                errorCode: null,
                errorType: null,
                message: "Graph API returned a success response without a message ID.",
                fbTraceId: null);
        }

        return new SendMessageResult(messageId);
    }

    private static async Task<GraphApiException> BuildGraphApiExceptionAsync(
        HttpResponseMessage response, CancellationToken ct)
    {
        GraphErrorResponse? errorResponse = null;

        try
        {
            errorResponse = await response.Content
                .ReadFromJsonAsync<GraphErrorResponse>(cancellationToken: ct)
                .ConfigureAwait(false);
        }
        catch (JsonException)
        {
            // Graph API returned a non-JSON or malformed error body; fall
            // through with a generic message rather than propagating the
            // parse failure.
        }

        var error = errorResponse?.Error;

        return new GraphApiException(
            response.StatusCode,
            errorCode: error?.Code,
            errorType: error?.Type,
            message: error?.Message ?? $"Graph API request failed with status {(int)response.StatusCode}.",
            fbTraceId: error?.FbTraceId);
    }

    private sealed record GraphSendMessageRequest(
        [property: JsonPropertyName("messaging_product")] string MessagingProduct,
        [property: JsonPropertyName("to")] string To,
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("text")] GraphSendMessageText Text);

    private sealed record GraphSendMessageText(
        [property: JsonPropertyName("body")] string Body);

    private sealed record GraphSendMessageResponse(
        [property: JsonPropertyName("messages")] IReadOnlyList<GraphSendMessageResponseMessage>? Messages);

    private sealed record GraphSendMessageResponseMessage(
        [property: JsonPropertyName("id")] string Id);

    private sealed record GraphErrorResponse(
        [property: JsonPropertyName("error")] GraphErrorDetail? Error);

    private sealed record GraphErrorDetail(
        [property: JsonPropertyName("message")] string? Message,
        [property: JsonPropertyName("type")] string? Type,
        [property: JsonPropertyName("code")] int? Code,
        [property: JsonPropertyName("fbtrace_id")] string? FbTraceId);
}
