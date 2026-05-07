using CloneDash.Common.Gamemodes;
using CloneDash.Common.Songs;
using CloneDash.Menu.Searching;
using Nucleus;

namespace CloneDash.Common;

public static class LevelTransitions
{
	public static event Action? OnLoadMainMenu;
	public static event Action<ISongChart, GameLoadGenericParameters>? OnLoadSongChart;
	public static event Action<SongSelector, ISong>? OnLoadSongSelector;

	public static void LoadMainMenu() => OnLoadMainMenu?.Invoke();
	public static void LoadSongChart(ReadOnlySpan<char> interludeText, ISongChart? chart, GameLoadGenericParameters parms) {
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

	static void loader(ReadOnlySpan<char> interludeText, ISongChart chart, GameLoadGenericParameters parms) {
		if (!interludeText.IsEmpty)
			Interlude.Begin(new(interludeText.SliceNullTerminatedString()));
		OnLoadSongChart?.Invoke(chart, parms);
		if (!interludeText.IsEmpty)
			Interlude.End();
	}

	public static void LoadSongSelector(SongSelector selector, ISong? song) {
		if (song == null)
			return;

		OnLoadSongSelector?.Invoke(selector, song);
	}
}
