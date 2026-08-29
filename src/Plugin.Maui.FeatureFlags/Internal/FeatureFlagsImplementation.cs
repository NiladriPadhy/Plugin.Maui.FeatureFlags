namespace Plugin.Maui.FeatureFlags;

sealed class FeatureFlagsImplementation : IFeatureFlags
{
    readonly FeatureFlagsOptions _options;
    readonly IClock _clock;
    readonly IFeatureFlagContextProvider _context;
    readonly IFeatureFlagProvider? _provider;
    readonly IFeatureFlagCache _cache;
    readonly HttpClient? _ownedHttp;
    readonly Dictionary<string, FeatureFlagDefinition> _localDefinitions;
    readonly object _gate = new();
    readonly SemaphoreSlim _refresh = new(1, 1);
    readonly ConcurrentDictionary<string, bool> _overrides = new(StringComparer.OrdinalIgnoreCase);

    FeatureFlagSnapshot? _snapshot;
    FeatureFlagSource _source = FeatureFlagSource.Default;
    FeatureFlagUser? _user;
    FeatureFlagEnvironment _environment;
    CancellationTokenSource? _timerCts;
    bool _started;
    bool _disposed;

    public FeatureFlagsImplementation(
        FeatureFlagsOptions options,
        IClock clock,
        IFeatureFlagContextProvider context,
        IFeatureFlagProvider? provider,
        IFeatureFlagCache cache,
        HttpClient? ownedHttp)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _provider = provider;
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _ownedHttp = ownedHttp;
        _environment = options.Environment;
        _localDefinitions = BuildLocalDefinitions(options);
    }

    public bool IsSupported => true;

    public FeatureFlagEnvironment Environment
    {
        get
        {
            lock (_gate)
            {
                return _environment;
            }
        }
    }

    public FeatureFlagUser? User
    {
        get
        {
            lock (_gate)
            {
                return _user;
            }
        }
    }

    public FeatureFlagSnapshot? Snapshot
    {
        get
        {
            lock (_gate)
            {
                return _snapshot;
            }
        }
    }

    public FeatureFlagSource SnapshotSource
    {
        get
        {
            lock (_gate)
            {
                return _source;
            }
        }
    }

    public event EventHandler<FeatureFlagsChangedEventArgs>? FlagsChanged;

    public void Start()
    {
        ThrowIfDisposed();

        lock (_gate)
        {
            if (_started)
            {
                return;
            }

            _started = true;
        }

        ApplyCacheOrLocal();
        StartTimer();
        _ = RefreshSafeAsync();
    }

    public bool IsEnabled(string key) => Evaluate(key).Enabled;

    public async Task<bool> IsEnabledAsync(string key, CancellationToken cancellationToken = default)
    {
        var evaluation = await EvaluateAsync(key, cancellationToken).ConfigureAwait(false);
        return evaluation.Enabled;
    }

    public FeatureFlagEvaluation Evaluate(string key)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        EnsureStarted();

        FeatureFlagSnapshot? snapshot;
        FeatureFlagSource source;
        FeatureFlagUser? user;
        FeatureFlagEnvironment environment;
        lock (_gate)
        {
            snapshot = _snapshot;
            source = _source;
            user = _user;
            environment = _environment;
        }

        return FeatureFlagEvaluator.Evaluate(
            key,
            _context.Capture(user, environment),
            snapshot,
            source,
            _localDefinitions,
            _options.LocalFlags,
            _overrides,
            _options.KillSwitches,
            _options.PreferLocalDefinitions,
            _options.DefaultWhenUnknown,
            _clock.UtcNow);
    }

    public async Task<FeatureFlagEvaluation> EvaluateAsync(string key, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        EnsureStarted();

        if (Snapshot is null && _provider is not null)
        {
            try
            {
                await RefreshAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // Evaluate against local fallback / cache already applied by Start.
            }
        }

        return Evaluate(key);
    }

    public IReadOnlyList<FeatureFlagEvaluation> EvaluateAll()
    {
        ThrowIfDisposed();
        EnsureStarted();

        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        lock (_gate)
        {
            if (_snapshot is not null)
            {
                foreach (var flag in _snapshot.Flags)
                {
                    if (!string.IsNullOrWhiteSpace(flag.Key))
                    {
                        keys.Add(flag.Key);
                    }
                }
            }
        }

        foreach (var key in _localDefinitions.Keys)
        {
            keys.Add(key);
        }

        foreach (var key in _options.LocalFlags.Keys)
        {
            keys.Add(key);
        }

        foreach (var key in _overrides.Keys)
        {
            keys.Add(key);
        }

        foreach (var key in _options.KillSwitches)
        {
            keys.Add(key);
        }

        return keys
            .OrderBy(static key => key, StringComparer.OrdinalIgnoreCase)
            .Select(Evaluate)
            .ToArray();
    }

    public void Identify(string userId, string? country = null) =>
        Identify(new FeatureFlagUser(userId) { Country = country });

    public void Identify(FeatureFlagUser user)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);

        lock (_gate)
        {
            _user = user;
        }
    }

    public void ClearIdentity()
    {
        ThrowIfDisposed();
        lock (_gate)
        {
            _user = null;
        }
    }

    public void SetEnvironment(FeatureFlagEnvironment environment)
    {
        ThrowIfDisposed();
        lock (_gate)
        {
            _environment = environment;
        }
    }

    public void SetOverride(string key, bool enabled)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _overrides[key] = enabled;
    }

    public void ClearOverride(string key)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _overrides.TryRemove(key, out _);
    }

    public void ClearOverrides()
    {
        ThrowIfDisposed();
        _overrides.Clear();
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        EnsureStarted();

        await _refresh.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_provider is null)
            {
                if (Snapshot is null)
                {
                    ApplyLocalSnapshot(raise: true);
                }

                return;
            }

            var request = new FeatureFlagRefreshRequest
            {
                ETag = Snapshot?.ETag,
                Environment = Environment,
                Context = GetContext()
            };

            var result = await _provider.FetchAsync(request, cancellationToken).ConfigureAwait(false);
            if (result.NotModified)
            {
                return;
            }

            if (result.Snapshot is null)
            {
                return;
            }

            result.Snapshot.Flags ??= [];
            if (result.Snapshot.FetchedAt == default)
            {
                result.Snapshot.FetchedAt = _clock.UtcNow;
            }

            if (_disposed)
            {
                return;
            }

            ApplySnapshot(result.Snapshot, FeatureFlagSource.Remote);
            try
            {
                _cache.Save(result.Snapshot);
            }
            catch
            {
                // Evaluation still uses the in-memory snapshot.
            }
        }
        finally
        {
            try
            {
                _refresh.Release();
            }
            catch (ObjectDisposedException)
            {
            }
        }
    }

    public FeatureFlagContext GetContext()
    {
        ThrowIfDisposed();
        FeatureFlagUser? user;
        FeatureFlagEnvironment environment;
        lock (_gate)
        {
            user = _user;
            environment = _environment;
        }

        return _context.Capture(user, environment);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _timerCts?.Cancel();
        _timerCts?.Dispose();
        _refresh.Dispose();
        _ownedHttp?.Dispose();
    }

    void EnsureStarted()
    {
        if (!_started)
        {
            Start();
        }
    }

    void ApplyCacheOrLocal()
    {
        FeatureFlagSnapshot? cached = null;
        try
        {
            cached = _cache.Load();
        }
        catch
        {
            cached = null;
        }

        if (cached?.Flags is { Count: > 0 } || cached is not null)
        {
            cached!.Flags ??= [];
            ApplySnapshot(cached, FeatureFlagSource.Cache);
            return;
        }

        ApplyLocalSnapshot(raise: false);
    }

    void ApplyLocalSnapshot(bool raise)
    {
        var snapshot = new FeatureFlagSnapshot
        {
            Version = 1,
            Environment = _environment.ToString(),
            FetchedAt = _clock.UtcNow,
            Flags = [.. _localDefinitions.Values]
        };

        foreach (var pair in _options.LocalFlags)
        {
            if (snapshot.Find(pair.Key) is null)
            {
                snapshot.Flags.Add(new FeatureFlagDefinition
                {
                    Key = pair.Key,
                    Enabled = pair.Value
                });
            }
        }

        ApplySnapshot(snapshot, FeatureFlagSource.Local, raise);
    }

    void ApplySnapshot(FeatureFlagSnapshot snapshot, FeatureFlagSource source, bool raise = true)
    {
        lock (_gate)
        {
            _snapshot = snapshot;
            _source = source;
        }

        if (raise)
        {
            FlagsChanged?.Invoke(this, new FeatureFlagsChangedEventArgs(snapshot, source));
        }
    }

    void StartTimer()
    {
        if (_provider is null || _options.RefreshInterval <= TimeSpan.Zero)
        {
            return;
        }

        _timerCts = new CancellationTokenSource();
        _ = RunTimerAsync(_timerCts.Token);
    }

    async Task RunTimerAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(_options.RefreshInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                await RefreshSafeAsync().ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Disposed.
        }
    }

    async Task RefreshSafeAsync()
    {
        try
        {
            await RefreshAsync().ConfigureAwait(false);
        }
        catch
        {
            // Offline / HTTP errors keep the last cache or local fallback.
        }
    }

    void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);

    static Dictionary<string, FeatureFlagDefinition> BuildLocalDefinitions(FeatureFlagsOptions options)
    {
        var map = new Dictionary<string, FeatureFlagDefinition>(StringComparer.OrdinalIgnoreCase);
        foreach (var definition in options.LocalDefinitions)
        {
            if (!string.IsNullOrWhiteSpace(definition.Key))
            {
                map[definition.Key] = definition;
            }
        }

        return map;
    }
}
