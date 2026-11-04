using WhatsAppCloudApi.Serialization;

namespace WhatsAppCloudApi.Tests;

public class ResultTests
{
    private static WhatsAppError CreateError() => new()
    {
        Message = "Something went wrong",
        Type = "OAuthException",
        Code = 190,
        ErrorSubcode = 460,
        FbtraceId = "trace-id",
    };

    [Fact]
    public void Success_ReturnsIsSuccessTrue_WithNoError()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Failure_ReturnsIsSuccessFalse_WithError()
    {
        var error = CreateError();

        var result = Result.Failure(error);

        Assert.False(result.IsSuccess);
        Assert.Same(error, result.Error);
    }

    [Fact]
    public void Failure_ThrowsArgumentNullException_WhenErrorIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => Result.Failure(null!));
    }

    [Fact]
    public void DefaultState_IsNotAValidSuccess()
    {
        var result = default(Result);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Error);
    }

    [Fact]
    public void GenericSuccess_ReturnsIsSuccessTrue_WithValue_AndNoError()
    {
        var result = Result<string>.Success("template-id-123");

        Assert.True(result.IsSuccess);
        Assert.Equal("template-id-123", result.Value);
        Assert.Null(result.Error);
    }

    [Fact]
    public void GenericFailure_ReturnsIsSuccessFalse_WithError_AndDefaultValue()
    {
        var error = CreateError();

        var result = Result<string>.Failure(error);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);
        Assert.Same(error, result.Error);
    }

    [Fact]
    public void GenericFailure_ThrowsArgumentNullException_WhenErrorIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => Result<string>.Failure(null!));
    }

    [Fact]
    public void GenericDefaultState_IsNotAValidSuccess()
    {
        var result = default(Result<string>);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);
        Assert.Null(result.Error);
    }
}
