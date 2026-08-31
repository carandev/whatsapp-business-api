using System.Text.Json;
using WhatsAppBusinessApi.Api.Webhooks;

namespace WhatsAppBusinessApi.Api.Tests.Webhooks;

/// <summary>
/// Unit coverage for deserializing <see cref="WhatsAppWebhookPayload"/> fixture
/// JSON and reporting the correct message type via
/// <see cref="WebhookEndpoints.DescribeReportedMessageTypes"/>, per design.md's
/// "Unit — webhook parsing" testing strategy row (text/image/status → correct
/// reported message type).
/// </summary>
public class WebhookPayloadParsingTests
{
    [Fact]
    public void DescribeReportedMessageTypes_TextMessagePayload_ReportsText()
    {
        var payload = Deserialize(TextMessageFixture);

        var reportedTypes = WebhookEndpoints.DescribeReportedMessageTypes(payload).ToList();

        Assert.Equal(["text"], reportedTypes);
    }

    [Fact]
    public void DescribeReportedMessageTypes_ImageMessagePayload_ReportsImage()
    {
        var payload = Deserialize(ImageMessageFixture);

        var reportedTypes = WebhookEndpoints.DescribeReportedMessageTypes(payload).ToList();

        Assert.Equal(["image"], reportedTypes);
    }

    [Fact]
    public void DescribeReportedMessageTypes_StatusUpdatePayload_ReportsStatus()
    {
        var payload = Deserialize(StatusUpdateFixture);

        var reportedTypes = WebhookEndpoints.DescribeReportedMessageTypes(payload).ToList();

        Assert.Equal(["status"], reportedTypes);
    }

    private static WhatsAppWebhookPayload Deserialize(string json)
    {
        var payload = JsonSerializer.Deserialize<WhatsAppWebhookPayload>(json);
        Assert.NotNull(payload);
        return payload!;
    }

    private const string TextMessageFixture = """
        {
          "object": "whatsapp_business_account",
          "entry": [
            {
              "id": "WHATSAPP_BUSINESS_ACCOUNT_ID",
              "changes": [
                {
                  "field": "messages",
                  "value": {
                    "messaging_product": "whatsapp",
                    "metadata": {
                      "display_phone_number": "16505551111",
                      "phone_number_id": "123456123"
                    },
                    "messages": [
                      {
                        "from": "16315551234",
                        "id": "wamid.TEXT123",
                        "timestamp": "1603059201",
                        "type": "text",
                        "text": { "body": "Hello there" }
                      }
                    ]
                  }
                }
              ]
            }
          ]
        }
        """;

    private const string ImageMessageFixture = """
        {
          "object": "whatsapp_business_account",
          "entry": [
            {
              "id": "WHATSAPP_BUSINESS_ACCOUNT_ID",
              "changes": [
                {
                  "field": "messages",
                  "value": {
                    "messaging_product": "whatsapp",
                    "metadata": {
                      "display_phone_number": "16505551111",
                      "phone_number_id": "123456123"
                    },
                    "messages": [
                      {
                        "from": "16315551234",
                        "id": "wamid.IMAGE123",
                        "timestamp": "1603059201",
                        "type": "image",
                        "image": {
                          "id": "media-id-123",
                          "mime_type": "image/jpeg",
                          "sha256": "abc123",
                          "caption": "Look at this"
                        }
                      }
                    ]
                  }
                }
              ]
            }
          ]
        }
        """;

    private const string StatusUpdateFixture = """
        {
          "object": "whatsapp_business_account",
          "entry": [
            {
              "id": "WHATSAPP_BUSINESS_ACCOUNT_ID",
              "changes": [
                {
                  "field": "messages",
                  "value": {
                    "messaging_product": "whatsapp",
                    "metadata": {
                      "display_phone_number": "16505551111",
                      "phone_number_id": "123456123"
                    },
                    "statuses": [
                      {
                        "id": "wamid.TEXT123",
                        "status": "delivered",
                        "timestamp": "1603059202",
                        "recipient_id": "16315551234"
                      }
                    ]
                  }
                }
              ]
            }
          ]
        }
        """;
}
