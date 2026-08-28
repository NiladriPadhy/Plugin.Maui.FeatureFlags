namespace Plugin.Maui.FeatureFlags;

/// <summary>
/// Default values used by <see cref="FeatureFlagsOptions"/>.
/// </summary>
public static class FeatureFlagsDefaults
{
    /// <summary>
    /// Folder name under app data for the offline cache.
    /// </summary>
    public const string StorageFolderName = "plugin.maui.featureflags";

    /// <summary>
    /// File name for the persisted snapshot.
    /// </summary>
    public const string CacheFileName = "feature-flags-cache.json";

    /// <summary>
    /// Preference key for the sticky anonymous device id.
    /// </summary>
    public const string DeviceIdKey = "plugin.maui.featureflags.deviceid";

    /// <summary>
    /// How often a configured remote provider is polled.
    /// </summary>
    public static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(15);

    /// <summary>
    /// HTTP timeout for remote configuration.
    /// </summary>
    public static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(10);
}
