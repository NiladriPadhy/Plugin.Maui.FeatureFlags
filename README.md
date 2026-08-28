# Plugin.Maui.FeatureFlags

A mobile-first feature flag system for **.NET MAUI** on **iOS** and **Android**.

```csharp
if (FeatureFlags.IsEnabled("new_checkout"))
{
    // ...
}
```

MAUI-aware targeting, in order:

```
Remote configuration
      ↓
Device
      ↓
OS / Version
      ↓
Country
      ↓
User
      ↓
Percentage rollout
```

```csharp
var enabled = await featureFlags.IsEnabledAsync("new_voip_engine");
```

## Install

```bash
dotnet add package Plugin.Maui.FeatureFlags
```

Target frameworks: `net10.0`, `net10.0-android`, `net10.0-ios`.

## Quick start

```csharp
using Plugin.Maui.FeatureFlags;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiFeatureFlags(options =>
            {
                options.Environment = FeatureFlagEnvironment.Production;
                options.RemoteUri = new Uri("https://cdn.example.com/flags.json");
                options.LocalFlags["new_checkout"] = false;
                options.LocalFlags["new_voip_engine"] = true;
            });

        return builder.Build();
    }
}
```

Resolve `IFeatureFlags` from dependency injection, or use `FeatureFlags.Current`.

```csharp
if (FeatureFlags.IsEnabled("new_checkout"))
{
    // last known snapshot — never blocks on the network
}

var enabled = await FeatureFlags.IsEnabledAsync("new_voip_engine");
```

## What you get

| Capability | How |
| --- | --- |
| **Local fallback** | `LocalFlags` and `LocalDefinitions` used when a key is absent remotely. |
| **Remote configuration** | HTTP JSON snapshot, or a custom `IFeatureFlagProvider`. |
| **Percentage rollout** | Sticky 0–99 bucket from `userId` (else device id). Same user stays in the same bucket. |
| **User targeting** | `Identify(userId)`, allow lists, and exclude lists. |
| **App-version targeting** | `minAppVersion` / `maxAppVersion`. |
| **Device / OS** | Device id allow list, `iOS` / `Android`, min/max OS version. |
| **Country** | ISO country from `Identify`, options, or the device locale. |
| **Kill switches** | `killed: true` on a definition, or `options.KillSwitches`. |
| **Offline cache** | Last successful snapshot persisted under app data. |
| **Expiration** | `expiresAt` turns the flag off after that UTC instant. |
| **Environment** | `Development` / `Staging` / `Production` (`Dev` / `Stage` / `Prod` aliases). |

A flag is **on** only after every step of the cascade matches and `enabled` is `true`.

## Remote JSON

```json
{
  "version": 1,
  "environment": "Production",
  "flags": [
    {
      "key": "new_voip_engine",
      "enabled": true,
      "killed": false,
      "expiresAt": "2027-06-01T00:00:00Z",
      "environments": ["Production", "Staging"],
      "platforms": ["iOS", "Android"],
      "minOsVersion": "15.0",
      "countries": ["US", "IN"],
      "userIds": [],
      "excludedUserIds": [],
      "deviceIds": [],
      "minAppVersion": "2.0.0",
      "percentage": 25,
      "description": "New VoIP media engine"
    }
  ]
}
```

Empty arrays mean “no restriction” for that dimension. `percentage` is 0–100; omit it to skip rollout.

Host the file on any HTTPS CDN or API. Add headers if you need them:

```csharp
options.RemoteUri = new Uri("https://cdn.example.com/flags.json");
options.ConfigureRequest = request =>
{
    request.Headers.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
};
```

`If-None-Match` is sent automatically when the last fetch returned an ETag.

## Identify a user

```csharp
FeatureFlags.Identify("user-42", country: "IN");

var evaluation = FeatureFlags.Evaluate("beta_chat");
evaluation.Enabled;
evaluation.Reason;   // Matched, UserMismatch, NotInRollout, KillSwitch, ...
evaluation.Source;   // Remote, Cache, Local, Override
evaluation.RolloutBucket;
```

`ClearIdentity()` falls rollout back to the sticky device id.

## Kill switches and QA overrides

```csharp
options.KillSwitches.Add("legacy_billing");

featureFlags.SetOverride("new_checkout", true); // process-local QA force-on
```

Overrides win over kill switches so you can still test the on path.

## Without the generic host

```csharp
var flags = FeatureFlags.Create(new FeatureFlagsOptions
{
    Environment = FeatureFlagEnvironment.Staging,
    RemoteUri = new Uri("https://cdn.example.com/flags.json"),
    LocalFlags = { ["new_checkout"] = false }
});

flags.Start();
var enabled = await flags.IsEnabledAsync("new_voip_engine");
```

Use `StaticFeatureFlagProvider` when the snapshot is already in memory (tests, demos, embedded JSON).

## Platform notes

**Android** — declare network access if the host app does not already (required for remote refresh):

```xml
<uses-permission android:name="android.permission.INTERNET" />
<uses-permission android:name="android.permission.ACCESS_NETWORK_STATE" />
```

**iOS** — no extra `Info.plist` keys. The sticky device id is stored in Preferences (User Defaults).

| | Android | iOS | `net10.0` |
| --- | --- | --- | --- |
| Evaluation / cache / rollout | Yes | Yes | Yes (tests) |
| Device / OS / app context | `DeviceInfo` / `AppInfo` | `DeviceInfo` / `AppInfo` | Configurable fakes |
| Country | Locale / `Identify` | Locale / `Identify` | Options / `Identify` |
| HTTP remote + ETag | Yes | Yes | Yes |

## Sample

`samples/Plugin.Maui.FeatureFlags.Sample` shows environment, user, country, kill switch, expiration, and percentage rollout on a live device.

```bash
dotnet build src/Plugin.Maui.FeatureFlags/Plugin.Maui.FeatureFlags.csproj
dotnet pack src/Plugin.Maui.FeatureFlags/Plugin.Maui.FeatureFlags.csproj -c Release -o artifacts
dotnet test tests/Plugin.Maui.FeatureFlags.Tests/Plugin.Maui.FeatureFlags.Tests.csproj
dotnet build samples/Plugin.Maui.FeatureFlags.Sample/Plugin.Maui.FeatureFlags.Sample.csproj -f net10.0-android
```

## Pack from source

```bash
dotnet pack src/Plugin.Maui.FeatureFlags/Plugin.Maui.FeatureFlags.csproj -c Release -o artifacts
```

The `.nupkg` is written to `artifacts/Plugin.Maui.FeatureFlags.1.0.0.nupkg`.

## License

MIT

## Support

> If this plugin saved you a weekend of native plumbing, consider buying me a coffee.
> Your support keeps it maintained, documented, and free.

[![Buy Me A Coffee](https://img.shields.io/badge/Buy%20Me%20a%20Coffee-ffdd00?style=for-the-badge&logo=buy-me-a-coffee&logoColor=black)](https://buymeacoffee.com/npadhy)

This library stays open source. A coffee helps cover time for bug fixes, new features, and docs.
