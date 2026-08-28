namespace Plugin.Maui.FeatureFlags;

sealed class FileFeatureFlagCache : IFeatureFlagCache
{
    readonly string _path;
    readonly object _gate = new();

    public FileFeatureFlagCache(string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        Directory.CreateDirectory(directory);
        _path = Path.Combine(directory, FeatureFlagsDefaults.CacheFileName);
    }

    public FeatureFlagSnapshot? Load()
    {
        lock (_gate)
        {
            if (!File.Exists(_path))
            {
                return null;
            }

            try
            {
                var json = File.ReadAllText(_path);
                return JsonSerializer.Deserialize(json, FeatureFlagsJsonContext.Default.FeatureFlagSnapshot);
            }
            catch
            {
                return null;
            }
        }
    }

    public void Save(FeatureFlagSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        lock (_gate)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            var json = JsonSerializer.Serialize(snapshot, FeatureFlagsJsonContext.Default.FeatureFlagSnapshot);
            var temp = _path + ".tmp";
            File.WriteAllText(temp, json);
            File.Copy(temp, _path, overwrite: true);
            File.Delete(temp);
        }
    }

    public void Clear()
    {
        lock (_gate)
        {
            if (File.Exists(_path))
            {
                File.Delete(_path);
            }
        }
    }
}
