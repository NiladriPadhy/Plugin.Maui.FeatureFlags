namespace Plugin.Maui.FeatureFlags;

/// <summary>
/// A versioned set of flag definitions, typically fetched remotely and cached offline.
/// </summary>
public sealed class FeatureFlagSnapshot
{
    /// <summary>
    /// Schema version. Defaults to 1.
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Optional environment label from the remote document.
    /// </summary>
    public string? Environment { get; set; }

    /// <summary>
    /// HTTP ETag from the last successful fetch, used for <c>If-None-Match</c>.
    /// </summary>
    public string? ETag { get; set; }

    /// <summary>
    /// When this snapshot was fetched or loaded.
    /// </summary>
    public DateTimeOffset FetchedAt { get; set; }

    /// <summary>
    /// Flag definitions in this snapshot.
    /// </summary>
    public List<FeatureFlagDefinition> Flags { get; set; } = [];

    /// <summary>
    /// Finds a definition by key (case-insensitive).
    /// </summary>
    public FeatureFlagDefinition? Find(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        foreach (var flag in Flags)
        {
            if (string.Equals(flag.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                return flag;
            }
        }

        return null;
    }
}
