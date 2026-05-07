using CloneDash.Characters;
using CloneDash.Common;
using CloneDash.Common.Game;
using CloneDash.Common.Gamemodes;
using CloneDash.Common.Gamemodes.MuseDash;
using CloneDash.Common.Gamemodes.MuseDash.V1;
using CloneDash.Common.Gamemodes.MuseDash.V1.Data;
using CloneDash.Common.Scenes;
using CloneDash.Common.Songs;
using CloneDash.Compatibility.MuseDash;
using CloneDash.Game.Entities;
using CloneDash.Game.Events;
using CloneDash.Game.Input;
using CloneDash.Game.Logic;
using CloneDash.Game.Statistics;
using CloneDash.Menu;
using CloneDash.Scenes;
using CloneDash.Settings;
using CloneDash.Systems;
using Nucleus;
using Nucleus.Audio;
using Nucleus.Commands;
using Nucleus.Common.Audio;
using Nucleus.Common.Commands;
using Nucleus.Common.Input;
using Nucleus.Core;
using Nucleus.Engine;
using Nucleus.Entities;
using Nucleus.Input;
using Nucleus.ManagedMemory;
using Nucleus.Models.Runtime;
using Nucleus.Types;
using Nucleus.UI;
using Nucleus.UI.Elements;
using Nucleus.Util;
using Raylib_cs;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;
using System.Text.RegularExpressions;
using Color = Nucleus.Common.Types.Color;

namespace CloneDash.Game;

public struct DashGameParams
{
	public MD1_SongChart? Chart;
	public bool Autoplay;
	public int Measure;

	public DashGameParams(MD1_SongChart sheet) {
		Chart = sheet;
	}
}

[Nucleus.MarkForStaticConstruction]
public partial class MuseDash1Gamemode : IGamemodeDescriptor
{
	public ReadOnlySpan<char> GetUUID() => UUID;

	public IGame Load(ISongChart chart, in GameLoadGenericParameters parms) {
		var game = new MuseDash1Game(new((MD1_SongChart)chart) {
			Autoplay = parms.Autoplay,
			Measure = parms.StartMeasure ?? 0
		});
		EngineCore.LoadLevel(game);
		return game;
	}

	public static readonly string UUID = "gamemode/musedash1/standard";
}

public partial class MuseDash1Game(DashGameParams gameParameters) : Level, IGame
{
	public ISongChart? GetChart() => gameParameters.Chart;
	public static ConCommand musicseek = new(nameof(musicseek), (_, in args) => {
		var level = EngineCore.Level.AsNullable<MuseDash1Game>();
		if (level == null) {
			Logs.Warn("Not in game context!");
			return;
		}

		double d = args.Arg(1, -1d);
		if (d == -1) Logs.Warn("Did not specify a time!");
		else level.SeekTo(d);
	});

	public static readonly ConVar musicspeed = new(nameof(musicspeed), "1", FCvar.None, "Sets the music speed for the game.", 0.1, 4, (cv, _, _) => {
		if (EngineCore.Level is MuseDash1Game game)
			game.InitSpeedFromCvar();
	});

	double speed = 1;
	public double GetSpeed() {
		return speed;
	}

	public void SetSpeed(double speed) {
		this.speed = speed;
		audiosystem.SetSoundPitchControl(Music, (float)speed);
	}

	public void InitSpeedFromCvar() {
		SetSpeed(musicspeed.GetDouble());
	}

	private static void clonedash_openmdlevel_execute(ConCommand cmd, in TokenizedCommand args) {
		var md_level = args.Arg(1);
		if (md_level == null) {
			Logs.Warn("Provide a name.");
			return;
		}

		var map = args.Arg(2, -1);
		if (map <= 0) {
			Logs.Warn("Provide a difficulty.");
			return;
		}

		MD1_Song? song = MuseDash1Compatibility.FindSong(md_level);
		if (song == null) {
			Logs.Warn("Can't find that song.");
			Logs.Print("Here are some similar names:");
			foreach (var s in MuseDash1Compatibility.FindSimilarSongs(md_level))
				Logs.Print($"    {s.Name} ({s.BaseName})");
			return;
		}

		LevelTransitions.LoadSongChart($"Loading '{song.FetchMetadata(HumanLanguage.GetCurrentLanguage()).Name}'...", song.GetSheet(map), new() {
			Autoplay = args.Arg(3, 0) == 1
		});
	}
	private static void clonedash_openmdlevel_autocomplete(ConCommandBase cmd, string argsStr, TokenizedCommand args, int curArgPos, ref string[] returns, ref string[]? returnHelp) {
		if (curArgPos == 1) {
			var songs = MuseDash1Compatibility.FindSongsStartingWith(args.Arg(1));
			returns = [.. songs.Select(s => s.BaseName)];
			returnHelp = [.. songs.Select(s => $" '{s.Name}'")];
		}
		else if (curArgPos == 2) {
			var values = Enum.GetValues<MuseDashDifficulty>();
			returns = [.. values.Select(d => ((int)d).ToString())];
			returnHelp = [.. values.Select(d => d.ToString())];
		}
		else if (curArgPos == 3) {
			returns = ["0", "1"];
			returnHelp = ["Autoplay Off", "Autoplay On"];
		}
	}

	public static ConCommand mdlevel = new(nameof(mdlevel), clonedash_openmdlevel_execute, clonedash_openmdlevel_autocomplete, "Opens a Muse Dash level.");

	public static ConCommand cdrestest = new(nameof(cdrestest), (_, in args) => {
		Vector2F winSize;
		switch (args.ArgS(1)) {
			case "16:9": winSize = new(1600, 900); break;
			case "19.5:9": winSize = new(1950, 900); break;
			case "4:3": winSize = new(1600, 1200); break;
			default:
				Logs.Warn($"Expected 16:9, 19.5:9, or 4:3.");
				return;

		}

		OSMonitor monitor = EngineCore.Window.Monitor;
		Vector2F monPos = monitor.Position;
		Vector2F monSize = monitor.Size;
		Vector2F winPos = (monPos + (monSize / 2)) - (winSize / 2);
		EngineCore.Window.Position = new((int)winPos.X, (int)winPos.Y);
		EngineCore.Window.Size = new((int)winSize.X, (int)winSize.Y);
	});

	public static ConVar profilegameload = new(nameof(profilegameload), "0", FCvar.None, "Profiles the game during loading, then triggers an engine interrupt afterwards to tell you how long each individual component took.");

	public override bool IsInGame => true;

	public bool IsSeeking { get; private set; } = false;
	public void SeekTo(double time) {
		time = Math.Clamp(time, 0, audiosystem.GetPlaybackDuration(in Music));
		IsSeeking = true;

		ExitMashState();

		if (time < 0.06f)
			audiosystem.RestartSound(Music);
		else
			audiosystem.SetSoundPlayhead(Music, time);

		Stats.Reset();
		foreach (var entity in Entities) {
			if (entity is not DashEnemy entCD)
				continue;

			entCD.Reset();
		}

		Boss.Reset();
		ResetPathwaySpeeds();

		Combo = 0;
		Health = (float)Character.GetDefaultHP();
		InFever = false;
		WhenDidFeverStart = -1000000d;
		LastFeverIncreaseTime = -2000;
		lastNoteHit = false;
		Score = 0;
		Fever = 0;
		Sustains.Reset();
		AutoPlayer.Reset();
		__whenjump = -2000000000000d;
		__whenHjump = -2000000000000d;
		ActiveEvents.Clear();
		HandledEvents.Clear();
		lastIFrameGivenTime = -10000d;

		if (time > 0) {
			foreach (var ev in Events) {
				switch (ev.TriggerType) {
					case EventTriggerType.AtTime:
						Conductor.ForceTimeTo(ev.Time);
						if (ev.Time < time)
							ev.Activate();

						Conductor.ForceTimeTo(ev.Time + ev.Length);
						if (ev.Time + ev.Length < time)
							ev.Deactivate();

						break;
					case EventTriggerType.AtTimeMinusLength:
						Conductor.ForceTimeTo(ev.Time - ev.Length);
						if (ev.Time - ev.Length < time)
							ev.Activate();

						Conductor.ForceTimeTo(ev.Time);
						if (ev.Time < time)
							ev.Deactivate();
						break;
				}
			}

			foreach (var entity in Entities) {
				if (entity is DashEnemy mEnt && mEnt.GetJudgementHitTime() < time) {
					Conductor.ForceTimeTo(mEnt.GetJudgementHitTime()); // Hack...

					// Evaluate if fever must end
					if (ShouldExitFever && InFever)
						ExitFever();

					switch (mEnt) {
						case Masher masher: // Mashers need a bit more trickery
							EnterMashState(masher);
							for (int i = 0; i < masher.MaxHits; i++)
								masher.Hit(mEnt.Pathway, 0);
							ExitMashState();
							masher.RewardPlayer();
							AutoPlayer.MarkEntityAsPassed(mEnt);
							break;
						case SustainBeam sustain:
							sustain.Hit(sustain.Pathway, 0);
							sustain.RewardPlayer();

							AutoPlayer.MarkSustainAsActive(sustain);
							// Force time to the smaller value: either the end of the sustain, or the seeking time.
							// This fixes issues when placing an autoplayer in the middle of a sustain.
							var endOfSustainIsh = sustain.GetJudgementHitTime() + sustain.Length + 0.01;
							Conductor.ForceTimeTo(Math.Min(endOfSustainIsh, time));

							// hack, but will force sustain to cancel if its ready
							InputState s = default;
							AutoPlayer.SustainHoldThink(ref s);

							AutoPlayer.MarkEntityAsPassed(mEnt);
							if (time > endOfSustainIsh)
								AutoPlayer.MarkSustainAsInactive(sustain);
							break;
						default:
							switch (mEnt.Interactivity) {
								case EntityInteractivity.Hit:
									mEnt.Hit(mEnt.Pathway, 0);
									mEnt.RewardPlayer();
									break;
								case EntityInteractivity.SamePath:
									mEnt.Hit(mEnt.Pathway, 0);
									mEnt.RewardPlayer();
									break;
								case EntityInteractivity.Avoid:
									mEnt.Pass();
									mEnt.RewardPlayer();
									break;
							}
							AutoPlayer.MarkEntityAsPassed(mEnt);
							break;
					}
					Conductor.RemoveForcedTime();
				}
			}

			// HACK - but it solves fever FX not playing
			// if (InFever) FeverFX?.Start(this);

			// ALSO A HACK - but it solves some animation issues when mid-sustain.
			if (Sustains.IsSustaining())
				PlayCharacterAnimation(CharacterAnimationType.Press);
		}

		IsSeeking = false;
		Conductor.InvalidateTime();
	}

