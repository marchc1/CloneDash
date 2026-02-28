using Nucleus;
using Nucleus.Types;
using Nucleus.UI;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Velopack;
using Velopack.Sources;

namespace CloneDash.Systems;

public static class UpdateChecker
{
    // TODO: Change to marchc1 before PR
    public const string RepoOwner = "ALLMarvelous";
    public const string RepoName = "CloneDash";

    public class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string? TagName { get; set; }

        [JsonPropertyName("published_at")]
        public DateTime? PublishedAt { get; set; }

        [JsonPropertyName("html_url")]
        public string? Url { get; set; }
    }

    /// <summary>
    /// Returns true if the update is being handled by Velopack
    /// Otherwise returns false
    /// </summary>
    public static async Task<bool> CheckAndApplyUpdates()
    {
        // Check GitHub for a new release
        var manager = new UpdateManager(new GithubSource($"https://github.com/{RepoOwner}/{RepoName}", null, false));

        if (!manager.IsInstalled)
        {
            // If it's running locally then do the other popup-based one
            Logs.Info("Running portable, skipping auto-update.");
            return false;
        }

        var newVersion = await manager.CheckForUpdatesAsync();
        if (newVersion == null)
        {
            Logs.Info("No newer release found.");
            return true;
        }

        Logs.Info($"Downloading update {newVersion.TargetFullRelease.Version}...");

        NumSlider? slider = null;

        MainThread.RunASAP(() =>
        {
            var ui = EngineCore.Level?.UI;
            if (ui == null)
            {
                Logs.Warn("Update available but UI is not ready to show popup.");
                return;
            }

            var dialog = ui.DialogBase("Updating");
            dialog.AutomateLayout = false;

            var lbl = dialog.Add<Label>();
            lbl.Text = $"Updating Clone Dash to v{newVersion.TargetFullRelease.Version}...";
            lbl.AutoSize = true;
            lbl.Anchor = lbl.Origin = Anchor.TopCenter;

            slider = dialog.Add<NumSlider>();
            slider.MinimumValue = 0;
            slider.MaximumValue = 100;
            slider.InputDisabled = true;
            slider.Dock = Dock.Bottom;
            slider.Anchor = slider.Origin = Anchor.BottomCenter;
            slider.Size = new Vector2F(256, 0);
        });

        await manager.DownloadUpdatesAsync(newVersion, (progress) =>
        {
            MainThread.RunASAP(() =>
            {
                if (slider == null) return;
                slider.Value = progress;
            });
        });

        manager.ApplyUpdatesAndRestart(newVersion);
        return true;
    }

    /// <summary>
    /// Returns the URL to the latest GitHub release if it's newer than the current build
    /// Otherwise returns null
    /// </summary>
    public static async Task<GitHubRelease?> CheckForNewReleaseAsync()
    {
        try
        {
            // Gets the build date from the thing in the csproj
            // If IDE is showing error, you probably haven't built the project yet
            var buildDate = DateTime.Parse(BuildInfo.BuildDate).ToUniversalTime();

            Logs.Info("CloneDash build date: " + buildDate.ToString());

            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("CloneDash");

            var url = $"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases/latest";

            var latestRelease = await client.GetFromJsonAsync<GitHubRelease>(url);

            if (latestRelease?.PublishedAt == null)
            {
                Logs.Warn("Failed to get latest release info from GitHub.");
                return null;
            }

            // Adding 15 minutes here to give time for GitHub Actions to compile
            // lol if you publish two releases within 15 minutes of each other
            if (latestRelease.PublishedAt.Value > buildDate.AddMinutes(15))
            {
                Logs.Info($"Newer release found: {latestRelease.TagName} published at {latestRelease.PublishedAt.Value.ToString()}");
                return latestRelease;
            }

            Logs.Info($"No newer release found ({latestRelease.PublishedAt}).");
        }
        catch (Exception ex)
        {
            Logs.Warn($"Update check failed: {ex.Message}");
        }

        return null;
    }
}