namespace Plugin.Maui.FeatureFlags;

/// <summary>
/// Inputs passed to <see cref="IFeatureFlagProvider.FetchAsync"/>.
/// </summary>
public sealed class FeatureFlagRefreshRequest
{
    /// <summary>
    /// ETag from the last successful fetch, if any.
    /// </summary>
    public string? ETag { get; init; }

    /// <summary>
    /// Current evaluation environment.
    /// </summary>
    public FeatureFlagEnvironment Environment { get; init; }

    /// <summary>
    /// Current device / user context. Custom providers may send this to the server.
    /// </summary>
    public required FeatureFlagContext Context { get; init; }
}