	public const string STRING_HP = "HP: {0}";
	public const string STRING_FEVERY = "FEVER! {0}s";
	public const string STRING_FEVERN = "FEVER: {0}/{1}";
	public const string STRING_COMBO = "COMBO";
	public const string STRING_SCORE = "SCORE";

	public const float PLAYER_OFFSET_X = 0.25f;
	public const float PLAYER_OFFSET_Y = 0.775f;
	public const float PLAYER_OFFSET_HIT_Y = -0.267f;

	public bool InHit { get; private set; } = false;

	public bool SuppressHitMessages { get; set; }
	public void EnterHitState() {
		InHit = true;
		SuppressHitMessages = false;
	}
	public void ExitHitState() {
		InHit = false;
	}

	[MemberNotNullWhen(true, nameof(MashingEntity))] public bool InMashState { get; private set; }
	public DashEnemy? MashingEntity;
	private SecondOrderSystem MashZoomSOS = new(1.1f, 0.9f, 2f, 0);
	private TextEffect? mashTextEffect;
	private const double TIME_BETWEEN_MASH_HITS = (1d / Masher.MASHER_PLAYER_MAX_HITS_PER_SECOND);
	private double LastMasherAttemptedHit;
	private double LastMasherRealHit;


	private void SubmitMashHit() {
		if (!InMashState)
			return;
		LastMasherAttemptedHit = Conductor.Time;
	}

	private bool CheckMashHit() {
		if (!InMashState) return false;
		if (double.IsNaN(LastMasherRealHit)) return false;

		if ((Conductor.Time - LastMasherRealHit) < TIME_BETWEEN_MASH_HITS)
			return false;

		if (!double.IsNaN(LastMasherAttemptedHit) && (Conductor.Time - LastMasherAttemptedHit) < TIME_BETWEEN_MASH_HITS) {
			LastMasherRealHit = LastMasherAttemptedHit;
			LastMasherAttemptedHit = double.NaN;
			return true;
		}

		return false;
	}



	/// <summary>
	/// Enters the mash state, which causes all attacks to be redirected into this entity.
	/// </summary>
	/// <param name="ent"></param>
	public void EnterMashState(DashEnemy ent) {
		if (IValidatable.IsValid(mashTextEffect))
			mashTextEffect.Remove();

		if (!IsSeeking) {
			mashTextEffect = SpawnTextEffect("HITS: 1", new(0), TextEffectTransitionOut.SlideUp, PathwayExts.PATHWAY_DUAL_COLOR);
			if (IValidatable.IsValid(mashTextEffect))
				mashTextEffect.SuppressAutoDeath = true;
			UpdateMashTextEffect();
		}
		InMashState = true;
		MashingEntity = ent;
		LastMasherRealHit = Conductor.Time;
		LastMasherAttemptedHit = Conductor.Time;
	}
	public void UpdateMashTextEffect() {
		if (!IValidatable.IsValid(mashTextEffect)) return;
		if (!IValidatable.IsValid(MashingEntity)) return;

		mashTextEffect.Position = GetPathway(PathwaySide.Top).Position;
		mashTextEffect.Text = $"HITS: {MashingEntity.Hits}";
	}
	/// <summary>
	/// Exits the mash state.
	/// </summary>
	public void ExitMashState() {
		if (IValidatable.IsValid(mashTextEffect))
			mashTextEffect.Remove();

		InMashState = false;
		MashingEntity = null;
		LastMasherRealHit = double.NaN;
		LastMasherAttemptedHit = double.NaN;
	}

	/// <summary>
	/// Is an entity on-screen and/or event currently warning the player? Used to draw the "!" warning on the side (and, if the entity wants to, on the entity itself)
	/// </summary>
	public bool IsWarning { get; set; } = false;


	// Player input system
	public InputState InputState { get; private set; }
	public List<ICloneDashInputSystem> InputReceivers { get; } = [];

	public AutoPlayer AutoPlayer { get; private set; }
	/// <summary>
	/// Timing system.
	/// </summary>
	public Conductor Conductor { get; private set; }
	public AudioPlaybackHandle Music;
	public IMuseDash1CharacterInstance Character { get; set; }
	// public ModelEntity Player { get; set; }
	// public ModelEntity HologramPlayer { get; set; }
	// public MD1_SpineActionController PlayerController { get; set; }
	// public MD1_SpineActionController HologramPlayerController { get; set; }
	public Boss Boss { get; set; }
	public Pathway TopPathway { get; set; }
	public Pathway BottomPathway { get; set; }

	/// <summary>
	/// Is the game currently paused
	/// </summary>
	public double UnpauseTime { get; private set; } = 0;
	public double DeltaUnpauseTime => Realtime - UnpauseTime;

	/// <summary>
	/// How many ticks have passed, meant for debugging
	/// </summary>
	public int Ticks { get; private set; } = 0;

	// WIP pausing
	// return false to not spawn the pause menu
	private bool startPause() {
		if (lastNoteHit)
			return false;
		if (Conductor.Time < 0)
			return false;

		audiosystem.PauseSound(in Music);
		Paused = true;
		UnpauseTime = 0;

		return true;
	}
	private void startUnpause() {
		activeScene.PlaySound(SceneSound.Unpause, 0);
		UnpauseTime = Realtime;
		Timers.Simple(3, () => {
			fullUnpause();
		});
	}
	private void fullUnpause() {
		audiosystem.ResumeSound(in Music);
		Paused = false;
		UnpauseTime = 0;
	}

	public void ForcePause() {
		audiosystem.PauseSound(in Music);
		Paused = true;
	}
	public void ForceUnpause() {
		audiosystem.ResumeSound(in Music);
		Paused = false;
	}

	int attackP = 0;
	int failP = 0;

	private bool __deferringAsync = false;

	public StatisticsData Stats;

	public void PlayCharacterAnimation(CharacterAnimationType type) {
		switch (type) {
			case CharacterAnimationType.Press:
			case CharacterAnimationType.DownPress:
			case CharacterAnimationType.UpPressStart:
			case CharacterAnimationType.UpPress:
			case CharacterAnimationType.UpPressEnd:
			case CharacterAnimationType.BigPress:
			case CharacterAnimationType.PressGroundToBig:
			case CharacterAnimationType.PressAirToBig:
			case CharacterAnimationType.PressBigToGround:
			case CharacterAnimationType.PressBigToAir:
			case CharacterAnimationType.PressHitToGround:
			case CharacterAnimationType.PressHitToAir:
			case CharacterAnimationType.UpPressHurt:
			case CharacterAnimationType.Run:
			case CharacterAnimationType.Jump:
				Character.GetPrimary().PlayAnimation(type);
				break;
			default:
				if (Sustains.IsSustaining()) {
					Character.GetSecondary().PlayAnimation(type);
				}
				else {
					Character.GetPrimary().PlayAnimation(type);
				}
				break;
		}
	}
	readonly List<SceneChange> sceneChanges = [];
	readonly List<IMuseDash1SceneInstance> scenes = [];
	IMuseDash1SceneInstance? activeScene;
	readonly Dictionary<UtlSymId_t, IMuseDash1SceneInstance> sceneLUT = [];
	bool canSceneChange = false;
	public IMuseDash1SceneInstance? GetActiveScene() => activeScene;
	public bool HasActiveScene([NotNullWhen(true)] out IMuseDash1SceneInstance? scene) => (scene = activeScene) != null;

	public IReadOnlyList<IMuseDash1SceneInstance> GetAllScenes() => scenes;
	public void SetScene(IMuseDash1SceneInstance? scene) {
		activeScene?.Deactivate(scene);
		var oldScene = activeScene;
		activeScene = scene;
		scene?.Activate(oldScene);
		BroadcastEntitySignal(null, EntitySignalType.SceneChange, (oldScene, scene));
	}

	public void SetScene(int sceneIdx) {
		SetScene(scenes[sceneIdx]);
	}

	public void SetScene(ReadOnlySpan<char> scene) {
		if (sceneLUT.TryGetValue(scene.SliceNullTerminatedString().Hash(), out var instance))
			SetScene(instance);
	}

	public void AddScene(IMuseDash1SceneInstance instance) {
		scenes.Add(instance);
		sceneLUT[instance.GetScene().GetUUID().Hash()] = instance;
		instance.SetSceneArrayIndex(scenes.Count - 1);
	}

	public int GetNumScenes() => scenes.Count;
	public int GetActiveSceneIdx() => activeScene?.GetSceneArrayIndex() ?? -1;
	public IMuseDash1SceneInstance? AddOrGetScene(ISceneDescriptor? descriptor) {
		if (descriptor == null) return null;
		if (sceneLUT.TryGetValue(descriptor.GetUUID().SliceNullTerminatedString().Hash(), out var instance))
			return instance;

		// Does the scene support the gamemode?
		if (!descriptor.SupportsGamemode(GamemodeMod.GetGamemode(MuseDash1Gamemode.UUID)!))
			return null;

		instance = (IMuseDash1SceneInstance)(object)descriptor.CreateInGame<IMuseDash1SceneInstance>(this)!;
		AddScene(instance);
		return instance;
	}
	IMuseDash1SceneInstance? FirstScene;
	public bool HasSceneInitialized(ISceneDescriptor descriptor) {
		return sceneLUT.ContainsKey(descriptor.GetUUID().Hash());
	}

