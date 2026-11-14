using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Carandev.WhatsApp.Sdk.Clients;
using Carandev.WhatsApp.Sdk.Models.Flows;
using Carandev.WhatsApp.Sdk.Serialization;
using Carandev.WhatsApp.Sdk.Tests.Fakes;

namespace Carandev.WhatsApp.Sdk.Tests.Flows;

/// <summary>
/// Flows management through the Graph API: <c>/{waba-id}/flows</c> and <c>/{flow-id}</c>.
/// </summary>
public class WhatsAppFlowsClientTests
{
    private const string SuccessBody = """{"success":true}""";
    private const string ErrorBody = """{"error":{"message":"Cannot update a published Flow","type":"OAuthException","code":139001}}""";
    private const string ValidationErrors = """[{"error":"INVALID_PROPERTY_VALUE","error_type":"FLOW_JSON_ERROR","message":"Invalid value found for property 'type'.","line_start":10,"line_end":10,"column_start":21,"column_end":34,"pointers":[{"line_start":10,"line_end":10,"column_start":21,"column_end":34,"path":"screens[0].layout.children[0].type"}]}]""";

    private static WhatsAppOptions CreateOptions(string? wabaId = "default-waba-id") => new()
    {
        AccessToken = "test-access-token",
        PhoneNumberId = "default-phone-id",
        WabaId = wabaId,
        ApiVersion = "v21.0",
        BaseUrl = "https://graph.facebook.com/{0}/",
    };

