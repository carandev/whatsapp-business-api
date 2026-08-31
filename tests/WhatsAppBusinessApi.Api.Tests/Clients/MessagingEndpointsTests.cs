using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WhatsAppBusinessApi.Api.Clients;

namespace WhatsAppBusinessApi.Api.Tests.Clients;

/// <summary>
/// Integration coverage for <c>POST /messages</c> through the full minimal
/// API pipeline, per design.md's "Integration" testing strategy row. A fake
/// primary <see cref="DelegatingHandler"/> is registered on the typed
/// <see cref="IWhatsAppClient"/> so no real Graph API call is made; failure
/// mapping is exercised end-to-end through <c>GraphApiExceptionHandler</c>.
/// </summary>
public class MessagingEndpointsTests
{
    private static readonly Dictionary<string, string?> ValidConfig = new()
    {
        ["GraphApi:PhoneNumberId"] = "1234567890",
        ["GraphApi:AccessToken"] = "test-access-token",
        ["Webhook:VerifyToken"] = "test-verify-token",
        ["Webhook:AppSecret"] = "test-app-secret",
    };

    [Fact]
    public async Task PostMessages_SuccessfulGraphResponse_Returns202WithMessageId()
    {
        using var factory = CreateFactory(new StubGraphHandler(_ =>
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    messaging_product = "whatsapp",
                    messages = new[] { new { id = "wamid.TEST123" } },
                }),
            };
        }));

        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/messages", new { to = "15551234567", body = "Hello" });

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<SendMessageResult>();
        Assert.Equal("wamid.TEST123", payload!.MessageId);
    }

    [Fact]
    public async Task PostMessages_WindowExpiredGraphError_ReturnsProblemDetails403()
    {
        using var factory = CreateFactory(new StubGraphHandler(_ =>
        {
            return new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = JsonContent.Create(new
                {
                    error = new
                    {
                        message = "Re-engagement message",
                        type = "OAuthException",
                        code = 131047,
                        fbtrace_id = "trace-window",
                    },
                }),
            };
        }));

        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/messages", new { to = "15551234567", body = "Hello" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static WebApplicationFactory<Program> CreateFactory(DelegatingHandler fakeHandler)
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configBuilder) =>
                {
                    configBuilder.AddInMemoryCollection(ValidConfig);
                });

                builder.ConfigureServices(services =>
                {
                    services.AddHttpClient<IWhatsAppClient, WhatsAppClient>()
                        .ConfigurePrimaryHttpMessageHandler(() => fakeHandler);
                });
            });
    }

    private sealed class StubGraphHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(responder(request));
        }
    }
}
