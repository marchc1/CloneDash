using Newtonsoft.Json;
using Nucleus;
using Nucleus.Audio;
using Nucleus.ManagedMemory;
using Nucleus.Util;
using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CloneDash.Systems.CustomAlbums
{
	public struct MDMCSheet
	{
		[JsonProperty("_id")]
		public string ID;

		[JsonProperty("chart_id")]
		public string ChartID;

		[JsonProperty("difficulty")]
		public string Difficulty;

		[JsonProperty("ranked_difficulty")]
		public float? RankedDifficulty;

		[JsonProperty("charter")]
		public string Charter;

		[JsonProperty("hash")]
		public string Hash;

		[JsonProperty("map")]
		public int Map;
	}

	public struct MDMCChartAnalytics
	{
		[JsonProperty("likes")]
		public int[] Likes;

		[JsonProperty("plays")]
		public int Plays;

		[JsonProperty("downloads")]
		public int Downloads;

		[JsonProperty("views")]
		public int Views;
	}

	public struct MDMCChart
	{
		[JsonProperty("_id")]
		public string ID;

		[JsonProperty("title")]
		public string Title;

		[JsonProperty("title_romanized")]
		public string? TitleRomanized;

		[JsonProperty("artist")]
		public string Artist;

		[JsonProperty("charter")]
		public string Charter;

		[JsonProperty("bpm")]
		public string BPM;

		[JsonProperty("length")]
		public double Length;

		[JsonProperty("owner_uid")]
		public int OwnerUID;

		[JsonProperty("sheets")]
		public MDMCSheet[] Sheets;

		[JsonProperty("analytics")]
		public MDMCChartAnalytics Analytics;

		[JsonProperty("ranked")]
		public bool Ranked;

		[JsonProperty("searchTags")]
		public string[] SearchTags;

		/// <summary>
		/// ISO 8601 timestamp
		/// </summary>
		[JsonProperty("timestamp")]
		public string Timestamp;

		public DateTime TimestampDateTime => DateTime.Parse(Timestamp);

		[JsonProperty("likes")]
		public int? Likes;

		[JsonProperty("maxDifficulty")]
		public int MaxDifficulty;

		public string CoverURL => $"https://cdn.mdmc.moe/{ID}/cover.png";
		public string MP3URL => $"https://cdn.mdmc.moe/{ID}/music.mp3";
		public string OGGURL => $"https://cdn.mdmc.moe/{ID}/music.ogg";
		public string DemoMP3URL => $"https://cdn.mdmc.moe/{ID}/demo.mp3";
		public string DemoOGGURL => $"https://cdn.mdmc.moe/{ID}/demo.ogg";

		public int GetLikes() => Likes ?? Analytics.Likes.Length;

		public void GetCoverAsTexture(Action<Texture?> callback) {
			string coverURL = CoverURL;
			ThreadSystem.SpawnBackgroundWorker(() => {
				var task = Task.Run(() => MDMCWebAPI.Http.GetAsync(coverURL));
				task.Wait();
				var response = task.Result;

				MainThread.RunASAP(() => {
					if (!response.IsSuccessStatusCode)
						callback?.Invoke(null);
					else {
						Image img = Raylib.LoadImageFromMemory(".png", response.Content.ReadAsStream().ToMemoryStream().ToArray());
						Texture tex = new Texture(EngineCore.Level.Textures, Raylib.LoadTextureFromImage(img), true);
						Raylib.UnloadImage(img);

						callback?.Invoke(tex);
					}
				});
			});
		}

		public void GetMusicTrack(Action<MusicTrack?> callback, bool demo) {
			string oggURL = demo ? DemoOGGURL : OGGURL;
			string mp3URL = demo ? DemoMP3URL : MP3URL;
			ThreadSystem.SpawnBackgroundWorker(() => {
				var task = Task.Run(() => MDMCWebAPI.Http.GetAsync(oggURL));
				task.Wait();
				var response = task.Result;

				if (!response.IsSuccessStatusCode) {
					task = Task.Run(() => MDMCWebAPI.Http.GetAsync(mp3URL));
					task.Wait();
					response = task.Result;
				}

				MainThread.RunASAP(() => {
					if (!response.IsSuccessStatusCode)
						callback?.Invoke(null);
					else {
						MusicTrack track = EngineCore.Level.Sounds.LoadMusicFromMemory(response.Content.ReadAsStream().ToMemoryStream().ToArray());

						callback?.Invoke(track);
					}
				});
			});
		}
		public void DownloadTo(string filename, Action<bool> callback) {
			var id = ID;

			ThreadSystem.SpawnBackgroundWorker(() => {
				var task = Task.Run(() => MDMCWebAPI.Http.GetAsync($"https://api.mdmc.moe/v2/charts/{id}/download"));
				task.Wait();
				var response = task.Result;

				MainThread.RunASAP(() => {
					if (!response.IsSuccessStatusCode)
						callback?.Invoke(false);
					else {
						Directory.CreateDirectory(Path.GetDirectoryName(filename));
						using (var content = response.Content.ReadAsStream())
						using (FileStream fileOut = new FileStream(filename, FileMode.Create, FileAccess.Write)) {
							content.CopyTo(fileOut);
						}
						callback?.Invoke(true);
					}
				});
			});
		}
	}

	public struct MDMCProfile
	{
		[JsonProperty("bio")] public string Bio;
		[JsonProperty("badges")] public string[] Badges;
		[JsonProperty("peropero_id")] public string? PeroperoID;
		[JsonProperty("avatar")] public string Avatar;
		[JsonProperty("banner")] public string Banner;
	}

	public struct MDMCTopScore
	{
		[JsonProperty("hash")] public string Hash;
		[JsonProperty("rankedScore")] public double RankedScore;
	}

	public struct MDMCRanking
	{
		[JsonProperty("ranked_score")] public double RankedScore;
		[JsonProperty("top_scores")] public MDMCTopScore[] TopScores;
	}

	public struct MDMCSheetUser
	{
		[JsonProperty("_id")] public string ID;
		[JsonProperty("uid")] public int UserID;
		[JsonProperty("discord_id")] public string DiscordID;
		[JsonProperty("username")] public string Username;
		[JsonProperty("banned")] public bool Banned;
		[JsonProperty("profile")] public MDMCProfile Profile;
		[JsonProperty("ranking")] public MDMCRanking Ranking;
	}

	public struct MDMCScore
	{
		[JsonProperty("_id")] public string ID;
		[JsonProperty("uid")] public int UserID;
		[JsonProperty("user")] public MDMCSheetUser User;
		[JsonProperty("hash")] public string Hash;
		[JsonProperty("sheet")] public string SheetID;
		[JsonProperty("score")] public int Score;
		[JsonProperty("acc")] public float Accuracy;
		[JsonProperty("combo")] public int Combo;
		[JsonProperty("character_id")] public int CharacterID;
		[JsonProperty("elfin_id")] public int ElfinID;
		[JsonProperty("timestamp")] public string Timestamp;
	}


	public class MDMCWebAPIResponse(HttpResponseMessage message)
	{
		public bool Errored => !message.IsSuccessStatusCode;

		public HttpStatusCode Status => message.StatusCode;

		public T? FromJSON<T>() {
			var task = Task.Run(() => message.Content.ReadAsStringAsync());
			task.Wait();
			var response = task.Result;

			return JsonConvert.DeserializeObject<T>(response);
		}
	}

	public class MDMCWebAPIPromise
	{

		private bool __finished = false;
		private Action<MDMCWebAPIResponse>? __callback = null;

		public bool Finished => __finished;

		public void Then(Action<MDMCWebAPIResponse> callback) => __callback = callback;

		public MDMCWebAPIPromise(string url) {
			ThreadSystem.SpawnBackgroundWorker(() => {
				var task = Task.Run(() => MDMCWebAPI.Http.GetAsync(url));
				task.Wait();
				var response = task.Result;

				MainThread.RunASAP(() => {
					__finished = true;
					__callback?.Invoke(new(response));
				});
			});
		}
	}

	/// <summary>
	/// mdmc.moe API v2 methods.
	/// </summary>
	public static class MDMCWebAPI
	{
		public static readonly HttpClient Http = new HttpClient();
		public const string WEBAPI_ENDPOINT = "https://api.mdmc.moe/v2";

		public enum Sort
		{
			LikesCount,
			Title,
			Timestamp,
			MaxDifficulty
		}
		private static string getSort(Sort sort) => sort switch {
			Sort.LikesCount => "likesCount",
			Sort.Title => "title",
			Sort.Timestamp => "timestamp",
			Sort.MaxDifficulty => "maxDifficulty",
			_ => throw new Exception("Unsupported Sort value!")
		};

		private static string buildParameters(Dictionary<string, object> parameters) {
			string[] list = new string[parameters.Count];
			int i = 0;
			foreach (var kvp in parameters) {
				string key = kvp.Key;
				string value = kvp.Value switch {
					string s => s,
					Sort s => getSort(s),
					_ => kvp.Value.ToString() ?? throw new Exception("null")
				};

				list[i] = $"{key}={value}";
				i++;
			}

			return string.Join("&", list);
		}

		public static string BuildFetchRequestURL(string endpoint, Dictionary<string, object> parameters) =>
			$"{WEBAPI_ENDPOINT}/{endpoint}?{buildParameters(parameters)}";

		public static MDMCWebAPIPromise SearchCharts(string? query = null, Sort sort = Sort.LikesCount, int page = 1, bool rankedOnly = false) {
			var url = BuildFetchRequestURL("charts/search", new() {
				{ "q", query ?? "" },
				{ "sort", sort },
				{ "page", page },
				{ "rankedOnly", rankedOnly }
			});

			return new MDMCWebAPIPromise(url);
		}

		public static MDMCWebAPIPromise ChartInfo(string chartID) => new MDMCWebAPIPromise($"{WEBAPI_ENDPOINT}/charts/{chartID}");
		public static MDMCWebAPIPromise ChartInfo(MDMCChart chart) => ChartInfo(chart.ID);

		public static MDMCWebAPIPromise ChartSheets(string chartID) => new MDMCWebAPIPromise($"{WEBAPI_ENDPOINT}/charts/{chartID}/sheets");
		public static MDMCWebAPIPromise ChartSheets(MDMCChart chart) => ChartSheets(chart.ID);

		public static MDMCWebAPIPromise ChartLeaderboards(string chartID, string sheetID, int pageID = 1) => new MDMCWebAPIPromise($"{WEBAPI_ENDPOINT}/charts/{chartID}/sheets/{sheetID}?page={pageID}");
		public static MDMCWebAPIPromise ChartLeaderboards(MDMCChart chart, string sheetID, int pageID = 1) => ChartLeaderboards(chart.ID, sheetID, pageID);
		public static MDMCWebAPIPromise ChartLeaderboards(MDMCChart chart, MDMCSheet sheet, int pageID = 1) => ChartLeaderboards(chart.ID, sheet.ID, pageID);
		public static MDMCWebAPIPromise ChartLeaderboards(MDMCSheet sheet, int pageID = 1) => ChartLeaderboards(sheet.ChartID, sheet.ID, pageID);
	}
}