	public override void Initialize(params object[] _) {
		ResetPathwaySpeeds();

		Stats = new(gameParameters.Chart);
		using (StaticSequentialProfiler.StartStackFrame("CD_GameLevel.RichPresenceUpdate")) {
			RichPresenceSystem.SetPresence(new() {
				Details = "In Game",
				State = $"Playing {gameParameters.Chart?.Song?.Name ?? "<null>"}"
			});
		}
		using (StaticSequentialProfiler.StartStackFrame("CD_GameLevel.Initialize")) {
			Interlude.Spin(submessage: "Retrieving descriptors...");

			var chart = gameParameters.Chart;
			var data = chart?.GetGamemodeData();

			if (data == null)
				throw new Exception("No gamemode data provided");
			if (data is not MD1_GamemodeData gamemodeData)
				throw new Exception("Gamemode data was not MD1");

			using (StaticSequentialProfiler.StartStackFrame("Get Descriptors")) {
				var charData = CharacterMod.GetCharacterData();
				if (charData == null) throw new ArgumentNullException(nameof(charData));
				Character = charData.CreateInGame<IMuseDash1CharacterInstance>(this);
				if (Character == null)
					throw new Exception("The character isn't supported for Muse Dash 1");

				var sceneData = SceneMod.GetSceneData();
				IMuseDash1SceneInstance? sceneToActivate = null;
				if (sceneData != null) {
					// Ignore scene changes. Only use this scene.
					var scene = AddOrGetScene(sceneData);
					if (scene != null)
						sceneToActivate = scene;
					canSceneChange = false;
				}
				else {
					sceneData = SceneMod.GetSceneData(gamemodeData.InitialScene);
					if (sceneData == null)
						throw new ArgumentNullException(nameof(sceneData));

					var scene = AddOrGetScene(sceneData);
					if (scene != null)
						sceneToActivate = scene;

					// Process scene changes
					canSceneChange = true;
					foreach (var sceneChange in gamemodeData.SceneChanges) {
						ISceneDescriptor? sceneDescToChangeTo = SceneMod.GetSceneData(sceneChange.SceneUID);

						var sceneChangeInstance = AddOrGetScene(sceneDescToChangeTo);
						if(sceneChangeInstance != null){
							var ev = new SceneChange(this, sceneChangeInstance.GetSceneArrayIndex());
							Events.Add(ev);
							readyToBuildEvents.Add(ev);
							ev.Time = sceneChange.Time;
							sceneChanges.Add(ev);
						}
					}
				}

				sceneChanges.Add(new(this, 0));
				SetScene(sceneToActivate);
				FirstScene = sceneToActivate;

				// var feverFX = FeverMod.GetFeverData();
				// FeverFX = feverFX;
			}

			Interlude.Spin(submessage: "Initializing the scene...");
			using (StaticSequentialProfiler.StartStackFrame("Initialize Scene/Fever")) {
				foreach (var scene in GetAllScenes())
					scene.Initialize();
				// FeverFX?.Initialize(this);
			}

			Interlude.Spin();

			MaxHealth = (float)Character.GetDefaultHP();
			Render3D = false;
			Health = MaxHealth;

			Interlude.Spin(submessage: "Initializing input...");
			using (StaticSequentialProfiler.StartStackFrame("Build Inputs")) {
				// build the input system
				var inputInterface = typeof(ICloneDashInputSystem);
				var inputs = AppDomain.CurrentDomain.GetAssemblies()
					.SelectMany(x => x.GetTypes())
					.Where(x => inputInterface.IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract)
					.Select(x => Activator.CreateInstance(x)).ToList();

				foreach (object input in inputs)
					InputReceivers.Add((ICloneDashInputSystem)input);
			}

			Interlude.Spin(submessage: "Initializing your character...");
			using (StaticSequentialProfiler.StartStackFrame("Initialize Character")) {
				Interlude.Spin();
				Character.Initialize();
				Interlude.Spin();

				PlayCharacterAnimation(CharacterAnimationType.In);
			}


			Interlude.Spin(submessage: "Loading boss...");
			using (StaticSequentialProfiler.StartStackFrame("Initialize Boss")) {
				Boss = Add(new Boss());
				Boss.RendersItself = false;
			}

			Interlude.Spin(submessage: "Loading internal entities...");

			using (StaticSequentialProfiler.StartStackFrame("Setup Internal Ents")) {
				AutoPlayer = Add<AutoPlayer>();
				AutoPlayer.Enabled = gameParameters.Autoplay;
				TopPathway = Add<Pathway>(PathwaySide.Top);
				BottomPathway = Add<Pathway>(PathwaySide.Bottom);
				Interlude.Spin();

				Conductor = Add<Conductor>();
				Interlude.Spin();
			}

			using (StaticSequentialProfiler.StartStackFrame("Load Enemies")) {
				Boss.PreBuildVisuals(this);
				foreach (var scene in GetAllScenes())
					Boss.BuildForScene(scene);

				if (chart != null) {
					if (!__deferringAsync) {
						foreach (var ent in gamemodeData.Entities)
							LoadEntity(ent);

						foreach (var ev in gamemodeData.Events)
							LoadEvent(ev);

						BuildQueues();
						Boss.PreBuildVisuals(this);
						foreach (var scene in GetAllScenes())
							Boss.BuildForScene(scene);
						BuildQueues();

						Entities.Sort((x, y) => (x is DashEnemy xE && y is DashEnemy yE) ? xE.GetJudgementHitTime().CompareTo(yE.GetJudgementHitTime()) : 0);
						Events.Sort((x, y) => (x is DashEvent xE && y is DashEvent yE) ? xE.Time.CompareTo(yE.Time) : 0);
					}
				}
			}
			Interlude.Spin(submessage: "Loading audio...");

			//foreach (var tempoChange in Sheet)
			if (gameParameters.Chart != null) {
				foreach (var bpmChange in gamemodeData.TempoChanges)
					Conductor.AddTempoChange(bpmChange.Time, bpmChange.Beat, bpmChange.BPM);

				foreach (var timeSigChange in gamemodeData.TimeSignatureChanges)
					Conductor.AddTimeSignatureChange(timeSigChange.Beat, timeSigChange.Percentage);
			}
			else
				Conductor.AddTempoChange(0, 0, 120);

			if (gameParameters.Chart != null && gamemodeData.TimeSignatureChanges.Count == 0)
				Conductor.AddTimeSignatureChange(0, 1);

			using (StaticSequentialProfiler.StartStackFrame("Sheet.Song.GetAudioTrack()")) {
				if (gameParameters.Chart != null) {
					Music = audiosystem.CreatePlayback(gameParameters.Chart.Song.GetAudioTrack(), AudioPlaybackSettings.Unaltered with {
						Looping = false,
						ManuallyUpdate = true,
						DoNotAutoDestroy = true,
						Stream = true
					});

					audiosystem.PlaySound(Music);
					InitSpeedFromCvar();
					if (gameParameters.Measure != 0)
						SeekTo(Conductor.MeasureToSeconds(gameParameters.Measure));
				}
				else
					Music = AudioPlaybackHandle.Null;
			}
			Interlude.Spin(submessage: "Ready!");

			UIBar = this.UI.Add<CD_Player_UIBar>();
			UIBar.Size = new(0, 64);

			Scorebar = this.UI.Add<CD_Player_Scorebar>();
			Scorebar.Size = new(0, 128);

			if (!CommandLine().CheckParm("-mdbmsc", out var p) && HasActiveScene(out var sceneInstance))
				sceneInstance.PlaySound(SceneSound.Begin, 0);
		}

		if (StaticSequentialProfiler.Profiling) {
			StaticSequentialProfiler.End(out var stack, out var accumulators);
			EngineCore.Interrupt(() => {
				Graphics2D.SetDrawColor(255, 255, 255);
				var lines = stack.ToStringArray();
				int y = 0;

				Graphics2D.DrawText(8, 8 + (y++ * 16), "Accumulators:", "Consolas", 15);
				for (int i = 0; i < accumulators.Count; i++, y++)
					Graphics2D.DrawText(8, 8 + (y * 16), $"  {accumulators[i].Key}: {accumulators[i].Value.Timer.Elapsed.TotalMilliseconds:F4} ms", "Consolas", 15);
				y++;
				Graphics2D.DrawText(8, 8 + (y++ * 16), "Stack:", "Consolas", 15);
				for (int i = 0; i < lines.Length; i++, y++)
					Graphics2D.DrawText(8, 8 + (y * 16), $"  {lines[i]}", "Consolas", 15);

			}, false);
		}

		MainThread.RunASAP(Interlude.End, ThreadExecutionTime.AfterFrame);
	}
	public bool Debug { get; set; } = true;
	public Panel PauseWindow { get; private set; }
	private bool lastNoteHit = false;


	static float TEMP_PLAYER_OFFSET => 0;
	// Its own function in case we have player-specific overrides (not sure if this exists yet)
	public Vector2F GetPathwayPosition(PathwaySide side) => HasActiveScene(out var scene) ? scene.GetPathwayPosition(side) : default;

	public float GetPlayerY(double jumpRatio) {
		if (Character.IsInAir())
			jumpRatio = 0;

		var height = EngineCore.GetWindowHeight();

		var top = GetPathwayPosition(PathwaySide.Top);
		var bot = GetPathwayPosition(PathwaySide.Bottom);

		return (float)(NMath.Remap(jumpRatio, 0, 1, bot.Y, top.Y)) + -1f; // TODO: re-evaluate
	}


	// This helps with doubles/sustains played in the same frame...
	// probably a better way to handle it, but this works
	readonly bool[] PlayedSceneSoundThisFrame = new bool[(int)SceneSound.Count];
	private void ResetSceneSoundsPlayedThisFrame() {
		for (int i = 0; i < PlayedSceneSoundThisFrame.Length; i++)
			PlayedSceneSoundThisFrame[i] = false;
	}
	public void PlaySceneSound(SceneSound sound, int hits = 0) {
		if (IsSeeking)
			return;
		if (PlayedSceneSoundThisFrame[(int)sound])
			return;
		PlayedSceneSoundThisFrame[(int)sound] = true;
		if (HasActiveScene(out var scene))
			scene.PlaySound(sound, hits);
	}


	private SecondOrderSystem? sos_yoff;

