using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using WhatsAppCloudApi;
using WhatsAppCloudApi.Models.Webhooks;

namespace WhatsAppCloudApi.Extensions.AspNetCore.Tests;

public class UseWhatsAppWebhooksTests
{
    private const string Path = "/webhooks/whatsapp";
    private const string AppSecret = "test-app-secret";
    private const string VerifyToken = "test-verify-token";

    private sealed class RecordingHandlers : WhatsAppWebhookHandlers
    {
        public List<WebhookMessage> Messages { get; } = [];

        public List<WebhookStatus> Statuses { get; } = [];

        public List<Exception> Errors { get; } = [];

        public override Task OnMessageAsync(WebhookMessage message, CancellationToken cancellationToken)
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }

        public override Task OnStatusAsync(WebhookStatus status, CancellationToken cancellationToken)
        {
            Statuses.Add(status);
            return Task.CompletedTask;
        }

        public override Task OnErrorAsync(Exception exception, CancellationToken cancellationToken)
        {
            Errors.Add(exception);
            return Task.CompletedTask;
        }
    }

    private const string BatchedPayloadJson = """
    {
      "object": "whatsapp_business_account",
      "entry": [
        {
          "id": "102290129340398",
          "changes": [
            {
              "field": "messages",
              "value": {
                "messaging_product": "whatsapp",
                "metadata": { "display_phone_number": "16505551111", "phone_number_id": "123456123" },
                "messages": [
                  { "from": "16315558151", "id": "wamid.AAA", "timestamp": "1603059201", "type": "text", "text": { "body": "Hello" } },
                  { "from": "16315558151", "id": "wamid.BBB", "timestamp": "1603059205", "type": "text", "text": { "body": "Second message" } }
                ]
              }
            }
          ]
        },
        {
          "id": "102290129340398",
          "changes": [
            {
              "field": "messages",
              "value": {
                "messaging_product": "whatsapp",
                "metadata": { "display_phone_number": "16505551111", "phone_number_id": "123456123" },
                "statuses": [
                  { "id": "wamid.AAA", "status": "delivered", "timestamp": "1603059215", "recipient_id": "16315558151" }
                ]
              }
            }
          ]
        }
      ]
    }
    """;

    [Fact]
    public async Task Get_ReturnsChallengeWith200_WhenVerifyTokenMatches()
    {
        using var server = CreateServer(new RecordingHandlers());
        using var client = server.CreateClient();

        var response = await client.GetAsync($"{Path}?hub.mode=subscribe&hub.verify_token={VerifyToken}&hub.challenge=challenge-value");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("challenge-value", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Get_Returns403WithoutEcho_WhenVerifyTokenMismatches()
    {
        using var server = CreateServer(new RecordingHandlers());
        using var client = server.CreateClient();

        var response = await client.GetAsync($"{Path}?hub.mode=subscribe&hub.verify_token=wrong-token&hub.challenge=challenge-value");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.DoesNotContain("challenge-value", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Post_InvokesHandlersOncePerItemAndReturns200_ForValidSignatureAndBatchedPayload()
    {
        var handlers = new RecordingHandlers();
        using var server = CreateServer(handlers);
        using var client = server.CreateClient();

        var response = await client.SendAsync(CreateSignedPostRequest(BatchedPayloadJson, ComputeSignatureHeader(AppSecret, BatchedPayloadJson)));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, handlers.Messages.Count);
        Assert.Single(handlers.Statuses);
        Assert.Empty(handlers.Errors);
    }

    [Fact]
    public async Task Post_Returns401AndDoesNotInvokeHandlers_ForInvalidSignature()
    {
        var handlers = new RecordingHandlers();
        using var server = CreateServer(handlers);
        using var client = server.CreateClient();

        var response = await client.SendAsync(CreateSignedPostRequest(BatchedPayloadJson, "sha256=" + new string('0', 64)));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Empty(handlers.Messages);
        Assert.Empty(handlers.Statuses);
    }

    [Fact]
    public async Task Post_Returns401AndDoesNotInvokeHandlers_ForMissingSignature()
    {
        var handlers = new RecordingHandlers();
        using var server = CreateServer(handlers);
        using var client = server.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Post, Path)
        {
            Content = new StringContent(BatchedPayloadJson, Encoding.UTF8, "application/json"),
        };

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Empty(handlers.Messages);
        Assert.Empty(handlers.Statuses);
    }

    [Fact]
    public async Task Post_Returns401_WhenAppSecretIsMissing()
    {
        var handlers = new RecordingHandlers();
        using var server = CreateServer(handlers, appSecret: null);
        using var client = server.CreateClient();

        var response = await client.SendAsync(CreateSignedPostRequest(BatchedPayloadJson, ComputeSignatureHeader(AppSecret, BatchedPayloadJson)));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Empty(handlers.Messages);
    }

    [Fact]
    public async Task Post_InvokesOnErrorAsyncAndReturns200_ForMalformedJsonAfterValidSignature()
    {
        var handlers = new RecordingHandlers();
        using var server = CreateServer(handlers);
        using var client = server.CreateClient();

        const string malformedBody = "{not-valid-json";
        var response = await client.SendAsync(CreateSignedPostRequest(malformedBody, ComputeSignatureHeader(AppSecret, malformedBody)));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(handlers.Errors);
        Assert.Empty(handlers.Messages);
        Assert.Empty(handlers.Statuses);
    }

    [Fact]
    public async Task UnrelatedPath_PassesThrough_WithoutInvokingMiddleware()
    {
        var handlers = new RecordingHandlers();
        var hostBuilder = new WebHostBuilder()
            .ConfigureServices(ConfigureOptions(AppSecret, VerifyToken))
            .Configure(app =>
            {
                app.UseWhatsAppWebhooks(Path, handlers);
                app.Run(context =>
                {
                    context.Response.StatusCode = StatusCodes.Status204NoContent;
                    return Task.CompletedTask;
                });
            });

        using var server = new TestServer(hostBuilder);
        using var client = server.CreateClient();

        var response = await client.GetAsync("/unrelated-path");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(handlers.Messages);
        Assert.Empty(handlers.Statuses);
    }

    private static TestServer CreateServer(WhatsAppWebhookHandlers handlers, string? appSecret = AppSecret, string? verifyToken = VerifyToken)
    {
        var hostBuilder = new WebHostBuilder()
            .ConfigureServices(ConfigureOptions(appSecret, verifyToken))
            .Configure(app => app.UseWhatsAppWebhooks(Path, handlers));

        return new TestServer(hostBuilder);
    }

    private static Action<IServiceCollection> ConfigureOptions(string? appSecret, string? verifyToken) => services =>
        services.Configure<WhatsAppOptions>(options =>
        {
            options.AccessToken = "test-access-token";
            options.PhoneNumberId = "123456123";
            options.AppSecret = appSecret;
            options.WebhookVerifyToken = verifyToken;
        });

    private static HttpRequestMessage CreateSignedPostRequest(string rawBody, string signatureHeader)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, Path)
        {
            Content = new StringContent(rawBody, Encoding.UTF8, "application/json"),
        };
        request.Headers.Add("X-Hub-Signature-256", signatureHeader);
        return request;
    }

    private static string ComputeSignatureHeader(string secret, string rawBody)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawBody));
        return "sha256=" + Convert.ToHexString(hash).ToLowerInvariant();
    }
}
