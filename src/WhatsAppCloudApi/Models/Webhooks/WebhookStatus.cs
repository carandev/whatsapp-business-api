using System.Text.Json;
using System.Text.Json.Serialization;

namespace WhatsAppCloudApi.Models.Webhooks;

/// <summary>
/// A single delivery-status update item within <see cref="StatusesWebhookPayload.Statuses"/>,
/// reporting the lifecycle of a previously sent outbound message
/// (<c>entry[].changes[].value.statuses[]</c>).
/// </summary>
public sealed record WebhookStatus
{
    /// <summary>
    /// The Meta-assigned id of the outbound message this status update applies to.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; init; } = default!;

    /// <summary>
    /// The delivery status reported by this update.
    /// </summary>
    [JsonPropertyName("status")]
    public WebhookStatusValue Status { get; init; }

    /// <summary>
    /// The Unix timestamp (seconds, as a string) this status was reported.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public string Timestamp { get; init; } = default!;

    /// <summary>
    /// The recipient's WhatsApp id (phone number, without a leading <c>+</c>) the original message
    /// was sent to.
    /// </summary>
    [JsonPropertyName("recipient_id")]
    public string RecipientId { get; init; } = default!;

    /// <summary>
    /// Present when this status update opens or continues a billable conversation window.
    /// </summary>
    [JsonPropertyName("conversation")]
    public WebhookConversation? Conversation { get; init; }

    /// <summary>
    /// Present when this status update carries billing information for the conversation.
    /// </summary>
    [JsonPropertyName("pricing")]
    public WebhookPricing? Pricing { get; init; }

    /// <summary>
    /// Present, and non-empty, when <see cref="Status"/> is <see cref="WebhookStatusValue.Failed"/>.
    /// </summary>
    [JsonPropertyName("errors")]
    public IReadOnlyList<WebhookError>? Errors { get; init; }
}

/// <summary>
/// The lifecycle state reported by a <see cref="WebhookStatus"/>.
/// </summary>
[JsonConverter(typeof(WebhookStatusValueJsonConverter))]
public enum WebhookStatusValue
{
    /// <summary>
    /// The message was accepted by WhatsApp for delivery.
    /// </summary>
    Sent,

    /// <summary>
    /// The message was delivered to the recipient's device.
    /// </summary>
    Delivered,

    /// <summary>
    /// The recipient read the message.
    /// </summary>
    Read,

    /// <summary>
    /// The message failed to send or deliver; see <see cref="WebhookStatus.Errors"/> for details.
    /// </summary>
    Failed,

    /// <summary>
    /// A status value not in Meta's currently documented set. Forward-compatibility fallback —
    /// never thrown for.
    /// </summary>
    Unknown,
}

/// <summary>
/// Converts <see cref="WebhookStatusValue"/> to and from Meta's lower-case JSON string values
/// (<c>sent</c>, <c>delivered</c>, <c>read</c>, <c>failed</c>). Unlike most closed-vocabulary
/// converters in this codebase, an unrecognized value maps to <see cref="WebhookStatusValue.Unknown"/>
/// instead of throwing, since Meta may introduce new status codes this SDK does not yet know about.
/// </summary>
public sealed class WebhookStatusValueJsonConverter : JsonConverter<WebhookStatusValue>
{
    /// <inheritdoc />
    public override WebhookStatusValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return value switch
        {
            "sent" => WebhookStatusValue.Sent,
            "delivered" => WebhookStatusValue.Delivered,
            "read" => WebhookStatusValue.Read,
            "failed" => WebhookStatusValue.Failed,
            _ => WebhookStatusValue.Unknown,
        };
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, WebhookStatusValue value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);

        writer.WriteStringValue(value switch
        {
            WebhookStatusValue.Sent => "sent",
            WebhookStatusValue.Delivered => "delivered",
            WebhookStatusValue.Read => "read",
            WebhookStatusValue.Failed => "failed",
            _ => "unknown",
        });
    }
}

/// <summary>
/// The conversation window a <see cref="WebhookStatus"/> update belongs to.
/// </summary>
public sealed record WebhookConversation
{
    /// <summary>
    /// The conversation's unique id.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; init; } = default!;

    /// <summary>
    /// What opened this conversation window.
    /// </summary>
    [JsonPropertyName("origin")]
    public WebhookConversationOrigin? Origin { get; init; }

    /// <summary>
    /// The Unix timestamp (seconds, as a string) this conversation window expires. Only present on
    /// the status update for the message that opened the conversation.
    /// </summary>
    [JsonPropertyName("expiration_timestamp")]
    public string? ExpirationTimestamp { get; init; }
}

/// <summary>
/// Identifies what opened a <see cref="WebhookConversation"/>.
/// </summary>
public sealed record WebhookConversationOrigin
{
    /// <summary>
    /// The conversation category, e.g. <c>business_initiated</c>, <c>customer_initiated</c>, or
    /// <c>referral_conversion</c>. Left as a raw string since Meta documents this as an evolving,
    /// non-exhaustive set.
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; init; }
}

/// <summary>
/// Billing information carried by a <see cref="WebhookStatus"/> update.
/// </summary>
public sealed record WebhookPricing
{
    /// <summary>
    /// <see langword="true"/> when this conversation is billable.
    /// </summary>
    [JsonPropertyName("billable")]
    public bool? Billable { get; init; }

    /// <summary>
    /// The pricing model applied, e.g. <c>CBP</c>.
    /// </summary>
    [JsonPropertyName("pricing_model")]
    public string? PricingModel { get; init; }

    /// <summary>
    /// The conversation pricing category, e.g. <c>business_initiated</c>, <c>utility</c>,
    /// <c>marketing</c>, <c>authentication</c>, <c>service</c>. Left as a raw string since Meta
    /// documents this as an evolving, non-exhaustive set.
    /// </summary>
    [JsonPropertyName("category")]
    public string? Category { get; init; }
}

/// <summary>
/// A single error reported by a failed <see cref="WebhookStatus"/> update.
/// </summary>
public sealed record WebhookError
{
    /// <summary>
    /// The Graph API numeric error code.
    /// </summary>
    [JsonPropertyName("code")]
    public int Code { get; init; }

    /// <summary>
    /// A short, human-readable error title.
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; init; }

    /// <summary>
    /// A human-readable error description.
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; init; }

    /// <summary>
    /// Additional structured error detail, when Meta provides it.
    /// </summary>
    [JsonPropertyName("error_data")]
    public WebhookErrorData? ErrorData { get; init; }
}

/// <summary>
/// Additional structured detail carried by a <see cref="WebhookError"/>.
/// </summary>
public sealed record WebhookErrorData
{
    /// <summary>
    /// A human-readable explanation of the error.
    /// </summary>
    [JsonPropertyName("details")]
    public string? Details { get; init; }
}
