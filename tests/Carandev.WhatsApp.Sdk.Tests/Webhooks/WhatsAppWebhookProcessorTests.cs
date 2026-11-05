using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Carandev.WhatsApp.Sdk.Models.Webhooks;
using Carandev.WhatsApp.Sdk.Webhooks;

namespace Carandev.WhatsApp.Sdk.Tests.Webhooks;

public class WhatsAppWebhookProcessorTests
{
    private const string AppSecret = "test-app-secret";
    private const string VerifyToken = "test-verify-token";

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

    private sealed class RecordingHandlers : WhatsAppWebhookHandlers
    {
        public List<WebhookMessage> Messages { get; } = [];

        public List<WebhookStatus> Statuses { get; } = [];

        public List<Exception> Errors { get; } = [];

        public Func<WebhookMessage, Task>? OnMessage { get; init; }

        public override async Task OnMessageAsync(WebhookMessage message, CancellationToken cancellationToken)
        {
            Messages.Add(message);
            if (OnMessage is not null)
            {
                await OnMessage(message);
            }
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

    private static WhatsAppWebhookProcessor CreateProcessor(string? appSecret = AppSecret, string? verifyToken = VerifyToken) =>
        new(Options.Create(new WhatsAppOptions
        {
            AccessToken = "test-access-token",
            PhoneNumberId = "123456123",
            AppSecret = appSecret,
            WebhookVerifyToken = verifyToken,
        }));

    private static string Sign(string secret, string rawBody)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return "sha256=" + Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(rawBody))).ToLowerInvariant();
    }

    [Fact]
    public void VerifySubscription_ReturnsChallenge_WhenVerifyTokenMatches()
    {
        var processor = CreateProcessor();

        Assert.Equal("challenge-value", processor.VerifySubscription(VerifyToken, "challenge-value"));
    }

    [Fact]
    public void VerifySubscription_ReturnsNull_WhenVerifyTokenMismatches()
    {
        var processor = CreateProcessor();

        Assert.Null(processor.VerifySubscription("wrong-token", "challenge-value"));
    }

    [Fact]
    public void VerifySubscription_ReturnsNull_WhenVerifyTokenIsNotConfigured()
    {
        var processor = CreateProcessor(verifyToken: null);

        Assert.Null(processor.VerifySubscription(string.Empty, "challenge-value"));
        Assert.Null(processor.VerifySubscription(null, "challenge-value"));
    }

    [Fact]
    public async Task ProcessAsync_InvokesHandlersOncePerItem_ForValidSignatureAndBatchedPayload()
    {
        var processor = CreateProcessor();
        var handlers = new RecordingHandlers();

        var result = await processor.ProcessAsync(BatchedPayloadJson, Sign(AppSecret, BatchedPayloadJson), handlers);

        Assert.Equal(WebhookProcessingResult.Processed, result);
        Assert.Equal(["wamid.AAA", "wamid.BBB"], handlers.Messages.Select(m => m.Id));
        Assert.Equal("wamid.AAA", Assert.Single(handlers.Statuses).Id);
        Assert.Empty(handlers.Errors);
    }

    [Fact]
    public async Task ProcessAsync_ReturnsInvalidSignatureAndDoesNotInvokeHandlers_ForInvalidSignature()
    {
        var processor = CreateProcessor();
        var handlers = new RecordingHandlers();

        var result = await processor.ProcessAsync(BatchedPayloadJson, "sha256=" + new string('0', 64), handlers);

        Assert.Equal(WebhookProcessingResult.InvalidSignature, result);
        Assert.Empty(handlers.Messages);
        Assert.Empty(handlers.Statuses);
        Assert.Empty(handlers.Errors);
    }

    [Fact]
    public async Task ProcessAsync_ReturnsInvalidSignature_ForMissingSignature()
    {
        var processor = CreateProcessor();
        var handlers = new RecordingHandlers();

        var result = await processor.ProcessAsync(BatchedPayloadJson, null, handlers);

        Assert.Equal(WebhookProcessingResult.InvalidSignature, result);
        Assert.Empty(handlers.Messages);
        Assert.Empty(handlers.Statuses);
    }

    [Fact]
    public async Task ProcessAsync_ReturnsInvalidSignature_WhenAppSecretIsNotConfigured()
    {
        var processor = CreateProcessor(appSecret: null);
        var handlers = new RecordingHandlers();

        var result = await processor.ProcessAsync(BatchedPayloadJson, Sign(AppSecret, BatchedPayloadJson), handlers);

        Assert.Equal(WebhookProcessingResult.InvalidSignature, result);
        Assert.Empty(handlers.Messages);
    }

    [Fact]
    public async Task ProcessAsync_ReturnsInvalidPayloadAndInvokesOnErrorOnce_ForMalformedJsonAfterValidSignature()
    {
        var processor = CreateProcessor();
        var handlers = new RecordingHandlers();
        const string malformedBody = "{not-valid-json";

        var result = await processor.ProcessAsync(malformedBody, Sign(AppSecret, malformedBody), handlers);

        Assert.Equal(WebhookProcessingResult.InvalidPayload, result);
        Assert.Single(handlers.Errors);
        Assert.Empty(handlers.Messages);
        Assert.Empty(handlers.Statuses);
    }

    [Fact]
    public async Task ProcessAsync_RoutesHandlerExceptionToOnError_AndKeepsDispatchingRemainingItems()
    {
        var processor = CreateProcessor();
        var failure = new InvalidOperationException("handler failed");
        var handlers = new RecordingHandlers
        {
            OnMessage = m => m.Id == "wamid.AAA" ? Task.FromException(failure) : Task.CompletedTask,
        };

        var result = await processor.ProcessAsync(BatchedPayloadJson, Sign(AppSecret, BatchedPayloadJson), handlers);

        Assert.Equal(WebhookProcessingResult.Processed, result);
        Assert.Same(failure, Assert.Single(handlers.Errors));
        Assert.Equal(2, handlers.Messages.Count);
        Assert.Single(handlers.Statuses);
    }

    private sealed class RethrowingErrorHandlers : WhatsAppWebhookHandlers
    {
        public int ErrorCalls { get; private set; }

        public Exception? Thrown { get; private set; }

        public override Task OnMessageAsync(WebhookMessage message, CancellationToken cancellationToken) =>
            message.Id == "wamid.AAA" ? throw new InvalidOperationException("handler failed") : Task.CompletedTask;

        public override Task OnErrorAsync(Exception exception, CancellationToken cancellationToken)
        {
            ErrorCalls++;
            Thrown = new ApplicationException("error handler failed", exception);
            throw Thrown;
        }
    }

    [Fact]
    public async Task ProcessAsync_PropagatesOnErrorException_WithoutRoutingItBackToOnError()
    {
        var processor = CreateProcessor();
        var handlers = new RethrowingErrorHandlers();

        var escaped = await Assert.ThrowsAsync<ApplicationException>(() =>
            processor.ProcessAsync(BatchedPayloadJson, Sign(AppSecret, BatchedPayloadJson), handlers));

        Assert.Same(handlers.Thrown, escaped);
        Assert.Equal(1, handlers.ErrorCalls);
    }

    [Fact]
    public void IsValidSignature_MatchesSignatureValidator()
    {
        var processor = CreateProcessor();

        Assert.True(processor.IsValidSignature(BatchedPayloadJson, Sign(AppSecret, BatchedPayloadJson)));
        Assert.False(processor.IsValidSignature(BatchedPayloadJson, Sign("other-secret", BatchedPayloadJson)));
    }

    [Fact]
    public async Task DispatchAsync_InvokesHandlersWithoutCheckingSignature_ForAlreadyVerifiedBody()
    {
        var processor = CreateProcessor(appSecret: null);
        var handlers = new RecordingHandlers();

        var result = await processor.DispatchAsync(BatchedPayloadJson, handlers);

        Assert.Equal(WebhookProcessingResult.Processed, result);
        Assert.Equal(2, handlers.Messages.Count);
        Assert.Single(handlers.Statuses);
    }
}
