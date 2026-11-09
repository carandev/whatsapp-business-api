using System.Text.Json.Serialization;

namespace Carandev.WhatsApp.Sdk.Models.Webhooks;

/// <summary>
/// An app subscribed to a WhatsApp Business Account's webhooks, as returned by
/// <c>IWhatsAppWebhooksClient.ListSubscribedAppsAsync</c>.
/// </summary>
public sealed class SubscribedApp
{
    /// <summary>
    /// The subscribed app (<c>whatsapp_business_api_data</c>).
    /// </summary>
    [JsonPropertyName("whatsapp_business_api_data")]
    public SubscribedAppData? App { get; set; }

    /// <summary>
    /// The WABA's callback URL override for this app, or null when webhooks go to the app's own
    /// callback.
    /// </summary>
    [JsonPropertyName("override_callback_uri")]
    public string? OverrideCallbackUri { get; set; }
}

/// <summary>
/// The app details of a <see cref="SubscribedApp"/>.
/// </summary>
public sealed class SubscribedAppData
{
    /// <summary>
    /// The Meta app id.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// The app's link.
    /// </summary>
    [JsonPropertyName("link")]
    public string? Link { get; set; }

    /// <summary>
    /// The app's name.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

/// <summary>
/// Wire body of <c>POST /{waba-id}/subscribed_apps</c> with a callback override, built by
/// <c>IWhatsAppWebhooksClient.SubscribeAppAsync</c>.
/// </summary>
public sealed record SubscribeAppRequest
{
    internal SubscribeAppRequest(string overrideCallbackUri, string verifyToken)
    {
        OverrideCallbackUri = overrideCallbackUri;
        VerifyToken = verifyToken;
    }

    /// <summary>
    /// The callback URL that receives this WABA's webhooks instead of the app's callback.
    /// </summary>
    [JsonPropertyName("override_callback_uri")]
    public string OverrideCallbackUri { get; }

    /// <summary>
    /// The token Meta sends to the callback to verify it.
    /// </summary>
    [JsonPropertyName("verify_token")]
    public string VerifyToken { get; }
}
