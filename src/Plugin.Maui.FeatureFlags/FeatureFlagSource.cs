namespace Plugin.Maui.FeatureFlags;

/// <summary>
/// Where the definition used for an evaluation came from.
/// </summary>
public enum FeatureFlagSource
{
    /// <summary>
    /// No definition was found; <see cref="FeatureFlagsOptions.DefaultWhenUnknown"/> was used.
    /// </summary>
    Default,

    /// <summary>
    /// A local definition or boolean fallback.
    /// </summary>
    Local,

    /// <summary>
    /// The last successfully persisted remote snapshot.
    /// </summary>
    Cache,

    /// <summary>
    /// A snapshot fetched from the remote provider.
    /// </summary>
    Remote,

    /// <summary>
    /// A runtime override.
    /// </summary>
    Override
}
