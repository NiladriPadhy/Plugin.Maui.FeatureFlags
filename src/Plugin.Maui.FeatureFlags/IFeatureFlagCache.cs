namespace Plugin.Maui.FeatureFlags;

/// <summary>
/// Offline persistence for the last successful snapshot.
/// </summary>
public interface IFeatureFlagCache
{
    /// <summary>
    /// Loads the last saved snapshot, or <c>null</c> when none exists.
    /// </summary>
    FeatureFlagSnapshot? Load();

    /// <summary>
    /// Replaces the persisted snapshot.
    /// </summary>
    void Save(FeatureFlagSnapshot snapshot);

    /// <summary>
    /// Deletes the persisted snapshot.
    /// </summary>
    void Clear();
}
