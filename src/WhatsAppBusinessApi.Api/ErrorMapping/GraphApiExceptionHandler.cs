using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace WhatsAppBusinessApi.Api.ErrorMapping;

/// <summary>
/// Central <see cref="IExceptionHandler"/> that catches <see cref="GraphApiException"/>
/// thrown anywhere in the request pipeline and writes the <see cref="ProblemDetails"/>
/// produced by <see cref="GraphApiErrorMapper"/>. Registered via
/// <c>AddExceptionHandler&lt;GraphApiExceptionHandler&gt;()</c> in Program.cs so every
/// Graph API failure — including the 24h-window case — always becomes the same
/// response shape, regardless of which endpoint triggered it.
/// </summary>
public sealed class GraphApiExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not GraphApiException graphApiException)
        {
            return false;
        }

        var problemDetails = GraphApiErrorMapper.Map(graphApiException);

        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status502BadGateway;

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
