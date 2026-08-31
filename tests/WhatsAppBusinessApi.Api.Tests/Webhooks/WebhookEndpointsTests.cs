using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace WhatsAppBusinessApi.Api.Tests.Webhooks;

/// <summary>
/// Integration coverage for <c>GET/POST /webhook</c> through the full minimal
/// API pipeline, per design.md's "Integration" testing strategy row.
/// </summary>
public class WebhookEndpointsTests
{
    private const string VerifyToken = "test-verify-token";
    private const string AppSecret = "test-app-secret";

    private static readonly Dictionary<string, string?> ValidConfig = new()
    {
        ["GraphApi:PhoneNumberId"] = "1234567890",
        ["GraphApi:AccessToken"] = "test-access-token",
        ["Webhook:VerifyToken"] = VerifyToken,
        ["Webhook:AppSecret"] = AppSecret,
    };

    [Fact]
    public async Task GetWebhook_MatchingVerifyToken_Returns200WithChallenge()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(
            $"/webhook?hub.mode=subscribe&hub.verify_token={VerifyToken}&hub.challenge=challenge-accepted");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("challenge-accepted", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task GetWebhook_MismatchingVerifyToken_Returns403()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(
            "/webhook?hub.mode=subscribe&hub.verify_token=wrong-token&hub.challenge=challenge-should-not-echo");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PostWebhook_ValidSignature_Returns200()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var body = """{"object":"whatsapp_business_account","entry":[]}""";
        using var request = BuildSignedRequest(body, AppSecret);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PostWebhook_InvalidSignature_Returns401()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var body = """{"object":"whatsapp_business_account","entry":[]}""";
        using var request = BuildSignedRequest(body, "wrong-secret");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostWebhook_MissingSignatureHeader_Returns401()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var body = """{"object":"whatsapp_business_account","entry":[]}""";
        using var request = new HttpRequestMessage(HttpMethod.Post, "/webhook")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static HttpRequestMessage BuildSignedRequest(string body, string signingSecret)
    {
        var bodyBytes = Encoding.UTF8.GetBytes(body);
        var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(signingSecret), bodyBytes);
        var signatureHeader = "sha256=" + Convert.ToHexString(hash).ToLowerInvariant();

        var request = new HttpRequestMessage(HttpMethod.Post, "/webhook")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        request.Headers.TryAddWithoutValidation("X-Hub-Signature-256", signatureHeader);

        return request;
    }

    private static WebApplicationFactory<Program> CreateFactory()
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configBuilder) =>
                {
                    configBuilder.AddInMemoryCollection(ValidConfig);
                });
            });
    }
}
