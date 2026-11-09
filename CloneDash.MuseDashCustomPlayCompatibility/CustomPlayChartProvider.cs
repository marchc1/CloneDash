using CloneDash.Charts;
using CloneDash.Common;
using CloneDash.Common.Compatibility;
using CloneDash.Common.Songs;
using Nucleus;
using Nucleus.Commands;
using Nucleus.Core;
using Nucleus.Files;
using Nucleus.Util;

namespace CloneDash.MuseDashCustomPlayCompatibility;

public class CustomPlayChartSource : BaseContiguousSongSource
{
	public CustomPlayChartSource(BaseContiguousChartSongFilter? filter = null, ISongSourceState? parent = null) : base(CustomPlayChartProvider.GetCustomSongs(), filter, parent) {

	}

	public override ISongSourceState ProduceNewSource(IChartSongFilter filter) {
		if (filter is not BaseContiguousChartSongFilter contigFilter)
			throw new InvalidCastException("Invalid contigFilter");
		return new CustomPlayChartSource(contigFilter, this.GetRootSource());
	}
}

[MarkForStaticConstruction]
public class CustomPlayChartProvider() : ModProvider<ISong>("Muse Dash CustomPlay", PROVIDER_ID), IChartSongProvider
{
	public const string PROVIDER_ID = "musedashcustomplay";

	public static readonly ConVar mdcp_lastsong = new ConVar(nameof(mdcp_lastsong), "", FCvar.Saved, "The last selected song ID");
	public static readonly ConVar mdcp_lastfilter = new ConVar(nameof(mdcp_lastfilter), "", FCvar.Saved, "The last selected song filter");
	public static readonly ConVar mdcp_datapath = new ConVar(nameof(mdcp_datapath), "", FCvar.Saved, "Path to a Muse Dash CustomPlay MDCustomData folder, charts in custom/charts/customplay are always loaded");

	public IChartSongFilter? GetSavedFilter() => mdcp_lastfilter.GetString().IsEmpty ? null : JSON.Deserialize<BaseContiguousChartSongFilter>(new(mdcp_lastfilter.GetString()));
	public ISong? GetSavedSong() => ChartMod.Instance.GetDescriptorByUID(mdcp_lastsong.GetString());
	public void UpdateSavedFilter(IChartSongFilter? filter) => mdcp_lastfilter.SetValue(filter == null ? "" : JSON.Serialize((BaseContiguousChartSongFilter)filter));
	public void UpdateSavedSong(ISong? selectedSong) => mdcp_lastsong.SetValue(selectedSong == null ? "" : selectedSong.GetUniqueID());

	public ISong? FindSongByName(ReadOnlySpan<char> name) {
		name = name.SliceNullTerminatedString();
		foreach (var song in GetCustomSongs()) {
			if (name.Equals(song.FetchMetadata().Name, StringComparison.InvariantCultureIgnoreCase))
				return song;
		}
		return null;
	}

	public override IEnumerable<string> GetAvailableIDs() {
		foreach (var song in GetCustomSongs())
			yield return song.GetID();
	}

	public override ISong? FindByID(ReadOnlySpan<char> id) {
		foreach (var song in GetCustomSongs())
			if (id.Equals(song.GetID(), StringComparison.Ordinal))
				return song;
		return null;
	}

	public ISongSourceState NewState() => new CustomPlayChartSource();

	static readonly List<ISong> songs = [];
	static bool loadedFromDisk = false;
	static readonly Dictionary<UtlSymId_t, MD1_CustomPlaySong> loadedSongFilepaths = [];

	static IEnumerable<string> GetSearchDirectories() {
		if (filesystem.GetSearchPathID("charts").FirstOrDefault() is DiskSearchPath charts) {
			var local = charts.ResolveToAbsolute("customplay");
			System.IO.Directory.CreateDirectory(local);
			yield return local;
		}

		var dataPath = mdcp_datapath.GetString().ToString();
		if (!string.IsNullOrWhiteSpace(dataPath) && System.IO.Directory.Exists(dataPath))
			yield return dataPath;
	}

	static void CheckLoadDisk() {
		if (loadedFromDisk)
			return;

		loadedFromDisk = true;

		foreach (var directory in GetSearchDirectories()) {
			foreach (var score in System.IO.Directory.EnumerateFiles(directory, MD1_CustomPlaySong.SCORE_FILENAME, new EnumerationOptions { RecurseSubdirectories = true, MaxRecursionDepth = 3, IgnoreInaccessible = true }))
				LoadSong(score);
		}
	}

	public static IReadOnlyList<ISong> GetCustomSongs() {
		CheckLoadDisk();
		return songs;
	}

	public static MD1_CustomPlaySong? LoadSong(string path) {
		CheckLoadDisk();

		if (!MD1_CustomPlaySong.IsCustomPlayPath(path))
			return null;

		var scorePath = Path.GetFullPath(System.IO.Directory.Exists(path) ? Path.Combine(path, MD1_CustomPlaySong.SCORE_FILENAME) : path);
		if (loadedSongFilepaths.TryGetValue(scorePath.Hash(), out var loadedSong))
			return loadedSong;

		try {
			var song = new MD1_CustomPlaySong(scorePath);
			songs.Add(song);
			loadedSongFilepaths.Add(scorePath.Hash(), song);
			return song;
		}
		catch (Exception ex) {
			Logs.Error($"CustomPlay: failed to load '{scorePath}': {ex.Message}");
			return null;
		}
	}

	public override int GetSortIndex() => 1001;
	public IChartSongProvider.NavigationButtonInstructions GetNavigationButtonInstructions() => new() {
		Name = "Play CustomPlay Charts",
		Description = "Play a Muse Dash CustomPlay chart (.mdcpmf format).",
		Hue = 190,
		Icon = "icons/orange-slice.png"
	};
}
