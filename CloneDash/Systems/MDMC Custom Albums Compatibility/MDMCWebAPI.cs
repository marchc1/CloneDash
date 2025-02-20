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
	public struct MDMCChartSheet
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
		public MDMCChartSheet[] Sheets;

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
		public int Likes;

		[JsonProperty("maxDifficulty")]
		public int MaxDifficulty;

		public string CoverURL => $"https://cdn.mdmc.moe/{ID}/cover.png";
		public string MP3URL => $"https://cdn.mdmc.moe/{ID}/music.mp3";
		public string OGGURL => $"https://cdn.mdmc.moe/{ID}/music.ogg";

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

		public void GetDemoAsMusicTrack(Action<MusicTrack?> callback) {
			string oggURL = OGGURL;
			string mp3URL = MP3URL;
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
	}
}
