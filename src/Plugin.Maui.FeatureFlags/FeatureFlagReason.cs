namespace Plugin.Maui.FeatureFlags;

/// <summary>
/// Why a flag evaluated to on or off.
/// </summary>
public enum FeatureFlagReason
{
    /// <summary>
    /// Targeting matched and the flag is enabled.
    /// </summary>
    Matched,

    /// <summary>
    /// The definition's <see cref="FeatureFlagDefinition.Enabled"/> is <c>false</c>.
    /// </summary>
    FlagOff,

    /// <summary>
    /// A kill switch forced the flag off.
    /// </summary>
    KillSwitch,

    /// <summary>
    /// <see cref="FeatureFlagDefinition.ExpiresAt"/> is in the past.
    /// </summary>
    Expired,

    /// <summary>
    /// The current <see cref="FeatureFlagEnvironment"/> is not allowed.
    /// </summary>
    EnvironmentMismatch,

    /// <summary>
    /// The current device id is not in the allow list.
    /// </summary>
    DeviceMismatch,

    /// <summary>
    /// The current platform or OS version is not allowed.
    /// </summary>
    OsMismatch,

    /// <summary>
    /// The current country is not in the allow list.
    /// </summary>
    CountryMismatch,

    /// <summary>
    /// The current user is excluded or not in the allow list.
    /// </summary>
    UserMismatch,

    /// <summary>
    /// The current app version is outside the allowed range.
    /// </summary>
    AppVersionMismatch,

    /// <summary>
    /// The sticky rollout bucket is outside the configured percentage.
    /// </summary>
    NotInRollout,

    /// <summary>
    /// No definition or local fallback exists for the key.
    /// </summary>
    NotFound,

    /// <summary>
    /// A local boolean fallback was used because the key is absent remotely.
    /// </summary>
    LocalFallback,

    /// <summary>
    /// A runtime override forced the result.
    /// </summary>
    Override
}
