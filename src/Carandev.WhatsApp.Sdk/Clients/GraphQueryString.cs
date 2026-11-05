namespace Carandev.WhatsApp.Sdk.Clients;

/// <summary>
/// Builds Graph API query strings for the resource clients.
/// </summary>
internal static class GraphQueryString
{
    /// <summary>
    /// Builds <c>?key=value&amp;...</c> from the parameters whose value is non-null, escaping each
    /// value; returns an empty string when every value is null.
    /// </summary>
    public static string Build(IReadOnlyList<(string Key, string? Value)> parameters)
    {
        List<string>? segments = null;
        foreach (var (key, value) in parameters)
        {
            if (value is null)
            {
                continue;
            }

            segments ??= [];
            segments.Add($"{key}={Uri.EscapeDataString(value)}");
        }

        return segments is null ? string.Empty : "?" + string.Join('&', segments);
    }
}
