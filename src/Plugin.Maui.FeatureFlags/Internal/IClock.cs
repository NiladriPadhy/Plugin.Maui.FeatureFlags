namespace Plugin.Maui.FeatureFlags;

interface IClock
{
    DateTimeOffset UtcNow { get; }
}
