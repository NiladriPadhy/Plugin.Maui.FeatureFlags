namespace Plugin.Maui.FeatureFlags.Tests;

public sealed class CacheTests
{
    [Fact]
    public void File_cache_round_trips_a_snapshot()
    {
        var directory = Directory.CreateTempSubdirectory("maui-featureflags-cache-").FullName;
        var cache = new FileFeatureFlagCache(directory);
        var snapshot = new FeatureFlagSnapshot
        {
            Version = 1,
            Environment = "Production",
            ETag = "\"abc\"",
            FetchedAt = new DateTimeOffset(2026, 8, 28, 12, 0, 0, TimeSpan.Zero),
            Flags =
            [
                new FeatureFlagDefinition
                {
                    Key = "new_voip_engine",
                    Enabled = true,
                    Percentage = 25,
                    Platforms = ["iOS", "Android"],
                    Countries = ["US", "IN"]
                }
            ]
        };

        cache.Save(snapshot);
        var loaded = cache.Load();

        Assert.NotNull(loaded);
        Assert.Equal("new_voip_engine", loaded!.Find("new_voip_engine")?.Key);
        Assert.Equal(25, loaded.Find("new_voip_engine")?.Percentage);
        Assert.Equal("\"abc\"", loaded.ETag);
    }

    [Fact]
    public void File_cache_returns_null_when_empty()
    {
        var directory = Directory.CreateTempSubdirectory("maui-featureflags-empty-").FullName;
        var cache = new FileFeatureFlagCache(directory);

        Assert.Null(cache.Load());
    }

    [Fact]
    public void Implementation_uses_offline_cache_before_remote()
    {
        var cached = new FeatureFlagSnapshot
        {
            FetchedAt = new DateTimeOffset(2026, 8, 28, 8, 0, 0, TimeSpan.Zero),
            Flags = [Harness.Flag("cached_flag")]
        };

        var (flags, _, _, cache) = Harness.Create();
        cache.Stored = cached;
        flags.Start();

        Assert.True(flags.IsEnabled("cached_flag"));
        Assert.Equal(FeatureFlagSource.Cache, flags.Evaluate("cached_flag").Source);
        Assert.Equal(FeatureFlagSource.Cache, flags.SnapshotSource);
    }
}
