/*
    A *LOT* of this is subject to change. This is a prototype, and just a testbed of basic game functionality.
*/

using CloneDash.Common;
using CloneDash.Common.Gamemodes.MuseDash.V1.Data;
using CloneDash.Common.Songs;
using CloneDash.Compatibility.MuseDash;
using CloneDash.Game;
using CloneDash.Menu.Searching;
using CloneDash.Settings;
using CloneDash.Systems;

using Nucleus;
using Nucleus.Commands;
using Nucleus.Common.Commands;
using Nucleus.Common.Engine;
using Nucleus.Common.FileSystem;
using Nucleus.Engine;
using Nucleus.Files;
using Nucleus.NewEngine;
using Nucleus.UI;
using System.Diagnostics;
using Velopack;
using static CloneDash.CustomAlbumsCompatibility.CustomAlbums.CustomAlbumsCompatibility;

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

		LevelTransitions.OnLoadMainMenu += () => EngineCore.LoadLevel(new MainMenuLevel());
		LevelTransitions.OnLoadSongChart += (chart, parms) => chart.GetGamemode().Load(chart, parms);
		LevelTransitions.OnLoadSongSelector += LevelTransitions_OnLoadSongSelector;

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

	// This entire function is gross.
	// TODO: fix this awfulness
	private static void LevelTransitions_OnLoadSongSelector(SongSelector selector, Common.Songs.ISong song) {
		if (song is MD1_CustomChartsSong customChartsSong) {
			customChartsSong.DownloadOrPullFromCache((c) => {
				if (EngineCore.Level is not MainMenuLevel mml) {
					Logs.Warn($"Downloading custom charts song '{c.Name}' completed downloading in a non-main menu context, ignoring.");
					return;
				}

				mml.LoadChartSelector(selector, c);
			});
		}
		else
			EngineCore.Level.As<MainMenuLevel>().LoadChartSelector(selector, song);
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
			MD1CompatLayerInitResult res;
			if ((res = MuseDash1Compatibility.InitializeCompatibilityLayer()) != MD1CompatLayerInitResult.OK) {
				throw new Exception($"Muse Dash compatibility layer failed to initialize: {res switch {
					MD1CompatLayerInitResult.SteamNotInstalled => "Steam is not installed or could not be found.",
					MD1CompatLayerInitResult.MuseDashNotInstalled => "Muse Dash is not installed or could not be found.",
					MD1CompatLayerInitResult.StreamingAssetsNotFound => "Muse Dash's assets could not be found, try validating MD game files",
					MD1CompatLayerInitResult.NoteDataManagerNotFound => "Muse Dash's note data could not be found, try validating MD game files",
					MD1CompatLayerInitResult.OperatingSystemNotCompatible => $"Your operating system, {Environment.OSVersion.ToString()}, is incompatible.",
					_ => res.ToString()
				}}");
			}
		}

		Interlude.Spin();

		// This sets up some base directories for the filesystem (default assets at the tail, with custom at the head)
		DiskSearchPath? musedash = null;
		if (MuseDash1Compatibility.WhereIsMuseDashInstalled != null)
			musedash = filesystem.AddSearchPath<DiskSearchPath>("musedash", MuseDash1Compatibility.WhereIsMuseDashInstalled);

		var game = filesystem.GetSearchPathID("game").First();
		var appcache = filesystem.GetSearchPathID("appcache").First();
		var appdata = filesystem.GetSearchPathID("appdata").First();
		{
			// Custom assets should always be top priority for the filesystem
			if (MuseDash1Compatibility.WhereIsMuseDashInstalled != null && musedash != null && Directory.Exists(Path.Combine(MuseDash1Compatibility.WhereIsMuseDashInstalled, "Custom_Albums")))
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
		if (CommandLine().CheckParm("-pretime", out _)) {
			EngineCore.Interrupt(() => { }, true, "The executable was started with the '-pretime' command line parameter, which has been deprecated in favor of '-mdbmsc'. \nReplace '-pretime 0' with '-mdbmsc 1' in your MDBMSC settings.");
		}

		if (CommandLine().CheckParm("-mdbmsc", out _)) {
			// Mark a few convars as unchangable for this programs lifetime
			AudioSettings.snd_musicvolume.AddFlags(FCvar.AlwaysDefault);
			AudioSettings.snd_hitvolume.AddFlags(FCvar.AlwaysDefault);
			AudioSettings.snd_voicevolume.AddFlags(FCvar.AlwaysDefault);
			InputSettings.offset_judgement.AddFlags(FCvar.AlwaysDefault);
			InputSettings.offset_visual.AddFlags(FCvar.AlwaysDefault);
		}

		Interlude.Spin();
		Interlude.End();

		// Add an event listener to the singleton
		// this would run pre-anything in EngineCore Frame()
		NucleusSingleton.Redirect += NucleusSingleton_Redirect;

		// Update checker
		new Task(async () => {
			if (CommandLine().HasParm("-noupdate")) return;

			var installerUpdate = await UpdateChecker.CheckAndApplyUpdates();
			if (installerUpdate) return; // Update is being handled by Velopack

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
			MD1_Song song = MuseDash1Compatibility.Songs.First(x => x.BaseName == md_level);
			var chart = song.GetSheet(difficulty);

			if (chart != null)
				LevelTransitions.LoadSongChart(first ? "" : "Interprocess load started!", chart, new() {
					Autoplay = cmd.FindParm("-autoplay") != 0
				});
		}

		else if (cmd.HasParm("-cam_level")) {
			string cam_level = cmd.ParmValue("-cam_level", "");
			Logs.Info($"cam_level specified: {cam_level}");
			int difficulty = cmd.ParmValue("-difficulty", 0);

			MD1_CustomChartsSong song = new MD1_CustomChartsSong(cam_level);
			MD1_SongChart? chart;
			switch (Path.GetExtension(cam_level)) {
				case ".bms":
					chart = song.LoadFromDiskBMS(cam_level);
					break;
				default:
					chart = song.GetSheet(difficulty);
					break;
			}
			if (chart != null)
				LevelTransitions.LoadSongChart(first ? "" : "Interprocess load started!", chart, new() {
					Autoplay = cmd.FindParm("-autoplay") != 0,
					StartMeasure = cmd.ParmValue("-startmeasure", 0)
				});
		}

		else if (first) {
			LevelTransitions.LoadMainMenu();
		}
	}
}
