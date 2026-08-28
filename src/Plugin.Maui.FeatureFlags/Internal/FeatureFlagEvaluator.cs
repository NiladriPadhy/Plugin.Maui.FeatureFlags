namespace Plugin.Maui.FeatureFlags;

static class FeatureFlagEvaluator
{
    public static FeatureFlagEvaluation Evaluate(
        string key,
        FeatureFlagContext context,
        FeatureFlagSnapshot? snapshot,
        FeatureFlagSource snapshotSource,
        IReadOnlyDictionary<string, FeatureFlagDefinition> localDefinitions,
        IReadOnlyDictionary<string, bool> localFlags,
        IReadOnlyDictionary<string, bool> overrides,
        IReadOnlySet<string> killSwitches,
        bool preferLocalDefinitions,
        bool defaultWhenUnknown,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (TryGetOverride(overrides, key, out var forced))
        {
            return Result(key, forced, FeatureFlagReason.Override, FeatureFlagSource.Override, null, null, now);
        }

        if (Contains(killSwitches, key))
        {
            return Result(key, false, FeatureFlagReason.KillSwitch, FeatureFlagSource.Override, null, null, now);
        }

        var resolved = Resolve(key, snapshot, snapshotSource, localDefinitions, localFlags, preferLocalDefinitions);
        if (resolved is null)
        {
            return Result(key, defaultWhenUnknown, FeatureFlagReason.NotFound, FeatureFlagSource.Default, null, null, now);
        }

        if (resolved.Value.LocalBoolean is { } localBoolean)
        {
            return Result(key, localBoolean, FeatureFlagReason.LocalFallback, FeatureFlagSource.Local, null, null, now);
        }

        var definition = resolved.Value.Definition!;
        var source = resolved.Value.Source;

        if (definition.Killed)
        {
            return Result(key, false, FeatureFlagReason.KillSwitch, source, definition, null, now);
        }

        if (definition.ExpiresAt is { } expires && now >= expires)
        {
            return Result(key, false, FeatureFlagReason.Expired, source, definition, null, now);
        }

        if (!EnvironmentNames.Matches(definition.Environments, context.Environment))
        {
            return Result(key, false, FeatureFlagReason.EnvironmentMismatch, source, definition, null, now);
        }

        if (!MatchesAllowList(definition.DeviceIds, context.DeviceId))
        {
            return Result(key, false, FeatureFlagReason.DeviceMismatch, source, definition, null, now);
        }

        if (!MatchesOs(definition, context))
        {
            return Result(key, false, FeatureFlagReason.OsMismatch, source, definition, null, now);
        }

        if (!MatchesAllowList(definition.Countries, context.Country))
        {
            return Result(key, false, FeatureFlagReason.CountryMismatch, source, definition, null, now);
        }

        if (Contains(definition.ExcludedUserIds, context.UserId))
        {
            return Result(key, false, FeatureFlagReason.UserMismatch, source, definition, null, now);
        }

        if (!MatchesAllowList(definition.UserIds, context.UserId))
        {
            return Result(key, false, FeatureFlagReason.UserMismatch, source, definition, null, now);
        }

        if (!MatchesAppVersion(definition, context.AppVersion))
        {
            return Result(key, false, FeatureFlagReason.AppVersionMismatch, source, definition, null, now);
        }

        int? bucket = null;
        if (definition.Percentage is { } percentage)
        {
            var clamped = Math.Clamp(percentage, 0, 100);
            var targetingKey = string.IsNullOrWhiteSpace(context.UserId)
                ? context.DeviceId ?? "anonymous"
                : context.UserId;
            bucket = RolloutHasher.Bucket(key, targetingKey);
            if (bucket.Value >= clamped)
            {
                return Result(key, false, FeatureFlagReason.NotInRollout, source, definition, bucket, now);
            }
        }

        if (!definition.Enabled)
        {
            return Result(key, false, FeatureFlagReason.FlagOff, source, definition, bucket, now);
        }

        return Result(key, true, FeatureFlagReason.Matched, source, definition, bucket, now);
    }

    static ResolvedFlag? Resolve(
        string key,
        FeatureFlagSnapshot? snapshot,
        FeatureFlagSource snapshotSource,
        IReadOnlyDictionary<string, FeatureFlagDefinition> localDefinitions,
        IReadOnlyDictionary<string, bool> localFlags,
        bool preferLocalDefinitions)
    {
        localDefinitions.TryGetValue(key, out var localDefinition);
        var remote = snapshot?.Find(key);

        if (preferLocalDefinitions && localDefinition is not null)
        {
            return new ResolvedFlag(localDefinition, FeatureFlagSource.Local, null);
        }

        if (remote is not null)
        {
            var source = snapshotSource == FeatureFlagSource.Default ? FeatureFlagSource.Remote : snapshotSource;
            return new ResolvedFlag(remote, source, null);
        }

        if (localDefinition is not null)
        {
            return new ResolvedFlag(localDefinition, FeatureFlagSource.Local, null);
        }

        if (TryGetOverride(localFlags, key, out var localBoolean))
        {
            return new ResolvedFlag(null, FeatureFlagSource.Local, localBoolean);
        }

        return null;
    }

    static bool MatchesOs(FeatureFlagDefinition definition, FeatureFlagContext context)
    {
        if (!MatchesAllowList(definition.Platforms, context.Platform))
        {
            return false;
        }

        if (definition.OsVersions is { Count: > 0 })
        {
            var matched = false;
            foreach (var allowed in definition.OsVersions)
            {
                if (VersionComparer.EqualsVersion(context.OsVersion, allowed) ||
                    string.Equals(context.OsVersion, allowed, StringComparison.OrdinalIgnoreCase))
                {
                    matched = true;
                    break;
                }
            }

            if (!matched)
            {
                return false;
            }
        }

        return VersionComparer.IsAtLeast(context.OsVersion, definition.MinOsVersion) &&
               VersionComparer.IsAtMost(context.OsVersion, definition.MaxOsVersion);
    }

    static bool MatchesAppVersion(FeatureFlagDefinition definition, string? appVersion) =>
        VersionComparer.IsAtLeast(appVersion, definition.MinAppVersion) &&
        VersionComparer.IsAtMost(appVersion, definition.MaxAppVersion);

    static bool MatchesAllowList(IReadOnlyList<string>? list, string? value)
    {
        if (list is null || list.Count == 0)
        {
            return true;
        }

        return Contains(list, value);
    }

    static bool Contains(IEnumerable<string>? list, string? value)
    {
        if (list is null || string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        foreach (var item in list)
        {
            if (string.Equals(item?.Trim(), value.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    static bool TryGetOverride(IReadOnlyDictionary<string, bool> map, string key, out bool value)
    {
        if (map.TryGetValue(key, out value))
        {
            return true;
        }

        foreach (var pair in map)
        {
            if (string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                value = pair.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    static FeatureFlagEvaluation Result(
        string key,
        bool enabled,
        FeatureFlagReason reason,
        FeatureFlagSource source,
        FeatureFlagDefinition? definition,
        int? bucket,
        DateTimeOffset now) =>
        new()
        {
            Key = key,
            Enabled = enabled,
            Reason = reason,
            Source = source,
            Definition = definition,
            RolloutBucket = bucket,
            EvaluatedAt = now
        };

    readonly record struct ResolvedFlag(FeatureFlagDefinition? Definition, FeatureFlagSource Source, bool? LocalBoolean);
}
