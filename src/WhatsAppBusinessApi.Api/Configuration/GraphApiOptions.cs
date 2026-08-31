namespace WhatsAppBusinessApi.Api.Configuration;

/// <summary>
/// Meta Graph API credentials and endpoint configuration.
/// Bound from the "GraphApi" configuration section.
/// </summary>
public sealed class GraphApiOptions
{
    /// <summary>
    /// Configuration section name this type binds to.
    /// </summary>
    public const string SectionName = "GraphApi";

    /// <summary>
    /// WhatsApp Business phone number ID used in the Graph API messages endpoint.
    /// </summary>
    public string PhoneNumberId { get; set; } = string.Empty;

    /// <summary>
    /// Bearer access token used to authenticate calls to the Graph API.
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Base URL for the Graph API. Has a sensible code default and is not
    /// required to be present in configuration.
    /// </summary>
    public string ApiBaseUrl { get; set; } = "https://graph.facebook.com/v21.0";
}
