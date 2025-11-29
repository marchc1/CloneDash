using Nucleus;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace CloneDash.Systems;

public static class UpdateChecker
{
    public const string RepoOwner = "marchc1";
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