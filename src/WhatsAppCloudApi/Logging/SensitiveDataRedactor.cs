namespace WhatsAppCloudApi.Logging;

/// <summary>
/// Redacts sensitive values (tokens, credentials) from key/value data before it is logged.
/// </summary>
public static class SensitiveDataRedactor
{
    /// <summary>
    /// The mask written in place of a redacted value.
    /// </summary>
    public const string Mask = "***REDACTED***";

    private static readonly string[] SensitiveKeys = ["token", "access_token", "authorization"];

    /// <summary>
    /// Returns a copy of <paramref name="fields"/> where every value whose key matches
    /// <c>token</c>, <c>access_token</c>, or <c>Authorization</c> (case-insensitive) is replaced
    /// with <see cref="Mask"/>. All other entries pass through unchanged.
    /// </summary>
    /// <param name="fields">The key/value pairs to redact, e.g. HTTP headers or a JSON body's fields.</param>
    public static IReadOnlyDictionary<string, string?> Redact(IReadOnlyDictionary<string, string?> fields)
    {
        ArgumentNullException.ThrowIfNull(fields);

        var result = new Dictionary<string, string?>(fields.Count, StringComparer.Ordinal);
        foreach (var (key, value) in fields)
        {
            result[key] = IsSensitiveKey(key) ? Mask : value;
        }

        return result;
    }

    /// <summary>
    /// Determines whether <paramref name="key"/> is a sensitive field name that must be masked.
    /// </summary>
    public static bool IsSensitiveKey(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        foreach (var sensitiveKey in SensitiveKeys)
        {
            if (string.Equals(sensitiveKey, key, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
