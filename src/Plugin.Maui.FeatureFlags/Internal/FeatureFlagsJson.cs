namespace Plugin.Maui.FeatureFlags;

[JsonSourceGenerationOptions(
    WriteIndented = false,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(FeatureFlagSnapshot))]
[JsonSerializable(typeof(FeatureFlagDefinition))]
[JsonSerializable(typeof(List<FeatureFlagDefinition>))]
sealed partial class FeatureFlagsJsonContext : JsonSerializerContext;
