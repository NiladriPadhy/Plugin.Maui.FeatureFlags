namespace Plugin.Maui.FeatureFlags;

/// <summary>
/// Mobile-first feature flags with local fallback, remote configuration, and MAUI targeting.
/// </summary>
public interface IFeatureFlags : IDisposable
{
    /// <summary>
    /// Always <c>true</c> for Android, iOS, and the shared <c>net10.0</c> surface.
    /// </summary>
    bool IsSupported { get; }

    /// <summary>
    /// Current evaluation environment.
    /// </summary>
    FeatureFlagEnvironment Environment { get; }

    /// <summary>
    /// Identified user, if any.
    /// </summary>
    FeatureFlagUser? User { get; }

    /// <summary>
    /// Last applied snapshot (remote, cache, or locally synthesized).
    /// </summary>
    FeatureFlagSnapshot? Snapshot { get; }

    /// <summary>
    /// Where <see cref="Snapshot"/> came from.
    /// </summary>
    FeatureFlagSource SnapshotSource { get; }

    /// <summary>
    /// Raised after the snapshot is replaced.
    /// </summary>
    event EventHandler<FeatureFlagsChangedEventArgs>? FlagsChanged;

    /// <summary>
    /// Loads the offline cache, applies local fallbacks, and starts periodic remote refresh.
    /// Safe to call more than once.
    /// </summary>
    void Start();

    /// <summary>
    /// Evaluates <paramref name="key"/> against the last known snapshot. Never waits on the network.
    /// </summary>
    bool IsEnabled(string key);

    /// <summary>
    /// Ensures a snapshot is loaded (cache or remote), then evaluates <paramref name="key"/>.
    /// </summary>
    Task<bool> IsEnabledAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluates <paramref name="key"/> and returns the reason, source, and rollout bucket.
    /// </summary>
    FeatureFlagEvaluation Evaluate(string key);

    /// <summary>
    /// Ensures a snapshot is loaded, then evaluates <paramref name="key"/>.
    /// </summary>
    Task<FeatureFlagEvaluation> EvaluateAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluates every known key (snapshot, local definitions, local fallbacks, and overrides).
    /// </summary>
    IReadOnlyList<FeatureFlagEvaluation> EvaluateAll();

    /// <summary>
    /// Sets the current user for targeting and sticky rollout.
    /// </summary>
    void Identify(string userId, string? country = null);

    /// <summary>
    /// Sets the current user for targeting and sticky rollout.
    /// </summary>
    void Identify(FeatureFlagUser user);

    /// <summary>
    /// Clears the identified user. Rollout falls back to the device id.
    /// </summary>
    void ClearIdentity();

    /// <summary>
    /// Changes the evaluation environment without fetching a new snapshot.
    /// </summary>
    void SetEnvironment(FeatureFlagEnvironment environment);

    /// <summary>
    /// Forces <paramref name="key"/> on or off for this process (QA / debug).
    /// </summary>
    void SetOverride(string key, bool enabled);

    /// <summary>
    /// Removes a runtime override.
    /// </summary>
    void ClearOverride(string key);

    /// <summary>
    /// Removes every runtime override.
    /// </summary>
    void ClearOverrides();

    /// <summary>
    /// Fetches remote configuration, updates the snapshot, and refreshes the offline cache.
    /// </summary>
    Task RefreshAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Captures the current device / user / environment context.
    /// </summary>
    FeatureFlagContext GetContext();
}
