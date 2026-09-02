namespace WhatsAppCloudApi.Clients;

/// <inheritdoc cref="IWhatsAppMessagesClient" />
public sealed class WhatsAppMessagesClient : IWhatsAppMessagesClient
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WhatsAppMessagesClient"/> class.
    /// </summary>
    /// <param name="client">
    /// The low-level HTTP client this resource client will use once its methods are implemented.
    /// Unused for now — wired here so future issues only add methods, never restructure DI.
    /// </param>
    public WhatsAppMessagesClient(IWhatsAppClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
        _ = client;
    }
}
