namespace WhatsAppCloudApi.Clients;

/// <inheritdoc cref="IWhatsAppPhoneNumbersClient" />
public sealed class WhatsAppPhoneNumbersClient : IWhatsAppPhoneNumbersClient
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WhatsAppPhoneNumbersClient"/> class.
    /// </summary>
    /// <param name="client">
    /// The low-level HTTP client this resource client will use once its methods are implemented.
    /// Unused for now — wired here so future issues only add methods, never restructure DI.
    /// </param>
    public WhatsAppPhoneNumbersClient(IWhatsAppClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
        _ = client;
    }
}
