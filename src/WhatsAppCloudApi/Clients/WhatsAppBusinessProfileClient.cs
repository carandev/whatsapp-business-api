namespace WhatsAppCloudApi.Clients;

/// <inheritdoc cref="IWhatsAppBusinessProfileClient" />
public sealed class WhatsAppBusinessProfileClient : IWhatsAppBusinessProfileClient
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WhatsAppBusinessProfileClient"/> class.
    /// </summary>
    /// <param name="client">
    /// The low-level HTTP client this resource client will use once its methods are implemented.
    /// Unused for now — wired here so future issues only add methods, never restructure DI.
    /// </param>
    public WhatsAppBusinessProfileClient(IWhatsAppClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
        _ = client;
    }
}
