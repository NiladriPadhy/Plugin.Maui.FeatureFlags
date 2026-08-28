namespace Plugin.Maui.FeatureFlags;

/// <summary>
/// Identified user used for user targeting and sticky percentage rollout.
/// </summary>
public sealed class FeatureFlagUser
{
    /// <summary>
    /// Creates a user with the supplied id.
    /// </summary>
    public FeatureFlagUser(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        Id = id;
    }

    /// <summary>
    /// Stable user id.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Optional ISO 3166-1 alpha-2 country that overrides the device locale.
    /// </summary>
    public string? Country { get; set; }

    /// <summary>
    /// Optional attributes carried on <see cref="FeatureFlagContext"/>.
    /// </summary>
    public Dictionary<string, string> Attributes { get; } = new(StringComparer.OrdinalIgnoreCase);
}
