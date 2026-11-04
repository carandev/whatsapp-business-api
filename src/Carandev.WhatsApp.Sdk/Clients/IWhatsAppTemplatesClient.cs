using Carandev.WhatsApp.Sdk.Models.Templates;

namespace Carandev.WhatsApp.Sdk.Clients;

/// <summary>
/// Client for the WhatsApp Cloud API message templates resource: create, list, fetch, update, and
/// delete WABA-scoped message templates. Every method resolves its target WABA id from a per-call
/// <c>wabaIdOverride</c> or, when omitted, <see cref="WhatsAppOptions.WabaId"/>. All methods are
/// non-throwing for expected failure modes — outcomes are reported via
/// <see cref="Result"/>/<see cref="Result{T}"/>, never an exception.
/// </summary>
public interface IWhatsAppTemplatesClient
{
    /// <summary>
    /// Creates a message template via <c>POST /{waba_id}/message_templates</c>.
    /// </summary>
    /// <param name="request">The validated template creation request.</param>
    /// <param name="wabaIdOverride">
    /// An optional WABA id that takes precedence over <see cref="WhatsAppOptions.WabaId"/> when
    /// resolving the target WABA. When omitted, <see cref="WhatsAppOptions.WabaId"/> is used.
    /// </param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> wrapping the created <see cref="Template"/> on success;
    /// a failed <see cref="Result{T}"/> when the WABA id cannot be resolved or the API call fails.
    /// </returns>
    Task<Result<Template>> CreateAsync(CreateTemplateRequest request, string? wabaIdOverride = null, CancellationToken ct = default);

    /// <summary>
    /// Lists message templates via <c>GET /{waba_id}/message_templates</c>, with optional filters
    /// and cursor-based pagination.
    /// </summary>
    /// <param name="options">Optional name/status/language filters and pagination parameters.</param>
    /// <param name="wabaIdOverride">
    /// An optional WABA id that takes precedence over <see cref="WhatsAppOptions.WabaId"/> when
    /// resolving the target WABA. When omitted, <see cref="WhatsAppOptions.WabaId"/> is used.
    /// </param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> wrapping a <see cref="TemplatesPage{T}"/> on success; a
    /// failed <see cref="Result{T}"/> when the WABA id cannot be resolved or the API call fails.
    /// </returns>
    Task<Result<TemplatesPage<Template>>> ListAsync(TemplatesListOptions? options = null, string? wabaIdOverride = null, CancellationToken ct = default);

    /// <summary>
    /// Fetches a single message template by id via <c>GET /{templateId}</c>.
    /// </summary>
    /// <param name="templateId">The template's Meta-assigned identifier.</param>
    /// <param name="wabaIdOverride">
    /// An optional WABA id that takes precedence over <see cref="WhatsAppOptions.WabaId"/> when
    /// resolving the target WABA. When omitted, <see cref="WhatsAppOptions.WabaId"/> is used.
    /// </param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> wrapping the full <see cref="Template"/> on success; a
    /// failed <see cref="Result{T}"/> (including a not-found error) when the WABA id cannot be
    /// resolved or the API call fails.
    /// </returns>
    Task<Result<Template>> GetAsync(string templateId, string? wabaIdOverride = null, CancellationToken ct = default);

    /// <summary>
    /// Updates a message template's category and/or components via <c>POST /{templateId}</c>.
    /// </summary>
    /// <param name="templateId">The template's Meta-assigned identifier.</param>
    /// <param name="request">The validated update request.</param>
    /// <param name="wabaIdOverride">
    /// An optional WABA id that takes precedence over <see cref="WhatsAppOptions.WabaId"/> when
    /// resolving the target WABA. When omitted, <see cref="WhatsAppOptions.WabaId"/> is used.
    /// </param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> wrapping the updated <see cref="Template"/> on success;
    /// a failed <see cref="Result{T}"/> when the WABA id cannot be resolved or the API call fails.
    /// </returns>
    Task<Result<Template>> UpdateAsync(string templateId, UpdateTemplateRequest request, string? wabaIdOverride = null, CancellationToken ct = default);

    /// <summary>
    /// Deletes a message template via <c>DELETE /{waba_id}/message_templates</c>. Meta requires
    /// both the template id and its exact name; there is no name-only bulk delete.
    /// </summary>
    /// <param name="templateId">The template's Meta-assigned identifier.</param>
    /// <param name="name">The template's exact name.</param>
    /// <param name="wabaIdOverride">
    /// An optional WABA id that takes precedence over <see cref="WhatsAppOptions.WabaId"/> when
    /// resolving the target WABA. When omitted, <see cref="WhatsAppOptions.WabaId"/> is used.
    /// </param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>
    /// A successful <see cref="Result"/> on success; a failed <see cref="Result"/> when
    /// <paramref name="name"/> is missing, the WABA id cannot be resolved, or the API call fails.
    /// </returns>
    Task<Result> DeleteAsync(string templateId, string name, string? wabaIdOverride = null, CancellationToken ct = default);
}
