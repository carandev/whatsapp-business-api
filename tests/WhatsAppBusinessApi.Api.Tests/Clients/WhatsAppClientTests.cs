using System.Net;
using System.Text;
using Moq;
using Moq.Protected;
using WhatsAppBusinessApi.Api.Clients;
using WhatsAppBusinessApi.Api.ErrorMapping;

namespace WhatsAppBusinessApi.Api.Tests.Clients;

/// <summary>
/// Unit coverage for <see cref="WhatsAppClient"/>: success and failure Graph
/// API responses, per design.md's "Unit — client" testing strategy row.
/// The underlying <see cref="HttpMessageHandler"/> is mocked via
/// <c>Moq.Protected()</c> so no real network call is made.
/// </summary>
public class WhatsAppClientTests
{
    private static WhatsAppClient CreateClient(HttpResponseMessage response)
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://graph.facebook.com/v21.0/"),
        };

        return new WhatsAppClient(httpClient);
    }

    private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string json) =>
        new(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };

    [Fact]
    public async Task SendTextMessageAsync_SuccessResponse_ReturnsGraphMessageId()
    {
        var responseBody = """
            {
              "messaging_product": "whatsapp",
              "contacts": [{"input":"15551234567","wa_id":"15551234567"}],
              "messages": [{"id":"wamid.TEST123"}]
            }
            """;

        var client = CreateClient(JsonResponse(HttpStatusCode.OK, responseBody));

        var result = await client.SendTextMessageAsync("15551234567", "Hello", CancellationToken.None);

        Assert.Equal("wamid.TEST123", result.MessageId);
    }

    [Fact]
    public async Task SendTextMessageAsync_GenericGraphError_ThrowsGraphApiExceptionWithCorrectFields()
    {
        var responseBody = """
            {
              "error": {
                "message": "Invalid parameter",
                "type": "OAuthException",
                "code": 100,
                "fbtrace_id": "trace-abc"
              }
            }
            """;

        var client = CreateClient(JsonResponse(HttpStatusCode.BadRequest, responseBody));

        var exception = await Assert.ThrowsAsync<GraphApiException>(
            () => client.SendTextMessageAsync("15551234567", "Hello", CancellationToken.None));

        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        Assert.Equal(100, exception.ErrorCode);
        Assert.Equal("OAuthException", exception.ErrorType);
        Assert.Equal("Invalid parameter", exception.Message);
        Assert.Equal("trace-abc", exception.FbTraceId);
    }

    [Fact]
    public async Task SendTextMessageAsync_WindowExpiredGraphError_ThrowsGraphApiExceptionWithErrorCode131047()
    {
        var responseBody = """
            {
              "error": {
                "message": "Re-engagement message",
                "type": "OAuthException",
                "code": 131047,
                "fbtrace_id": "trace-window"
              }
            }
            """;

        var client = CreateClient(JsonResponse(HttpStatusCode.BadRequest, responseBody));

        var exception = await Assert.ThrowsAsync<GraphApiException>(
            () => client.SendTextMessageAsync("15551234567", "Hello", CancellationToken.None));

        Assert.Equal(131047, exception.ErrorCode);
        Assert.Equal("OAuthException", exception.ErrorType);
        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        Assert.Equal("trace-window", exception.FbTraceId);
    }

    [Fact]
    public async Task SendTextMessageAsync_NonJsonErrorBody_ThrowsGraphApiExceptionWithGenericMessage()
    {
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("<html>upstream error</html>", Encoding.UTF8, "text/html"),
        };

        var client = CreateClient(response);

        var exception = await Assert.ThrowsAsync<GraphApiException>(
            () => client.SendTextMessageAsync("15551234567", "Hello", CancellationToken.None));

        Assert.Equal(HttpStatusCode.InternalServerError, exception.StatusCode);
        Assert.Null(exception.ErrorCode);
        Assert.Contains("500", exception.Message);
    }
}
