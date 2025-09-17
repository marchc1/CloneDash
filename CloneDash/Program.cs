/*
    A *LOT* of this is subject to change. This is a prototype, and just a testbed of basic game functionality.
*/

using CloneDash.Compatibility.MuseDash;
using CloneDash.Data;
using CloneDash.Game;

using Nucleus;
using Nucleus.Engine;
using Nucleus.Files;

using static CloneDash.Compatibility.CustomAlbums.CustomAlbumsCompatibility;

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
		if (!NucleusSingleton.TryRedirect("Clone Dash", args))
			return;		
		EngineCore.GameInfo = new() {
			AppName = "Clone Dash",
			AppVersion = GameVersion.Current.ToString(),
			AppIdentifier = "com.github.marchc1.CloneDash",
			AppCreator = "March (github/marchc1)",
			AppURL = "https://github.com/marchc1/CloneDash",
			AppType = Nucleus.Types.AppType.Game
		};
		EngineCore.Initialize(1600, 900, "Clone Dash", args, gameThreadInit: GameMain);
		EngineCore.StartMainThread();
		RichPresenceSystem.Shutdown();
	}
	static void AddCustomPath(SearchPath basePath, bool createIfMissing = true) {
		var custom = Filesystem.AddSearchPath("custom", DiskSearchPath.Combine(basePath, "custom", createIfMissing: createIfMissing));
		{
			Filesystem.AddSearchPath("chars", DiskSearchPath.Combine(custom, "chars/", createIfMissing: createIfMissing));
			Filesystem.AddSearchPath("charts", DiskSearchPath.Combine(custom, "charts/", createIfMissing: createIfMissing));
			Filesystem.AddSearchPath("fevers", DiskSearchPath.Combine(custom, "fevers/", createIfMissing: createIfMissing));
			Filesystem.AddSearchPath("interludes", DiskSearchPath.Combine(custom, "interludes/", createIfMissing: createIfMissing));
			Filesystem.AddSearchPath("scenes", DiskSearchPath.Combine(custom, "scenes/", createIfMissing: createIfMissing));
		}
	}
	static void GameMain() {
		/*new Platform.MessageBoxBuilder()
			.WithTitle("This is a message box test!")
			.WithMessage(Environment.StackTrace)
			.WithIcon(MessageBoxIcon.Information)
			.WithButton("Print 'OK!'", () => Logs.Print("OK!"))
			.WithButton("Print 'No!'", () => Logs.Print("No!"))
			.Show();*/

		RichPresenceSystem.Initialize();
		NucleusSingleton.Request("Clone Dash");
		Interlude.ShouldSelectInterludeTexture = false;
		Interlude.Begin($"Initializing Clone Dash v{GameVersion.Current}...");

		{
			Interlude.Spin(submessage: "Initializing the Muse Dash compatibility layer...");
			MDCompatLayerInitResult res;
			if ((res = MuseDashCompatibility.InitializeCompatibilityLayer()) != MDCompatLayerInitResult.OK) {
				throw new Exception($"Muse Dash compatibility layer failed to initialize: {res switch {
					MDCompatLayerInitResult.SteamNotInstalled => "Steam is not installed or could not be found.",
					MDCompatLayerInitResult.MuseDashNotInstalled => "Muse Dash is not installed or could not be found.",
					MDCompatLayerInitResult.StreamingAssetsNotFound => "Muse Dash's assets could not be found, try validating MD game files",
					MDCompatLayerInitResult.NoteDataManagerNotFound => "Muse Dash's note data could not be found, try validating MD game files",
					MDCompatLayerInitResult.OperatingSystemNotCompatible => $"Your operating system, {Environment.OSVersion.ToString()}, is incompatible.",
					_ => res.ToString()
				}}");
			}
		}

		Interlude.Spin();

		// This sets up some base directories for the filesystem (default assets at the tail, with custom at the head)
		DiskSearchPath? musedash = null;
		if (MuseDashCompatibility.WhereIsMuseDashInstalled != null)
			musedash = Filesystem.AddSearchPath<DiskSearchPath>("musedash", MuseDashCompatibility.WhereIsMuseDashInstalled);

		var game = Filesystem.GetSearchPathID("game")[0];
		var appcache = Filesystem.GetSearchPathID("appcache")[0];
		var appdata = Filesystem.GetSearchPathID("appdata")[0];
		{
			// Custom assets should always be top priority for the filesystem
			if (MuseDashCompatibility.WhereIsMuseDashInstalled != null && musedash != null && Directory.Exists(Path.Combine(MuseDashCompatibility.WhereIsMuseDashInstalled, "Custom_Albums")))
				Filesystem.AddSearchPath("charts", DiskSearchPath.Combine(musedash, "Custom_Albums", createIfMissing: false));

			// Prioritize custom assets in order of new appdata/ -> game/
			AddCustomPath(appdata, createIfMissing: true);
			AddCustomPath(game, createIfMissing: false);

			// Downloaded charts, etc, mostly for MDMC API
			var download = Filesystem.AddSearchPath("download", DiskSearchPath.Combine(appcache, "download"));
			{
				Filesystem.AddSearchPath("charts", DiskSearchPath.Combine(download, "charts/"));
			}

			// tail: default asset fallbacks.
			// These get shipped with the game so they are readonly
			Filesystem.AddSearchPath("chars", DiskSearchPath.Combine(game, "assets/chars/", createIfMissing: false).MakeReadOnly());
			Filesystem.AddSearchPath("charts", DiskSearchPath.Combine(game, "assets/charts/", createIfMissing: false).MakeReadOnly());
			Filesystem.AddSearchPath("fevers", DiskSearchPath.Combine(game, "assets/fevers/", createIfMissing: false).MakeReadOnly());
			Filesystem.AddSearchPath("interludes", DiskSearchPath.Combine(game, "assets/interludes/", createIfMissing: false).MakeReadOnly());
			Filesystem.AddSearchPath("scenes", DiskSearchPath.Combine(game, "assets/scenes/", createIfMissing: false).MakeReadOnly());
		}

		DoCmdLineOps(CommandLine.Singleton, true);

		Interlude.Spin();
		Interlude.End();

		// Add an event listener to the singleton
		// this would run pre-anything in EngineCore Frame()
		NucleusSingleton.Redirect += NucleusSingleton_Redirect;
	}

	private static void NucleusSingleton_Redirect(string[] args) {
		Logs.Info("Received interprocess redirect!");
		CommandLineParser cmd = new CommandLineParser();
		cmd.FromArgs(args);
		EngineCore.Window.FocusWindow();
		DoCmdLineOps(cmd, false);
	}

	private static void DoCmdLineOps(CommandLineParser cmd, bool first) {
		if (cmd.TryGetParam<string>("md_level", out var md_level)) {
			cmd.TryGetParam<int>("difficulty", out var difficulty);
			MuseDashSong song = MuseDashCompatibility.Songs.First(x => x.BaseName == md_level);
			var sheet = song.GetSheet(difficulty);

			var lvl = new DashGameLevel(sheet);
			if (!first) Interlude.Begin("Interprocess load started!");
			EngineCore.LoadLevel(lvl, cmd.IsParamTrue("autoplay"));
			if (!first) Interlude.End();
		}

		else if (cmd.TryGetParam<string>("cam_level", out var cam_level)) {
			Logs.Info($"cam_level specified: {cam_level}");
			cmd.TryGetParam<int>("difficulty", out var difficulty);

			CustomChartsSong song = new CustomChartsSong(cam_level);
			ChartSheet sheet;
			switch (Path.GetExtension(cam_level)) {
				case ".bms":
					sheet = song.LoadFromDiskBMS(cam_level);
					break;
				default:
					sheet = song.GetSheet(difficulty);
					break;
			}

			var lvl = new DashGameLevel(sheet);
			if (!first) Interlude.Begin("Interprocess load started!");
			EngineCore.LoadLevel(lvl, cmd.IsParamTrue("autoplay"), cmd.GetParam("startmeasure", 0d));
			if (!first) Interlude.End();
		}

		else if(first) {
			EngineCore.LoadLevel(new MainMenuLevel());
		}
	}
}
