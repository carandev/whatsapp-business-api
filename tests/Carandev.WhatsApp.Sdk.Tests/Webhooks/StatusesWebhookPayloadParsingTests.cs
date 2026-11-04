using System.Text.Json;
using Carandev.WhatsApp.Sdk.Models.Webhooks;
using Carandev.WhatsApp.Sdk.Serialization;

namespace Carandev.WhatsApp.Sdk.Tests.Webhooks;

/// <summary>
/// Golden-JSON parsing tests for <see cref="WebhookStatus"/> (all 4 documented status values plus
/// the unrecognized-value fallback), its nested <see cref="WebhookConversation"/>/
/// <see cref="WebhookPricing"/>/<see cref="WebhookError"/> objects, and the envelope-reconciliation
/// behavior where a single <see cref="WebhookEnvelope"/> batch may carry messages-only,
/// statuses-only, or both across its entries. Fixtures live under <c>Webhooks/TestData/*.json</c>
/// and are copied to the test output directory.
/// </summary>
public class StatusesWebhookPayloadParsingTests
{
    [Fact]
    public void Sent_ParsesWithConversationAndPricing()
    {
        var status = Deserialize("status-sent.json");

        Assert.Equal(WebhookStatusValue.Sent, status.Status);
        Assert.Equal("16315558151", status.RecipientId);
        Assert.Equal("1603059201", status.Timestamp);
        Assert.NotNull(status.Conversation);
        Assert.Equal("CONVERSATION_ID", status.Conversation!.Id);
        Assert.Equal("business_initiated", status.Conversation.Origin?.Type);
        Assert.Equal("1603062801", status.Conversation.ExpirationTimestamp);
        Assert.NotNull(status.Pricing);
        Assert.True(status.Pricing!.Billable);
        Assert.Equal("CBP", status.Pricing.PricingModel);
        Assert.Equal("business_initiated", status.Pricing.Category);
        Assert.Null(status.Errors);
    }

    [Fact]
    public void Delivered_ParsesWithConversationAndPricing()
    {
        var status = Deserialize("status-delivered.json");

        Assert.Equal(WebhookStatusValue.Delivered, status.Status);
        Assert.NotNull(status.Conversation);
        Assert.Null(status.Conversation!.ExpirationTimestamp);
        Assert.NotNull(status.Pricing);
    }

    [Fact]
    public void Read_ParsesWithoutConversationOrPricing()
    {
        var status = Deserialize("status-read.json");

        Assert.Equal(WebhookStatusValue.Read, status.Status);
        Assert.Null(status.Conversation);
        Assert.Null(status.Pricing);
        Assert.Null(status.Errors);
    }

    [Fact]
    public void Failed_CarriesErrorDetails()
    {
        var status = Deserialize("status-failed.json");

        Assert.Equal(WebhookStatusValue.Failed, status.Status);
        Assert.NotNull(status.Errors);
        var error = Assert.Single(status.Errors!);
        Assert.Equal(131047, error.Code);
        Assert.Equal("Re-engagement message", error.Title);
        Assert.Equal("Re-engagement message", error.Message);
        Assert.Equal(
            "Message failed to send because more than 24 hours have passed since the customer last replied to this number.",
            error.ErrorData?.Details);
    }

    [Fact]
    public void UnrecognizedStatus_FallsBackToUnknown_WithoutThrowing()
    {
        var status = Deserialize("status-unrecognized.json");

        Assert.Equal(WebhookStatusValue.Unknown, status.Status);
        Assert.Equal("16315558151", status.RecipientId);
    }

    [Fact]
    public void FullEnvelope_StatusesOnly_ParsesToStatusesWebhookPayload()
    {
        var json = LoadFixture("full-envelope-statuses.json");

        var envelope = JsonSerializer.Deserialize(json, WhatsAppJsonSerializerContext.Default.WebhookEnvelope);

        Assert.NotNull(envelope);
        var entry = Assert.Single(envelope!.Entry!);
        var change = Assert.Single(entry.Changes!);
        Assert.Equal("messages", change.Field);
        Assert.False(change.Value.TryGetProperty("messages", out _));
        Assert.True(change.Value.TryGetProperty("statuses", out _));

        var payload = JsonSerializer.Deserialize(change.Value, WhatsAppJsonSerializerContext.Default.StatusesWebhookPayload);

        Assert.NotNull(payload);
        Assert.Equal("whatsapp", payload!.MessagingProduct);
        Assert.NotNull(payload.Statuses);
        var status = Assert.Single(payload.Statuses!);
        Assert.Equal(WebhookStatusValue.Delivered, status.Status);
    }

    [Fact]
    public void FullEnvelope_MessagesOnly_HasNoStatusesProperty()
    {
        var json = LoadFixture("full-envelope.json");

        var envelope = JsonSerializer.Deserialize(json, WhatsAppJsonSerializerContext.Default.WebhookEnvelope);

        Assert.NotNull(envelope);
        var change = Assert.Single(envelope!.Entry![0].Changes!);
        Assert.True(change.Value.TryGetProperty("messages", out _));
        Assert.False(change.Value.TryGetProperty("statuses", out _));
    }

    [Fact]
    public void BatchedEnvelope_MessagesAndStatusesAcrossEntries_FlattensToExpectedPerItemCounts()
    {
        var json = LoadFixture("batched-messages-and-statuses.json");

        var envelope = JsonSerializer.Deserialize(json, WhatsAppJsonSerializerContext.Default.WebhookEnvelope);

        Assert.NotNull(envelope);
        Assert.Equal(2, envelope!.Entry!.Count);

        var messageCount = 0;
        var statusCount = 0;

        foreach (var entry in envelope.Entry!)
        {
            foreach (var change in entry.Changes!)
            {
                if (change.Value.TryGetProperty("messages", out _))
                {
                    var messages = JsonSerializer.Deserialize(change.Value, WhatsAppJsonSerializerContext.Default.MessagesWebhookPayload);
                    messageCount += messages?.Messages?.Count ?? 0;
                }

                if (change.Value.TryGetProperty("statuses", out _))
                {
                    var statuses = JsonSerializer.Deserialize(change.Value, WhatsAppJsonSerializerContext.Default.StatusesWebhookPayload);
                    statusCount += statuses?.Statuses?.Count ?? 0;
                }
            }
        }

        Assert.Equal(2, messageCount);
        Assert.Equal(1, statusCount);
    }

    private static WebhookStatus Deserialize(string fixtureFileName)
    {
        var json = LoadFixture(fixtureFileName);
        var status = JsonSerializer.Deserialize(json, WhatsAppJsonSerializerContext.Default.WebhookStatus);
        Assert.NotNull(status);
        return status!;
    }

    private static string LoadFixture(string fixtureFileName) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Webhooks", "TestData", fixtureFileName));
}
