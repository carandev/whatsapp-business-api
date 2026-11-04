using System.Text.Json;
using Carandev.WhatsApp.Sdk.Serialization;

namespace Carandev.WhatsApp.Sdk.Tests;

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

    [Fact]
    public void Deserializes_ErrorUserTitleAndErrorUserMsg_FromFullEnvelope()
    {
        const string json = """
            {
              "error": {
                "message": "Invalid parameter",
                "type": "OAuthException",
                "code": 100,
                "error_subcode": 2018001,
                "error_user_title": "Invalid Parameter",
                "error_user_msg": "The template parameter you provided is invalid.",
                "fbtrace_id": "AbCdEfGhIjK"
              }
            }
            """;

        var deserialized = JsonSerializer.Deserialize(json, WhatsAppJsonSerializerContext.Default.WhatsAppErrorResponse);

        Assert.NotNull(deserialized?.Error);
        Assert.Equal("Invalid Parameter", deserialized!.Error!.ErrorUserTitle);
        Assert.Equal("The template parameter you provided is invalid.", deserialized.Error.ErrorUserMsg);
    }

    [Theory]
    [InlineData(503, null, true)]
    [InlineData(80007, null, true)]
    [InlineData(100, 131048, true)]
    [InlineData(400, 2018001, false)]
    public void IsTransient_DelegatesToTransientErrorClassifier(int code, int? subcode, bool expectedTransient)
    {
        var error = new WhatsAppError { Code = code, ErrorSubcode = subcode };

        Assert.Equal(expectedTransient, error.IsTransient);
    }
}
