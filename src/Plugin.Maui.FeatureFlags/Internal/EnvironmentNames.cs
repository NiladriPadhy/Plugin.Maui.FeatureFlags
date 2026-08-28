namespace Plugin.Maui.FeatureFlags;

static class EnvironmentNames
{
    public static bool Matches(IReadOnlyList<string>? allowed, FeatureFlagEnvironment environment)
    {
        if (allowed is null || allowed.Count == 0)
        {
            return true;
        }

        var aliases = Aliases(environment);
        foreach (var item in allowed)
        {
            if (string.IsNullOrWhiteSpace(item))
            {
                continue;
            }

            foreach (var alias in aliases)
            {
                if (string.Equals(item.Trim(), alias, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public static string[] Aliases(FeatureFlagEnvironment environment) => environment switch
    {
        FeatureFlagEnvironment.Development => ["Development", "Dev"],
        FeatureFlagEnvironment.Staging => ["Staging", "Stage"],
        FeatureFlagEnvironment.Production => ["Production", "Prod"],
        _ => [environment.ToString()]
    };
}
