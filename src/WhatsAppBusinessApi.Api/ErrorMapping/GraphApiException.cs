using System.Net;

namespace WhatsAppBusinessApi.Api.ErrorMapping;

/// <summary>
/// Represents a failure response returned by Meta's Graph API when sending a
/// WhatsApp message. Carries the HTTP status code plus Graph's own error
/// fields so <see cref="GraphApiErrorMapper"/> can translate it into a
/// <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> without leaking the
/// raw Graph payload to callers.
/// </summary>
public sealed class GraphApiException(
    HttpStatusCode statusCode,
    int? errorCode,
    string? errorType,
    string message,
    string? fbTraceId) : Exception(message)
{
    /// <summary>
    /// HTTP status code returned by the Graph API call.
    /// </summary>
    public HttpStatusCode StatusCode { get; } = statusCode;

    /// <summary>
    /// Graph API numeric error code (e.g. 131047 for the 24h customer-service
    /// window rejection). Null when Graph did not include one.
    /// </summary>
    public int? ErrorCode { get; } = errorCode;

    /// <summary>
    /// Graph API error type string (e.g. "OAuthException"). Null when Graph
    /// did not include one.
    /// </summary>
    public string? ErrorType { get; } = errorType;

    /// <summary>
    /// Facebook trace ID for correlating with Meta support, if provided.
    /// </summary>
    public string? FbTraceId { get; } = fbTraceId;
}
