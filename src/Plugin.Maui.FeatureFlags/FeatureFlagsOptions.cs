namespace Plugin.Maui.FeatureFlags;

/// <summary>
/// Configuration for an <see cref="IFeatureFlags"/> instance.
/// </summary>
public sealed class FeatureFlagsOptions
{
    /// <summary>
    /// Environment used when evaluating <see cref="FeatureFlagDefinition.Environments"/>.
    /// </summary>
    public FeatureFlagEnvironment Environment { get; set; } = FeatureFlagEnvironment.Production;

    /// <summary>
    /// Remote configuration URL. When set and <see cref="Provider"/> is null, an HTTP provider is created.
    /// </summary>
    public Uri? RemoteUri { get; set; }

    /// <summary>
    /// How often to poll the remote provider. <see cref="TimeSpan.Zero"/> disables periodic refresh.
    /// </summary>
    public TimeSpan RefreshInterval { get; set; } = FeatureFlagsDefaults.RefreshInterval;

    /// <summary>
    /// HTTP timeout for the built-in remote provider.
    /// </summary>
    public TimeSpan RequestTimeout { get; set; } = FeatureFlagsDefaults.RequestTimeout;

    /// <summary>
    /// Value returned when a key has no remote, cached, or local definition.
    /// </summary>
    public bool DefaultWhenUnknown { get; set; }

    /// <summary>
    /// Optional ISO 3166-1 alpha-2 country used when the identified user does not supply one.
    /// </summary>
    public string? Country { get; set; }

    /// <summary>
    /// Optional device id. When null, a sticky anonymous id is created and persisted.
    /// </summary>
    public string? DeviceId { get; set; }

    /// <summary>
    /// Simple local fallbacks used when a key is absent from the snapshot.
    /// </summary>
    public Dictionary<string, bool> LocalFlags { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Full local definitions used when a key is absent from the snapshot (or when <see cref="PreferLocalDefinitions"/> is true).
    /// </summary>
    public List<FeatureFlagDefinition> LocalDefinitions { get; } = [];

    /// <summary>
    /// Keys that are always off, even if the remote definition is enabled.
    /// </summary>
    public HashSet<string> KillSwitches { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// When true, <see cref="LocalDefinitions"/> win over a remote flag with the same key.
    /// </summary>
    public bool PreferLocalDefinitions { get; set; }

    /// <summary>
    /// Override the persistence folder. Tests and custom hosts set this.
    /// When null, files go under app data / <see cref="FeatureFlagsDefaults.StorageFolderName"/>.
    /// </summary>
    public string? StorageDirectory { get; set; }

    /// <summary>
    /// Custom remote provider. When set, <see cref="RemoteUri"/> is ignored.
    /// </summary>
    public IFeatureFlagProvider? Provider { get; set; }

    /// <summary>
    /// Custom offline cache. When null, a JSON file cache is used.
    /// </summary>
    public IFeatureFlagCache? Cache { get; set; }

    /// <summary>
    /// Custom context collector. When null, MAUI device / app APIs are used on Android and iOS.
    /// </summary>
    public IFeatureFlagContextProvider? ContextProvider { get; set; }

    /// <summary>
    /// Optional <see cref="HttpClient"/> for the built-in HTTP provider. The plugin does not dispose a supplied client.
    /// </summary>
    public HttpClient? HttpClient { get; set; }

    /// <summary>
    /// Optional hook to add headers (for example authorization) to remote fetches.
    /// </summary>
    public Action<HttpRequestMessage>? ConfigureRequest { get; set; }
}
