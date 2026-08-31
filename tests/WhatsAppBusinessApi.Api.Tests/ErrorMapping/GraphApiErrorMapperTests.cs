using System.Net;
using Microsoft.AspNetCore.Http;
using WhatsAppBusinessApi.Api.ErrorMapping;

namespace WhatsAppBusinessApi.Api.Tests.ErrorMapping;

public class GraphApiErrorMapperTests
{
    public static TheoryData<GraphApiException, int, string> MappingCases()
    {
        return new TheoryData<GraphApiException, int, string>
        {
            {
                new GraphApiException(
                    HttpStatusCode.BadRequest,
                    errorCode: 131047,
                    errorType: "OAuthException",
                    message: "Re-engagement message",
                    fbTraceId: "trace-window"),
                StatusCodes.Status403Forbidden,
                GraphApiErrorMapper.CustomerServiceWindowExpiredType
            },
            {
                new GraphApiException(
                    HttpStatusCode.BadRequest,
                    errorCode: 100,
                    errorType: "OAuthException",
                    message: "Invalid recipient",
                    fbTraceId: "trace-invalid"),
                StatusCodes.Status502BadGateway,
                GraphApiErrorMapper.GraphApiErrorType
            },
            {
                new GraphApiException(
                    HttpStatusCode.InternalServerError,
                    errorCode: null,
                    errorType: null,
                    message: "Unknown Graph failure",
                    fbTraceId: null),
                StatusCodes.Status502BadGateway,
                GraphApiErrorMapper.GraphApiErrorType
            },
        };
    }

    [Theory]
    [MemberData(nameof(MappingCases))]
    public void Map_GivenGraphApiException_ReturnsExpectedStatusAndType(
        GraphApiException exception, int expectedStatus, string expectedType)
    {
        var problemDetails = GraphApiErrorMapper.Map(exception);

        Assert.Equal(expectedStatus, problemDetails.Status);
        Assert.Equal(expectedType, problemDetails.Type);
    }

    [Fact]
    public void Map_WindowExpiredErrorCode_DoesNotExposeGraphExtensions()
    {
        var exception = new GraphApiException(
            HttpStatusCode.BadRequest,
            errorCode: 131047,
            errorType: "OAuthException",
            message: "Re-engagement message",
            fbTraceId: "trace-window");

        var problemDetails = GraphApiErrorMapper.Map(exception);

        Assert.Equal(403, problemDetails.Status);
        Assert.Equal(GraphApiErrorMapper.CustomerServiceWindowExpiredType, problemDetails.Type);
        Assert.Empty(problemDetails.Extensions);
    }

    [Fact]
    public void Map_GenericGraphError_IncludesErrorCodeErrorTypeAndFbTraceIdAsExtensions()
    {
        var exception = new GraphApiException(
            HttpStatusCode.BadRequest,
            errorCode: 100,
            errorType: "OAuthException",
            message: "Invalid recipient",
            fbTraceId: "trace-invalid");

        var problemDetails = GraphApiErrorMapper.Map(exception);

        Assert.Equal(502, problemDetails.Status);
        Assert.Equal(GraphApiErrorMapper.GraphApiErrorType, problemDetails.Type);
        Assert.Equal(100, problemDetails.Extensions["errorCode"]);
        Assert.Equal("OAuthException", problemDetails.Extensions["errorType"]);
        Assert.Equal("trace-invalid", problemDetails.Extensions["fbTraceId"]);
    }

    [Fact]
    public void Map_GenericGraphErrorWithoutOptionalFields_OmitsThoseExtensions()
    {
        var exception = new GraphApiException(
            HttpStatusCode.InternalServerError,
            errorCode: null,
            errorType: null,
            message: "Unknown Graph failure",
            fbTraceId: null);

        var problemDetails = GraphApiErrorMapper.Map(exception);

        Assert.Empty(problemDetails.Extensions);
    }
}