	InputState inputState = new();
	public override void PreThink(ref FrameState frameState) {
		Ticks++;
		ResetSceneSoundsPlayedThisFrame();

		if (Music.IsValid() && lastNoteHit && audiosystem.IsPlaybackComplete(Music) && gameParameters.Chart != null && !IValidatable.IsValid(CurrentStatisticsPanel)) {
			Stats.UploadScore(Score);
			OpenStatistics();
			audiosystem.PauseSound(Music);
			return;
		}

		if (ShouldExitFever && InFever)
			ExitFever();

		inputState.Reset();
		if (AutoPlayer.Enabled) {
			AutoPlayer.Play(ref inputState);
			foreach (ICloneDashInputSystem playerInput in InputReceivers)
				playerInput.Poll(ref frameState, ref inputState, InputAction.PauseGame);
		}

		else if (!IValidatable.IsValid(UI.KeyboardFocusedElement)) {
			foreach (ICloneDashInputSystem playerInput in InputReceivers)
				playerInput.Poll(ref frameState, ref inputState);
		}

		InputState = inputState;

		if (InMashState) {
			UpdateMashTextEffect();
			if (CheckMashHit())
				MashingEntity.Hit(PathwaySide.Bottom, 0);
		}

		if (inputState.PauseButton) {
			if (Music.IsValid() && audiosystem.IsPlaybackPaused(Music)) {
				startUnpause();
				if (IValidatable.IsValid(PauseWindow))
					PauseWindow.Remove();
			}
			else {
				if (startPause()) {
					PauseWindow = this.UI.Add<Panel>();
					PauseWindow.Size = new(300, 400);
					PauseWindow.Center();

					var flex = PauseWindow.Add<FlexPanel>();
					flex.Dock = Dock.Fill;
					flex.Direction = Directional180.Vertical;
					flex.ChildrenResizingMode = FlexChildrenResizingMode.StretchToFit;
					flex.DockPadding = RectangleF.TLRB(4);

					var play = flex.Add<Button>();
					play.BorderSize = 0;
					play.Text = "Return to Game";
					play.TextSize = 24;
					play.Image = Textures.LoadTextureFromFile("ui/pause_play.png");
					play.ImageOrientation = ImageOrientation.Fit;
					play.MouseReleaseEvent += delegate (Element self, FrameState state, ButtonCode clickedButton) {
						PauseWindow.Remove();
						startUnpause();
					};
					play.PaintOverride += Button_PaintOverride;

					var restart = flex.Add<Button>();
					restart.BorderSize = 0;
					restart.Text = "Restart Level";
					restart.TextSize = 24;
					restart.Image = Textures.LoadTextureFromFile("ui/pause_restart.png");
					restart.ImageOrientation = ImageOrientation.Fit;
					restart.MouseReleaseEvent += delegate (Element self, FrameState state, ButtonCode clickedButton) {
						// Interlude.Begin($"Reloading '{gameParameters.Chart?.Song?.Name ?? "<NULL>"}'...");
						// 
						// if (profilegameload.GetBool())
						// 	StaticSequentialProfiler.Start();
						// 
						// EngineCore.LoadLevel(new MuseDash1Game(gameParameters));

						// Maybe seeking is stable enough now to justify this though?
						SeekTo(0);
						PauseWindow.Remove();
						fullUnpause();
					};
					restart.PaintOverride += Button_PaintOverride;

					var settings = flex.Add<Button>();
					settings.BorderSize = 0;
					settings.Text = "Open Preferences...";
					settings.TextSize = 24;
					settings.Image = Textures.LoadTextureFromFile("ui/pause_settings.png");
					settings.ImageOrientation = ImageOrientation.Fit;
					settings.MouseReleaseEvent += delegate (Element self, FrameState state, ButtonCode clickedButton) {
						var panel = UI.Add<Panel>();
						panel.DrawPanelBackground = false;
						panel.Anchor = Anchor.Center;
						panel.Origin = Anchor.Center;
						panel.DynamicallySized = true;
						panel.Size = new(0.9f);

						var titlebar = panel.Add<Titlebar>();
						titlebar.Dock = Dock.Top;
						titlebar.MinimizeButton.Visible = false;
						titlebar.MaximizeButton.Visible = false;
						titlebar.CloseButton.MouseReleaseEvent += (_, _, _) => {
							panel.Remove();
						};
						titlebar.Title = "Settings";

						var settings = panel.Add<SettingsEditor>();
						settings.Dock = Dock.Fill;
						settings.DockMargin = RectangleF.TLRB(0, 8, 8, 0);

						panel.MakePopup();
					};
					settings.PaintOverride += Button_PaintOverride;

					var back2menu = flex.Add<Button>();
					back2menu.BorderSize = 0;
					back2menu.Text = "Exit to Menu";
					back2menu.TextSize = 24;
					back2menu.Image = Textures.LoadTextureFromFile("ui/pause_exit.png");
					back2menu.ImageOrientation = ImageOrientation.Fit;
					back2menu.MouseReleaseEvent += delegate (Element self, FrameState state, ButtonCode clickedButton) {
						LevelTransitions.LoadMainMenu();
					};
					back2menu.PaintOverride += Button_PaintOverride;
				}
			}
			return;
		}

		float? yoff = null;

		bool holdingTop = Sustains.IsSustaining(PathwaySide.Top), holdingBottom = Sustains.IsSustaining(PathwaySide.Bottom);
		bool holding = holdingTop || holdingBottom;
		if ((holdingTop && holdingBottom) || InMashState)
			yoff = GetPathwayPosition(PathwaySide.Both).Y;
		else if (holdingTop)
			yoff = GetPathwayPosition(PathwaySide.Top).Y;
		else if (holdingBottom)
			yoff = GetPathwayPosition(PathwaySide.Bottom).Y;

		if (yoff.HasValue) {
			if (sos_yoff == null)
				sos_yoff = new(15, 1, 1, yoff.Value);

			yoff = yoff.Value + -1f;
		}
		else
			sos_yoff = null;

		var playerY = yoff ?? GetPlayerY(CharacterYRatio);

		float conductorInTime = Conductor.PreStartTime <= 0 ? 1 : (float)NMath.Remap(Conductor.Time, -Conductor.PreStartTime, -Conductor.PreStartTime / 1.5f, 0, 1, clampInput: true);
		conductorInTime = 1 - NMath.Ease.OutQuad(conductorInTime);

		// This sucks... todo, figure out how to get this proper
		var leftPlayer = GetPathwayPosition(PathwaySide.Both).X;

		Character.GetPrimary().SetPos(new Vector2F(
			(leftPlayer - 1) - (conductorInTime * 1),
			-(sos_yoff?.Update(playerY) ?? playerY)
		));
		Character.GetPrimary().SetScale(new(PlayerScale));

		Character.GetSecondary().SetPos(new Vector2F(
			(leftPlayer - 1),
			-GetPlayerY(HologramCharacterYRatio)
		));
		Character.GetSecondary().SetScale(new(PlayerScale));

		Character.Think();

		VisibleEntities.Clear();

		foreach (var entity in Entities) {
			if (entity is Boss) continue;
			if (entity is not DashEnemy)
				continue;

			var entCD = entity as DashEnemy;
			// Visibility testing
			// ShouldDraw overrides ForceDraw here, which is intentional, although the naming convention is confusing and should be adjusted (maybe the names swapped?)
			if ((entCD.CheckVisTest(frameState) || entCD.ForceDraw) && entCD.ShouldDraw) {
				VisibleEntities.Add(entCD);

				if (entCD.Warns && !entCD.Dead && !InMashState)
					IsWarning = true;
			}
		}

		var lastEntity = (DashEnemy)Entities.Last(x => x is DashEnemy);

		if (lastEntity.GetJudgementHitTime() + lastEntity.Length < Conductor.Time && !lastNoteHit) {
			lastNoteHit = true;
			if (Stats.CalculateFullCombo()) {
				Logs.Info("Full combo achieved.");
				PlaySceneSound(SceneSound.FullCombo, 0);
			}
		}

		// Sort the visible entities by their hit time
		VisibleEntities.Sort(VisibleEntitySorter);

		IterateEvents();

		//LockEntityBuffer();

		// Removes entities marked for removal safely
		foreach (var entity in Entities)
			if (entity is DashEnemy && ((DashEnemy)entity).MarkedForRemoval)
				Remove(entity);

		//UnlockEntityBuffer(); LockEntityBuffer();

		//foreach (var e in Events)
		//e.TryCall();

		// Resets the player animation state controller.
		// Does not reset any actively playing animations, just the internal state machines
		// used to determine when animations are triggered and on what.

		// Start input processing.
		// Bottom is executed first, so if two pathway attacks happen on the same frame, it can exit the jump state
		// before jumping again, allowing the attack to work as expected
		HitLogic(PathwaySide.Bottom);
		HitLogic(PathwaySide.Top);

		// This loop is mostly for per-tick polls that need to occur, ie. when entities have been fully missed.
		// It is ran after input processing.
		foreach (var entity in VisibleEntities) {
			var timeToHit = entity.GetJudgementTimeUntilHit();
			switch (entity.Interactivity) {
				case EntityInteractivity.Hit:
				case EntityInteractivity.Sustain:
					if (!entity.Dead) {
						PathwaySide currentPathway = Pathway;

						// Is it too late for the player to hit this entity anyway?
						if (timeToHit < -entity.PreGreatRange
							&& !(entity is SustainBeam se && se.HeldState == true)
							&& !(entity is Masher me && me.Hits > 0)
						) {
							entity.Miss();
						}
					}
					break;
				case EntityInteractivity.SamePath:
					if (NMath.InRange(timeToHit, -entity.PreGreatRange, 0)) {
						PathwaySide pathCurrentCharacter = Pathway;
						if ((pathCurrentCharacter == PathwaySide.Both || pathCurrentCharacter == entity.Pathway) && entity.Hits == 0) {
							entity.Hit(pathCurrentCharacter, 0);
							PlaySceneSound(entity.Type switch {
								EntityType.Heart => SceneSound.GotHeart,
								EntityType.Score => SceneSound.GotScore,
								_ => SceneSound.GotScore
							}, 1);
						}
					}
					break;
				case EntityInteractivity.Avoid:
					// If the player is sustaining on the pathway this entity is on, then ignore
					if (Sustains.IsSustaining(entity.Pathway))
						break;

					// Checks if the player has completely failed to avoid the entity, and if so, damages the player.

					if (Pathway == entity.Pathway && timeToHit < -entity.PrePerfectRange && !entity.DidRewardPlayer) {
						//entity.Hit(Game.PlayerController.Pathway);
						entity.DamagePlayer();
					}

					// If the player is now avoiding the entity, then reward the player for missing it, and make it so they cant be damaged by it)
					if (Pathway != entity.Pathway && timeToHit < 0 && !entity.DidDamagePlayer) {
						entity.Pass();
					}

					break;
			}
			//entity.WhenVisible();
		}

		AddDebugString("HoldingTopPathwaySustain", Sustains.GetSustainsActiveCount(PathwaySide.Top));
		AddDebugString("HoldingBottomPathwaySustain", Sustains.GetSustainsActiveCount(PathwaySide.Top));

		if (HasActiveScene(out var scene)) {
			scene.Think();
		}
		// if (InFever)
		// FeverFX?.Think(this);
	}

