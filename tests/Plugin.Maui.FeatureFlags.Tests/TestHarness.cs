namespace Plugin.Maui.FeatureFlags.Tests;

sealed class FakeClock : IClock
{
    public DateTimeOffset UtcNow { get; set; } = new(2026, 8, 28, 12, 0, 0, TimeSpan.Zero);

    public void Advance(TimeSpan duration) => UtcNow += duration;
}

sealed class FakeContext : IFeatureFlagContextProvider
{
    public FeatureFlagContext Context { get; set; } = new()
    {
        Environment = FeatureFlagEnvironment.Production,
        Platform = "Android",
        OsVersion = "16",
        AppVersion = "1.2.3",
        AppBuild = "45",
        DeviceId = "device-1",
        DeviceModel = "Pixel Test",
        DeviceManufacturer = "Acme",
        Country = "US"
    };

    public FeatureFlagContext Capture(FeatureFlagUser? user, FeatureFlagEnvironment environment) =>
        Context with
        {
            Environment = environment,
            UserId = user?.Id ?? Context.UserId,
            Country = user?.Country ?? Context.Country,
            Attributes = user?.Attributes ?? Context.Attributes
        };
}

sealed class MemoryCache : IFeatureFlagCache
{
    public FeatureFlagSnapshot? Stored { get; set; }

    public FeatureFlagSnapshot? Load() => Stored;

    public void Save(FeatureFlagSnapshot snapshot) => Stored = snapshot;

    public void Clear() => Stored = null;
}

static class Harness
{
    public static FeatureFlagDefinition Flag(string key, Action<FeatureFlagDefinition>? configure = null)
    {
        var definition = new FeatureFlagDefinition { Key = key, Enabled = true };
        configure?.Invoke(definition);
        return definition;
    }

    public static FeatureFlagEvaluation Evaluate(
        FeatureFlagDefinition definition,
        FeatureFlagContext? context = null,
        DateTimeOffset? now = null,
        bool defaultWhenUnknown = false,
        IReadOnlyDictionary<string, bool>? localFlags = null,
        IReadOnlyDictionary<string, bool>? overrides = null,
        IReadOnlySet<string>? killSwitches = null)
    {
        var snapshot = new FeatureFlagSnapshot
        {
            FetchedAt = now ?? new DateTimeOffset(2026, 8, 28, 12, 0, 0, TimeSpan.Zero),
            Flags = [definition]
        };

        return FeatureFlagEvaluator.Evaluate(
            definition.Key,
            context ?? new FakeContext().Context,
            snapshot,
            FeatureFlagSource.Remote,
            new Dictionary<string, FeatureFlagDefinition>(StringComparer.OrdinalIgnoreCase),
            localFlags ?? new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase),
            overrides ?? new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase),
            killSwitches ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            preferLocalDefinitions: false,
            defaultWhenUnknown,
            now ?? snapshot.FetchedAt);
    }

    public static (FeatureFlagsImplementation Flags, FakeClock Clock, FakeContext Context, MemoryCache Cache) Create(
        Action<FeatureFlagsOptions>? configure = null,
        IFeatureFlagProvider? provider = null)
    {
        var root = Directory.CreateTempSubdirectory("maui-featureflags-").FullName;
        var clock = new FakeClock();
        var context = new FakeContext();
        var cache = new MemoryCache();
        var options = new FeatureFlagsOptions
        {
            StorageDirectory = root,
            Environment = FeatureFlagEnvironment.Production,
            RefreshInterval = TimeSpan.Zero,
            DeviceId = "device-1",
            Country = "US",
            ContextProvider = context,
            Cache = cache,
            Provider = provider
        };
        configure?.Invoke(options);

        var flags = FeatureFlags.Create(options, clock, context, options.Provider, options.Cache ?? cache);
        return (flags, clock, context, cache);
    }
}
