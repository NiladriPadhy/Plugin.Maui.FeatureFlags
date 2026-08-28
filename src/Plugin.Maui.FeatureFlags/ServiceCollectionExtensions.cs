namespace Plugin.Maui.FeatureFlags;

/// <summary>
/// Registers feature-flag services without MAUI lifecycle hooks.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds <see cref="IFeatureFlags"/> using the supplied options instance.
    /// </summary>
    public static IServiceCollection AddMauiFeatureFlags(this IServiceCollection services, FeatureFlagsOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        services.AddSingleton(options);
        services.TryAddSingleton<IFeatureFlags>(sp =>
        {
            var resolved = sp.GetService<FeatureFlagsOptions>() ?? options;
            var flags = FeatureFlags.Create(resolved);
            FeatureFlags.SetDefault(flags);
            return flags;
        });

        return services;
    }

    /// <summary>
    /// Adds <see cref="IFeatureFlags"/> and applies <paramref name="configure"/> to a new options instance.
    /// </summary>
    public static IServiceCollection AddMauiFeatureFlags(this IServiceCollection services, Action<FeatureFlagsOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new FeatureFlagsOptions();
        configure?.Invoke(options);
        return services.AddMauiFeatureFlags(options);
    }
}