	class StatisticsPanel(MuseDash1Game game) : Panel() {
		ICharacterVictoryInstance victory = null!;
		ISongChart? chart;
		StatisticsData stats = null!;
		double start = 0;
		double Time() => game.Curtime - start;

		protected override void Initialize() {
			chart = game.GetChart();
			if (chart == null) return;
			start = game.Curtime;
			stats = game.Stats;

			ICharacterDescriptor? character = game.Character.GetCharacter();
			if (character == null) return;

			victory = character.CreateVictory();
			victory.Initialize(game);
			victory.PlayAudio();
			stats.Compute();

			var bottom = Add<Panel>();
			bottom.DrawPanelBackground = false;

			bottom.DynamicallySized = true;
			bottom.Size = new(0.07f);
			bottom.Dock = Dock.Bottom;

			var restart = bottom.Add<Button>();
			restart.DynamicallySized = true;
			restart.Size = new(.2f);
			restart.Text = "Restart";
			restart.Dock = Dock.Left;
			restart.MouseReleaseEvent += (_, _, _) => {
				// TODO: Probably should just hard restart it...
				// Maybe seeking is stable enough now to justify this though?
				game.SeekTo(0);
				this.Remove();
			};

			var back = bottom.Add<Button>();
			back.DynamicallySized = true;
			back.Size = new(.2f);
			back.Text = "Main Menu";
			back.Dock = Dock.Right;
			back.MouseReleaseEvent += (_, _, _) => LevelTransitions.LoadMainMenu();

			BorderSize = 0;
		}
		void RenderOneLine(ReadOnlySpan<char> line, int fs, ref int y){
			Graphics2D.DrawText(16, 16 + y, line, Graphics2D.UI_FONT_NAME, fs);
			y += fs + 4;
		}
		public override void Paint(float width, float height) {
			BackgroundColor = new(0, 0, 0, (int)(220 * (float)NMath.Ease.OutQuad(NMath.Remap(Time(), 0, 0.5, 0, 1, true))));
			base.Paint(width, height);

			Vector2F position = new(width / 2, (1 - (float)NMath.Ease.OutElastic(Math.Clamp(Time() * 0.2, 0, 1))) * (height));
			EngineCore.Window.BeginMode2D(new() {
				Zoom = height / 900 / 2.4f,
				Offset = (new Vector2F(0, height / 1) + position).ToNumerics()
			});
			victory.Render();
			EngineCore.Window.EndMode2D();

			var chart = (MD1_SongChart?)this.chart;
			if (chart == null) return;
			if (stats == null) return;

			Graphics2D.SetDrawColor(255, 255, 255);
			stats.Compute();
			var fs = 24;
			var y = 0;

			Match boldRegexMatch = Util.BoldRegex.Match(chart.Song.Name);
			Graphics2D.DrawText(16, 16 + y,
								boldRegexMatch.Success ? boldRegexMatch.Groups[1].Value : chart.Song.Name,
								boldRegexMatch.Success ? Graphics2D.UI_MONO_BOLD_FONT_NAME : Graphics2D.UI_CN_JP_FONT_NAME,
								fs);
			y += fs + 4;

			RenderOneLine($"      Rating: {chart.Rating}", fs, ref y);
			RenderOneLine($"      Grade: {stats.Grade}", fs, ref y);
			RenderOneLine($"      Accuracy: {stats.Accuracy}", fs, ref y);
			RenderOneLine($"      Score: {stats.Score}", fs, ref y);
			RenderOneLine($"      Max Combo: {stats.MaxCombo}", fs, ref y);
			RenderOneLine("", fs, ref y);
			RenderOneLine($"      Perfects: {stats.Perfects}", fs, ref y);
			RenderOneLine($"      Greats: {stats.Greats}", fs, ref y);
			RenderOneLine($"      Passes: {stats.Passes}", fs, ref y);
			RenderOneLine($"      Misses: {stats.Misses}", fs, ref y);
			RenderOneLine("", fs, ref y);
			RenderOneLine($"      Earlys: {stats.Earlys}", fs, ref y);
			RenderOneLine($"      Exacts: {stats.Exacts}", fs, ref y);
			RenderOneLine($"      Lates: {stats.Lates}", fs, ref y);
			RenderOneLine("", fs, ref y);
			RenderOneLine($"      Registered: {stats.OrderedEnemies.Count}", fs, ref y);
		}
		protected override void OnThink(FrameState frameState) {
			base.OnThink(frameState);
			if (victory != null) {
				victory.Think();
			}
		}
	}

	StatisticsPanel? CurrentStatisticsPanel;
	private void OpenStatistics() {
		if (IValidatable.IsValid(CurrentStatisticsPanel)) return;

		CurrentStatisticsPanel = UI.Add(new StatisticsPanel(this));
		CurrentStatisticsPanel.Size = new(1, 1);
		CurrentStatisticsPanel.DynamicallySized = true;

		FirstScene?.PlaySound(SceneSound.Victory, 0);
	}

	public int EnemySortIndexCounter;

	private static int VisibleEntitySorter(DashEnemy x, DashEnemy y) {
		return x.SortIndex.CompareTo(y.SortIndex);
	}

	private void Button_PaintOverride(Element self, float width, float height) {
		Button b = self as Button;
		var backpre = self.BackgroundColor;

		var back = Element.MixColorBasedOnMouseState(self, backpre, new(0, 0.8f, 2.4f, 1f), new(0, 1.2f, 0.6f, 1f));
		var fore = Element.MixColorBasedOnMouseState(self, self.ForegroundColor, new(0, 0.8f, 1.8f, 1f), new(0, 1.2f, 0.6f, 1f));

		Graphics2D.SetDrawColor(back);
		Graphics2D.DrawRectangle(0, 0, width, height);
		var text = b.Text;
		var tSize = Graphics2D.GetTextSize(text, b.Font, b.TextSize);
		b.ImageDrawing(new((height / -4) + (tSize.X / -2), 0), new(height, height));
		Graphics2D.SetDrawColor(255, 255, 255);
		Graphics2D.DrawText(new((width / 2) + (height / 4), height / 2), text, b.Font, b.TextSize, Anchor.Center);
	}

	/// <summary>
	/// Gets the games <see cref="Pathway"/> from a <see cref="PathwaySide"/><br></br>
	/// Note: If strict is off (default), it will return the bottom pathway if PathwaySide.Middle is passed, otherwise it will throw an exception.
	/// </summary>
	/// <param name="pathway"></param>
	/// <param name="strict"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentException"></exception>
	public Pathway GetPathway(PathwaySide pathway, bool strict = false) {
		switch (pathway) {
			case PathwaySide.Top:
				return TopPathway;
			case PathwaySide.Bottom:
				return BottomPathway;
			case PathwaySide.Both:
				if (strict)
					break;
				else
					return BottomPathway;
			case PathwaySide.None:
				break;
		}

		throw new ArgumentException("pathway");
	}

	public Pathway GetPathway(DashEnemy ent) => GetPathway(ent.Pathway);

	/// <summary>
	/// Creates an entity from a C# type and adds it to <see cref="GameplayManager.Entities"/>.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <returns></returns>
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8604 // Possible null reference argument.
	public T CreateEntity<T>() where T : DashEnemy => (T)Add((T)Activator.CreateInstance(typeof(T)));
#pragma warning restore CS8604 // Possible null reference argument.
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

	/// <summary>
	/// Creates an event from an EventType enumeration, and adds it to <see cref="GameplayManager.Events"/>.
	/// </summary>
	/// <param name="t"></param>
	/// <returns></returns>
	/*public CD_BaseEvent AddEvent(EventType t) {
            CD_BaseEvent e = CD_BaseEvent.CreateFromType(this.Game, t);
            return e;
        }*/

	public PollResult LastPollResult = PollResult.Empty;

	/// <summary>
	/// Polling function which figures out the closest, potentially-hit entity and returns the result.
	/// </summary>
	/// <param name="pathway"></param>
	/// <returns>A <see cref="PollResult"/>, if it hit something, Hit is true, and vice versa.</returns>
	public PollResult Poll(in PollParams parms) {
		foreach (DashEnemy entity in VisibleEntities) {
			// If the entity has no interactivity, ignore it in the poll
			if (!entity.Interactive)
				continue;

			// If the entity says its dead, ignore it
			if (entity.Dead)
				continue;

			switch (entity.Interactivity) {
				case EntityInteractivity.Hit:
				case EntityInteractivity.Sustain:
					bool isValidHit = entity.Interactivity != EntityInteractivity.Sustain ? !entity.Dead : !(entity as SustainBeam)!.HeldState;
					if (isValidHit && Game.Pathway.ComparePathwayType(entity.Pathway, parms.Pathway)) {
						double distance = entity.GetJudgementTimeUntilHit();
						double pregreat = -entity.PreGreatRange, postgreat = entity.PostGreatRange;
						double preperfect = -entity.PrePerfectRange, postperfect = entity.PostPerfectRange;
						if (NMath.InRange(distance, pregreat, postgreat)) { // hit occured
							var greatness = (NMath.InRange(distance, preperfect, postperfect) ? "PERFECT" : "GREAT") + " " + Math.Round(distance * 1000, 1) + "ms";
							LastPollResult = PollResult.Create(entity, distance, greatness);
							return LastPollResult;
						}
					}
					break;
			}
		}

		LastPollResult = PollResult.Empty;
		return LastPollResult;
	}

