/*
    A *LOT* of this is subject to change. This is a prototype, and just a testbed of basic game functionality.
*/

using System.Diagnostics;
using System.Runtime.InteropServices;
using CloneDash.Game;
using CloneDash.Scripting;
using Nucleus;
using Nucleus.Engine;
using Nucleus.Files;
using Raylib_cs;
using static CloneDash.CustomAlbumsCompatibility;
using static CloneDash.MuseDashCompatibility;

namespace CloneDash;

// I've been testing with these levels:
/*
        8bit_adventurer_map3
        bass_telekinesis_map3
        can_i_friend_you_on_bassbook_lol_map3
        night_of_knights_map3
        hg_makaizou_polyvinyl_shounen_map3

        kyouki_ranbu_map3
        ourovoros_map3
        mujinku_vacuum_track_add8e6_map3
        the_89s_momentum_map3
    */


internal class Program
{
	static void Main(string[] args) {
		MainThread.Thread = Thread.CurrentThread; // allows logging before engine core fully gets setup

		EngineCore.Initialize(1600, 900, "Clone Dash", args);
		EngineCore.GameInfo = new() {
			GameName = "Clone Dash"
		};

		Interlude.ShouldSelectInterludeTexture = false;
		Interlude.Begin("Initializing...");

		{
			Interlude.Spin(submessage: "Initializing the Muse Dash compatibility layer...");
			MuseDashCompatibility.InitializeCompatibilityLayer();
		}

		if (CommandLineArguments.IsParamTrue("fullscreen")) {
			var monitor = Raylib.GetCurrentMonitor();
			Raylib.SetWindowSize(Raylib.GetMonitorWidth(monitor), Raylib.GetMonitorHeight(monitor));
			EngineCore.InFullscreen = true;
		}

		if (CommandLineArguments.TryGetParam<string>("md_level", out var md_level)) {
			CommandLineArguments.TryGetParam<int>("difficulty", out var difficulty);
			MuseDashSong song = MuseDashCompatibility.Songs.First(x => x.BaseName == md_level);
			var sheet = song.GetSheet(difficulty);

			var lvl = new CD_GameLevel(sheet);
			EngineCore.LoadLevel(lvl, CommandLineArguments.IsParamTrue("autoplay"));
		}

		else if (CommandLineArguments.TryGetParam<string>("cam_level", out var cam_level)) {
			CommandLineArguments.TryGetParam<int>("difficulty", out var difficulty);
			CustomChartsSong song = new CustomChartsSong(cam_level);
			var sheet = song.GetSheet(difficulty);

			var lvl = new CD_GameLevel(sheet);
			EngineCore.LoadLevel(lvl, CommandLineArguments.IsParamTrue("autoplay"));
		}

		else {
			EngineCore.LoadLevel(new CD_MainMenu());
		}

		Interlude.Spin();

		// This sets up some base directories for the filesystem (default assets at the tail, with custom at the head)
		DiskSearchPath? musedash = null;
		if (MuseDashCompatibility.WhereIsMuseDashInstalled != null)
			musedash = Filesystem.AddSearchPath<DiskSearchPath>("musedash", MuseDashCompatibility.WhereIsMuseDashInstalled);

		var game = Filesystem.GetSearchPathID("game")[0];
		{
			// Custom assets should always be top priority for the filesystem
			if (MuseDashCompatibility.WhereIsMuseDashInstalled != null && musedash != null && Directory.Exists(Path.Combine(MuseDashCompatibility.WhereIsMuseDashInstalled, "Custom_Albums")))
				Filesystem.AddSearchPath("charts", DiskSearchPath.Combine(musedash, "Custom_Albums"));

			var custom = Filesystem.AddSearchPath("custom", DiskSearchPath.Combine(game, "custom"));
			{
				Filesystem.AddSearchPath("chars", DiskSearchPath.Combine(custom, "chars/"));
				Filesystem.AddSearchPath("charts", DiskSearchPath.Combine(custom, "charts/"));
				Filesystem.AddSearchPath("interludes", DiskSearchPath.Combine(custom, "interludes/"));
				Filesystem.AddSearchPath("scenes", DiskSearchPath.Combine(custom, "scenes/"));
			}

			// Downloaded charts, etc, mostly for MDMC API
			var download = Filesystem.AddSearchPath("download", DiskSearchPath.Combine(game, "download"));
			{
				Filesystem.AddSearchPath("charts", DiskSearchPath.Combine(download, "charts/"));
			}

			// tail: default asset fallbacks
			Filesystem.AddSearchPath("chars", DiskSearchPath.Combine(game, "assets/chars/"));
			Filesystem.AddSearchPath("charts", DiskSearchPath.Combine(game, "assets/charts/"));
			Filesystem.AddSearchPath("interludes", DiskSearchPath.Combine(game, "assets/interludes/"));
			Filesystem.AddSearchPath("scenes", DiskSearchPath.Combine(game, "assets/scenes/"));
		}
		Interlude.Spin();
		Interlude.End();
		EngineCore.Start();
	}
}