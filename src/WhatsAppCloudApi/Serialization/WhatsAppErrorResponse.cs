using System.Text.Json.Serialization;

namespace WhatsAppCloudApi.Serialization;

/// <summary>
/// Represents the error envelope returned by the Meta Graph API on a failed request.
/// </summary>
public sealed class WhatsAppErrorResponse
{
    /// <summary>
    /// The error details, when the response represents a failure.
    /// </summary>
    [JsonPropertyName("error")]
    public WhatsAppError? Error { get; set; }
}

/// <summary>
/// Represents the <c>error</c> object within a <see cref="WhatsAppErrorResponse"/>.
/// </summary>
public sealed class WhatsAppError
{
    /// <summary>
    /// A human-readable description of the error.
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>
    /// The Graph API error type, e.g. <c>OAuthException</c>.
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// The Graph API numeric error code.
    /// </summary>
    [JsonPropertyName("code")]
    public int Code { get; set; }

    /// <summary>
    /// An optional, more specific error subcode.
    /// </summary>
    [JsonPropertyName("error_subcode")]
    public int? ErrorSubcode { get; set; }

    /// <summary>
    /// A Facebook trace identifier useful when contacting Meta support.
    /// </summary>
    [JsonPropertyName("fbtrace_id")]
    public string? FbtraceId { get; set; }
}
