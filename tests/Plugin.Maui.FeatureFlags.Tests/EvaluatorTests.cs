namespace Plugin.Maui.FeatureFlags.Tests;

public sealed class EvaluatorTests
{
    [Fact]
    public void Enabled_flag_matches()
    {
        var result = Harness.Evaluate(Harness.Flag("new_checkout"));

        Assert.True(result.Enabled);
        Assert.Equal(FeatureFlagReason.Matched, result.Reason);
        Assert.Equal(FeatureFlagSource.Remote, result.Source);
    }

    [Fact]
    public void Master_off_is_flag_off()
    {
        var result = Harness.Evaluate(Harness.Flag("new_checkout", flag => flag.Enabled = false));

        Assert.False(result.Enabled);
        Assert.Equal(FeatureFlagReason.FlagOff, result.Reason);
    }

    [Fact]
    public void Kill_switch_on_definition_wins()
    {
        var result = Harness.Evaluate(Harness.Flag("new_voip_engine", flag => flag.Killed = true));

        Assert.False(result.Enabled);
        Assert.Equal(FeatureFlagReason.KillSwitch, result.Reason);
    }

    [Fact]
    public void Options_kill_switch_wins_without_definition()
    {
        var result = FeatureFlagEvaluator.Evaluate(
            "broken_pay",
            new FakeContext().Context,
            snapshot: null,
            FeatureFlagSource.Default,
            new Dictionary<string, FeatureFlagDefinition>(),
            new Dictionary<string, bool>(),
            new Dictionary<string, bool>(),
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "broken_pay" },
            preferLocalDefinitions: false,
            defaultWhenUnknown: true,
            DateTimeOffset.UtcNow);

