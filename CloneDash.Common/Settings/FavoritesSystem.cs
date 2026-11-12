using CloneDash.Charts;
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
	static Dictionary<string, FavoritedSong>.AlternateLookup<ReadOnlySpan<char>> GetLookup() => favoritedUIDs.GetAlternateLookup<ReadOnlySpan<char>>();
	
	public static bool FavoriteSong(ISong song) {
		ReadOnlySpan<char> uid = song.GetUniqueID();
		var lookup = GetLookup();

		if (lookup.TryGetValue(uid, out _))
			return false;

		lookup[uid] = new() {
			Time = DateTime.UtcNow
		};
		SaveQueued = true;
		return true;
	}

	public static bool UnfavoriteSong(ISong song) {
		ReadOnlySpan<char> uid = song.GetUniqueID();
		var lookup = GetLookup();

		if (!lookup.Remove(uid))
			return false;

		SaveQueued = true;
		return true;
	}

	public static bool IsSongFavorited(ISong song) {
		return GetLookup().ContainsKey(song.GetUniqueID());
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

	public static IReadOnlyList<ISong> GetFavoritedSongs(){
		List<ISong> songs = new List<ISong>(favoritedUIDs.Count);
		foreach(var kvp in favoritedUIDs){
			ISong? song = ChartMod.Instance.GetDescriptorByUID(kvp.Key);
			if (song == null)
				continue;
			songs.Add(song);
		}
		return songs;
	}

	public static void LoadFavorites() {
		favoritedUIDs = JsonConvert.DeserializeObject<Dictionary<string, FavoritedSong>>(filesystem.ReadAllText("cfg", "favorites.json") ?? "{}") ?? [];
	}

	public static void SaveFavorites() {
		filesystem.WriteAllText("cfg", "favorites.json", JsonConvert.SerializeObject(favoritedUIDs));
	}
}
