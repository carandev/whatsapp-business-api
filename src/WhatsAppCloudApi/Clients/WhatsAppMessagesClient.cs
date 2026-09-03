using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using WhatsAppCloudApi.Models.Messages;
using WhatsAppCloudApi.Models.Messages.Interactive;
using WhatsAppCloudApi.Serialization;

namespace WhatsAppCloudApi.Clients;

/// <inheritdoc cref="IWhatsAppMessagesClient" />
public sealed class WhatsAppMessagesClient : IWhatsAppMessagesClient
{
    private const string MessagesPathTemplate = "{phone_number_id}/messages";

    private readonly IWhatsAppClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="WhatsAppMessagesClient"/> class.
    /// </summary>
    /// <param name="client">The low-level HTTP client used to build and send requests.</param>
    public WhatsAppMessagesClient(IWhatsAppClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
        _client = client;
    }

    /// <inheritdoc />
    public Task<Result<MessageResponse>> SendTextAsync(string to, string body, bool previewUrl = false, Context? context = null, string? phoneNumberIdOverride = null, CancellationToken ct = default)
    {
        var request = new SendTextMessageRequest(to, new TextContent(body, previewUrl), context);
        return SendAsync(request, WhatsAppJsonSerializerContext.Default.SendTextMessageRequest, phoneNumberIdOverride, ct);
    }

    /// <inheritdoc />
    public Task<Result<MessageResponse>> SendTemplateAsync(string to, string templateName, string languageCode, IReadOnlyList<TemplateParameterComponent>? components = null, Context? context = null, string? phoneNumberIdOverride = null, CancellationToken ct = default)
    {
        var template = new TemplateReference(templateName, new LanguageCode(languageCode), components);
        var request = new SendTemplateMessageRequest(to, template, context);
        return SendAsync(request, WhatsAppJsonSerializerContext.Default.SendTemplateMessageRequest, phoneNumberIdOverride, ct);
    }

    /// <inheritdoc />
    public Task<Result<MessageResponse>> SendImageAsync(string to, string? mediaId = null, string? link = null, string? caption = null, Context? context = null, string? phoneNumberIdOverride = null, CancellationToken ct = default)
    {
        var request = new SendImageMessageRequest(to, mediaId, link, caption, context);
        return SendAsync(request, WhatsAppJsonSerializerContext.Default.SendImageMessageRequest, phoneNumberIdOverride, ct);
    }

    /// <inheritdoc />
    public Task<Result<MessageResponse>> SendDocumentAsync(string to, string? mediaId = null, string? link = null, string? caption = null, Context? context = null, string? phoneNumberIdOverride = null, CancellationToken ct = default)
    {
        var request = new SendDocumentMessageRequest(to, mediaId, link, caption, context);
        return SendAsync(request, WhatsAppJsonSerializerContext.Default.SendDocumentMessageRequest, phoneNumberIdOverride, ct);
    }

    /// <inheritdoc />
    public Task<Result<MessageResponse>> SendVideoAsync(string to, string? mediaId = null, string? link = null, string? caption = null, Context? context = null, string? phoneNumberIdOverride = null, CancellationToken ct = default)
    {
        var request = new SendVideoMessageRequest(to, mediaId, link, caption, context);
        return SendAsync(request, WhatsAppJsonSerializerContext.Default.SendVideoMessageRequest, phoneNumberIdOverride, ct);
    }

    /// <inheritdoc />
    public Task<Result<MessageResponse>> SendAudioAsync(string to, string? mediaId = null, string? link = null, Context? context = null, string? phoneNumberIdOverride = null, CancellationToken ct = default)
    {
        var request = new SendAudioMessageRequest(to, mediaId, link, context);
        return SendAsync(request, WhatsAppJsonSerializerContext.Default.SendAudioMessageRequest, phoneNumberIdOverride, ct);
    }

    /// <inheritdoc />
    public Task<Result<MessageResponse>> SendStickerAsync(string to, string? mediaId = null, string? link = null, Context? context = null, string? phoneNumberIdOverride = null, CancellationToken ct = default)
    {
        var request = new SendStickerMessageRequest(to, mediaId, link, context);
        return SendAsync(request, WhatsAppJsonSerializerContext.Default.SendStickerMessageRequest, phoneNumberIdOverride, ct);
    }

    /// <inheritdoc />
    public Task<Result<MessageResponse>> SendLocationAsync(string to, double latitude, double longitude, string? name = null, string? address = null, Context? context = null, string? phoneNumberIdOverride = null, CancellationToken ct = default)
    {
        var request = new SendLocationMessageRequest(to, new Location(latitude, longitude, name, address), context);
        return SendAsync(request, WhatsAppJsonSerializerContext.Default.SendLocationMessageRequest, phoneNumberIdOverride, ct);
    }

    /// <inheritdoc />
    public Task<Result<MessageResponse>> SendContactsAsync(string to, IReadOnlyList<Contact> contacts, Context? context = null, string? phoneNumberIdOverride = null, CancellationToken ct = default)
    {
        var request = new SendContactsMessageRequest(to, contacts, context);
        return SendAsync(request, WhatsAppJsonSerializerContext.Default.SendContactsMessageRequest, phoneNumberIdOverride, ct);
    }

    /// <inheritdoc />
    public Task<Result<MessageResponse>> SendReplyButtonsAsync(string to, string bodyText, IReadOnlyList<ReplyButton> buttons, InteractiveHeader? header = null, string? footerText = null, Context? context = null, string? phoneNumberIdOverride = null, CancellationToken ct = default)
    {
        var request = new SendReplyButtonsMessageRequest(to, bodyText, buttons, header, footerText, context);
        return SendAsync(request, WhatsAppJsonSerializerContext.Default.SendReplyButtonsMessageRequest, phoneNumberIdOverride, ct);
    }

    private async Task<Result<MessageResponse>> SendAsync<TRequest>(TRequest request, JsonTypeInfo<TRequest> typeInfo, string? phoneNumberIdOverride, CancellationToken ct)
    {
        var httpRequest = _client.CreateRequest(HttpMethod.Post, MessagesPathTemplate, phoneNumberIdOverride);
        httpRequest.Content = CreateJsonContent(request, typeInfo);

        var response = await _client.SendAsync(httpRequest, ct).ConfigureAwait(false);
        return await ReadResultAsync(response, ct).ConfigureAwait(false);
    }

    private static HttpContent CreateJsonContent<T>(T value, JsonTypeInfo<T> typeInfo)
    {
        var json = JsonSerializer.Serialize(value, typeInfo);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    private static async Task<Result<MessageResponse>> ReadResultAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
        {
            var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            var envelope = await JsonSerializer.DeserializeAsync(stream, SendMessageResponseSerializerContext.Default.SendMessageResponseEnvelope, ct).ConfigureAwait(false);
            var messageId = envelope?.Messages?.Count > 0 ? envelope.Messages[0].Id : null;
            return Result<MessageResponse>.Success(new MessageResponse { MessageId = messageId });
        }

        return Result<MessageResponse>.Failure(await ReadErrorAsync(response, ct).ConfigureAwait(false));
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
