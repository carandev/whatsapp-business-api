using System.Text.Json.Serialization;
using Carandev.WhatsApp.Sdk.Resilience;

namespace Carandev.WhatsApp.Sdk.Serialization;

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

    /// <summary>
    /// A short, user-facing error title suitable for display, when provided by Meta.
    /// </summary>
    [JsonPropertyName("error_user_title")]
    public string? ErrorUserTitle { get; set; }

    /// <summary>
    /// A user-facing error message suitable for display, when provided by Meta.
    /// </summary>
    [JsonPropertyName("error_user_msg")]
    public string? ErrorUserMsg { get; set; }

    /// <summary>
    /// Whether this error is considered transient (safe to retry) based on <see cref="Code"/> and
    /// <see cref="ErrorSubcode"/>. Reuses <see cref="Code"/>'s dual Graph-error/HTTP-status meaning:
    /// callers that only have a raw HTTP status (no structured JSON body) store it in <see cref="Code"/>.
    /// </summary>
    [JsonIgnore]
    public bool IsTransient =>
        TransientErrorClassifier.IsTransientGraphError(Code, ErrorSubcode) || Code is >= 500 and <= 599;
}
