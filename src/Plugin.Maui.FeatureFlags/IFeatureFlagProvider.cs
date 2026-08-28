namespace Plugin.Maui.FeatureFlags;

/// <summary>
/// Fetches a feature-flag snapshot from a remote source.
/// </summary>
public interface IFeatureFlagProvider
{
    /// <summary>
    /// Downloads the latest snapshot. Return <see cref="FeatureFlagFetchResult.Unchanged"/> when the server reports 304.
    /// </summary>
    Task<FeatureFlagFetchResult> FetchAsync(FeatureFlagRefreshRequest request, CancellationToken cancellationToken = default);
}
