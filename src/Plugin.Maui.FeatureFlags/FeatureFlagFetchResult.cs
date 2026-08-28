namespace Plugin.Maui.FeatureFlags;

/// <summary>
/// Outcome of a remote fetch.
/// </summary>
public readonly struct FeatureFlagFetchResult
{
    FeatureFlagFetchResult(bool notModified, FeatureFlagSnapshot? snapshot)
    {
        NotModified = notModified;
        Snapshot = snapshot;
    }

    /// <summary>
    /// The remote document has not changed (for example HTTP 304).
    /// </summary>
    public bool NotModified { get; }

    /// <summary>
    /// The fetched snapshot, when one was returned.
    /// </summary>
    public FeatureFlagSnapshot? Snapshot { get; }

    /// <summary>
    /// The server reported no change.
    /// </summary>
    public static FeatureFlagFetchResult Unchanged { get; } = new(true, null);

    /// <summary>
    /// Wraps a newly fetched snapshot.
    /// </summary>
    public static FeatureFlagFetchResult From(FeatureFlagSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return new FeatureFlagFetchResult(false, snapshot);
    }
}
