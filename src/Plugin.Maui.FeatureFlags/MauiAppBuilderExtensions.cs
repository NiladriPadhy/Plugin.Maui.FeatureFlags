using Microsoft.Maui.Hosting;

namespace Plugin.Maui.FeatureFlags;

/// <summary>
/// MAUI host registration for feature flags.
/// </summary>
public static class MauiAppBuilderExtensions
{
    /// <summary>
    /// Registers <see cref="IFeatureFlags"/> as a singleton and loads the offline cache at startup.
    /// </summary>
    /// <example>
    /// <code>
    /// builder.UseMauiFeatureFlags(options =>
    /// {
    ///     options.Environment = FeatureFlagEnvironment.Production;
    ///     options.RemoteUri = new Uri("https://cdn.example.com/flags.json");
    /// });
    /// </code>
    /// </example>
    public static MauiAppBuilder UseMauiFeatureFlags(this MauiAppBuilder builder, Action<FeatureFlagsOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var options = new FeatureFlagsOptions();
        configure?.Invoke(options);

        builder.Services.AddMauiFeatureFlags(options);
        builder.Services.AddTransient<IMauiInitializeService, FeatureFlagsInitializer>();
        return builder;
    }
}
