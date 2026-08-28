namespace Plugin.Maui.FeatureFlags;

/// <summary>
/// Deployment environment used when evaluating flags.
/// </summary>
public enum FeatureFlagEnvironment
{
    /// <summary>
    /// Local and internal builds. JSON aliases: <c>Development</c>, <c>Dev</c>.
    /// </summary>
    Development = 0,

    /// <summary>
    /// Pre-production. JSON aliases: <c>Staging</c>, <c>Stage</c>.
    /// </summary>
    Staging = 1,

    /// <summary>
    /// Production. JSON aliases: <c>Production</c>, <c>Prod</c>.
    /// </summary>
    Production = 2
}
