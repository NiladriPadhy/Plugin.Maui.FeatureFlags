namespace Plugin.Maui.FeatureFlags;

/// <summary>
/// Raised after the in-memory snapshot is replaced.
/// </summary>
public sealed class FeatureFlagsChangedEventArgs : EventArgs
{
    /// <summary>
    /// Creates event arguments for a snapshot update.
    /// </summary>
    public FeatureFlagsChangedEventArgs(FeatureFlagSnapshot snapshot, FeatureFlagSource source)
    {
        Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        Source = source;
    }

    /// <summary>
    /// The snapshot now in use.
    /// </summary>
    public FeatureFlagSnapshot Snapshot { get; }

    /// <summary>
    /// Whether the snapshot came from remote, cache, or local definitions.
    /// </summary>
    public FeatureFlagSource Source { get; }
}
