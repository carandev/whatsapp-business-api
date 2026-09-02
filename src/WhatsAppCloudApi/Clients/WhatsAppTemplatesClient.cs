namespace WhatsAppCloudApi.Clients;

/// <inheritdoc cref="IWhatsAppTemplatesClient" />
public sealed class WhatsAppTemplatesClient : IWhatsAppTemplatesClient
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WhatsAppTemplatesClient"/> class.
    /// </summary>
    /// <param name="client">
    /// The low-level HTTP client this resource client will use once its methods are implemented.
    /// Unused for now — wired here so future issues only add methods, never restructure DI.
    /// </param>
    public WhatsAppTemplatesClient(IWhatsAppClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
        _ = client;
    }
}
