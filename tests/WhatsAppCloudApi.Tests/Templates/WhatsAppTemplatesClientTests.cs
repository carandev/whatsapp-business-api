using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WhatsAppCloudApi.Clients;
using WhatsAppCloudApi.Models.Templates;
using WhatsAppCloudApi.Tests.Fakes;

namespace WhatsAppCloudApi.Tests.Templates;

public class WhatsAppTemplatesClientTests
{
    private static WhatsAppOptions CreateOptions(string? wabaId = "default-waba-id") => new()
    {
        AccessToken = "test-access-token",
        PhoneNumberId = "default-phone-id",
        WabaId = wabaId,
        ApiVersion = "v21.0",
        BaseUrl = "https://graph.facebook.com/{0}/",
    };

    private static (WhatsAppTemplatesClient Client, RecordingHandler Handler) CreateClient(
        WhatsAppOptions options,
        Func<HttpRequestMessage, HttpResponseMessage>? respond = null)
    {
        var handler = respond is null ? new RecordingHandler() : new RecordingHandler(respond);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(string.Format(options.BaseUrl, options.ApiVersion)),
        };
        var whatsAppClient = new WhatsAppClient(httpClient, Options.Create(options), NullLogger<WhatsAppClient>.Instance);
        var client = new WhatsAppTemplatesClient(whatsAppClient, Options.Create(options));
        return (client, handler);
    }

    private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string json) =>
        new(statusCode) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    /// <summary>
    /// The full absolute URI a relative path resolves to once <see cref="HttpClient"/> sends the
    /// request through its pipeline (the recording handler observes the resolved absolute URI, not
    /// the relative one the request was built with).
    /// </summary>
    private static string ExpectedRequestUri(WhatsAppOptions options, string relativePath) =>
        string.Format(options.BaseUrl, options.ApiVersion) + relativePath;

    private static CreateTemplateRequest SampleCreateRequest() => new(
        "order_confirmation",
        TemplateCategory.Utility,
        "en_US",
        [new BodyComponent("Hello {{1}}")]);

    // ---------- CreateAsync ----------

    [Fact]
    public async Task CreateAsync_SendsPostToMessageTemplatesEndpoint()
    {
        var options = CreateOptions();
        var (client, handler) = CreateClient(
            options,
            _ => JsonResponse(HttpStatusCode.OK, """{"id":"123","status":"PENDING","category":"UTILITY"}"""));

        await client.CreateAsync(SampleCreateRequest());

        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal(ExpectedRequestUri(options, $"{options.WabaId}/message_templates"), handler.LastRequest.RequestUri!.ToString());
    }

    [Fact]
    public async Task CreateAsync_ReturnsSuccessResult_WithDeserializedTemplate_On2xxResponse()
    {
        var options = CreateOptions();
        var (client, _) = CreateClient(
            options,
            _ => JsonResponse(HttpStatusCode.OK, """{"id":"123","status":"PENDING","category":"UTILITY"}"""));

        var result = await client.CreateAsync(SampleCreateRequest());

        Assert.True(result.IsSuccess);
        Assert.Equal("123", result.Value!.Id);
        Assert.Equal("PENDING", result.Value.Status);
        Assert.Equal(TemplateCategory.Utility, result.Value.Category);
    }

    [Fact]
    public async Task CreateAsync_ReturnsFailureResult_WithDeserializedError_OnNon2xxResponse()
    {
        var options = CreateOptions();
        var (client, _) = CreateClient(
            options,
            _ => JsonResponse(HttpStatusCode.BadRequest, """{"error":{"message":"Invalid parameter","type":"OAuthException","code":100}}"""));

        var result = await client.CreateAsync(SampleCreateRequest());

        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid parameter", result.Error!.Message);
        Assert.Equal("OAuthException", result.Error.Type);
        Assert.Equal(100, result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_ReturnsFailure_AndSendsNoRequest_WhenWabaIdIsUnresolved()
    {
        var options = CreateOptions(wabaId: null);
        var (client, handler) = CreateClient(options);

        var result = await client.CreateAsync(SampleCreateRequest());

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal(0, handler.RequestCount);
    }

    // ---------- ListAsync ----------

    [Fact]
    public async Task ListAsync_SendsGetToMessageTemplatesEndpoint_WithoutQuery_WhenOptionsIsNull()
    {
        var options = CreateOptions();
        var (client, handler) = CreateClient(
            options,
            _ => JsonResponse(HttpStatusCode.OK, """{"data":[]}"""));

        await client.ListAsync();

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Equal(ExpectedRequestUri(options, $"{options.WabaId}/message_templates"), handler.LastRequest.RequestUri!.ToString());
    }

    [Fact]
    public async Task ListAsync_ReturnsSuccessResult_WithDeserializedPage_On2xxResponse()
    {
        var options = CreateOptions();
        var (client, _) = CreateClient(
            options,
            _ => JsonResponse(
                HttpStatusCode.OK,
                """{"data":[{"id":"1","name":"one","status":"APPROVED","category":"MARKETING"}],"paging":{"cursors":{"before":"b","after":"a"}}}"""));

        var result = await client.ListAsync();

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Data!);
        Assert.Equal("1", result.Value.Data![0].Id);
        Assert.Equal("a", result.Value.Paging!.Cursors!.After);
        Assert.Equal("b", result.Value.Paging.Cursors.Before);
    }

    [Fact]
    public async Task ListAsync_ReturnsFailureResult_WithDeserializedError_OnNon2xxResponse()
    {
        var options = CreateOptions();
        var (client, _) = CreateClient(
            options,
            _ => JsonResponse(HttpStatusCode.Unauthorized, """{"error":{"message":"Invalid token","type":"OAuthException","code":190}}"""));

        var result = await client.ListAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid token", result.Error!.Message);
    }

    [Fact]
    public async Task ListAsync_ReturnsFailure_AndSendsNoRequest_WhenWabaIdIsUnresolved()
    {
        var options = CreateOptions(wabaId: null);
        var (client, handler) = CreateClient(options);

        var result = await client.ListAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(0, handler.RequestCount);
    }

    [Theory]
    [InlineData(true, false, false, false, false, false)]
    [InlineData(false, true, false, false, false, false)]
    [InlineData(false, false, true, false, false, false)]
    [InlineData(false, false, false, true, false, false)]
    [InlineData(false, false, false, false, true, false)]
    [InlineData(false, false, false, false, false, true)]
    public async Task ListAsync_IncludesOnlySetFilters_InQueryString(
        bool setName, bool setStatus, bool setLanguage, bool setLimit, bool setAfter, bool setBefore)
    {
        var options = CreateOptions();
        var (client, handler) = CreateClient(
            options,
            _ => JsonResponse(HttpStatusCode.OK, """{"data":[]}"""));

        var listOptions = new TemplatesListOptions
        {
            Name = setName ? "order_confirmation" : null,
            Status = setStatus ? "APPROVED" : null,
            Language = setLanguage ? "en_US" : null,
            Limit = setLimit ? 25 : null,
            After = setAfter ? "after-cursor" : null,
            Before = setBefore ? "before-cursor" : null,
        };

        await client.ListAsync(listOptions);

        var query = handler.LastRequest!.RequestUri!.ToString();
        Assert.Equal(setName, query.Contains("name=order_confirmation", StringComparison.Ordinal));
        Assert.Equal(setStatus, query.Contains("status=APPROVED", StringComparison.Ordinal));
        Assert.Equal(setLanguage, query.Contains("language=en_US", StringComparison.Ordinal));
        Assert.Equal(setLimit, query.Contains("limit=25", StringComparison.Ordinal));
        Assert.Equal(setAfter, query.Contains("after=after-cursor", StringComparison.Ordinal));
        Assert.Equal(setBefore, query.Contains("before=before-cursor", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ListAsync_OmitsQueryString_WhenAllFiltersAreNull()
    {
        var options = CreateOptions();
        var (client, handler) = CreateClient(
            options,
            _ => JsonResponse(HttpStatusCode.OK, """{"data":[]}"""));

        await client.ListAsync(new TemplatesListOptions());

        Assert.DoesNotContain('?', handler.LastRequest!.RequestUri!.ToString());
    }

    // ---------- GetAsync ----------

    [Fact]
    public async Task GetAsync_SendsGetToTemplateIdEndpoint()
    {
        var options = CreateOptions();
        var (client, handler) = CreateClient(
            options,
            _ => JsonResponse(HttpStatusCode.OK, """{"id":"123","status":"APPROVED","category":"UTILITY"}"""));

        await client.GetAsync("123");

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Equal(ExpectedRequestUri(options, "123"), handler.LastRequest.RequestUri!.ToString());
    }

    [Fact]
    public async Task GetAsync_ReturnsSuccessResult_WithDeserializedTemplate_On2xxResponse()
    {
        var options = CreateOptions();
        var (client, _) = CreateClient(
            options,
            _ => JsonResponse(HttpStatusCode.OK, """{"id":"123","status":"APPROVED","category":"UTILITY"}"""));

        var result = await client.GetAsync("123");

        Assert.True(result.IsSuccess);
        Assert.Equal("123", result.Value!.Id);
        Assert.Equal("APPROVED", result.Value.Status);
    }

    [Fact]
    public async Task GetAsync_ReturnsFailureResult_WithDeserializedError_OnNon2xxResponse()
    {
        var options = CreateOptions();
        var (client, _) = CreateClient(
            options,
            _ => JsonResponse(HttpStatusCode.NotFound, """{"error":{"message":"Template not found","type":"GraphMethodException","code":803}}"""));

        var result = await client.GetAsync("missing-id");

        Assert.False(result.IsSuccess);
        Assert.Equal("Template not found", result.Error!.Message);
    }

    [Fact]
    public async Task GetAsync_ReturnsFailure_AndSendsNoRequest_WhenWabaIdIsUnresolved()
    {
        var options = CreateOptions(wabaId: null);
        var (client, handler) = CreateClient(options);

        var result = await client.GetAsync("123");

        Assert.False(result.IsSuccess);
        Assert.Equal(0, handler.RequestCount);
    }

    // ---------- UpdateAsync ----------

    [Fact]
    public async Task UpdateAsync_SendsPostToTemplateIdEndpoint()
    {
        var options = CreateOptions();
        var (client, handler) = CreateClient(
            options,
            _ => JsonResponse(HttpStatusCode.OK, """{"id":"123","status":"APPROVED","category":"MARKETING"}"""));

        await client.UpdateAsync("123", new UpdateTemplateRequest(category: TemplateCategory.Marketing));

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal(ExpectedRequestUri(options, "123"), handler.LastRequest.RequestUri!.ToString());
    }

    [Fact]
    public async Task UpdateAsync_ReturnsSuccessResult_WithDeserializedTemplate_On2xxResponse()
    {
        var options = CreateOptions();
        var (client, _) = CreateClient(
            options,
            _ => JsonResponse(HttpStatusCode.OK, """{"id":"123","status":"APPROVED","category":"MARKETING"}"""));

        var result = await client.UpdateAsync("123", new UpdateTemplateRequest(category: TemplateCategory.Marketing));

        Assert.True(result.IsSuccess);
        Assert.Equal(TemplateCategory.Marketing, result.Value!.Category);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsFailureResult_WithDeserializedError_OnNon2xxResponse()
    {
        var options = CreateOptions();
        var (client, _) = CreateClient(
            options,
            _ => JsonResponse(
                HttpStatusCode.BadRequest,
                """{"error":{"message":"Cannot edit an approved template's components","type":"OAuthException","code":100}}"""));

        var result = await client.UpdateAsync("123", new UpdateTemplateRequest(category: TemplateCategory.Marketing));

        Assert.False(result.IsSuccess);
        Assert.Equal("Cannot edit an approved template's components", result.Error!.Message);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsFailure_AndSendsNoRequest_WhenWabaIdIsUnresolved()
    {
        var options = CreateOptions(wabaId: null);
        var (client, handler) = CreateClient(options);

        var result = await client.UpdateAsync("123", new UpdateTemplateRequest(category: TemplateCategory.Marketing));

        Assert.False(result.IsSuccess);
        Assert.Equal(0, handler.RequestCount);
    }

    // ---------- DeleteAsync ----------

    [Fact]
    public async Task DeleteAsync_SendsDeleteToMessageTemplatesEndpoint_WithIdAndNameInQuery()
    {
        var options = CreateOptions();
        var (client, handler) = CreateClient(options, _ => new HttpResponseMessage(HttpStatusCode.OK));

        await client.DeleteAsync("123", "order_confirmation");

        Assert.Equal(HttpMethod.Delete, handler.LastRequest!.Method);
        var uri = handler.LastRequest.RequestUri!.ToString();
        Assert.StartsWith(ExpectedRequestUri(options, $"{options.WabaId}/message_templates?"), uri, StringComparison.Ordinal);
        Assert.Contains("hsm_id=123", uri, StringComparison.Ordinal);
        Assert.Contains("name=order_confirmation", uri, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsSuccessResult_On2xxResponse()
    {
        var options = CreateOptions();
        var (client, _) = CreateClient(options, _ => new HttpResponseMessage(HttpStatusCode.OK));

        var result = await client.DeleteAsync("123", "order_confirmation");

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFailureResult_WithDeserializedError_OnNon2xxResponse()
    {
        var options = CreateOptions();
        var (client, _) = CreateClient(
            options,
            _ => JsonResponse(HttpStatusCode.NotFound, """{"error":{"message":"Template not found","type":"GraphMethodException","code":803}}"""));

        var result = await client.DeleteAsync("missing-id", "order_confirmation");

        Assert.False(result.IsSuccess);
        Assert.Equal("Template not found", result.Error!.Message);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFailure_AndSendsNoRequest_WhenNameIsMissing()
    {
        var options = CreateOptions();
        var (client, handler) = CreateClient(options);

        var result = await client.DeleteAsync("123", string.Empty);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFailure_AndSendsNoRequest_WhenWabaIdIsUnresolved()
    {
        var options = CreateOptions(wabaId: null);
        var (client, handler) = CreateClient(options);

        var result = await client.DeleteAsync("123", "order_confirmation");

        Assert.False(result.IsSuccess);
        Assert.Equal(0, handler.RequestCount);
    }
}
