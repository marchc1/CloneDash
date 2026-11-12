using CloneDash.Common.Game;
using CloneDash.Common.Gamemodes;
using CloneDash.Common.Songs;
using CloneDash.Common.UI.Searching;
using CloneDash.Menu.Searching;
using Nucleus;

namespace CloneDash.Common;

public static class LevelTransitions
{
	public static event Action? OnLoadMainMenu;
	public static event Action<ISongChart, RhythmGameLoadParameters>? OnLoadSongChart;
	public static event Action<SongSelector, ISong>? OnLoadSongSelector;

	public static string? PendingError { get; private set; }

	public static void LoadMainMenu() => OnLoadMainMenu?.Invoke();

	public static void LoadMainMenu(string error) {
		PendingError = error;
		LoadMainMenu();
	}

	public static string? TakePendingError() {
		var error = PendingError;
		PendingError = null;
		return error;
	}
	public static void LoadSongChart(ReadOnlySpan<char> interludeText, ISongChart? chart, RhythmGameLoadParameters parms) {
		if (chart == null)
			return;

		if(EngineCore.InLevelFrame){
			string interludeStr = new(interludeText.SliceNullTerminatedString());
			MainThread.RunASAP(() => loader(interludeStr, chart, parms));
		}
		else{
			loader(interludeText, chart, parms);
		}
	}

	static void loader(ReadOnlySpan<char> interludeText, ISongChart chart, RhythmGameLoadParameters parms) {
		if (!interludeText.IsEmpty)
			Interlude.Begin(new(interludeText.SliceNullTerminatedString()));
		try {
			OnLoadSongChart?.Invoke(chart, parms);
		}
		catch (ContentSlotException ex) {
			Logs.Error($"Couldn't load the chart: {ex.Message}");
			LoadMainMenu(ex.Message);
		}
		finally {
			if (!interludeText.IsEmpty)
				Interlude.End();
		}
	}

	public static void LoadSongSelector(SongSelector selector, ISong? song) {
		if (song == null)
			return;

		OnLoadSongSelector?.Invoke(selector, song);
	}
}