	/// <summary>
	/// Spawns a <see cref="TextEffect"/> into the game and adds it to the game.
	/// </summary>
	/// <param name="text">The text</param>
	/// <param name="position">Where it spawns (it will rise upwards after being spawned)</param>
	/// <param name="color">The color of the text</param>
	public TextEffect? SpawnTextEffect(string text, Vector2F position, TextEffectTransitionOut transitionOut = TextEffectTransitionOut.SlideUp, Color? color = null) {
		if (IsSeeking) return null;
		if (color == null)
			color = new Color(255, 255, 255, 255);

		return Add(new TextEffect(text, position, transitionOut, color.Value));
	}

	public List<DashEvent> Events = [];
	public HashSet<DashEvent> ActiveEvents = [];
	public HashSet<DashEvent> HandledEvents = [];

	private bool shouldActivateEvent(DashEvent ev) => ev.TriggerType switch {
		EventTriggerType.AtTimeMinusLength => Conductor.Time >= (ev.Time - ev.Length),
		EventTriggerType.AtTime => Conductor.Time >= ev.Time,
		_ => false
	};
	private bool shouldDeactivateEvent(DashEvent ev) => ev.TriggerType switch {
		EventTriggerType.AtTimeMinusLength => Conductor.Time >= ev.Time,
		EventTriggerType.AtTime => Conductor.Time >= (ev.Time + ev.Length),
		_ => false
	};

	public void IterateEvents() {
		foreach (var ev in Events) {
			if (ActiveEvents.Contains(ev)) {
				// Determine if the event needs to be deactivated
				if (shouldDeactivateEvent(ev)) {
					HandledEvents.Add(ev);
					ActiveEvents.Remove(ev);
					ev.Deactivate();
					Logs.Debug($"Deactivating {ev.GetType().Name}");
				}
			}
			else if (!HandledEvents.Contains(ev)) {
				// Determine if the event needs to be activated
				if (shouldActivateEvent(ev)) {
					ActiveEvents.Add(ev);
					ev.Activate();
					Logs.Debug($"Activating {ev.GetType().Name}");
				}
			}
			// The event has both been activated and deactivated, so its ignored
		}
	}

	/// <summary>
	/// Loads an event from a <see cref="ChartEvent"/> representation, builds a <see cref="MapEvent"/> out of it, and adds it to  <see cref="GameplayManager.Events"/>.
	/// </summary>
	/// <param name="ChartEvent"></param>
	public void LoadEvent(MD1_SongChartEvent ChartEvent) {
		Interlude.Spin(submessage: "Loading events...");
		var ev = DashEvent.CreateFromType(this, ChartEvent.Type);

		ev.Time = ChartEvent.Time;
		ev.Length = ChartEvent.Length;

		ev.Score = ChartEvent.Score;
		ev.Fever = ChartEvent.Fever;
		ev.Damage = ChartEvent.Damage;
		ev.BossAction = ChartEvent.BossAction;

		Events.Add(ev);
		readyToBuildEvents.Add(ev);
	}

	public int CurrentAirSpeed;
	public int CurrentGroundSpeed;

	public void ResetPathwaySpeeds() {
		CurrentAirSpeed = 1;
		CurrentGroundSpeed = 1;
	}

	public void SetPathwaySpeed(PathwaySide pathway, int speed) {
		System.Diagnostics.Debug.Assert(speed >= 1);
		System.Diagnostics.Debug.Assert(speed <= 3);

		if ((pathway & PathwaySide.Top) != 0)
			CurrentAirSpeed = speed;

		if ((pathway & PathwaySide.Bottom) != 0)
			CurrentGroundSpeed = speed;
	}

	/// <summary>
	/// Loads an entity from a <see cref="ChartEntity"/> representation, builds a <see cref="MapEntity"/> out of it, and adds it to <see cref="GameplayManager.Entities"/>.
	/// </summary>
	/// <param name="ChartEntity"></param>
	public void LoadEntity(MD1_SongChartEntity ChartEntity) {
		Interlude.Spin(submessage: "Loading entities...");

		if (!DashEnemy.TryCreateFromType(this, ChartEntity.Type, out DashEnemy? ent)) {
			Console.WriteLine("No load entity handler for type " + ChartEntity.Type);
			return;
		}

		ent.Pathway = ChartEntity.Pathway;
		ent.EnterDirection = ChartEntity.EnterDirection;
		ent.Variant = ChartEntity.Variant;

		ent.HitTime = ChartEntity.HitTime;
		ent.ShowTime = ChartEntity.ShowTime;
		ent.Length = ChartEntity.Length;
		ent.Speed = ChartEntity.Speed;
		ent.Flipped = ChartEntity.Flipped;
		ent.Blood = ChartEntity.Blood;

		ent.FeverGiven = ChartEntity.Fever;
		ent.DamageTaken = ChartEntity.Damage;
		ent.ScoreGiven = ChartEntity.Score;
		ent.HealthGiven = ChartEntity.Health;

		ent.RelatedToBoss = ChartEntity.RelatedToBoss;

		ent.RendersItself = false;
		ent.DebuggingInfo = ChartEntity.DebuggingInfo;

		Stats.RegisterEnemy(ent);
		readyToBuildEntities.Add(ent);
	}

	List<DashEnemy> readyToBuildEntities = [];
	List<DashEvent> readyToBuildEvents = [];

	void BuildQueues() {
		// This prepares the arrays...
		foreach (var ent in readyToBuildEntities) {
			if (ent is DashEnemy dashEnemy)
				dashEnemy.PreBuildVisuals(this);
		}

		// ...and then this builds the visuals in those arrays
		foreach (var scene in GetAllScenes()) {
			foreach (var ent in readyToBuildEntities) {
				if (ent is DashEnemy dashEnemy)
					dashEnemy.BuildForScene(scene);
			}
		}

		readyToBuildEntities.Clear();
		foreach (var ev in readyToBuildEvents)
			ev.Build();

		readyToBuildEvents.Clear();

		// If new entities exist, build those then (events might create entities)
		if (readyToBuildEntities.Count != 0)
			BuildQueues();
	}

	public float PlayerScale => 1 / 200f;
	public float PlayScale => 1 / 200f;
	public static float GlobalScale => 1 / 200f;

	public override void PreRenderBackground(FrameState frameState) {
		Boss.Position = new(0, 2.25f);
	}

	public override void PreRender(FrameState frameState) {
		base.PreRender(frameState);
		//Stopwatch test = Stopwatch.StartNew();
		if (HasActiveScene(out var scene))
			scene.RenderBackground();
		// if (InFever)
		// FeverFX?.Render(this);
		//Logs.Info(test.Elapsed.TotalMilliseconds);
	}

	public override void CalcView2D(FrameState frameState, ref Camera2D cam) {
		var zoomValue = MashZoomSOS.Update(InMashState ? 1 : 0) * .5f;
		cam.Zoom = ((frameState.WindowHeight / 900) * 120) + (zoomValue * 45);
		cam.Rotation = 0.0f;
		cam.Offset = new(frameState.WindowWidth / 2, frameState.WindowHeight / 2);
		cam.Target = new(zoomValue * -5, 0);
		cam.Offset += cam.Target;

		//cam.Offset = new(frameState.WindowWidth * Game.Pathway.PATHWAY_LEFT_PERCENTAGE * .5f, frameState.WindowHeight * 0.5f);
		//cam.Target = cam.Offset;
	}

	public void ConditionallyRenderVisibleEntities(FrameState frameState, Predicate<DashEnemy> enemyPredicate) {
		DeadEntityVisibility deadVis = GameSettings.DeadEntityVisibility;
		foreach (Entity ent in VisibleEntities) {
			if (ent is not DashEnemy entCD) continue;
			if (!enemyPredicate(entCD)) continue;

			if(entCD.Dead){
				switch(deadVis){
					case DeadEntityVisibility.UseGamemodeDefaults:
					case DeadEntityVisibility.FullyVisible:
						Model4System.PushRenderBlend(new(255, 255, 255));
						break;
					case DeadEntityVisibility.Dimmed:
						Model4System.PushRenderBlend(new(70, 70, 70));
						break;
					case DeadEntityVisibility.Invisible:
						continue;
				}
			}
			ent.Render(frameState);
			Rlgl.DrawRenderBatchActive();
			Model4System.PopRenderBlend();
		}
	}
	public override void Render(FrameState frameState) {
		Rlgl.DrawRenderBatchActive();

		Rlgl.DisableDepthTest();
		Rlgl.DisableDepthMask();
		Rlgl.DisableBackfaceCulling();

		//Raylib.DrawLineV(new(-100000, 0), new(100000, 0), Color.Red);
		//Raylib.DrawLineV(new(0, -100000), new(0, 100000), Color.Green);

		// Pathways
		TopPathway.Render();
		BottomPathway.Render();

		// Hold notes
		ConditionallyRenderVisibleEntities(frameState, x => x.Type == EntityType.SustainBeam);

		// Boss
		Boss.Render();

		// The other entities, that aren't sustain beams, in order of top -> bottom pathway
		ConditionallyRenderVisibleEntities(frameState, x => x.Type != EntityType.SustainBeam && x.Pathway == PathwaySide.Top);
		ConditionallyRenderVisibleEntities(frameState, x => x.Type != EntityType.SustainBeam && x.Pathway == PathwaySide.Bottom);

		AddDebugString("Visible Entities", VisibleEntities.Count);
		AddDebugString("Player Y", CharacterYRatio);
		AddDebugString("Hologram-Player Y", HologramCharacterYRatio);

		Rlgl.DrawRenderBatchActive();
	}

	public override void Render2D(FrameState frameState) {
		base.Render2D(frameState);

		foreach (Entity ent in VisibleEntities) {
			if (ent is not DashEnemy)
				continue;

			var entCD = (DashEnemy)ent;
			//Graphics2D.DrawText(ent.Position, entCD.DebuggingInfo, "Consolas", 20);
		}
	}

	/// <summary>
	/// Currently visible entities this tick
	/// </summary>
	public List<DashEnemy> VisibleEntities { get; private set; } = [];

	private double LastAttackTime;
	private PathwaySide LastAttackPathway;

