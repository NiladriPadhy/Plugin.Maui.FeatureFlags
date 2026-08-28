namespace Plugin.Maui.FeatureFlags;

static class RolloutHasher
{
    const uint Offset = 2166136261;
    const uint Prime = 16777619;

    /// <summary>
    /// Stable 0–99 bucket for <paramref name="key"/> + <paramref name="targetingKey"/>.
    /// Does not use <see cref="string.GetHashCode()"/> (randomized per process).
    /// </summary>
    public static int Bucket(string key, string targetingKey)
    {
        var input = key + "\n" + targetingKey;
        uint hash = Offset;
        foreach (var b in Encoding.UTF8.GetBytes(input))
        {
            hash ^= b;
            hash *= Prime;
        }

        return (int)(hash % 100);
    }
}
