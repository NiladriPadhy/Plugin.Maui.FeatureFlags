namespace Plugin.Maui.FeatureFlags;

/// <summary>
/// Entry point for feature flags when dependency injection is not used.
/// </summary>
public static class FeatureFlags
{
    static IFeatureFlags? _current;

    /// <summary>
    /// Gets the shared <see cref="IFeatureFlags"/> instance.
    /// </summary>
    public static IFeatureFlags Current => _current ??= Create(new FeatureFlagsOptions());

    /// <summary>
    /// Evaluates <paramref name="key"/> against the last known snapshot. Never waits on the network.
    /// </summary>
    /// <example>
    /// <code>
    /// if (FeatureFlags.IsEnabled("new_checkout"))
    /// {
    ///     // ...
    /// }
    /// </code>
    /// </example>
    public static bool IsEnabled(string key) => Current.IsEnabled(key);

    /// <summary>
    /// Ensures a snapshot is loaded, then evaluates <paramref name="key"/>.
    /// </summary>
    /// <example>
    /// <code>
    /// var enabled = await FeatureFlags.IsEnabledAsync("new_voip_engine");
    /// </code>
    /// </example>
    public static Task<bool> IsEnabledAsync(string key, CancellationToken cancellationToken = default) =>
        Current.IsEnabledAsync(key, cancellationToken);

    /// <summary>
    /// Evaluates <paramref name="key"/> and returns the reason, source, and rollout bucket.
    /// </summary>
    public static FeatureFlagEvaluation Evaluate(string key) => Current.Evaluate(key);

    /// <summary>
    /// Ensures a snapshot is loaded, then evaluates <paramref name="key"/>.
    /// </summary>
    public static Task<FeatureFlagEvaluation> EvaluateAsync(string key, CancellationToken cancellationToken = default) =>
        Current.EvaluateAsync(key, cancellationToken);

    /// <summary>
    /// Sets the current user for targeting and sticky rollout.
    /// </summary>
    public static void Identify(string userId, string? country = null) => Current.Identify(userId, country);

    /// <summary>
    /// Sets the current user for targeting and sticky rollout.
    /// </summary>
    public static void Identify(FeatureFlagUser user) => Current.Identify(user);

    /// <summary>
    /// Clears the identified user.
    /// </summary>
    public static void ClearIdentity() => Current.ClearIdentity();

    /// <summary>
    /// Fetches remote configuration and refreshes the offline cache.
    /// </summary>
    public static Task RefreshAsync(CancellationToken cancellationToken = default) =>
        Current.RefreshAsync(cancellationToken);

    /// <summary>
    /// Creates a feature-flag client with MAUI context collection and optional HTTP remote configuration.
    /// </summary>
    public static IFeatureFlags Create(FeatureFlagsOptions? options = null)
    {
        options ??= new FeatureFlagsOptions();
        var directory = StoragePath.Resolve(options);
        var cache = options.Cache ?? new FileFeatureFlagCache(directory);
        var context = options.ContextProvider ?? CreateContextProvider(options, directory);
        var (provider, ownedHttp) = ResolveProvider(options);
        return new FeatureFlagsImplementation(options, SystemClock.Instance, context, provider, cache, ownedHttp);
    }

    /// <summary>
    /// Replaces the shared instance. Intended for tests and custom implementations.
    /// </summary>
    public static void SetDefault(IFeatureFlags implementation) =>
        _current = implementation ?? throw new ArgumentNullException(nameof(implementation));

    internal static FeatureFlagsImplementation Create(
        FeatureFlagsOptions options,
        IClock clock,
        IFeatureFlagContextProvider context,
        IFeatureFlagProvider? provider,
        IFeatureFlagCache cache) =>
        new(options, clock, context, provider, cache, ownedHttp: null);

    static IFeatureFlagContextProvider CreateContextProvider(FeatureFlagsOptions options, string directory)
    {
#if ANDROID || IOS
        return new MauiFeatureFlagContextProvider(options, directory);
#else
        return new FallbackFeatureFlagContextProvider(options);
#endif
    }

    static (IFeatureFlagProvider? Provider, HttpClient? OwnedHttp) ResolveProvider(FeatureFlagsOptions options)
    {
        if (options.Provider is not null)
        {
            return (options.Provider, null);
        }

        if (options.RemoteUri is null)
        {
            return (null, null);
        }

        if (options.HttpClient is not null)
        {
            return (new HttpFeatureFlagProvider(options.HttpClient, options.RemoteUri, options.ConfigureRequest), null);
        }

        var http = new HttpClient { Timeout = options.RequestTimeout };
        return (new HttpFeatureFlagProvider(http, options.RemoteUri, options.ConfigureRequest), http);
    }
}