	public void BroadcastEntitySignal(Entity? entityFrom, EntitySignalType signalType, object? data = null) {
		DashEnemy? mentFrom = null;
		if (entityFrom != null) {
			if (entityFrom is not DashEnemy mentFromC)
				return;
			mentFrom = mentFromC;
		}

		foreach (var entity in Entities) {
			if (entity is not DashEnemy ment) continue;
			ment.OnSignalReceived(mentFrom, signalType, data);
		}
	}
	public void SendEntitySignal(Entity? entityFrom, Entity entityTo, EntitySignalType signalType, object? data = null) {
		DashEnemy? mentFrom = null;
		if (entityFrom != null) {
			if (entityFrom is not DashEnemy mentFromC)
				return;
			mentFrom = mentFromC;
		}

		if (entityTo is not DashEnemy ment) return;
		ment.OnSignalReceived(mentFrom, signalType, data);
	}


	private void HitLogic(PathwaySide pathway) {
		int amountOfTimesHit = pathway == PathwaySide.Top ? InputState.TopClicked : InputState.BottomClicked;
		bool keyHitOnThisSide = amountOfTimesHit > 0;

		if (!keyHitOnThisSide)
			return;

		PollParams pollParams = new();
		pollParams.AmountOfTimesHit = amountOfTimesHit;
		pollParams.HitsRemaining = amountOfTimesHit;
		pollParams.Pathway = pathway;

		while (pollParams.HitsRemaining > 0) {
			EnterHitState();

			LastAttackTime = Conductor.Time;
			LastAttackPathway = pathway;

			// Hit testing
			PollResult pollResult = default;
			if (InMashState) {
				//if (Debug)
				//Console.WriteLine($"mashing entity = {MashingEntity}");
				SubmitMashHit();
			}
			else {
				pollResult = Poll(in pollParams);

				if (pollResult.Hit) {
					pollResult.HitEntity.WasHitPerfect = pollResult.IsPerfect;
					pollResult.HitEntity.Hit(pathway, pollResult.DistanceToHit);

					if (SuppressHitMessages == false && !IsSeeking) {
						Color c = pollResult.HitEntity.HitColor;
						SpawnTextEffect(pollResult.Greatness, GetPathway(pathway).Position, TextEffectTransitionOut.SlideUp, c);

						PlaySceneSound(pollResult.HitEntity.Type switch {
							EntityType.Single => pollResult.HitEntity.Variant switch {
								EntityVariant.Small => SceneSound.HitSmall,
								EntityVariant.Medium1 => SceneSound.HitMedium1,
								EntityVariant.Medium2 => SceneSound.HitMedium2,
								EntityVariant.Large1 => SceneSound.HitLarge1,
								EntityVariant.Large2 => SceneSound.HitLarge2,
								EntityVariant.Boss1 => SceneSound.HitBoss1,
								EntityVariant.Boss2 => SceneSound.HitBoss2,
								EntityVariant.Boss3 => SceneSound.HitBoss3,
								EntityVariant.BossHitFast => SceneSound.HitBossFast,
								EntityVariant.BossHitSlow => SceneSound.HitBossSlow,
								_ => SceneSound.HitMedium1
							},
							EntityType.Hammer => SceneSound.HitHammer,
							EntityType.Double => SceneSound.HitGemini,
							EntityType.SustainBeam => SceneSound.StartedHold,
							EntityType.Raider => SceneSound.HitRaider,
							EntityType.Ghost => SceneSound.HitGhost,
							// I think HP/score get handled on their own
							_ => SceneSound.HitMedium1
						}, 1);
					}
				}
			}

			// Trigger animation events on the player controller
			var hitSomething = pollResult.Hit;
			if (pathway == PathwaySide.Top)
				AttackAir(pollResult);
			else
				AttackGround(pollResult);

			ExitHitState();

			pollParams.HitsRemaining--;
		}
		//if (Debug)
		//Console.WriteLine($"poll.Hit = {hitSomething}, entity = {((pollResult.HasValue && pollResult.Value.Hit) ? pollResult.Value.HitEntity.ToString() : "NULL")}");
	}

	/// <summary>
	/// Current health of the player<br></br>
	/// Default: 250
	/// </summary>
	public float Health { get; private set; }

	/// <summary>
	/// Maximum health the player can have, the player will have this much health on spawn<br></br>
	/// Default: 250
	/// </summary>
	public float MaxHealth { get; set; } = 250;

	/// <summary>
	/// How much health does the player lose every second?<br></br>
	/// Default: 0
	/// </summary>
	public float HealthDrain { get; set; } = 0;

	/// <summary>
	/// How long an invincibility frame lasts in seconds.
	/// </summary>
	public double IFrameLength { get; set; } = 1.25;
	private double lastIFrameGivenTime = -200000;

	/// <summary>
	/// Time since the last invincibility frame was given
	/// </summary>
	public double TimeSinceLastIFrame => Conductor.Time - lastIFrameGivenTime;
	/// <summary>
	/// Currently in an invincibility frame?
	/// </summary>
	public bool InIFrame => TimeSinceLastIFrame < IFrameLength;
	/// <summary>
	/// Gives the player an invincibility frame. No checks are performed here
	/// </summary>
	internal void SetIFrameTime() {
		lastIFrameGivenTime = Conductor.Time;
	}

	/// <summary>
	/// Current fever bar.<br></br>
	/// Default: 0
	/// </summary>
	public float Fever { get; private set; } = 0;

	/// <summary>
	/// How much fever needs to be obtained until entering fever state<br></br>
	/// Default: 120
	/// </summary>
	public float MaxFever { get; set; } = 120;

	/// <summary>
	/// How much fever, in seconds, does a full fever bar provide?<br></br>
	/// Default: 6
	/// </summary>
	public float FeverTime { get; set; } = 6;
	/// <summary>
	/// Is the player currently in fever?
	/// </summary>
	public bool InFever { get; private set; } = false;
	/// <summary>
	/// When did the fever start?
	/// </summary>
	public double WhenDidFeverStart { get; private set; } = -1000000d;
	/// <summary>
	/// Should the player exit fever?
	/// </summary>
	private bool ShouldExitFever => (Conductor.Time - WhenDidFeverStart) >= FeverTime;
	/// <summary>
	/// How much fever time is left?
	/// </summary>
	public double FeverTimeLeft => FeverTime - (Conductor.Time - WhenDidFeverStart);
	/// <summary>
	/// Returns the fever time left as a value of 0-1, where 0 is the end and 1 is the start. Good for animation.
	/// </summary>
	private double FeverRatio => 1f - ((Conductor.Time - WhenDidFeverStart) / FeverTime);
	/// <summary>
	/// Current score of the player.
	/// </summary>
	public int Score { get; private set; } = 0;
	/// <summary>
	/// Which entity is being held on the top pathway
	/// </summary>
	//public CD_BaseMEntity? HoldingTopPathwaySustain { get; private set; } = null;
	/// <summary>
	/// Which entity is being held on the bottom pathway
	/// </summary>
	//public CD_BaseMEntity? HoldingBottomPathwaySustain { get; private set; } = null;


	/// <summary>
	/// Is the player in the air right now?
	/// </summary>
	public bool InAir => Conductor.Time - __whenjump < __jumpmax;

	public double AirTime => (Conductor.Time - __whenjump);
	public double TimeToAnimationEnds => __jumpAnimationStops - (Conductor.Time - __whenjump);

	public double Hologram_AirTime => (Conductor.Time - __whenHjump);
	public double Hologram_TimeToAnimationEnds => __jumpAnimationHStops - (Conductor.Time - __whenHjump);

	public ISustainManager Sustains = new StackBasedSustainManager();

	/// <summary>
	/// Can the player jump right now?
	/// </summary>
	public bool CanJump => !InAir;

	private double __jumpmax = 0.5d;
	private double __jumpAnimationStops = 0.5d;
	private double __jumpAnimationHStops = 0.5d;
	private double __whenjump = -2000000000000d;
	private double __whenHjump = -2000000000000d;

	public void Heal(float health) {
		Health = Math.Clamp(Health + health, 0, MaxHealth);
	}

	/// <summary>
	/// Damage the player.
	/// </summary>
	/// <param name="entity"></param>
	/// <param name="damage"></param>
	public void Damage(DashEnemy? entity, float damage) {
		if (!InIFrame) {
			Health -= damage;
			SetIFrameTime();

			if (InAir)
				PlayCharacterAnimation(CharacterAnimationType.JumpHurt);
			else
				PlayCharacterAnimation(CharacterAnimationType.Hurt);
		}

		ResetCombo();
	}
	public double LastFeverIncreaseTime { get; private set; } = -2000;
	/// <summary>
	/// Adds to the players fever value, and automatically enters fever when the player has maxed out the fever bar.
	/// </summary>
	/// <param name="fever"></param>
	public void AddFever(float fever) {
		if (InFever) return;

		Fever = Math.Clamp(Fever + fever, 0, MaxFever);
		LastFeverIncreaseTime = Conductor.Time;
		if (Fever >= MaxFever)
			EnterFever();
	}
	/// <summary>
	/// Enters fever.
	/// </summary>
	private void EnterFever() {
		InFever = true;
		WhenDidFeverStart = Conductor.Time;
		if (!IsSeeking) {
			// FeverFX?.Start(this);
			PlaySceneSound(SceneSound.Fever, 0);
		}
	}
	/// <summary>
	/// Exits fever.
	/// </summary>
	private void ExitFever() {
		InFever = false;
		Fever = 0;
		WhenDidFeverStart = -1000000d;
	}
	/// <summary>
	/// Adds 1 to the players combo.
	/// </summary>
	public void AddCombo() {
		Combo++;
		__lastCombo = Conductor.Time;
	}

	/// <summary>
	/// Resets the players combo.
	/// </summary>
	public void ResetCombo() => Combo = 0;
	/// <summary>
	/// Adds to the players score.
	/// </summary>
	/// <param name="score"></param>
	public void AddScore(int score) {
		float s = (float)score;
		Score += (int)s;
	}
	/// <summary>
	/// Removes from the players score.
	/// </summary>
	/// <param name="score"></param>
	public void RemoveScore(int score) => Score -= score;

