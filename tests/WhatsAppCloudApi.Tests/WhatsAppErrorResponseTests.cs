using System.Text.Json;
using WhatsAppCloudApi.Serialization;

namespace WhatsAppCloudApi.Tests;

public class WhatsAppErrorResponseTests
{
    [Fact]
    public void RoundTrips_ViaSourceGeneratedContext()
    {
        const string json = """
            {
              "error": {
                "message": "Invalid OAuth access token",
                "type": "OAuthException",
                "code": 190,
                "error_subcode": 463,
                "fbtrace_id": "AbCdEfGhIjK"
              }
            }
            """;

        var deserialized = JsonSerializer.Deserialize(json, WhatsAppJsonSerializerContext.Default.WhatsAppErrorResponse);

        Assert.NotNull(deserialized);
        Assert.NotNull(deserialized!.Error);
        Assert.Equal("Invalid OAuth access token", deserialized.Error!.Message);
        Assert.Equal("OAuthException", deserialized.Error.Type);
        Assert.Equal(190, deserialized.Error.Code);
        Assert.Equal(463, deserialized.Error.ErrorSubcode);
        Assert.Equal("AbCdEfGhIjK", deserialized.Error.FbtraceId);

        var serialized = JsonSerializer.Serialize(deserialized, WhatsAppJsonSerializerContext.Default.WhatsAppErrorResponse);
        var roundTripped = JsonSerializer.Deserialize(serialized, WhatsAppJsonSerializerContext.Default.WhatsAppErrorResponse);

        Assert.Equal(deserialized.Error.Message, roundTripped!.Error!.Message);
        Assert.Equal(deserialized.Error.Code, roundTripped.Error.Code);
    }
}
