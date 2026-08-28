namespace Plugin.Maui.FeatureFlags;

/// <summary>
/// Device, OS, app, country, user, and environment used by the targeting cascade.
/// </summary>
public sealed record FeatureFlagContext
{
    /// <summary>
    /// Current <see cref="FeatureFlagEnvironment"/>.
    /// </summary>
    public FeatureFlagEnvironment Environment { get; init; } = FeatureFlagEnvironment.Production;

    /// <summary>
    /// Identified user id, if any.
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>
    /// Sticky anonymous device / installation id.
    /// </summary>
    public string? DeviceId { get; init; }

    /// <summary>
    /// Device model (for example <c>Pixel 9</c> or <c>iPhone16,2</c>).
    /// </summary>
    public string? DeviceModel { get; init; }

    /// <summary>
    /// Device manufacturer.
    /// </summary>
    public string? DeviceManufacturer { get; init; }

    /// <summary>
    /// Platform name: <c>Android</c> or <c>iOS</c>.
    /// </summary>
    public string Platform { get; init; } = "Unknown";

    /// <summary>
    /// OS version string from the device.
    /// </summary>
    public string? OsVersion { get; init; }

    /// <summary>
    /// App marketing version.
    /// </summary>
    public string? AppVersion { get; init; }

    /// <summary>
    /// App build number.
    /// </summary>
    public string? AppBuild { get; init; }

    /// <summary>
    /// ISO 3166-1 alpha-2 country, from the identified user, options, or the device locale.
    /// </summary>
    public string? Country { get; init; }

    /// <summary>
    /// Optional attributes supplied with <see cref="FeatureFlagUser"/>.
    /// </summary>
    public IReadOnlyDictionary<string, string> Attributes { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
