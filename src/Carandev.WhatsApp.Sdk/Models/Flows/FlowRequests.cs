using System.Text.Json.Serialization;

namespace Carandev.WhatsApp.Sdk.Models.Flows;

/// <summary>
/// The body of <c>POST /{waba-id}/flows</c>, sent by <c>IWhatsAppFlowsClient.CreateFlowAsync</c>.
/// Optional members left <see langword="null"/> are omitted from the request.
/// </summary>
public sealed record CreateFlowRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateFlowRequest"/> class.
    /// </summary>
    /// <param name="name">The Flow name. Must not be null, empty, or whitespace.</param>
    /// <param name="categories">The Flow's categories. At least one, and never <see cref="FlowCategory.Unknown"/>.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="name"/> is null, empty, or whitespace; or <paramref name="categories"/> is
    /// empty or contains <see cref="FlowCategory.Unknown"/>.
    /// </exception>
    /// <exception cref="ArgumentNullException"><paramref name="categories"/> is null.</exception>
    public CreateFlowRequest(string name, IReadOnlyList<FlowCategory> categories)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        FlowRequestValidation.ThrowIfInvalidCategories(categories, nameof(categories));
        Name = name;
        Categories = categories;
    }

    /// <summary>
    /// The Flow name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; }

    /// <summary>
    /// The Flow's categories.
    /// </summary>
    [JsonPropertyName("categories")]
    public IReadOnlyList<FlowCategory> Categories { get; }

    /// <summary>
    /// The Flow JSON, as a string. Can also be uploaded later with <c>UploadFlowJsonAsync</c>.
    /// </summary>
    [JsonPropertyName("flow_json")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FlowJson { get; init; }

    /// <summary>
    /// Whether to publish the Flow right away. Only takes effect when <see cref="FlowJson"/> is
    /// valid.
    /// </summary>
    [JsonPropertyName("publish")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Publish { get; init; }

    /// <summary>
    /// The id of an existing Flow to copy. Published Flows cannot be edited, so cloning is how to
    /// change one.
    /// </summary>
    [JsonPropertyName("clone_flow_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CloneFlowId { get; init; }

    /// <summary>
    /// The URL of the Flow's data endpoint (Flow JSON 3.0 and later).
    /// </summary>
    [JsonPropertyName("endpoint_uri")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Uri? EndpointUri { get; init; }
}

/// <summary>
/// The body of <c>POST /{flow-id}</c>, sent by <c>IWhatsAppFlowsClient.UpdateFlowMetadataAsync</c>.
/// Members left <see langword="null"/> are omitted and keep their current value; at least one
/// must be set.
/// </summary>
public sealed record UpdateFlowMetadataRequest
{
    /// <summary>
    /// The new Flow name.
    /// </summary>
    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; init; }

    /// <summary>
    /// The new categories. At least one, and never <see cref="FlowCategory.Unknown"/>.
    /// </summary>
    [JsonPropertyName("categories")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<FlowCategory>? Categories { get; init; }

    /// <summary>
    /// The new URL of the Flow's data endpoint.
    /// </summary>
    [JsonPropertyName("endpoint_uri")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Uri? EndpointUri { get; init; }

    /// <summary>
    /// The id of the Meta app the Flow's endpoint belongs to.
    /// </summary>
    [JsonPropertyName("application_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ApplicationId { get; init; }

    internal void Validate(string paramName)
    {
        if (Name is null && Categories is null && EndpointUri is null && ApplicationId is null)
        {
            throw new ArgumentException("At least one of Name, Categories, EndpointUri, or ApplicationId must be set.", paramName);
        }

        if (Name is not null && string.IsNullOrWhiteSpace(Name))
        {
            throw new ArgumentException("Name must not be empty or whitespace.", paramName);
        }

        if (Categories is not null)
        {
            FlowRequestValidation.ThrowIfInvalidCategories(Categories, paramName);
        }
    }
}

internal static class FlowRequestValidation
{
    public static void ThrowIfInvalidCategories(IReadOnlyList<FlowCategory> categories, string paramName)
    {
        ArgumentNullException.ThrowIfNull(categories, paramName);
        if (categories.Count == 0)
        {
            throw new ArgumentException("At least one category is required.", paramName);
        }

        if (categories.Contains(FlowCategory.Unknown))
        {
            throw new ArgumentException("FlowCategory.Unknown cannot be sent to Meta.", paramName);
        }
    }
}
