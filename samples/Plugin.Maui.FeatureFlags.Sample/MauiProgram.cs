using Microsoft.Extensions.Logging;
using Plugin.Maui.FeatureFlags;

namespace Plugin.Maui.FeatureFlags.Sample;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.Services.AddSingleton<MainPage>();

        builder
            .UseMauiApp<App>()
            .UseMauiFeatureFlags(options =>
            {
#if DEBUG
                options.Environment = FeatureFlagEnvironment.Development;
#else
                options.Environment = FeatureFlagEnvironment.Production;
#endif
                options.RefreshInterval = TimeSpan.Zero;
                options.LocalFlags["dark_mode"] = true;
                options.Provider = new StaticFeatureFlagProvider(DemoSnapshot.Create());
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}

static class DemoSnapshot
{
    public static FeatureFlagSnapshot Create() => new()
    {
        Version = 1,
        Environment = "mixed",
        FetchedAt = DateTimeOffset.UtcNow,
        Flags =
        [
            new FeatureFlagDefinition
            {
                Key = "new_checkout",
                Enabled = true,
                Description = "50% sticky rollout",
                Percentage = 50
            },
            new FeatureFlagDefinition
            {
                Key = "new_voip_engine",
                Enabled = true,
                Description = "iOS + Android, app 1.0+",
                Platforms = ["iOS", "Android"],
                MinAppVersion = "1.0.0"
            },
            new FeatureFlagDefinition
            {
                Key = "legacy_billing",
                Enabled = true,
                Killed = true,
                Description = "Kill switch"
            },
            new FeatureFlagDefinition
            {
                Key = "holiday_promo",
                Enabled = true,
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1),
                Description = "Expired yesterday"
            },
            new FeatureFlagDefinition
            {
                Key = "beta_chat",
                Enabled = true,
                UserIds = ["user-42"],
                Description = "User targeting"
            },
            new FeatureFlagDefinition
            {
                Key = "india_offers",
                Enabled = true,
                Countries = ["IN"],
                Description = "Country targeting"
            },
            new FeatureFlagDefinition
            {
                Key = "android_only_ui",
                Enabled = true,
                Platforms = ["Android"],
                Description = "OS targeting"
            },
            new FeatureFlagDefinition
            {
                Key = "prod_payments",
                Enabled = true,
                Environments = ["Production"],
                Description = "Production only"
            }
        ]
    };
}
