namespace Plugin.Maui.FeatureFlags;

/// <summary>
/// A single feature flag and its MAUI targeting constraints.
/// Empty collections mean "no restriction" for that dimension.
/// </summary>
public sealed class FeatureFlagDefinition
{
    /// <summary>
    /// Stable key used by <see cref="IFeatureFlags.IsEnabled"/>.
    /// </summary>
    public string Key { get; set; } = "";

    /// <summary>
    /// Intended state after targeting matches. <c>false</c> turns the flag off for everyone who reaches this step.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Emergency off switch. Wins over <see cref="Enabled"/> and targeting.
    /// </summary>
    public bool Killed { get; set; }

    /// <summary>
    /// UTC instant after which the flag evaluates as expired (off).
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>
    /// Allowed environments. Accepts <c>Development</c>/<c>Dev</c>, <c>Staging</c>/<c>Stage</c>, <c>Production</c>/<c>Prod</c>.
    /// </summary>
    public List<string> Environments { get; set; } = [];

    /// <summary>
    /// Allowed platforms, for example <c>iOS</c> and <c>Android</c>.
    /// </summary>
    public List<string> Platforms { get; set; } = [];

    /// <summary>
    /// Exact OS version allow list (for example <c>16.0</c>). Combined with <see cref="MinOsVersion"/> / <see cref="MaxOsVersion"/> when set.
    /// </summary>
    public List<string> OsVersions { get; set; } = [];

    /// <summary>
    /// Inclusive minimum OS version.
    /// </summary>
    public string? MinOsVersion { get; set; }

    /// <summary>
    /// Inclusive maximum OS version.
    /// </summary>
    public string? MaxOsVersion { get; set; }

    /// <summary>
    /// ISO 3166-1 alpha-2 country allow list (for example <c>US</c>, <c>IN</c>).
    /// </summary>
    public List<string> Countries { get; set; } = [];

    /// <summary>
    /// User id allow list. Empty means every user (who is not excluded) may match.
    /// </summary>
    public List<string> UserIds { get; set; } = [];

    /// <summary>
    /// User ids that never receive the flag.
    /// </summary>
    public List<string> ExcludedUserIds { get; set; } = [];

    /// <summary>
    /// Device id allow list.
    /// </summary>
    public List<string> DeviceIds { get; set; } = [];

    /// <summary>
    /// Inclusive minimum app version (<see cref="FeatureFlagContext.AppVersion"/>).
    /// </summary>
    public string? MinAppVersion { get; set; }

    /// <summary>
    /// Inclusive maximum app version.
    /// </summary>
    public string? MaxAppVersion { get; set; }

    /// <summary>
    /// Sticky percentage rollout from 0 to 100. <c>null</c> skips rollout (everyone who matched targeting is in).
    /// </summary>
    public int? Percentage { get; set; }

    /// <summary>
    /// Optional human-readable description.
    /// </summary>
    public string? Description { get; set; }
}
