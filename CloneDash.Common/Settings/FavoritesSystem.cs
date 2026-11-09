using CloneDash.Common.Songs;
using Newtonsoft.Json;
using Nucleus.Core;

namespace CloneDash.Common.Settings;

public class FavoritedSong
{
	public DateTime Time;
}

public class FavoritesSystem
{
	static Dictionary<string, FavoritedSong> favoritedUIDs = [];
	static DateTime LastSave = DateTime.MinValue;
	static bool SaveQueued;
	public static void FavoriteSong(ISong song) {
		string uid = song.GetUniqueID().ToString();
	}
	public static void UnfavoriteSong(ISong song) {

	}
	public static void Think() {
		if (!SaveQueued)
			return;

		DateTime now = DateTime.UtcNow;
		if ((now - LastSave).TotalSeconds > 1) {
			SaveFavorites();
			LastSave = now;
			SaveQueued = false;
		}
	}
	public static void LoadFavorites() {
		favoritedUIDs = JsonConvert.DeserializeObject<Dictionary<string, FavoritedSong>>(filesystem.ReadAllText("cfg", "favorites.json") ?? "{}") ?? [];
	}
	public static void SaveFavorites() {
		filesystem.WriteAllText("cfg", "favorites.json", JsonConvert.SerializeObject(favoritedUIDs));
	}
}
