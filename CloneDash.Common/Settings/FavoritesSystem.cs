using CloneDash.Common.Songs;

namespace CloneDash.Common.Settings;

public class FavoritedSong
{
	public DateTime Time;
}

public class FavoritesSystem
{
	readonly Dictionary<string, FavoritedSong> favoritedUIDs = [];
	public static void FavoriteSong(ISong song) { } // todo
}
