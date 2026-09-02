using System.Security.Cryptography;
using System.Text;

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
    readonly string? _signatureKey;

    /// <summary>
    /// Creates a provider that GETs <paramref name="uri"/>.
    /// </summary>
    public HttpFeatureFlagProvider(HttpClient httpClient, Uri uri, Action<HttpRequestMessage>? configureRequest = null, string? signatureKey = null)
        : this(httpClient, uri, configureRequest, SystemClock.Instance, signatureKey)
    {
    }

    internal HttpFeatureFlagProvider(HttpClient httpClient, Uri uri, Action<HttpRequestMessage>? configureRequest, IClock clock, string? signatureKey = null)
    {
        _http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _uri = uri ?? throw new ArgumentNullException(nameof(uri));
        _configure = configureRequest;
        _clock = clock;
        _signatureKey = signatureKey;
    }

    /// <summary>
    /// Rejects non-HTTPS remotes unless the host opted in.
    /// </summary>
    public static void ValidateRemoteUri(Uri uri, bool requireHttps)
    {
        ArgumentNullException.ThrowIfNull(uri);
        if (requireHttps && !uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Feature-flag RemoteUri must use https. Set RequireHttps to false to allow http.");
        }
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

        var body = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
        VerifySignature(response, body);

        var snapshot = JsonSerializer.Deserialize(body, FeatureFlagsJsonContext.Default.FeatureFlagSnapshot)
            ?? throw new InvalidOperationException("Remote feature-flag document was empty.");

        snapshot.ETag = response.Headers.ETag?.Tag ?? snapshot.ETag;
        snapshot.FetchedAt = _clock.UtcNow;
        snapshot.Flags ??= [];
        return FeatureFlagFetchResult.From(snapshot);
    }

    void VerifySignature(HttpResponseMessage response, byte[] body)
    {
        if (string.IsNullOrWhiteSpace(_signatureKey))
        {
            return;
        }

        if (!response.Headers.TryGetValues("X-FeatureFlags-Signature", out var values)
            && !response.Content.Headers.TryGetValues("X-FeatureFlags-Signature", out values))
        {
            throw new InvalidOperationException("Remote feature-flag document is missing X-FeatureFlags-Signature.");
        }

        var provided = values.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(provided))
        {
            throw new InvalidOperationException("Remote feature-flag document is missing X-FeatureFlags-Signature.");
        }

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_signatureKey));
        var expected = hmac.ComputeHash(body);
        byte[] actual;
        try
        {
            actual = Convert.FromHexString(provided.Trim());
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException("Remote feature-flag signature is not valid hex.", ex);
        }

        if (actual.Length != expected.Length
            || !CryptographicOperations.FixedTimeEquals(expected, actual))
        {
            throw new InvalidOperationException("Remote feature-flag signature is invalid.");
        }
    }
}
