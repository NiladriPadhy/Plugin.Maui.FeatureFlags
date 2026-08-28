namespace Plugin.Maui.FeatureFlags;

/// <summary>
/// Fetches a <see cref="FeatureFlagSnapshot"/> JSON document over HTTP.
/// </summary>
public sealed class HttpFeatureFlagProvider : IFeatureFlagProvider
{
    readonly HttpClient _http;
    readonly Uri _uri;
    readonly Action<HttpRequestMessage>? _configure;
    readonly IClock _clock;

    /// <summary>
    /// Creates a provider that GETs <paramref name="uri"/>.
    /// </summary>
    public HttpFeatureFlagProvider(HttpClient httpClient, Uri uri, Action<HttpRequestMessage>? configureRequest = null)
        : this(httpClient, uri, configureRequest, SystemClock.Instance)
    {
    }

    internal HttpFeatureFlagProvider(HttpClient httpClient, Uri uri, Action<HttpRequestMessage>? configureRequest, IClock clock)
    {
        _http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _uri = uri ?? throw new ArgumentNullException(nameof(uri));
        _configure = configureRequest;
        _clock = clock;
    }

    /// <inheritdoc />
    public async Task<FeatureFlagFetchResult> FetchAsync(FeatureFlagRefreshRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var message = new HttpRequestMessage(HttpMethod.Get, _uri);
        message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (!string.IsNullOrWhiteSpace(request.ETag))
        {
            var tag = request.ETag.Trim();
            if (!tag.StartsWith('"') && !tag.StartsWith("W/", StringComparison.Ordinal))
            {
                tag = "\"" + tag.Trim('"') + "\"";
            }

            if (EntityTagHeaderValue.TryParse(tag, out var etag))
            {
                message.Headers.IfNoneMatch.Add(etag);
            }
        }

        _configure?.Invoke(message);

        using var response = await _http.SendAsync(message, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotModified)
        {
            return FeatureFlagFetchResult.Unchanged;
        }

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var snapshot = await JsonSerializer
            .DeserializeAsync(stream, FeatureFlagsJsonContext.Default.FeatureFlagSnapshot, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Remote feature-flag document was empty.");

        snapshot.ETag = response.Headers.ETag?.Tag ?? snapshot.ETag;
        snapshot.FetchedAt = _clock.UtcNow;
        snapshot.Flags ??= [];
        return FeatureFlagFetchResult.From(snapshot);
    }
}
