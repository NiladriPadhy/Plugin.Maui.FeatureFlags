#if ANDROID || IOS || MACCATALYST || WINDOWS
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;
#endif

namespace Plugin.Maui.FeatureFlags;

sealed class MauiFeatureFlagContextProvider : IFeatureFlagContextProvider
{
    readonly FeatureFlagsOptions _options;
    readonly string _storageDirectory;

    public MauiFeatureFlagContextProvider(FeatureFlagsOptions options, string storageDirectory)
    {
        _options = options;
        _storageDirectory = storageDirectory;
    }

    public FeatureFlagContext Capture(FeatureFlagUser? user, FeatureFlagEnvironment environment)
    {
        var context = new FeatureFlagContext
        {
            Environment = environment,
            UserId = user?.Id,
            DeviceId = ResolveDeviceId(),
            Country = FirstNonEmpty(user?.Country, _options.Country, TryRegion()),
            Attributes = user?.Attributes ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        };

#if ANDROID || IOS || MACCATALYST || WINDOWS
        Try(() =>
        {
            context = context with
            {
                DeviceManufacturer = DeviceInfo.Current.Manufacturer,
                DeviceModel = DeviceInfo.Current.Model,
                Platform = DeviceInfo.Current.Platform.ToString(),
                OsVersion = DeviceInfo.Current.VersionString
            };
        });

        Try(() =>
        {
            context = context with
            {
                AppVersion = AppInfo.Current.VersionString,
                AppBuild = AppInfo.Current.BuildString
            };
        });
#else
        context = context with { Platform = "Unknown" };
#endif

        return context;
    }

    string ResolveDeviceId()
    {
        if (!string.IsNullOrWhiteSpace(_options.DeviceId))
        {
            return _options.DeviceId;
        }

#if ANDROID || IOS || MACCATALYST || WINDOWS
        try
        {
            var existing = Preferences.Default.Get(FeatureFlagsDefaults.DeviceIdKey, "");
            if (!string.IsNullOrWhiteSpace(existing))
            {
                return existing;
            }

            var created = Guid.NewGuid().ToString("N");
            Preferences.Default.Set(FeatureFlagsDefaults.DeviceIdKey, created);
            return created;
        }
        catch
        {
            // Fall through to the file-backed id.
        }
#endif

        return FileDeviceId.GetOrCreate(_storageDirectory);
    }

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

    static void Try(Action action)
    {
        try
        {
            action();
        }
        catch
        {
            // Device / app APIs can throw before MAUI is fully started.
        }
    }
}

static class FileDeviceId
{
    public static string GetOrCreate(string directory)
    {
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "device-id.txt");
        try
        {
            if (File.Exists(path))
            {
                var existing = File.ReadAllText(path).Trim();
                if (!string.IsNullOrWhiteSpace(existing))
                {
                    return existing;
                }
            }

            var created = Guid.NewGuid().ToString("N");
            File.WriteAllText(path, created);
            return created;
        }
        catch
        {
            return Guid.NewGuid().ToString("N");
        }
    }
}
