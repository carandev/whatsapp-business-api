using System.Net;
using System.Text;
using Carandev.WhatsApp.Sdk.Exceptions;
using Carandev.WhatsApp.Sdk.DependencyInjection.Resilience;

namespace Carandev.WhatsApp.Sdk.Tests.Resilience;

/// <summary>
/// Unit tests for <see cref="WhatsAppRetryExhaustionHandler"/> in isolation — a fake inner
/// <see cref="DelegatingHandler"/> stands in for "the final outcome after the resilience pipeline
/// gave up", with no real Polly pipeline involved.
/// </summary>
public class WhatsAppRetryExhaustionHandlerTests
{
    private static HttpMessageInvoker CreateInvoker(Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        var handler = new WhatsAppRetryExhaustionHandler
        {
            InnerHandler = new StubInnerHandler(respond),
        };

        return new HttpMessageInvoker(handler);
    }

    [Fact]
    public async Task SuccessResponse_PassesThroughUnchanged()
    {
        using var invoker = CreateInvoker(_ => new HttpResponseMessage(HttpStatusCode.OK));

        var response = await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://example.test/x"), CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PermanentNonTransient4xx_PassesThroughUnchanged_BodyStillReadableDownstream()
    {
        const string body = """{"error":{"message":"Invalid parameter","type":"OAuthException","code":100,"error_subcode":2018001}}""";
        using var invoker = CreateInvoker(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        });

        var response = await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://example.test/x"), CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var readBody = await response.Content.ReadAsStringAsync();
        Assert.Equal(body, readBody);
    }

    [Fact]
    public async Task Exhausted5xxWithNoParsableBody_Throws_BaseWhatsAppException()
    {
        using var invoker = CreateInvoker(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        var exception = await Assert.ThrowsAsync<WhatsAppException>(() =>
            invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://example.test/x"), CancellationToken.None));

        Assert.IsType<WhatsAppException>(exception);
        Assert.Equal((int)HttpStatusCode.ServiceUnavailable, exception.Code);
        Assert.True(exception.IsTransient);
    }

    [Fact]
    public async Task ExhaustedRateLimitCode_Throws_WhatsAppRateLimitException_WithFullContext()
    {
        const string body = """{"message":"Too many requests","type":"OAuthException","code":80007,"error_subcode":null,"fbtrace_id":"trace-123","error_user_title":"Rate limited","error_user_msg":"Please slow down"}""";
        var wrapped = $$"""{"error":{{body}}}""";
        using var invoker = CreateInvoker(_ => new HttpResponseMessage(HttpStatusCode.TooManyRequests)
        {
            Content = new StringContent(wrapped, Encoding.UTF8, "application/json"),
        });

        var exception = await Assert.ThrowsAsync<WhatsAppRateLimitException>(() =>
            invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://example.test/x"), CancellationToken.None));

        Assert.Equal(80007, exception.Code);
        Assert.Equal("trace-123", exception.FbTraceId);
        Assert.Equal("Rate limited", exception.UserTitle);
        Assert.Equal("Please slow down", exception.UserMessage);
        Assert.True(exception.IsTransient);
    }

    [Fact]
    public async Task ExhaustedTransientSubcodeIn4xxBody_Throws_MatchingException()
    {
        const string wrapped = """{"error":{"message":"Rate limited","type":"OAuthException","code":100,"error_subcode":131048}}""";
        using var invoker = CreateInvoker(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(wrapped, Encoding.UTF8, "application/json"),
        });

        var exception = await Assert.ThrowsAsync<WhatsAppException>(() =>
            invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://example.test/x"), CancellationToken.None));

        Assert.Equal(100, exception.Code);
        Assert.Equal(131048, exception.Subcode);
        Assert.True(exception.IsTransient);
    }

    private sealed class StubInnerHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(respond(request));
        }
    }
}
