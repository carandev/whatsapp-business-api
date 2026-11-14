using System.Text.Json.Serialization;

namespace Carandev.WhatsApp.Sdk.Models.Flows;

/// <summary>
/// A WhatsApp Flow, as returned by <c>IWhatsAppFlowsClient.ListFlowsAsync</c> and
/// <c>GetFlowAsync</c>. Fields Meta did not return (because they were not requested) are
/// <see langword="null"/>.
/// </summary>
public sealed class Flow
{
    /// <summary>
    /// The Flow id.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// The Flow name.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// The Flow's lifecycle status.
    /// </summary>
    [JsonPropertyName("status")]
    public FlowStatus? Status { get; set; }

    /// <summary>
    /// The Flow's categories.
    /// </summary>
    [JsonPropertyName("categories")]
    public IReadOnlyList<FlowCategory>? Categories { get; set; }

    /// <summary>
    /// Problems found in the current Flow JSON. A Flow cannot be published while this is non-empty.
    /// </summary>
    [JsonPropertyName("validation_errors")]
    public IReadOnlyList<FlowValidationError>? ValidationErrors { get; set; }

    /// <summary>
    /// The Flow JSON version, e.g. <c>5.0</c>.
    /// </summary>
    [JsonPropertyName("json_version")]
    public string? JsonVersion { get; set; }

    /// <summary>
    /// The data API version the Flow's endpoint uses, e.g. <c>3.0</c>.
    /// </summary>
    [JsonPropertyName("data_api_version")]
    public string? DataApiVersion { get; set; }

    /// <summary>
    /// The URL of the Flow's data endpoint.
    /// </summary>
    [JsonPropertyName("endpoint_uri")]
    public string? EndpointUri { get; set; }

    /// <summary>
    /// The Flow's web preview, when the <c>preview</c> field was requested.
    /// </summary>
    [JsonPropertyName("preview")]
    public FlowPreview? Preview { get; set; }
}

/// <summary>
/// A link to a web preview of a Flow, returned by <c>IWhatsAppFlowsClient.GetFlowPreviewAsync</c>.
/// </summary>
public sealed class FlowPreview
{
    /// <summary>
    /// The preview URL. It expires after 30 days.
    /// </summary>
    [JsonPropertyName("preview_url")]
    public string? PreviewUrl { get; set; }

    /// <summary>
    /// When <see cref="PreviewUrl"/> expires, as Meta sent it (e.g. <c>2023-05-21T11:18:09+0000</c>).
    /// </summary>
    [JsonPropertyName("expires_at")]
    public string? ExpiresAt { get; set; }
}

/// <summary>
/// A problem found in a Flow JSON, with its position in the document.
/// </summary>
public sealed class FlowValidationError
{
    /// <summary>
    /// The error code, e.g. <c>INVALID_PROPERTY_VALUE</c>.
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>
    /// The error type, e.g. <c>FLOW_JSON_ERROR</c>.
    /// </summary>
    [JsonPropertyName("error_type")]
    public string? ErrorType { get; set; }

    /// <summary>
    /// A human-readable description.
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>
    /// The first line of the problem.
    /// </summary>
    [JsonPropertyName("line_start")]
    public int? LineStart { get; set; }

    /// <summary>
    /// The last line of the problem.
    /// </summary>
    [JsonPropertyName("line_end")]
    public int? LineEnd { get; set; }

    /// <summary>
    /// The first column of the problem.
    /// </summary>
    [JsonPropertyName("column_start")]
    public int? ColumnStart { get; set; }

    /// <summary>
    /// The last column of the problem.
    /// </summary>
    [JsonPropertyName("column_end")]
    public int? ColumnEnd { get; set; }

    /// <summary>
    /// The places in the Flow JSON the problem points to.
    /// </summary>
    [JsonPropertyName("pointers")]
    public IReadOnlyList<FlowValidationErrorPointer>? Pointers { get; set; }
}

/// <summary>
/// One place in a Flow JSON that a <see cref="FlowValidationError"/> points to.
/// </summary>
public sealed class FlowValidationErrorPointer
{
    /// <summary>
    /// The first line.
    /// </summary>
    [JsonPropertyName("line_start")]
    public int? LineStart { get; set; }

    /// <summary>
    /// The last line.
    /// </summary>
    [JsonPropertyName("line_end")]
    public int? LineEnd { get; set; }

    /// <summary>
    /// The first column.
    /// </summary>
    [JsonPropertyName("column_start")]
    public int? ColumnStart { get; set; }

    /// <summary>
    /// The last column.
    /// </summary>
    [JsonPropertyName("column_end")]
    public int? ColumnEnd { get; set; }

    /// <summary>
    /// The JSON path, e.g. <c>screens[0].layout.children[0].type</c>.
    /// </summary>
    [JsonPropertyName("path")]
    public string? Path { get; set; }
}

/// <summary>
/// The response of <c>IWhatsAppFlowsClient.CreateFlowAsync</c>.
/// </summary>
public sealed class CreateFlowResponse
{
    /// <summary>
    /// The new Flow's id.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Whether Meta created the Flow.
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// Problems found in the supplied Flow JSON, if any.
    /// </summary>
    [JsonPropertyName("validation_errors")]
    public IReadOnlyList<FlowValidationError>? ValidationErrors { get; set; }
}

/// <summary>
/// The response of <c>IWhatsAppFlowsClient.UploadFlowJsonAsync</c>.
/// </summary>
public sealed class FlowJsonUploadResponse
{
    /// <summary>
    /// Whether Meta stored the Flow JSON.
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// Problems found in the uploaded Flow JSON, if any. The JSON is stored even when this is
    /// non-empty, but the Flow cannot be published until they are fixed.
    /// </summary>
    [JsonPropertyName("validation_errors")]
    public IReadOnlyList<FlowValidationError>? ValidationErrors { get; set; }
}

/// <summary>
/// Paging options for <c>IWhatsAppFlowsClient.ListFlowsAsync</c>.
/// </summary>
public sealed record FlowsListOptions
{
    /// <summary>
    /// The maximum number of Flows per page.
    /// </summary>
    public int? Limit { get; init; }

    /// <summary>
    /// The cursor of the page after which to start.
    /// </summary>
    public string? After { get; init; }

    /// <summary>
    /// The cursor of the page before which to end.
    /// </summary>
    public string? Before { get; init; }
}
