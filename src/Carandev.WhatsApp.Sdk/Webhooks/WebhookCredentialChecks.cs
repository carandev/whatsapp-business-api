namespace Carandev.WhatsApp.Sdk.Webhooks;

/// <summary>
/// The explicit-credential webhook checks shared by <see cref="WhatsAppWebhookProcessor"/> and the
/// default implementations on <see cref="IWhatsAppWebhookProcessor"/>.
/// </summary>
internal static class WebhookCredentialChecks
{
    public static string? VerifySubscription(string? verifyToken, string? challenge, string expectedVerifyToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(expectedVerifyToken);
        return string.Equals(verifyToken, expectedVerifyToken, StringComparison.Ordinal) ? challenge ?? string.Empty : null;
    }

    public static bool IsValidSignature(string rawBody, string? signatureHeader, string appSecret)
    {
        ArgumentNullException.ThrowIfNull(rawBody);
        ArgumentException.ThrowIfNullOrEmpty(appSecret);
        return new WhatsAppSignatureValidator(appSecret).Verify(rawBody, signatureHeader);
    }
}
