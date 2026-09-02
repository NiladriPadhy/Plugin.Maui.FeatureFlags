using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Plugin.Maui.FeatureFlags.Tests;

public sealed class HttpFeatureFlagProviderTests
{
    [Fact]
    public void ValidateRemoteUri_RejectsHttp_ByDefault()
    {
        Assert.Throws<InvalidOperationException>(() =>
            HttpFeatureFlagProvider.ValidateRemoteUri(new Uri("http://cdn.example.com/flags.json"), requireHttps: true));

        HttpFeatureFlagProvider.ValidateRemoteUri(new Uri("https://cdn.example.com/flags.json"), requireHttps: true);
        HttpFeatureFlagProvider.ValidateRemoteUri(new Uri("http://localhost/flags.json"), requireHttps: false);
    }

    [Fact]
    public async Task Fetch_RequiresValidSignature_WhenKeyConfigured()
    {
        var json = """{"flags":[{"key":"beta","enabled":true}]}""";
        var body = Encoding.UTF8.GetBytes(json);
        const string key = "test-secret";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var signature = Convert.ToHexString(hmac.ComputeHash(body));

        var handler = new ScriptedHandler(signature, json);
        using var http = new HttpClient(handler);
        var provider = new HttpFeatureFlagProvider(http, new Uri("https://cdn.example.com/flags.json"), signatureKey: key);

        var result = await provider.FetchAsync(new FeatureFlagRefreshRequest { Context = new FeatureFlagContext() }, CancellationToken.None);

        Assert.False(result.NotModified);
        Assert.Contains(result.Snapshot!.Flags, f => f.Key == "beta");
    }

    [Fact]
    public async Task Fetch_RejectsMissingSignature_WhenKeyConfigured()
    {
        var handler = new ScriptedHandler(signature: null, """{"flags":[]}""");
        using var http = new HttpClient(handler);
        var provider = new HttpFeatureFlagProvider(http, new Uri("https://cdn.example.com/flags.json"), signatureKey: "test-secret");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            provider.FetchAsync(new FeatureFlagRefreshRequest { Context = new FeatureFlagContext() }, CancellationToken.None));
    }

    sealed class ScriptedHandler : HttpMessageHandler
    {
        readonly string? _signature;
        readonly string _json;

        public ScriptedHandler(string? signature, string json)
        {
            _signature = signature;
            _json = json;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_json, Encoding.UTF8, "application/json")
            };
            if (_signature is not null)
            {
                response.Headers.TryAddWithoutValidation("X-FeatureFlags-Signature", _signature);
            }

            return Task.FromResult(response);
        }
    }
}
