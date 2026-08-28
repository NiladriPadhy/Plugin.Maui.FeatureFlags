namespace Plugin.Maui.FeatureFlags;

sealed class FallbackFeatureFlagContextProvider : IFeatureFlagContextProvider
{
    readonly FeatureFlagsOptions _options;

    public FallbackFeatureFlagContextProvider(FeatureFlagsOptions options)
    {
        _options = options;
    }

    public string Platform { get; set; } = "Unknown";

    public string? OsVersion { get; set; }

    public string? AppVersion { get; set; }

    public string? AppBuild { get; set; }

    public string? DeviceModel { get; set; }

    public string? DeviceManufacturer { get; set; }

    public FeatureFlagContext Capture(FeatureFlagUser? user, FeatureFlagEnvironment environment) =>
        new()
        {
            Environment = environment,
            UserId = user?.Id,
            DeviceId = _options.DeviceId ?? "test-device",
            DeviceModel = DeviceModel,
            DeviceManufacturer = DeviceManufacturer,
            Platform = Platform,
            OsVersion = OsVersion,
            AppVersion = AppVersion,
            AppBuild = AppBuild,
            Country = FirstNonEmpty(user?.Country, _options.Country, TryRegion()),
            Attributes = user?.Attributes ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        };

    static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    static string? TryRegion()
    {
        try
        {
            var name = RegionInfo.CurrentRegion.TwoLetterISORegionName;
            return string.IsNullOrWhiteSpace(name) || name is "IV" ? null : name;
        }
        catch
        {
            return null;
        }
    }
}
