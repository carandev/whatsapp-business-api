using Microsoft.AspNetCore.Mvc;

namespace WhatsAppBusinessApi.Api.ErrorMapping;

/// <summary>
/// Translates a <see cref="GraphApiException"/> into a
/// <see cref="ProblemDetails"/> response. Meta's Graph error code 131047
/// (the 24h customer-service-window rejection) is special-cased to a
/// distinct, identifiable 403 response per design.md's "24h-window
/// detection" decision; every other Graph error maps to a generic 502.
/// </summary>
public static class GraphApiErrorMapper
{
    /// <summary>
    /// Graph API error code for "Re-engagement message" — the message was
    /// sent outside Meta's 24-hour customer-service window.
    /// </summary>
    public const int CustomerServiceWindowExpiredErrorCode = 131047;

    public const string CustomerServiceWindowExpiredType =
        "https://whatsappbusinessapi.local/errors/customer-service-window-expired";

    public const string GraphApiErrorType =
        "https://whatsappbusinessapi.local/errors/graph-api-error";

    /// <summary>
    /// Maps a <see cref="GraphApiException"/> to a <see cref="ProblemDetails"/>
    /// that never leaks raw Graph payload internals to the caller.
    /// </summary>
    public static ProblemDetails Map(GraphApiException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (exception.ErrorCode == CustomerServiceWindowExpiredErrorCode)
        {
            return new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Type = CustomerServiceWindowExpiredType,
                Title = "Customer service window expired",
                Detail = "The target number is outside Meta's 24-hour customer-service window. " +
                          "A free-form text message cannot be sent; use a message template instead.",
            };
        }

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status502BadGateway,
            Type = GraphApiErrorType,
            Title = "Graph API error",
            Detail = "The WhatsApp Graph API rejected the request.",
        };

        if (exception.ErrorCode is not null)
        {
            problemDetails.Extensions["errorCode"] = exception.ErrorCode;
        }

        if (exception.ErrorType is not null)
        {
            problemDetails.Extensions["errorType"] = exception.ErrorType;
        }

        if (exception.FbTraceId is not null)
        {
            problemDetails.Extensions["fbTraceId"] = exception.FbTraceId;
        }

        return problemDetails;
    }
}
