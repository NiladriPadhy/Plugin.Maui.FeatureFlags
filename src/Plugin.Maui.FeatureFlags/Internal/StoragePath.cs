#if ANDROID || IOS || MACCATALYST || WINDOWS
using Microsoft.Maui.Storage;
#endif

namespace Plugin.Maui.FeatureFlags;

static class StoragePath
{
    public static string Resolve(FeatureFlagsOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.StorageDirectory))
        {
            return options.StorageDirectory;
        }

        var root = TryAppData() ?? Path.Combine(Path.GetTempPath(), FeatureFlagsDefaults.StorageFolderName);
        return Path.Combine(root, FeatureFlagsDefaults.StorageFolderName);
    }

    static string? TryAppData()
    {
#if ANDROID || IOS || MACCATALYST || WINDOWS
        try
        {
            return FileSystem.AppDataDirectory;
        }
        catch
        {
            return null;
        }
#else
        return null;
#endif
    }
}
