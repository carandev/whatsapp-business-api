using Carandev.WhatsApp.Sdk.Models.Flows;
using Carandev.WhatsApp.Sdk.Models.Templates;

namespace Carandev.WhatsApp.Sdk.Clients;

/// <summary>
/// Client for managing WhatsApp Flows through the Graph API: <c>/{waba_id}/flows</c> and
/// <c>/{flow_id}</c>. A Flow is created as a draft, gets its Flow JSON uploaded as an asset, and
/// is published once it has no validation errors. Published Flows cannot be updated or deleted;
/// deprecate them, or clone them into a new draft. Failures from Meta are reported via
/// <see cref="Result"/>/<see cref="Result{T}"/>, never an exception; invalid arguments throw
/// before any HTTP request is sent.
/// </summary>
public interface IWhatsAppFlowsClient
{
    /// <summary>
    /// Creates a Flow, optionally with its Flow JSON, publishing it, or cloning another Flow, via
    /// <c>POST /{waba_id}/flows</c>.
    /// </summary>
    /// <param name="request">The Flow to create.</param>
    /// <param name="wabaIdOverride">
    /// An optional WABA id that takes precedence over <see cref="WhatsAppOptions.WabaId"/>.
    /// </param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>
    /// The new Flow's id and any Flow JSON validation errors; a failed result when the WABA id
    /// cannot be resolved (no request is sent) or the API call fails.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> is null.</exception>
    Task<Result<CreateFlowResponse>> CreateFlowAsync(CreateFlowRequest request, string? wabaIdOverride = null, CancellationToken ct = default);

    /// <summary>
    /// Lists a WABA's Flows via <c>GET /{waba_id}/flows</c>.
    /// </summary>
    /// <param name="options">Optional paging options.</param>
    /// <param name="wabaIdOverride">
    /// An optional WABA id that takes precedence over <see cref="WhatsAppOptions.WabaId"/>.
    /// </param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>
    /// A page of <see cref="Flow"/>; a failed result when the WABA id cannot be resolved (no
    /// request is sent) or the API call fails.
    /// </returns>
    Task<Result<TemplatesPage<Flow>>> ListFlowsAsync(FlowsListOptions? options = null, string? wabaIdOverride = null, CancellationToken ct = default);

    /// <summary>
    /// Gets a Flow via <c>GET /{flow_id}</c>.
    /// </summary>
    /// <param name="flowId">The Flow id.</param>
    /// <param name="fields">
    /// The fields to return, e.g. <c>json_version</c>, <c>data_api_version</c>,
    /// <c>endpoint_uri</c>, or <c>preview</c>. When omitted, Meta returns <c>id</c>, <c>name</c>,
    /// <c>status</c>, <c>categories</c>, and <c>validation_errors</c>.
    /// </param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>The Flow; a failed result when the API call fails.</returns>
    /// <exception cref="ArgumentException"><paramref name="flowId"/> is null, empty, or whitespace.</exception>
    Task<Result<Flow>> GetFlowAsync(string flowId, IReadOnlyList<string>? fields = null, CancellationToken ct = default);

    /// <summary>
    /// Updates a draft Flow's name, categories, endpoint, or app via <c>POST /{flow_id}</c>.
    /// </summary>
    /// <param name="flowId">The Flow id.</param>
    /// <param name="request">The metadata to change. At least one member must be set.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A successful result; a failed result when the API call fails.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="flowId"/> is null, empty, or whitespace; or <paramref name="request"/> sets
    /// nothing, an empty name, or invalid categories.
    /// </exception>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> is null.</exception>
    Task<Result> UpdateFlowMetadataAsync(string flowId, UpdateFlowMetadataRequest request, CancellationToken ct = default);

    /// <summary>
    /// Uploads a draft Flow's Flow JSON via <c>POST /{flow_id}/assets</c> (multipart
    /// <c>file</c>, <c>name=flow.json</c>, <c>asset_type=FLOW_JSON</c>). Meta stores the JSON
    /// even when it has validation errors.
    /// </summary>
    /// <param name="flowId">The Flow id.</param>
    /// <param name="flowJson">The Flow JSON document (up to 10 MB).</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>Any validation errors; a failed result when the API call fails.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="flowId"/> or <paramref name="flowJson"/> is null, empty, or whitespace.
    /// </exception>
    Task<Result<FlowJsonUploadResponse>> UploadFlowJsonAsync(string flowId, string flowJson, CancellationToken ct = default);

    /// <summary>
    /// Publishes a draft Flow via <c>POST /{flow_id}/publish</c>. Fails while the Flow has
    /// validation errors.
    /// </summary>
    /// <param name="flowId">The Flow id.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A successful result; a failed result when the API call fails.</returns>
    /// <exception cref="ArgumentException"><paramref name="flowId"/> is null, empty, or whitespace.</exception>
    Task<Result> PublishFlowAsync(string flowId, CancellationToken ct = default);

    /// <summary>
    /// Deprecates a published Flow via <c>POST /{flow_id}/deprecate</c>, so it can no longer be
    /// sent or opened.
    /// </summary>
    /// <param name="flowId">The Flow id.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A successful result; a failed result when the API call fails.</returns>
    /// <exception cref="ArgumentException"><paramref name="flowId"/> is null, empty, or whitespace.</exception>
    Task<Result> DeprecateFlowAsync(string flowId, CancellationToken ct = default);

    /// <summary>
    /// Deletes a draft Flow via <c>DELETE /{flow_id}</c>. Published Flows cannot be deleted.
    /// </summary>
    /// <param name="flowId">The Flow id.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A successful result; a failed result when the API call fails.</returns>
    /// <exception cref="ArgumentException"><paramref name="flowId"/> is null, empty, or whitespace.</exception>
    Task<Result> DeleteFlowAsync(string flowId, CancellationToken ct = default);

    /// <summary>
    /// Gets a web preview link for a Flow via
    /// <c>GET /{flow_id}?fields=preview.invalidate(&lt;invalidate&gt;)</c>. The link expires after
    /// 30 days.
    /// </summary>
    /// <param name="flowId">The Flow id.</param>
    /// <param name="invalidate">Whether to revoke the current link and generate a new one.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>The preview link; a failed result when the API call fails or returns no preview.</returns>
    /// <exception cref="ArgumentException"><paramref name="flowId"/> is null, empty, or whitespace.</exception>
    Task<Result<FlowPreview>> GetFlowPreviewAsync(string flowId, bool invalidate = false, CancellationToken ct = default);
}
