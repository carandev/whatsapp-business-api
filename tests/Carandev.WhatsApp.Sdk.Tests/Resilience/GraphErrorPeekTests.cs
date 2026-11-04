using System.Net;
using System.Text;
using Carandev.WhatsApp.Sdk.Resilience;

namespace Carandev.WhatsApp.Sdk.Tests.Resilience;

public class GraphErrorPeekTests
{
    [Fact]
    public async Task TryReadAsync_ValidErrorEnvelope_ReturnsParsedError()
    {
        const string json = """
            {
              "error": {
                "message": "Too Many Requests",
                "type": "OAuthException",
                "code": 80007,
                "error_subcode": null,
                "fbtrace_id": "AbCdEfGhIjK"
              }
            }
            """;
        using var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };

        var error = await GraphErrorPeek.TryReadAsync(response);

        Assert.NotNull(error);
        Assert.Equal(80007, error!.Code);
        Assert.Equal("Too Many Requests", error.Message);
    }

    [Fact]
    public async Task TryReadAsync_DoesNotConsumeBody_DownstreamReadStillReturnsFullContent()
    {
        const string json = """
            {
              "error": {
                "message": "Invalid parameter",
                "type": "OAuthException",
                "code": 100,
                "error_subcode": 2018001
              }
            }
            """;
        using var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };

        await GraphErrorPeek.TryReadAsync(response);

        var downstreamBody = await response.Content.ReadAsStringAsync();
        Assert.Equal(json, downstreamBody);
    }

    [Fact]
    public async Task TryReadAsync_PreservesContentTypeHeaderOnReplacementContent()
    {
        const string json = """{"error":{"message":"m","type":"t","code":1}}""";
        using var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };

        await GraphErrorPeek.TryReadAsync(response);

        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task TryReadAsync_MalformedJson_ReturnsNullWithoutThrowing()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("{not valid json", Encoding.UTF8, "application/json"),
        };

        var error = await GraphErrorPeek.TryReadAsync(response);

        Assert.Null(error);
    }

    [Fact]
    public async Task TryReadAsync_NonJsonBody_ReturnsNullWithoutThrowing()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.BadGateway)
        {
            Content = new StringContent("<html><body>Bad Gateway</body></html>", Encoding.UTF8, "text/html"),
        };

        var error = await GraphErrorPeek.TryReadAsync(response);

        Assert.Null(error);
    }

    [Fact]
    public async Task TryReadAsync_MalformedBody_StillReadableDownstreamAfterPeek()
    {
        const string body = "not json at all";
        using var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent(body, Encoding.UTF8, "text/plain"),
        };

        var error = await GraphErrorPeek.TryReadAsync(response);
        var downstreamBody = await response.Content.ReadAsStringAsync();

        Assert.Null(error);
        Assert.Equal(body, downstreamBody);
    }

    [Fact]
    public async Task TryReadAsync_EmptyBody_ReturnsNullWithoutThrowing()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.NoContent)
        {
            Content = new ByteArrayContent([]),
        };

        var error = await GraphErrorPeek.TryReadAsync(response);

        Assert.Null(error);
    }

    [Fact]
    public async Task TryReadAsync_ValidJsonButMissingErrorObject_ReturnsNull()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"messages":[{"id":"wamid.123"}]}""", Encoding.UTF8, "application/json"),
        };

        var error = await GraphErrorPeek.TryReadAsync(response);

        Assert.Null(error);
    }

    [Fact]
    public async Task TryReadAsync_NullResponse_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => GraphErrorPeek.TryReadAsync(null!));
    }
}
