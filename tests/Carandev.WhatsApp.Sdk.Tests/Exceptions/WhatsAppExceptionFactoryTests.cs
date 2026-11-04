using Carandev.WhatsApp.Sdk.Exceptions;
using Carandev.WhatsApp.Sdk.Serialization;

namespace Carandev.WhatsApp.Sdk.Tests.Exceptions;

public class WhatsAppExceptionFactoryTests
{
    [Theory]
    [InlineData(80007, null, typeof(WhatsAppRateLimitException), "rate-limit code maps to WhatsAppRateLimitException")]
    [InlineData(190, null, typeof(WhatsAppAuthenticationException), "invalid/expired access token code maps to WhatsAppAuthenticationException")]
    [InlineData(200, null, typeof(WhatsAppAuthenticationException), "permission error code maps to WhatsAppAuthenticationException")]
    [InlineData(132000, null, typeof(WhatsAppTemplateRejectedException), "template-pacing code maps to WhatsAppTemplateRejectedException")]
    [InlineData(132001, null, typeof(WhatsAppTemplateRejectedException), "template-does-not-exist code maps to WhatsAppTemplateRejectedException")]
    [InlineData(132005, null, typeof(WhatsAppTemplateRejectedException), "template-paused code maps to WhatsAppTemplateRejectedException")]
    [InlineData(132007, null, typeof(WhatsAppTemplateRejectedException), "template-format-mismatch code maps to WhatsAppTemplateRejectedException")]
    [InlineData(132012, null, typeof(WhatsAppTemplateRejectedException), "template-parameter-count-mismatch code maps to WhatsAppTemplateRejectedException")]
    [InlineData(132015, null, typeof(WhatsAppTemplateRejectedException), "template-disabled code maps to WhatsAppTemplateRejectedException")]
    [InlineData(132016, null, typeof(WhatsAppTemplateRejectedException), "template-param-format-mismatch code maps to WhatsAppTemplateRejectedException")]
    [InlineData(100, 2018001, typeof(WhatsAppException), "unmatched code with a subcode falls back to base WhatsAppException")]
    [InlineData(4, null, typeof(WhatsAppException), "unmatched code without a subcode falls back to base WhatsAppException")]
    public void Create_MapsCodeToExpectedExceptionType(int code, int? subcode, Type expectedType, string because)
    {
        var error = new WhatsAppError { Code = code, ErrorSubcode = subcode };

        var exception = WhatsAppExceptionFactory.Create(error);

        Assert.True(exception.GetType() == expectedType, because);
    }

    [Theory]
    [InlineData(80007, true)]
    [InlineData(190, false)]
    [InlineData(132000, false)]
    [InlineData(4, false)]
    public void Create_CopiesAllEightFieldsFromSourceError(int code, bool expectedTransient)
    {
        var error = new WhatsAppError
        {
            Message = "Something went wrong",
            Type = "OAuthException",
            Code = code,
            ErrorSubcode = 999,
            FbtraceId = "trace-123",
            ErrorUserTitle = "User Title",
            ErrorUserMsg = "User-facing message",
        };

        var exception = WhatsAppExceptionFactory.Create(error);

        Assert.Equal(error.Code, exception.Code);
        Assert.Equal(error.ErrorSubcode, exception.Subcode);
        Assert.Equal(error.Type, exception.Title);
        Assert.Equal(error.Message, exception.Message);
        Assert.Equal(error.ErrorUserTitle, exception.UserTitle);
        Assert.Equal(error.ErrorUserMsg, exception.UserMessage);
        Assert.Equal(error.FbtraceId, exception.FbTraceId);
        Assert.Equal(expectedTransient, error.IsTransient);
        Assert.Equal(expectedTransient, exception.IsTransient);
    }

    [Fact]
    public void Create_FallsBackToBaseMessage_WhenSourceMessageIsNull()
    {
        var error = new WhatsAppError { Code = 4 };

        var exception = WhatsAppExceptionFactory.Create(error);

        Assert.Equal(typeof(WhatsAppException), exception.GetType());
        Assert.False(string.IsNullOrEmpty(exception.Message));
    }

    [Fact]
    public void Create_ThrowsArgumentNullException_WhenErrorIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => WhatsAppExceptionFactory.Create(null!));
    }

    [Fact]
    public void Create_PreservesInnerException()
    {
        var error = new WhatsAppError { Code = 80007 };
        var inner = new InvalidOperationException("retry attempts exhausted");

        var exception = WhatsAppExceptionFactory.Create(error, inner);

        Assert.Same(inner, exception.InnerException);
    }
}
