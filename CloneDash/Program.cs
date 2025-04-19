/*
    A *LOT* of this is subject to change. This is a prototype, and just a testbed of basic game functionality.
*/

using System.Runtime.InteropServices;
using CloneDash.Game;

using Nucleus;
using Nucleus.Core;
using Nucleus.Engine;
using Raylib_cs;
using static CloneDash.CustomAlbumsCompatibility;
using static CloneDash.MuseDashCompatibility;

namespace CloneDash
{
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
			MuseDashCompatibility.InitializeCompatibilityLayer();
			EngineCore.Initialize(1600, 900, "Clone Dash", args);
			EngineCore.GameInfo = new() {
				GameName = "Clone Dash"
			};

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

			// need a better way to implement custom scenes

			Filesystem.AddPath("custom", Path.Combine(Filesystem.Resolve("game"), "custom/"));
			Filesystem.AddPath("download", Path.Combine(Filesystem.Resolve("game"), "download/"));

			Filesystem.AddPath("chars", Path.Combine(Filesystem.Resolve("custom"), "chars/"));
			Filesystem.AddPath("charts", Path.Combine(Filesystem.Resolve("custom"), "charts/"));
			Filesystem.AddPath("interludes", Path.Combine(Filesystem.Resolve("custom"), "interludes/"));
			Filesystem.AddPath("scenes", Path.Combine(Filesystem.Resolve("custom"), "scenes/"));

			Filesystem.AddPath("chars", Path.Combine(Filesystem.Resolve("game"), "assets/chars/"));
			Filesystem.AddPath("charts", Path.Combine(Filesystem.Resolve("game"), "assets/charts/"));
			Filesystem.AddPath("interludes", Path.Combine(Filesystem.Resolve("game"), "assets/interludes/"));
			Filesystem.AddPath("scenes", Path.Combine(Filesystem.Resolve("game"), "assets/scenes/"));

			Filesystem.AddPath("charts", Path.Combine(Filesystem.Resolve("download"), "charts/")); // TODO: mdmc.moe downloaded charts should go in here!

			EngineCore.Start();
		}
	}
}
