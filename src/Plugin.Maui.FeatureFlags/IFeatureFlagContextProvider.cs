namespace Plugin.Maui.FeatureFlags;

/// <summary>
/// Collects the MAUI device, OS, app, and locale used by the targeting cascade.
/// </summary>
public interface IFeatureFlagContextProvider
{
    /// <summary>
    /// Builds a context for the current device and optional identified user.
    /// </summary>
    FeatureFlagContext Capture(FeatureFlagUser? user, FeatureFlagEnvironment environment);
}
