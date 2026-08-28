namespace Plugin.Maui.FeatureFlags.Tests;

public sealed class FeatureFlagsTests
{
    [Fact]
    public async Task IsEnabled_and_IsEnabledAsync_agree()
    {
        var provider = new StaticFeatureFlagProvider(new FeatureFlagSnapshot
        {
            Flags = [Harness.Flag("new_voip_engine")]
        });
        var (flags, _, _, _) = Harness.Create(provider: provider);
        flags.Start();
        await flags.RefreshAsync();

        Assert.True(flags.IsEnabled("new_voip_engine"));
        Assert.True(await flags.IsEnabledAsync("new_voip_engine"));
    }

    [Fact]
    public async Task Local_fallback_is_used_when_remote_omits_the_key()
    {
        var provider = new StaticFeatureFlagProvider(new FeatureFlagSnapshot
        {
            Flags = [Harness.Flag("new_checkout")]
        });
        var (flags, _, _, _) = Harness.Create(options =>
        {
            options.LocalFlags["dark_mode"] = true;
        }, provider);

        flags.Start();
        await flags.RefreshAsync();

        Assert.True(flags.IsEnabled("new_checkout"));
        Assert.True(flags.IsEnabled("dark_mode"));
        Assert.Equal(FeatureFlagReason.LocalFallback, flags.Evaluate("dark_mode").Reason);
    }

    [Fact]
    public async Task Identify_changes_user_targeting()
    {
        var provider = new StaticFeatureFlagProvider(new FeatureFlagSnapshot
        {
            Flags = [Harness.Flag("beta_chat", flag => flag.UserIds.Add("user-42"))]
        });
        var (flags, _, _, _) = Harness.Create(provider: provider);
        flags.Start();
        await flags.RefreshAsync();

        Assert.False(flags.IsEnabled("beta_chat"));

        flags.Identify("user-42", "IN");
        Assert.True(flags.IsEnabled("beta_chat"));
        Assert.Equal("user-42", flags.GetContext().UserId);
        Assert.Equal("IN", flags.GetContext().Country);

        flags.ClearIdentity();
        Assert.False(flags.IsEnabled("beta_chat"));
    }

    [Fact]
    public async Task SetEnvironment_re_evaluates_without_a_fetch()
    {
        var provider = new StaticFeatureFlagProvider(new FeatureFlagSnapshot
        {
            Flags = [Harness.Flag("prod_only", flag => flag.Environments.Add("Production"))]
        });
        var (flags, _, _, _) = Harness.Create(provider: provider);
        flags.Start();
        await flags.RefreshAsync();

        Assert.True(flags.IsEnabled("prod_only"));

        flags.SetEnvironment(FeatureFlagEnvironment.Development);
        Assert.False(flags.IsEnabled("prod_only"));
        Assert.Equal(FeatureFlagReason.EnvironmentMismatch, flags.Evaluate("prod_only").Reason);
    }

    [Fact]
    public void Override_can_force_a_kill_switch_on_for_qa()
    {
        var (flags, _, _, _) = Harness.Create(options =>
        {
            options.LocalFlags["new_checkout"] = false;
            options.KillSwitches.Add("new_checkout");
        });
        flags.Start();

        Assert.False(flags.IsEnabled("new_checkout"));

        flags.SetOverride("new_checkout", true);
        Assert.True(flags.IsEnabled("new_checkout"));
        Assert.Equal(FeatureFlagReason.Override, flags.Evaluate("new_checkout").Reason);

        flags.ClearOverride("new_checkout");
        Assert.False(flags.IsEnabled("new_checkout"));
    }

    [Fact]
    public async Task Refresh_writes_the_offline_cache()
    {
        var provider = new StaticFeatureFlagProvider(new FeatureFlagSnapshot
        {
            ETag = "\"v2\"",
            Flags = [Harness.Flag("new_voip_engine")]
        });
        var (flags, _, _, cache) = Harness.Create(provider: provider);
        flags.Start();
        await flags.RefreshAsync();

        Assert.Equal(FeatureFlagSource.Remote, flags.SnapshotSource);
        Assert.NotNull(cache.Stored);
        Assert.NotNull(cache.Stored!.Find("new_voip_engine"));
    }

    [Fact]
    public async Task Failed_refresh_keeps_the_cached_snapshot()
    {
        var cache = new MemoryCache
        {
            Stored = new FeatureFlagSnapshot
            {
                Flags = [Harness.Flag("cached_ok")]
            }
        };
        var (flags, _, _, _) = Harness.Create(options =>
        {
            options.Cache = cache;
            options.Provider = new ThrowingProvider();
        });
        flags.Start();

        Assert.True(flags.IsEnabled("cached_ok"));
        await Assert.ThrowsAsync<HttpRequestException>(() => flags.RefreshAsync());
        Assert.True(flags.IsEnabled("cached_ok"));
        Assert.Equal(FeatureFlagSource.Cache, flags.SnapshotSource);
    }

    [Fact]
    public async Task EvaluateAll_includes_snapshot_local_and_override_keys()
    {
        var provider = new StaticFeatureFlagProvider(new FeatureFlagSnapshot
        {
            Flags = [Harness.Flag("remote_a")]
        });
        var (flags, _, _, _) = Harness.Create(options =>
        {
            options.LocalFlags["local_b"] = true;
        }, provider);
        flags.Start();
        await flags.RefreshAsync();
        flags.SetOverride("override_c", false);

        var keys = flags.EvaluateAll().Select(item => item.Key).ToArray();
        Assert.Contains("remote_a", keys);
        Assert.Contains("local_b", keys);
        Assert.Contains("override_c", keys);
    }

    [Fact]
    public void Static_facade_uses_set_default()
    {
        var (flags, _, _, _) = Harness.Create(options => options.LocalFlags["new_checkout"] = true);
        flags.Start();
        FeatureFlags.SetDefault(flags);

        Assert.True(FeatureFlags.IsEnabled("new_checkout"));
    }

    sealed class ThrowingProvider : IFeatureFlagProvider
    {
        public Task<FeatureFlagFetchResult> FetchAsync(FeatureFlagRefreshRequest request, CancellationToken cancellationToken = default) =>
            throw new HttpRequestException("offline");
    }
}