	/// <summary>
	/// This is a callback for <see cref="ISustainManager"/> implementations. It expects wasSustaining, inSustaining, and sustainCount to be relative to the current pathway.
	/// </summary>
	/// <param name="sustain"></param>
	/// <param name="pathway"></param>
	/// <param name="wasSustainingBefore"></param>
	/// <param name="isSustainingNow"></param>
	/// <param name="sustainCount"></param>
	public void OnSustainCallback(SustainBeam sustain, PathwaySide pathway, bool wasSustainingBefore, bool isSustainingNow, int sustainCount) {
		bool nowInsustain = Sustains.ActiveSustains() > 0;

		// todo
		if (isSustainingNow) {
			// TODO: Evaluate this...
			// if (InAir || Sustains.IsSustaining(PathwaySide.Top) && pathway == PathwaySide.Bottom)
			// 	PlayCharacterAnimation(CharacterAnimationType.DownPress);
			// else if (!InAir || Sustains.IsSustaining(PathwaySide.Bottom))
			// 	PlayCharacterAnimation(CharacterAnimationType.UpPress);
			// else

			PlayCharacterAnimation(CharacterAnimationType.Press);
		}
		else if (!nowInsustain) {
			if (pathway == PathwaySide.Top)
				PlayCharacterAnimation(CharacterAnimationType.UpPressEnd);
			else
				PlayCharacterAnimation(CharacterAnimationType.Run);
		}

		if (HasActiveScene(out var scene))
			scene.OnPressStateChange(nowInsustain, wasSustainingBefore);
	}

	public delegate void AttackEvent(MuseDash1Game game, PathwaySide side);
	public event AttackEvent? OnAirAttack;
	public event AttackEvent? OnGroundAttack;

	public float CharacterYRatio => (float)Math.Clamp(NMath.Ease.OutExpo(TimeToAnimationEnds * 10), 0, 1);
	public float HologramCharacterYRatio => (float)Math.Clamp(NMath.Ease.OutExpo(Hologram_TimeToAnimationEnds * 10), 0, 1);


	// todo: confirm this is the right behavior
	static bool PathwayTransitionAnimation => true;

	public bool AttackAir(in PollResult result) {
		if (InMashState) {
			PlayCharacterAnimation(CharacterAnimationType.JumpHit);

			OnAirAttack?.Invoke(this, PathwaySide.Top);
			return true;
		}

		if (result.Hit) {
			var isDHE = result.Hit && result.HitEntity is DoubleHitEnemy;

			if (isDHE)
				PlayCharacterAnimation(CharacterAnimationType.BigHit);
			else if (result.HitEntity is not SustainBeam)
				if (!InAir && !Sustains.IsSustaining() && PathwayTransitionAnimation)
					PlayCharacterAnimation(CharacterAnimationType.UpHit);
				else
					PlayCharacterAnimation(result.IsPerfect ? CharacterAnimationType.JumpHit : CharacterAnimationType.JumpHitGreat);

			OnAirAttack?.Invoke(this, PathwaySide.Top);

			if (Sustains.IsSustaining())
				__whenHjump = Conductor.Time;
			else
				__whenjump = Conductor.Time;

			return true;
		}
		else if (CanJump) {
			if (!Sustains.IsSustaining())
				PlayCharacterAnimation(CharacterAnimationType.Jump);
			OnAirAttack?.Invoke(this, PathwaySide.Top);

			if (Sustains.IsSustaining())
				__whenHjump = Conductor.Time;
			else
				__whenjump = Conductor.Time;

			return true;
		}

		return false;
	}

	public void AttackGround(PollResult result) {
		if (InMashState) {
			PlayCharacterAnimation(CharacterAnimationType.AttackPerfect);
			OnGroundAttack?.Invoke(this, PathwaySide.Bottom);
			return;
		}

		if (result.Hit) {
			if (result.HitEntity is DoubleHitEnemy)
				PlayCharacterAnimation(CharacterAnimationType.BigHit);
			else if (result.HitEntity is not SustainBeam)
				if (InAir && PathwayTransitionAnimation)
					PlayCharacterAnimation(CharacterAnimationType.DownHit);
				else
					PlayCharacterAnimation(result.IsPerfect ? CharacterAnimationType.AttackPerfect : CharacterAnimationType.AttackGreat);
		}
		else if (!Sustains.IsSustaining()) {
			if (InAir)
				PlayCharacterAnimation(CharacterAnimationType.DownHit);
			else
				PlayCharacterAnimation(CharacterAnimationType.AttackMiss);
		}

		__whenjump = -2000000000000d;
		__whenHjump = -2000000000000d;
		OnGroundAttack?.Invoke(this, PathwaySide.Bottom);
	}
	/// <summary>
	/// Gets the current pathway the player is on. Returns Top if jumping, else bottom.
	/// </summary>
	public PathwaySide Pathway {
		get {
			var state = Sustains.GetSustainState();
			if (state == PathwaySide.None) return InAir ? PathwaySide.Top : PathwaySide.Bottom;
			return state;
		}
	}

	public bool CanHit(PathwaySide pathway) {
		if (Sustains.IsSustaining(pathway))
			return false;

		if (pathway == PathwaySide.Top && InAir)
			return false;

		return true;
	}

	internal void SetEnemyPosition(DashEnemy ent) {
		ent.Position = new(0, 2.25f);
	}

	internal void SetEnemyKilledPosition(DashEnemy ent) {
		var pos = GetPathwayPosition(ent.Pathway);
		ent.Position = new(pos.X, -pos.Y);
	}

	public virtual IGamemodeDescriptor GetGamemode() => GamemodeMod.GetGamemode(MuseDash1Gamemode.UUID)!;
	ISong? IGame.GetSong() => gameParameters.Chart?.GetSong();
	ISongChart? IGame.GetSongChart() => gameParameters.Chart;
	object? IGame.GetGamemodeData() => gameParameters.Chart?.GetGamemodeData();
	IConductor IGame.GetConductor() => Conductor;

	public IMuseDash1SceneInstance GetSceneAtTime(double time) {
		for (int i = sceneChanges.Count - 1; i >= 0; i--) {
			var sceneChange = sceneChanges[i];
			if (time >= sceneChange.Time)
				return scenes[sceneChange.ArrayIdx];
		}
		return scenes[0];
	}

	/// <summary>
	/// Current combo of the player (how many successful hits/avoids in a row)
	/// </summary>
	public int Combo { get; private set; } = 0;
	private double __lastCombo = -2000; // Last time a combo occured in game-time

	public double LastCombo => __lastCombo;

	internal CD_Player_UIBar UIBar;
	internal class CD_Player_UIBar : Element
	{
		public CD_Player_UIBar() {

		}
		protected override void Initialize() {
			base.Initialize();
			Dock = Dock.Bottom;
		}
		public override void Paint(float width, float height) {
			var lvl = Level.As<MuseDash1Game>();

			var startAtX = width / 4f;
			var totalW = width / 2f;
			var endAtX = startAtX + totalW;

			Graphics2D.ScissorRect(RectangleF.XYWH(startAtX, 0, endAtX, height));

			Graphics2D.SetDrawColor(255, 60, 42);
			Graphics2D.DrawRectangle(width / 4f, 0, (width / 2f) * (lvl.Health / lvl.MaxHealth), 24);
			Graphics2D.SetDrawColor(255 / 2, 60 / 2, 42 / 2);
			Graphics2D.DrawRectangleOutline(width / 4f, 0, (width / 2f), 24, 2);
			Graphics2D.SetDrawColor(255, 220, 200);
			Graphics2D.DrawText(width / 2f, 12, $"HP: {lvl.Health}/{lvl.MaxHealth}", Graphics2D.UI_FONT_NAME, 22, Anchor.Center);
			float feverRatio;
			if (lvl.InFever)
				feverRatio = (float)lvl.FeverTimeLeft / lvl.FeverTime;
			else
				feverRatio = (float)lvl.Fever / lvl.MaxFever;

			var lastTimeHit = lvl.LastFeverIncreaseTime;

			Graphics2D.SetDrawColor(72, 160, 255);
			Graphics2D.DrawRectangle(width / 4f, 32, (width / 2f) * feverRatio, 24);

			// when hit gradient
			var gradSize = 48;
			var gradColor = new Color(162, 220, 255, (int)(float)NMath.Remap(lvl.Conductor.Time, lastTimeHit, lastTimeHit + .2f, 255, 0, clampOutput: true));
			Graphics2D.DrawGradient(new(startAtX + ((width / 2f) * feverRatio) - gradSize, 33), new(gradSize, 24 - 2), gradColor, new(gradColor.R, gradColor.G, gradColor.B, (byte)0), Dock.Left);



			Graphics2D.SetDrawColor(72 / 2, 160 / 2, 255 / 2);
			Graphics2D.DrawRectangleOutline(startAtX, 32, (width / 2f), 24, 2);
			Graphics2D.SetDrawColor(200, 220, 255);
			Graphics2D.DrawText(width / 2f, 32 + 12, lvl.InFever ? $"FEVER! {Math.Round(lvl.FeverTimeLeft, 2):0.00}s remaining" : $"FEVER: {Math.Round((lvl.Fever / lvl.MaxFever) * 100)}%", Graphics2D.UI_FONT_NAME, 22, Anchor.Center);

			Graphics2D.ScissorRect();
		}
	}

	internal CD_Player_Scorebar Scorebar;
	internal class CD_Player_Scorebar : Element
	{
		public CD_Player_Scorebar() {

		}
		protected override void Initialize() {
			base.Initialize();
			Dock = Dock.Top;
		}
		public override void Paint(float width, float height) {
			Graphics2D.SetDrawColor(255, 255, 255, 255);
			//if (Level.AutoPlayer.Enabled)
			//Graphics2D.DrawText(width / 2f, 32 + 48, $"AUTO", Graphics2D.UI_FONT_NAME, 32, Anchor.Center);
			var lvl = Level.As<MuseDash1Game>();
			Graphics2D.DrawText(width * 0.4f, 32 + 24, $"{lvl.Combo}", Graphics2D.UI_FONT_NAME, (int)NMath.Remap(lvl.Conductor.Time - lvl.LastCombo, 0.2f, 0, 32, 40, clampOutput: true), Anchor.Center);
			Graphics2D.DrawText(width * 0.4f, 32 + 56, "COMBO", Graphics2D.UI_FONT_NAME, 24, Anchor.Center);

			Graphics2D.DrawText(width * 0.6f, 32 + 24, $"{lvl.Score}", Graphics2D.UI_FONT_NAME, 32, Anchor.Center);
			Graphics2D.DrawText(width * 0.6f, 32 + 56, "SCORE", Graphics2D.UI_FONT_NAME, 24, Anchor.Center);
		}
	}
}
