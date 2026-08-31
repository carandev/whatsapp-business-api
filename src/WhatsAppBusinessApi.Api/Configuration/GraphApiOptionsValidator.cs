using Microsoft.Extensions.Options;

namespace WhatsAppBusinessApi.Api.Configuration;

/// <summary>
/// Fail-fast validation for <see cref="GraphApiOptions"/>, run at host startup
/// via <c>ValidateOnStart()</c>. Requires <see cref="GraphApiOptions.PhoneNumberId"/>
/// and <see cref="GraphApiOptions.AccessToken"/> to be present and non-empty.
/// </summary>
public sealed class GraphApiOptionsValidator : IValidateOptions<GraphApiOptions>
{
    public ValidateOptionsResult Validate(string? name, GraphApiOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.PhoneNumberId))
        {
            failures.Add($"{GraphApiOptions.SectionName}:{nameof(GraphApiOptions.PhoneNumberId)} is required and was not found or empty.");
        }

        if (string.IsNullOrWhiteSpace(options.AccessToken))
        {
            failures.Add($"{GraphApiOptions.SectionName}:{nameof(GraphApiOptions.AccessToken)} is required and was not found or empty.");
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
