using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Carandev.WhatsApp.Sdk.Clients;
using Carandev.WhatsApp.Sdk.Models.PhoneNumbers;
using Carandev.WhatsApp.Sdk.Tests.Fakes;

namespace Carandev.WhatsApp.Sdk.Tests.PhoneNumbers;

public class WhatsAppPhoneNumbersClientTests
{
    private const string PhoneNumberJson =
        """{"id":"106540352242922","display_name":"Acme Support","display_phone_number":"+1 555-0100","quality_rating":"GREEN","verified_name":"Acme Inc","name_status":"APPROVED","code_verification_status":"VERIFIED","platform_type":"CLOUD_API","throughput":{"level":"STANDARD"},"is_official_business_account":true}""";

    private const string ErrorBody = """{"error":{"message":"Invalid parameter","type":"OAuthException","code":100}}""";

    private static WhatsAppOptions CreateOptions(string? wabaId = "default-waba-id") => new()
    {
        AccessToken = "test-access-token",
        PhoneNumberId = "default-phone-id",
        WabaId = wabaId,
        ApiVersion = "v21.0",
        BaseUrl = "https://graph.facebook.com/{0}/",
    };

    private static (WhatsAppPhoneNumbersClient Client, RecordingHandler Handler) CreateClient(
        WhatsAppOptions options,
        Func<HttpRequestMessage, HttpResponseMessage>? respond = null)
    {
        var handler = respond is null ? new RecordingHandler() : new RecordingHandler(respond);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(string.Format(options.BaseUrl, options.ApiVersion)),
        };
        var whatsAppClient = new WhatsAppClient(httpClient, Options.Create(options), NullLogger<WhatsAppClient>.Instance);
        var client = new WhatsAppPhoneNumbersClient(whatsAppClient, Options.Create(options));
        return (client, handler);
    }

    private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string json) =>
        new(statusCode) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private static string ExpectedRequestUri(WhatsAppOptions options, string relativePath) =>
        string.Format(options.BaseUrl, options.ApiVersion) + relativePath;

    private static async Task<string> ReadRequestBodyAsync(HttpRequestMessage request) =>
        await request.Content!.ReadAsStringAsync();

    // ---------- ListPhoneNumbersAsync ----------

    [Fact]
    public async Task ListPhoneNumbersAsync_SendsGetToPhoneNumbersEndpoint()
    {
        var options = CreateOptions();
        var (client, handler) = CreateClient(
            options,
            _ => JsonResponse(HttpStatusCode.OK, """{"data":[]}"""));

        await client.ListPhoneNumbersAsync();

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Equal(ExpectedRequestUri(options, $"{options.WabaId}/phone_numbers"), handler.LastRequest.RequestUri!.ToString());
    }

    [Fact]
    public async Task ListPhoneNumbersAsync_OverrideResolvesAheadOfConfiguredDefault()
    {
        var options = CreateOptions(wabaId: "A");
        var (client, handler) = CreateClient(
            options,
            _ => JsonResponse(HttpStatusCode.OK, """{"data":[]}"""));

        await client.ListPhoneNumbersAsync("B");

        Assert.Equal(ExpectedRequestUri(options, "B/phone_numbers"), handler.LastRequest!.RequestUri!.ToString());
    }

    [Fact]
    public async Task ListPhoneNumbersAsync_ReturnsSuccessResult_WithDeserializedPage_On2xxResponse()
    {
        var options = CreateOptions();
        var (client, _) = CreateClient(
            options,
            _ => JsonResponse(
                HttpStatusCode.OK,
                """{"data":[""" + PhoneNumberJson + """],"paging":{"cursors":{"before":"b","after":"a"}}}"""));

        var result = await client.ListPhoneNumbersAsync();

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Data!);
        Assert.Equal("106540352242922", result.Value.Data![0].Id);
        Assert.Equal("a", result.Value.Paging!.Cursors!.After);
        Assert.Equal("b", result.Value.Paging.Cursors.Before);
    }

    [Fact]
    public async Task ListPhoneNumbersAsync_ReturnsFailureResult_WithDeserializedError_OnNon2xxResponse()
    {
        var options = CreateOptions();
        var (client, _) = CreateClient(options, _ => JsonResponse(HttpStatusCode.Unauthorized, ErrorBody));

        var result = await client.ListPhoneNumbersAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid parameter", result.Error!.Message);
        Assert.Equal("OAuthException", result.Error.Type);
        Assert.Equal(100, result.Error.Code);
    }

    [Fact]
    public async Task ListPhoneNumbersAsync_ReturnsFailure_AndSendsNoRequest_WhenWabaIdIsUnresolved()
    {
        var options = CreateOptions(wabaId: null);
        var (client, handler) = CreateClient(options);

        var result = await client.ListPhoneNumbersAsync();

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal(0, handler.RequestCount);
    }

    // ---------- GetPhoneNumberAsync ----------

    [Fact]
    public async Task GetPhoneNumberAsync_SendsGetToPhoneNumberIdEndpoint()
    {
        var options = CreateOptions();
        var (client, handler) = CreateClient(options, _ => JsonResponse(HttpStatusCode.OK, PhoneNumberJson));

        await client.GetPhoneNumberAsync();

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Equal(ExpectedRequestUri(options, options.PhoneNumberId!), handler.LastRequest.RequestUri!.ToString());
    }

    [Fact]
    public async Task GetPhoneNumberAsync_UsesOverride_WhenProvided()
    {
        var options = CreateOptions();
        var (client, handler) = CreateClient(options, _ => JsonResponse(HttpStatusCode.OK, PhoneNumberJson));

        await client.GetPhoneNumberAsync("override-phone-id");

        Assert.Equal(ExpectedRequestUri(options, "override-phone-id"), handler.LastRequest!.RequestUri!.ToString());
    }

    [Fact]
    public async Task GetPhoneNumberAsync_ReturnsSuccessResult_WithAllNineFields_On2xxResponse()
    {
        var options = CreateOptions();
        var (client, _) = CreateClient(options, _ => JsonResponse(HttpStatusCode.OK, PhoneNumberJson));

        var result = await client.GetPhoneNumberAsync();

        Assert.True(result.IsSuccess);
        var phoneNumber = result.Value!;
        Assert.Equal("Acme Support", phoneNumber.DisplayName);
        Assert.Equal("+1 555-0100", phoneNumber.DisplayPhoneNumber);
        Assert.Equal(PhoneNumberQualityRating.Green, phoneNumber.QualityRating);
        Assert.Equal("Acme Inc", phoneNumber.VerifiedName);
        Assert.Equal(PhoneNumberNameStatus.Approved, phoneNumber.NameStatus);
        Assert.Equal("VERIFIED", phoneNumber.CodeVerificationStatus);
        Assert.Equal("CLOUD_API", phoneNumber.PlatformType);
        Assert.Equal("STANDARD", phoneNumber.Throughput!.Level);
        Assert.True(phoneNumber.IsOfficialBusinessAccount);
    }

    [Fact]
    public async Task GetPhoneNumberAsync_ReturnsFailureResult_WithDeserializedError_WhenNotFound()
    {
        var options = CreateOptions();
        var (client, _) = CreateClient(
            options,
            _ => JsonResponse(HttpStatusCode.NotFound, """{"error":{"message":"Phone number not found","type":"GraphMethodException","code":803}}"""));

        var result = await client.GetPhoneNumberAsync("missing-id");

        Assert.False(result.IsSuccess);
        Assert.Equal("Phone number not found", result.Error!.Message);
    }

    [Fact]
    public async Task GetPhoneNumberAsync_ReturnsFailureResult_OnApiFailure_WithoutThrowing()
    {
        var options = CreateOptions();
        var (client, _) = CreateClient(options, _ => JsonResponse(HttpStatusCode.InternalServerError, ErrorBody));

        var result = await client.GetPhoneNumberAsync();

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
    }

    // ---------- RequestVerificationCodeAsync ----------

    [Fact]
    public async Task RequestVerificationCodeAsync_SendsPostToRequestCodeEndpoint_WithCodeMethodAndLanguageBody()
    {
        var options = CreateOptions();
        var (client, handler) = CreateClient(options, _ => JsonResponse(HttpStatusCode.OK, """{"success":true}"""));

        await client.RequestVerificationCodeAsync(VerificationCodeMethod.Sms, "en_US");

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal(ExpectedRequestUri(options, $"{options.PhoneNumberId}/request_code"), handler.LastRequest.RequestUri!.ToString());
        var body = await ReadRequestBodyAsync(handler.LastRequest);
        Assert.Equal("""{"code_method":"sms","language":"en_US"}""", body);
    }

    [Fact]
    public async Task RequestVerificationCodeAsync_VoiceMethod_SerializesLowercaseVoice()
    {
        var options = CreateOptions();
        var (client, handler) = CreateClient(options, _ => JsonResponse(HttpStatusCode.OK, """{"success":true}"""));

        await client.RequestVerificationCodeAsync(VerificationCodeMethod.Voice, "en_US");

        var body = await ReadRequestBodyAsync(handler.LastRequest!);
        Assert.Contains("\"code_method\":\"voice\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RequestVerificationCodeAsync_DoesNotSend_CodeIdField_InBody()
    {
        var options = CreateOptions();
        var (client, handler) = CreateClient(options, _ => JsonResponse(HttpStatusCode.OK, """{"success":true}"""));

        await client.RequestVerificationCodeAsync(VerificationCodeMethod.Sms, "en_US");

        var body = await ReadRequestBodyAsync(handler.LastRequest!);
        Assert.DoesNotContain("code_id", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RequestVerificationCodeAsync_ReturnsSuccess_OnSuccessResponse_WithNoCodeIdExposed()
    {
        var options = CreateOptions();
        var (client, _) = CreateClient(options, _ => JsonResponse(HttpStatusCode.OK, """{"success":true}"""));

        var result = await client.RequestVerificationCodeAsync(VerificationCodeMethod.Sms, "en_US");

        Assert.True(result.IsSuccess);
        Assert.IsType<Result>(result);
    }

    [Fact]
    public async Task RequestVerificationCodeAsync_ReturnsFailure_WithDeserializedError_OnNon2xxResponse()
    {
        var options = CreateOptions();
        var (client, _) = CreateClient(
            options,
            _ => JsonResponse(HttpStatusCode.BadRequest, """{"error":{"message":"Phone number already verified","type":"OAuthException","code":100}}"""));

        var result = await client.RequestVerificationCodeAsync(VerificationCodeMethod.Sms, "en_US");

        Assert.False(result.IsSuccess);
        Assert.Equal("Phone number already verified", result.Error!.Message);
    }

    // ---------- VerifyCodeAsync ----------

    [Fact]
    public async Task VerifyCodeAsync_SendsPostToVerifyCodeEndpoint_WithCodeOnlyBody()
    {
        var options = CreateOptions();
        var (client, handler) = CreateClient(options, _ => JsonResponse(HttpStatusCode.OK, """{"success":true}"""));

        await client.VerifyCodeAsync("123456");

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal(ExpectedRequestUri(options, $"{options.PhoneNumberId}/verify_code"), handler.LastRequest.RequestUri!.ToString());
        var body = await ReadRequestBodyAsync(handler.LastRequest);
        Assert.Equal("""{"code":"123456"}""", body);
    }

    [Fact]
    public async Task VerifyCodeAsync_DoesNotSend_CodeIdField_InBody()
    {
        var options = CreateOptions();
        var (client, handler) = CreateClient(options, _ => JsonResponse(HttpStatusCode.OK, """{"success":true}"""));

        await client.VerifyCodeAsync("123456");

        var body = await ReadRequestBodyAsync(handler.LastRequest!);
        Assert.DoesNotContain("code_id", body, StringComparison.Ordinal);
        Assert.DoesNotContain("codeId", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task VerifyCodeAsync_UsesOverride_WhenProvided()
    {
        var options = CreateOptions();
        var (client, handler) = CreateClient(options, _ => JsonResponse(HttpStatusCode.OK, """{"success":true}"""));

        await client.VerifyCodeAsync("123456", "override-phone-id");

        Assert.Equal(ExpectedRequestUri(options, "override-phone-id/verify_code"), handler.LastRequest!.RequestUri!.ToString());
    }

    [Fact]
    public async Task VerifyCodeAsync_ReturnsSuccess_OnCorrectCode()
    {
        var options = CreateOptions();
        var (client, _) = CreateClient(options, _ => JsonResponse(HttpStatusCode.OK, """{"success":true}"""));

        var result = await client.VerifyCodeAsync("123456");

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task VerifyCodeAsync_ReturnsFailure_WithDeserializedError_OnIncorrectCode()
    {
        var options = CreateOptions();
        var (client, _) = CreateClient(
            options,
            _ => JsonResponse(HttpStatusCode.BadRequest, """{"error":{"message":"Invalid verification code","type":"OAuthException","code":100}}"""));

        var result = await client.VerifyCodeAsync("000000");

        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid verification code", result.Error!.Message);
    }
}
