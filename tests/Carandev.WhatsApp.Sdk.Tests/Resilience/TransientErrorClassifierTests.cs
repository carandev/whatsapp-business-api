using Carandev.WhatsApp.Sdk.Resilience;

namespace Carandev.WhatsApp.Sdk.Tests.Resilience;

public class TransientErrorClassifierTests
{
    [Theory]
    [InlineData(80007, null, true, "rate-limit code is always transient")]
    [InlineData(100, 131048, true, "seeded transient subcode: spam rate limit hit")]
    [InlineData(100, 131056, true, "seeded transient subcode: phone-number pair rate limit")]
    [InlineData(100, 133016, true, "seeded transient subcode: message throughput exceeded")]
    [InlineData(190, 463, false, "expired access token is a permanent business error")]
    [InlineData(100, 2018001, false, "invalid template parameter is a permanent business error")]
    [InlineData(4, null, false, "non-seeded code without a matching subcode is not transient")]
    public void IsTransientGraphError_ClassifiesByCodeAndSubcode(int code, int? subcode, bool expected, string because)
    {
        var actual = TransientErrorClassifier.IsTransientGraphError(code, subcode);

        Assert.True(actual == expected, because);
    }

    [Theory]
    [InlineData(500)]
    [InlineData(502)]
    [InlineData(503)]
    [InlineData(599)]
    public void IsTransientGraphError_DoesNotClassifyByHttpStatusAlone(int httpStatusStoredAsCode)
    {
        // TransientErrorClassifier only knows about Graph error codes/subcodes; 5xx-as-HTTP-status
        // classification is layered on top by WhatsAppError.IsTransient (Code's dual meaning).
        Assert.False(TransientErrorClassifier.IsTransientGraphError(httpStatusStoredAsCode, subcode: null));
    }

    [Fact]
    public void TransientSubcodes_ContainsSeededValues()
    {
        Assert.Contains(131048, TransientErrorClassifier.TransientSubcodes);
        Assert.Contains(131056, TransientErrorClassifier.TransientSubcodes);
        Assert.Contains(133016, TransientErrorClassifier.TransientSubcodes);
    }
}
