namespace Plugin.Maui.FeatureFlags;

/// <summary>
/// In-memory provider for tests, samples, and fully local configuration.
/// </summary>
public sealed class StaticFeatureFlagProvider : IFeatureFlagProvider
{
    readonly Func<FeatureFlagRefreshRequest, FeatureFlagSnapshot> _factory;

    /// <summary>
    /// Serves a fixed snapshot on every fetch.
    /// </summary>
    public StaticFeatureFlagProvider(FeatureFlagSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        _factory = _ => Clone(snapshot);
    }

    /// <summary>
    /// Serves a snapshot built from <paramref name="factory"/> on every fetch.
    /// </summary>
    public StaticFeatureFlagProvider(Func<FeatureFlagRefreshRequest, FeatureFlagSnapshot> factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    /// <inheritdoc />
    public Task<FeatureFlagFetchResult> FetchAsync(FeatureFlagRefreshRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(FeatureFlagFetchResult.From(_factory(request)));
    }

    static FeatureFlagSnapshot Clone(FeatureFlagSnapshot snapshot) => new()
    {
        Version = snapshot.Version,
        Environment = snapshot.Environment,
        ETag = snapshot.ETag,
        FetchedAt = snapshot.FetchedAt,
        Flags = [.. snapshot.Flags]
    };
}
