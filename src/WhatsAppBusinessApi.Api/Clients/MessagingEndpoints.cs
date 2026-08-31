namespace WhatsAppBusinessApi.Api.Clients;

/// <summary>
/// Maps the outbound messaging endpoint. Failure mapping (Graph API errors
/// to <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/>) is handled
/// centrally by <see cref="WhatsAppBusinessApi.Api.ErrorMapping.GraphApiExceptionHandler"/>,
/// so this endpoint only needs to handle the success path.
/// </summary>
public static class MessagingEndpoints
{
    public static IEndpointRouteBuilder MapMessagingEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/messages", async (
            SendMessageRequest request,
            IWhatsAppClient client,
            CancellationToken ct) =>
        {
            var result = await client.SendTextMessageAsync(request.To, request.Body, ct);
            return Results.Accepted(uri: (string?)null, value: result);
        });

        return app;
    }
}

/// <summary>
/// Request body for <c>POST /messages</c>: target WhatsApp number and free-form text body.
/// </summary>
public sealed record SendMessageRequest(string To, string Body);
