/*
    A *LOT* of this is subject to change. This is a prototype, and just a testbed of basic game functionality.
*/

using CloneDash.Compatibility.MuseDash;
using CloneDash.Data;
using CloneDash.Game;
using CloneDash.Systems;

using Nucleus;
using Nucleus.Common.Commands;
using Nucleus.Common.Engine;
using Nucleus.Common.FileSystem;
using Nucleus.Engine;
using Nucleus.Files;
using Nucleus.NewEngine;
using Nucleus.UI;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using Velopack;
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
	static void Main() {
		// Installer stuff, ignored if using zip.
		VelopackApp.Build().Run();

		if (!NucleusSingleton.TryRedirect("Clone Dash", Environment.CommandLine))
			return;

		CommandLineParser commandLine = new();
		commandLine.CreateCmdLine(Environment.CommandLine);

		IEngineAPI engineAPI = new EngineBuilder(commandLine)
			.WithComponent<IGameDLL, GameDLL>()
			.WithStandardComponents()
			.Build();

		engineAPI.SetStartupInfo(new() {
			AppName = "Clone Dash",
			AppVersion = GameVersion.Current.ToString(),
			AppIdentifier = "com.github.marchc1.CloneDash",
			AppCreator = "March (github/marchc1)",
			AppURL = "https://github.com/marchc1/CloneDash",
			AppType = Nucleus.Types.AppType.Game
		});

		using ServiceLocatorScope locatorScope = new(engineAPI);
		engineAPI.Run();
	}
}

public class GameDLL : IGameDLL
{
	public void Init() {
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
			musedash = filesystem.AddSearchPath<DiskSearchPath>("musedash", MuseDashCompatibility.WhereIsMuseDashInstalled);

		var game = filesystem.GetSearchPathID("game").First();
		var appcache = filesystem.GetSearchPathID("appcache").First();
		var appdata = filesystem.GetSearchPathID("appdata").First();
		{
			// Custom assets should always be top priority for the filesystem
			if (MuseDashCompatibility.WhereIsMuseDashInstalled != null && musedash != null && Directory.Exists(Path.Combine(MuseDashCompatibility.WhereIsMuseDashInstalled, "Custom_Albums")))
				filesystem.AddSearchPath("charts", DiskSearchPath.Combine(musedash, "Custom_Albums", createIfMissing: false));

			// Prioritize custom assets in order of new appdata/ -> game/
			AddCustomPath(appdata, createIfMissing: true);
			AddCustomPath(game, createIfMissing: false);

			// Downloaded charts, etc, mostly for MDMC API
			var download = filesystem.AddSearchPath("download", DiskSearchPath.Combine(appcache, "download"));
			{
				filesystem.AddSearchPath("charts", DiskSearchPath.Combine(download, "charts/"));
			}

			// tail: default asset fallbacks.
			// These get shipped with the game so they are readonly
			filesystem.AddSearchPath("chars", DiskSearchPath.Combine(game, "assets/chars/", createIfMissing: false).MakeReadOnly());
			filesystem.AddSearchPath("charts", DiskSearchPath.Combine(game, "assets/charts/", createIfMissing: false).MakeReadOnly());
			filesystem.AddSearchPath("fevers", DiskSearchPath.Combine(game, "assets/fevers/", createIfMissing: false).MakeReadOnly());
			filesystem.AddSearchPath("interludes", DiskSearchPath.Combine(game, "assets/interludes/", createIfMissing: false).MakeReadOnly());
			filesystem.AddSearchPath("scenes", DiskSearchPath.Combine(game, "assets/scenes/", createIfMissing: false).MakeReadOnly());
		}

		DoCmdLineOps(CommandLine(), true);

		Interlude.Spin();
		Interlude.End();

		// Add an event listener to the singleton
		// this would run pre-anything in EngineCore Frame()
		NucleusSingleton.Redirect += NucleusSingleton_Redirect;

		// Update checker
		new Task(async () => {
			var installerUpdate = await UpdateChecker.CheckAndApplyUpdates();
			if (installerUpdate) return; // Program is installed and no update needed

			// Program is not installed (portable), run local update check
			var release = await UpdateChecker.CheckForNewReleaseAsync();

			if (release != null) {
				MainThread.RunASAP(() => {
					try {
						var ui = EngineCore.Level?.UI;
						if (ui == null) {
							Logs.Warn("Update available but UI is not ready to show popup.");
							return;
						}

						string message = $"A new release ({release.TagName}) is available. Would you like to open the release page?";
						ui.DialogOKCancel("Update available", message, () => {
							try {
								var url = release.Url ?? $"https://github.com/{UpdateChecker.RepoOwner}/{UpdateChecker.RepoName}/releases";
								Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
							}
							catch (Exception ex) {
								Logs.Warn($"Failed to open release URL: {ex.Message}");
							}
						});
					}
					catch (Exception ex) {
						Logs.Warn($"Failed to display update popup: {ex.Message}");
					}
				});
			}
		}).Start();
	}

	static void AddCustomPath(SearchPath basePath, bool createIfMissing = true) {
		var custom = filesystem.AddSearchPath("custom", DiskSearchPath.Combine(basePath, "custom", createIfMissing: createIfMissing));
		{
			filesystem.AddSearchPath("chars", DiskSearchPath.Combine(custom, "chars/", createIfMissing: createIfMissing));
			filesystem.AddSearchPath("charts", DiskSearchPath.Combine(custom, "charts/", createIfMissing: createIfMissing));
			filesystem.AddSearchPath("fevers", DiskSearchPath.Combine(custom, "fevers/", createIfMissing: createIfMissing));
			filesystem.AddSearchPath("interludes", DiskSearchPath.Combine(custom, "interludes/", createIfMissing: createIfMissing));
			filesystem.AddSearchPath("scenes", DiskSearchPath.Combine(custom, "scenes/", createIfMissing: createIfMissing));
		}
	}
	private static void NucleusSingleton_Redirect(string args) {
		Logs.Info("Received interprocess redirect!");
		CommandLineParser cmd = new CommandLineParser();
		cmd.CreateCmdLine(args);
		EngineCore.Window.FocusWindow();
		DoCmdLineOps(cmd, false);
	}

	private static void DoCmdLineOps(ICommandLine cmd, bool first) {
		if (cmd.HasParm("-md_level")) {
			string md_level = cmd.ParmValue("-md_level", "");
			int difficulty = cmd.ParmValue("-difficulty", 0);
			MuseDashSong song = MuseDashCompatibility.Songs.First(x => x.BaseName == md_level);
			var sheet = song.GetSheet(difficulty);

			var lvl = new DashGameLevel(new DashGameParams(sheet).WithAutoplay(cmd.FindParm("-autoplay") != 0));
			if (!first) Interlude.Begin("Interprocess load started!");
			EngineCore.LoadLevel(lvl);
			if (!first) Interlude.End();
		}

		else if (cmd.HasParm("-cam_level")) {
			string cam_level = cmd.ParmValue("-cam_level", "");
			Logs.Info($"cam_level specified: {cam_level}");
			int difficulty = cmd.ParmValue("-difficulty", 0);

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

			var lvl = new DashGameLevel(new DashGameParams(sheet).WithAutoplay(cmd.FindParm("-autoplay") != 0).WithMeasure(cmd.ParmValue("-startmeasure", 0)));
			if (!first) Interlude.Begin("Interprocess load started!");
			EngineCore.LoadLevel(lvl);
			if (!first) Interlude.End();
		}

		else if (first) {
			EngineCore.LoadLevel(new MainMenuLevel());
		}
	}
}
