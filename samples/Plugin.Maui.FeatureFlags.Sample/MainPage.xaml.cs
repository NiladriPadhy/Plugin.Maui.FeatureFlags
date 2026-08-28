using Plugin.Maui.FeatureFlags;

namespace Plugin.Maui.FeatureFlags.Sample;

public partial class MainPage : ContentPage
{
    readonly IFeatureFlags _flags;

    public MainPage(IFeatureFlags flags)
    {
        InitializeComponent();
        _flags = flags;
        _flags.FlagsChanged += (_, _) => MainThread.BeginInvokeOnMainThread(Refresh);
        UserEntry.Text = "user-42";
        CountryEntry.Text = "IN";
        Refresh();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Refresh();
    }

    void OnDevClicked(object? sender, EventArgs e) => SetEnvironment(FeatureFlagEnvironment.Development);

    void OnStagingClicked(object? sender, EventArgs e) => SetEnvironment(FeatureFlagEnvironment.Staging);

    void OnProductionClicked(object? sender, EventArgs e) => SetEnvironment(FeatureFlagEnvironment.Production);

    void OnIdentifyClicked(object? sender, EventArgs e)
    {
        var userId = string.IsNullOrWhiteSpace(UserEntry.Text) ? "user-42" : UserEntry.Text.Trim();
        var country = string.IsNullOrWhiteSpace(CountryEntry.Text) ? null : CountryEntry.Text.Trim();
        _flags.Identify(userId, country);
        Refresh();
    }

    void OnClearUserClicked(object? sender, EventArgs e)
    {
        _flags.ClearIdentity();
        Refresh();
    }

    async void OnRefreshClicked(object? sender, EventArgs e)
    {
        try
        {
            await _flags.RefreshAsync();
        }
        catch (Exception ex)
        {
            FlagsLabel.Text = ex.Message;
            return;
        }

        Refresh();
    }

    void OnOverrideClicked(object? sender, EventArgs e)
    {
        _flags.SetOverride("new_checkout", true);
        Refresh();
    }

    void SetEnvironment(FeatureFlagEnvironment environment)
    {
        _flags.SetEnvironment(environment);
        Refresh();
    }

    void Refresh()
    {
        var context = _flags.GetContext();
        ContextLabel.Text =
            $"Env {context.Environment} · {context.Platform} {context.OsVersion}{Environment.NewLine}" +
            $"Device {context.DeviceManufacturer} {context.DeviceModel} ({Short(context.DeviceId)}){Environment.NewLine}" +
            $"App {context.AppVersion} ({context.AppBuild}) · Country {context.Country ?? "—"}{Environment.NewLine}" +
            $"User {context.UserId ?? "(anonymous)"} · Snapshot {_flags.SnapshotSource}";

        var lines = _flags.EvaluateAll().Select(static evaluation =>
        {
            var state = evaluation.Enabled ? "ON " : "OFF";
            var bucket = evaluation.RolloutBucket is { } value ? $" bucket={value}" : "";
            return $"{state}  {evaluation.Key,-18}  {evaluation.Reason} · {evaluation.Source}{bucket}";
        });

        FlagsLabel.Text = string.Join(Environment.NewLine, lines);
    }

    static string Short(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "—" : value.Length <= 8 ? value : value[..8];
}