    private static (WhatsAppFlowsClient Client, RecordingHandler Handler) CreateClient(
        string body = SuccessBody,
        HttpStatusCode status = HttpStatusCode.OK,
        WhatsAppOptions? options = null)
    {
        options ??= CreateOptions();
        var handler = new RecordingHandler(_ => new HttpResponseMessage(status)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        });
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(string.Format(options.BaseUrl, options.ApiVersion)),
        };
        var whatsAppClient = new WhatsAppClient(httpClient, Options.Create(options), NullLogger<WhatsAppClient>.Instance);
        return (new WhatsAppFlowsClient(whatsAppClient), handler);
    }

    private static string Uri(string relativePath) => "https://graph.facebook.com/v21.0/" + relativePath;

    // ---------- CreateFlowAsync ----------

    [Fact]
    public async Task CreateFlowAsync_PostsAllFields_ToConfiguredWaba_AndReadsIdAndValidationErrors()
    {
        var (client, handler) = CreateClient($$"""{"id":"flow-1","success":true,"validation_errors":{{ValidationErrors}}}""");
        var request = new CreateFlowRequest("My first flow", [FlowCategory.Survey, FlowCategory.Other])
        {
            FlowJson = """{"version":"5.0","screens":[]}""",
            Publish = true,
            EndpointUri = new Uri("https://example.com/flows"),
        };

        var result = await client.CreateFlowAsync(request);

        Assert.True(result.IsSuccess);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal(Uri("default-waba-id/flows"), handler.LastRequest.RequestUri!.ToString());
        using var body = JsonDocument.Parse(await handler.LastRequest.Content!.ReadAsStringAsync());
        Assert.Equal(
            ["name", "categories", "flow_json", "publish", "endpoint_uri"],
            body.RootElement.EnumerateObject().Select(property => property.Name));
        Assert.Equal("My first flow", body.RootElement.GetProperty("name").GetString());
        Assert.Equal("""["SURVEY","OTHER"]""", body.RootElement.GetProperty("categories").GetRawText());
        Assert.Equal("""{"version":"5.0","screens":[]}""", body.RootElement.GetProperty("flow_json").GetString());
        Assert.True(body.RootElement.GetProperty("publish").GetBoolean());
        Assert.Equal("https://example.com/flows", body.RootElement.GetProperty("endpoint_uri").GetString());
        Assert.Equal("flow-1", result.Value!.Id);
        Assert.True(result.Value.Success);
        var error = Assert.Single(result.Value.ValidationErrors!);
        Assert.Equal("INVALID_PROPERTY_VALUE", error.Error);
        Assert.Equal("FLOW_JSON_ERROR", error.ErrorType);
        Assert.Equal(10, error.LineStart);
        Assert.Equal(34, error.ColumnEnd);
        Assert.Equal("screens[0].layout.children[0].type", Assert.Single(error.Pointers!).Path);
    }

    [Fact]
    public async Task CreateFlowAsync_Clone_SendsOnlySuppliedFields_ToOverriddenWaba()
    {
        var (client, handler) = CreateClient("""{"id":"flow-2","success":true}""");

        var result = await client.CreateFlowAsync(new CreateFlowRequest("Copy", [FlowCategory.AppointmentBooking]) { CloneFlowId = "flow-1" }, "other-waba");

        Assert.True(result.IsSuccess);
        Assert.Equal(Uri("other-waba/flows"), handler.LastRequest!.RequestUri!.ToString());
        Assert.Equal(
            """{"name":"Copy","categories":["APPOINTMENT_BOOKING"],"clone_flow_id":"flow-1"}""",
            await handler.LastRequest.Content!.ReadAsStringAsync());
    }

    [Fact]
    public void CreateFlowRequest_Rejects_BlankName_And_EmptyCategories()
    {
        Assert.ThrowsAny<ArgumentException>(() => new CreateFlowRequest(" ", [FlowCategory.Other]));
        var ex = Assert.Throws<ArgumentException>(() => new CreateFlowRequest("Name", []));
        Assert.Contains("At least one category", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateFlowAsync_ReturnsFailure_WithoutSending_WhenWabaIdIsMissing()
    {
        var (client, handler) = CreateClient(options: CreateOptions(wabaId: null));

        var result = await client.CreateFlowAsync(new CreateFlowRequest("Name", [FlowCategory.Other]));

        Assert.False(result.IsSuccess);
        Assert.Contains("WabaId", result.Error!.Message, StringComparison.Ordinal);
        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public async Task CreateFlowAsync_ReturnsGraphError_OnFailure()
    {
        var (client, _) = CreateClient(ErrorBody, HttpStatusCode.BadRequest);

        var result = await client.CreateFlowAsync(new CreateFlowRequest("Name", [FlowCategory.Other]));

        Assert.False(result.IsSuccess);
        Assert.Equal(139001, result.Error!.Code);
    }

    // ---------- ListFlowsAsync ----------

    [Fact]
    public async Task ListFlowsAsync_GetsFlowsWithPaging()
    {
        var (client, handler) = CreateClient("""{"data":[{"id":"flow-1","name":"flow 1","status":"DRAFT","categories":["CONTACT_US"],"validation_errors":[]}],"paging":{"cursors":{"before":"B","after":"A"}}}""");

        var result = await client.ListFlowsAsync(new FlowsListOptions { Limit = 10, After = "cursor" });

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Equal(Uri("default-waba-id/flows?limit=10&after=cursor"), handler.LastRequest.RequestUri!.ToString());
        var flow = Assert.Single(result.Value!.Data!);
        Assert.Equal("flow-1", flow.Id);
        Assert.Equal(FlowStatus.Draft, flow.Status);
        Assert.Equal([FlowCategory.ContactUs], flow.Categories);
        Assert.Equal("A", result.Value.Paging!.Cursors!.After);
    }

    [Fact]
    public async Task ListFlowsAsync_WithoutOptions_HasNoQuery()
    {
        var (client, handler) = CreateClient("""{"data":[]}""");

        await client.ListFlowsAsync();

        Assert.Equal(Uri("default-waba-id/flows"), handler.LastRequest!.RequestUri!.ToString());
    }

    [Fact]
    public async Task ListFlowsAsync_MapsUnknownStatusAndCategory_ToUnknown()
    {
        var (client, _) = CreateClient("""{"data":[{"id":"f","status":"SOMETHING_NEW","categories":["NEW_CATEGORY"]}]}""");

        var result = await client.ListFlowsAsync();

        var flow = Assert.Single(result.Value!.Data!);
        Assert.Equal(FlowStatus.Unknown, flow.Status);
        Assert.Equal([FlowCategory.Unknown], flow.Categories);
    }

    // ---------- GetFlowAsync ----------

    [Fact]
    public async Task GetFlowAsync_RequestsFields_AndReadsDetails()
    {
        var (client, handler) = CreateClient("""{"id":"flow-1","name":"Booking","status":"PUBLISHED","categories":["APPOINTMENT_BOOKING"],"validation_errors":[],"json_version":"5.0","data_api_version":"3.0","endpoint_uri":"https://example.com/flows"}""");

        var result = await client.GetFlowAsync("flow-1", ["id", "name", "status", "json_version", "data_api_version", "endpoint_uri"]);

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Equal(Uri("flow-1?fields=id%2Cname%2Cstatus%2Cjson_version%2Cdata_api_version%2Cendpoint_uri"), handler.LastRequest.RequestUri!.ToString());
        Assert.Equal(FlowStatus.Published, result.Value!.Status);
        Assert.Equal("5.0", result.Value.JsonVersion);
        Assert.Equal("3.0", result.Value.DataApiVersion);
        Assert.Equal("https://example.com/flows", result.Value.EndpointUri);
    }

    [Fact]
    public async Task GetFlowAsync_WithoutFields_UsesMetaDefaults_AndEscapesFlowId()
    {
        var (client, handler) = CreateClient("""{"id":"a/b"}""");

        await client.GetFlowAsync("a/b");

        Assert.Equal(Uri("a%2Fb"), handler.LastRequest!.RequestUri!.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task FlowIdMethods_ThrowBeforeSending_ForBlankFlowId(string? flowId)
    {
        var (client, handler) = CreateClient();

        await Assert.ThrowsAnyAsync<ArgumentException>(() => client.GetFlowAsync(flowId!));
        await Assert.ThrowsAnyAsync<ArgumentException>(() => client.PublishFlowAsync(flowId!));
        await Assert.ThrowsAnyAsync<ArgumentException>(() => client.DeprecateFlowAsync(flowId!));
        await Assert.ThrowsAnyAsync<ArgumentException>(() => client.DeleteFlowAsync(flowId!));
        await Assert.ThrowsAnyAsync<ArgumentException>(() => client.GetFlowPreviewAsync(flowId!));
        await Assert.ThrowsAnyAsync<ArgumentException>(() => client.UploadFlowJsonAsync(flowId!, "{}"));
        await Assert.ThrowsAnyAsync<ArgumentException>(() => client.UpdateFlowMetadataAsync(flowId!, new UpdateFlowMetadataRequest { Name = "x" }));
        Assert.Equal(0, handler.RequestCount);
    }

    // ---------- UpdateFlowMetadataAsync ----------

    [Fact]
    public async Task UpdateFlowMetadataAsync_PostsOnlySuppliedFields()
    {
        var (client, handler) = CreateClient();

        var result = await client.UpdateFlowMetadataAsync("flow-1", new UpdateFlowMetadataRequest
        {
            Name = "Renamed",
            Categories = [FlowCategory.LeadGeneration],
            EndpointUri = new Uri("https://example.com/v2"),
            ApplicationId = "app-1",
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal(Uri("flow-1"), handler.LastRequest.RequestUri!.ToString());
        Assert.Equal(
            """{"name":"Renamed","categories":["LEAD_GENERATION"],"endpoint_uri":"https://example.com/v2","application_id":"app-1"}""",
            await handler.LastRequest.Content!.ReadAsStringAsync());
    }

    [Fact]
    public async Task UpdateFlowMetadataAsync_ThrowsBeforeSending_WhenNothingToUpdate()
    {
        var (client, handler) = CreateClient();

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => client.UpdateFlowMetadataAsync("flow-1", new UpdateFlowMetadataRequest()));
        Assert.Contains("At least one", ex.Message, StringComparison.Ordinal);
        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public async Task UpdateFlowMetadataAsync_ReturnsGraphError_OnFailure()
    {
        var (client, _) = CreateClient(ErrorBody, HttpStatusCode.BadRequest);

        var result = await client.UpdateFlowMetadataAsync("flow-1", new UpdateFlowMetadataRequest { Name = "x" });

        Assert.False(result.IsSuccess);
        Assert.Equal(139001, result.Error!.Code);
    }

    // ---------- UploadFlowJsonAsync ----------

    [Fact]
    public async Task UploadFlowJsonAsync_PostsMultipartAsset_AndReadsValidationErrors()
    {
        var (client, handler) = CreateClient($$"""{"success":true,"validation_errors":{{ValidationErrors}}}""");

        var result = await client.UploadFlowJsonAsync("flow-1", """{"version":"5.0"}""");

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.ValidationErrors!);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal(Uri("flow-1/assets"), handler.LastRequest.RequestUri!.ToString());
        var multipart = Assert.IsType<MultipartFormDataContent>(handler.LastRequest.Content);
        var parts = multipart.ToDictionary(part => part.Headers.ContentDisposition!.Name!.Trim('"'));
        Assert.Equal("flow.json", await parts["name"].ReadAsStringAsync());
        Assert.Equal("FLOW_JSON", await parts["asset_type"].ReadAsStringAsync());
        Assert.Equal("""{"version":"5.0"}""", await parts["file"].ReadAsStringAsync());
        Assert.Equal("application/json", parts["file"].Headers.ContentType!.MediaType);
        Assert.Equal("flow.json", parts["file"].Headers.ContentDisposition!.FileName!.Trim('"'));
    }

    [Fact]
    public async Task UploadFlowJsonAsync_ThrowsBeforeSending_ForBlankJson()
    {
        var (client, handler) = CreateClient();

        await Assert.ThrowsAnyAsync<ArgumentException>(() => client.UploadFlowJsonAsync("flow-1", " "));
        Assert.Equal(0, handler.RequestCount);
    }

    // ---------- publish / deprecate / delete ----------

    [Fact]
    public async Task PublishFlowAsync_PostsToPublish()
    {
        var (client, handler) = CreateClient();

        var result = await client.PublishFlowAsync("flow-1");

        Assert.True(result.IsSuccess);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal(Uri("flow-1/publish"), handler.LastRequest.RequestUri!.ToString());
    }

    [Fact]
    public async Task DeprecateFlowAsync_PostsToDeprecate()
    {
        var (client, handler) = CreateClient();

        var result = await client.DeprecateFlowAsync("flow-1");

        Assert.True(result.IsSuccess);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal(Uri("flow-1/deprecate"), handler.LastRequest.RequestUri!.ToString());
    }

    [Fact]
    public async Task DeleteFlowAsync_SendsDelete()
    {
        var (client, handler) = CreateClient();

        var result = await client.DeleteFlowAsync("flow-1");

        Assert.True(result.IsSuccess);
        Assert.Equal(HttpMethod.Delete, handler.LastRequest!.Method);
        Assert.Equal(Uri("flow-1"), handler.LastRequest.RequestUri!.ToString());
    }

    [Fact]
    public async Task PublishFlowAsync_ReturnsGraphError_OnFailure()
    {
        var (client, _) = CreateClient("""{"error":{"message":"Invalid state","code":139002}}""", HttpStatusCode.BadRequest);

        var result = await client.PublishFlowAsync("flow-1");

        Assert.False(result.IsSuccess);
        Assert.Equal(139002, result.Error!.Code);
    }

    [Fact]
    public async Task DeleteFlowAsync_ReturnsGenericError_WhenBodyIsNotGraphError()
    {
        var (client, _) = CreateClient("oops", HttpStatusCode.InternalServerError);

        var result = await client.DeleteFlowAsync("flow-1");

        Assert.False(result.IsSuccess);
        Assert.Equal(500, result.Error!.Code);
    }

    // ---------- GetFlowPreviewAsync ----------

    [Theory]
    [InlineData(false, "flow-1?fields=preview.invalidate%28false%29")]
    [InlineData(true, "flow-1?fields=preview.invalidate%28true%29")]
    public async Task GetFlowPreviewAsync_RequestsPreviewField_AndReadsUrl(bool invalidate, string expectedPath)
    {
        var (client, handler) = CreateClient("""{"preview":{"preview_url":"https://business.facebook.com/wa/manage/flows/1/preview/?token=t","expires_at":"2023-05-21T11:18:09+0000"},"id":"flow-1"}""");

        var result = await client.GetFlowPreviewAsync("flow-1", invalidate);

        Assert.Equal(Uri(expectedPath), handler.LastRequest!.RequestUri!.ToString());
        Assert.Equal("https://business.facebook.com/wa/manage/flows/1/preview/?token=t", result.Value!.PreviewUrl);
        Assert.Equal("2023-05-21T11:18:09+0000", result.Value.ExpiresAt);
    }

    [Fact]
    public async Task GetFlowPreviewAsync_ReturnsFailure_WhenResponseHasNoPreview()
    {
        var (client, _) = CreateClient("""{"id":"flow-1"}""");

        var result = await client.GetFlowPreviewAsync("flow-1");

        Assert.False(result.IsSuccess);
        Assert.Contains("preview", result.Error!.Message, StringComparison.Ordinal);
    }

    // ---------- serialization ----------

    [Fact]
    public void FlowCategory_WritesMetaValues()
    {
        var values = new[]
        {
            FlowCategory.SignUp, FlowCategory.SignIn, FlowCategory.AppointmentBooking, FlowCategory.LeadGeneration,
            FlowCategory.ContactUs, FlowCategory.CustomerSupport, FlowCategory.Survey, FlowCategory.Other,
        };

        var json = JsonSerializer.Serialize(values, WhatsAppJsonSerializerContext.Default.FlowCategoryArray);

        Assert.Equal("""["SIGN_UP","SIGN_IN","APPOINTMENT_BOOKING","LEAD_GENERATION","CONTACT_US","CUSTOMER_SUPPORT","SURVEY","OTHER"]""", json);
    }

    [Fact]
    public void FlowCategory_Unknown_CannotBeWritten()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Serialize(FlowCategory.Unknown, WhatsAppJsonSerializerContext.Default.FlowCategory));
    }
}