        Assert.False(result.Enabled);
        Assert.Equal(FeatureFlagReason.KillSwitch, result.Reason);
    }

    [Fact]
    public void Expired_flag_is_off()
    {
        var now = new DateTimeOffset(2026, 8, 28, 12, 0, 0, TimeSpan.Zero);
        var result = Harness.Evaluate(
            Harness.Flag("holiday_promo", flag => flag.ExpiresAt = now.AddMinutes(-1)),
            now: now);

        Assert.False(result.Enabled);
        Assert.Equal(FeatureFlagReason.Expired, result.Reason);
    }

    [Fact]
    public void Future_expiration_still_matches()
    {
        var now = new DateTimeOffset(2026, 8, 28, 12, 0, 0, TimeSpan.Zero);
        var result = Harness.Evaluate(
            Harness.Flag("holiday_promo", flag => flag.ExpiresAt = now.AddHours(1)),
            now: now);

        Assert.True(result.Enabled);
        Assert.Equal(FeatureFlagReason.Matched, result.Reason);
    }

    [Theory]
    [InlineData(FeatureFlagEnvironment.Production, "Prod", true)]
    [InlineData(FeatureFlagEnvironment.Production, "Production", true)]
    [InlineData(FeatureFlagEnvironment.Development, "Dev", true)]
    [InlineData(FeatureFlagEnvironment.Staging, "Production", false)]
    public void Environment_aliases_are_accepted(FeatureFlagEnvironment environment, string allowed, bool expected)
    {
        var context = new FakeContext().Context with { Environment = environment };
        var result = Harness.Evaluate(
            Harness.Flag("env_flag", flag => flag.Environments.Add(allowed)),
            context);

        Assert.Equal(expected, result.Enabled);
        if (!expected)
        {
            Assert.Equal(FeatureFlagReason.EnvironmentMismatch, result.Reason);
        }
    }

    [Fact]
    public void Device_allow_list_rejects_unknown_device()
    {
        var result = Harness.Evaluate(Harness.Flag("device_flag", flag => flag.DeviceIds.Add("device-9")));

        Assert.False(result.Enabled);
        Assert.Equal(FeatureFlagReason.DeviceMismatch, result.Reason);
    }

    [Fact]
    public void Platform_allow_list_rejects_ios_on_android()
    {
        var result = Harness.Evaluate(Harness.Flag("ios_only", flag => flag.Platforms.Add("iOS")));

        Assert.False(result.Enabled);
        Assert.Equal(FeatureFlagReason.OsMismatch, result.Reason);
    }

    [Fact]
    public void Min_os_version_rejects_older_devices()
    {
        var context = new FakeContext().Context with { OsVersion = "14.8" };
        var result = Harness.Evaluate(
            Harness.Flag("new_voip_engine", flag => flag.MinOsVersion = "15.0"),
            context);

        Assert.False(result.Enabled);
        Assert.Equal(FeatureFlagReason.OsMismatch, result.Reason);
    }

    [Fact]
    public void Country_allow_list_rejects_other_regions()
    {
        var result = Harness.Evaluate(Harness.Flag("india_offers", flag => flag.Countries.Add("IN")));

        Assert.False(result.Enabled);
        Assert.Equal(FeatureFlagReason.CountryMismatch, result.Reason);
    }

    [Fact]
    public void User_allow_list_requires_identified_user()
    {
        var result = Harness.Evaluate(Harness.Flag("beta_chat", flag => flag.UserIds.Add("user-42")));

        Assert.False(result.Enabled);
        Assert.Equal(FeatureFlagReason.UserMismatch, result.Reason);
    }

    [Fact]
    public void Identified_user_matches_allow_list()
    {
        var context = new FakeContext().Context with { UserId = "user-42" };
        var result = Harness.Evaluate(
            Harness.Flag("beta_chat", flag => flag.UserIds.Add("user-42")),
            context);

        Assert.True(result.Enabled);
    }

    [Fact]
    public void Excluded_user_is_rejected()
    {
        var context = new FakeContext().Context with { UserId = "user-42" };
        var result = Harness.Evaluate(
            Harness.Flag("beta_chat", flag => flag.ExcludedUserIds.Add("user-42")),
            context);

        Assert.False(result.Enabled);
        Assert.Equal(FeatureFlagReason.UserMismatch, result.Reason);
    }

    [Fact]
    public void App_version_below_minimum_is_rejected()
    {
        var context = new FakeContext().Context with { AppVersion = "1.0.0" };
        var result = Harness.Evaluate(
            Harness.Flag("new_voip_engine", flag => flag.MinAppVersion = "2.0.0"),
            context);

        Assert.False(result.Enabled);
        Assert.Equal(FeatureFlagReason.AppVersionMismatch, result.Reason);
    }

    [Fact]
    public void Unknown_key_uses_default()
    {
        var result = FeatureFlagEvaluator.Evaluate(
            "missing",
            new FakeContext().Context,
            snapshot: null,
            FeatureFlagSource.Default,
            new Dictionary<string, FeatureFlagDefinition>(),
            new Dictionary<string, bool>(),
            new Dictionary<string, bool>(),
            new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            preferLocalDefinitions: false,
            defaultWhenUnknown: false,
            DateTimeOffset.UtcNow);

        Assert.False(result.Enabled);
        Assert.Equal(FeatureFlagReason.NotFound, result.Reason);
        Assert.Equal(FeatureFlagSource.Default, result.Source);
    }

    [Fact]
    public void Local_boolean_fallback_is_used_when_remote_is_missing()
    {
        var result = FeatureFlagEvaluator.Evaluate(
            "dark_mode",
            new FakeContext().Context,
            snapshot: null,
            FeatureFlagSource.Default,
            new Dictionary<string, FeatureFlagDefinition>(),
            new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase) { ["dark_mode"] = true },
            new Dictionary<string, bool>(),
            new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            preferLocalDefinitions: false,
            defaultWhenUnknown: false,
            DateTimeOffset.UtcNow);

        Assert.True(result.Enabled);
        Assert.Equal(FeatureFlagReason.LocalFallback, result.Reason);
        Assert.Equal(FeatureFlagSource.Local, result.Source);
    }

    [Fact]
    public void Override_wins_over_kill_switch_and_definition()
    {
        var result = Harness.Evaluate(
            Harness.Flag("new_checkout", flag => flag.Killed = true),
            overrides: new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase) { ["new_checkout"] = true });

        Assert.True(result.Enabled);
        Assert.Equal(FeatureFlagReason.Override, result.Reason);
    }

    [Fact]
    public void Targeting_cascade_order_reports_the_first_mismatch()
    {
        var context = new FakeContext().Context with
        {
            Environment = FeatureFlagEnvironment.Development,
            Platform = "iOS",
            OsVersion = "14.0",
            Country = "FR",
            UserId = "other",
            AppVersion = "0.9.0"
        };

        var result = Harness.Evaluate(
            Harness.Flag("new_voip_engine", flag =>
            {
                flag.Environments.Add("Production");
                flag.Platforms.Add("Android");
                flag.MinOsVersion = "15.0";
                flag.Countries.Add("US");
                flag.UserIds.Add("user-1");
                flag.MinAppVersion = "2.0.0";
                flag.Percentage = 1;
            }),
            context);

        Assert.False(result.Enabled);
        Assert.Equal(FeatureFlagReason.EnvironmentMismatch, result.Reason);
    }
}
