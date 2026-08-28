using Microsoft.Maui.Hosting;

namespace Plugin.Maui.FeatureFlags;

sealed class FeatureFlagsInitializer : IMauiInitializeService
{
    public void Initialize(IServiceProvider services)
    {
        var flags = services.GetService<IFeatureFlags>() ?? FeatureFlags.Current;
        FeatureFlags.SetDefault(flags);
        flags.Start();
    }
}
