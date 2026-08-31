using Microsoft.Extensions.Options;

namespace WhatsAppBusinessApi.Api.Configuration;

/// <summary>
/// Fail-fast validation for <see cref="WebhookOptions"/>, run at host startup
/// via <c>ValidateOnStart()</c>. Requires <see cref="WebhookOptions.VerifyToken"/>
/// and <see cref="WebhookOptions.AppSecret"/> to be present and non-empty.
/// </summary>
public sealed class WebhookOptionsValidator : IValidateOptions<WebhookOptions>
{
    public ValidateOptionsResult Validate(string? name, WebhookOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.VerifyToken))
        {
            failures.Add($"{WebhookOptions.SectionName}:{nameof(WebhookOptions.VerifyToken)} is required and was not found or empty.");
        }

        if (string.IsNullOrWhiteSpace(options.AppSecret))
        {
            failures.Add($"{WebhookOptions.SectionName}:{nameof(WebhookOptions.AppSecret)} is required and was not found or empty.");
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
