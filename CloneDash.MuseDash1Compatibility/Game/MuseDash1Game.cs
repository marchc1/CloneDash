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
using CloneDash.MD1_Compat.Compatibility;
using CloneDash.MD1_Compat.Game.Events;
using CloneDash.Menu;
using CloneDash.Scenes;
using CloneDash.Settings;
using CloneDash.Systems;
using CommunityToolkit.HighPerformance;
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
using Nucleus.Models;
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
using Image = Nucleus.UI.Elements.Image;

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

	public const string UUID = "gamemode/musedash1/standard";
}

[MarkForStaticConstruction]
public partial class MuseDash1Game(DashGameParams gameParameters) : Level, IGame
{
	public readonly MuseDash1EnemyManager EnemyManager = new();
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
		if (md_level.IsEmpty) {
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
	public void Restart() => SeekTo(0);
	public override void OnUnload() {
		SceneUI?.Dispose();
		SceneUI = null;
	}
	public bool IsSeeking { get; private set; } = false;

	/// <summary>
	/// Resets the entire game state and variables.
	/// </summary>
	public virtual void Reset() {
		ExitMashState();
		ResetScreenspaceEffects();

		if (Sustains.IsSustaining() && HasActiveScene(out var scene))
			scene.OnPressStateChange(false, true);

		SceneUI?.UpdateAllPerfect(true);
		SceneUI?.UpdateFullCombo(true);
		SceneUI?.Reset();
		SceneUI?.SetSeeking(true);
		SceneUI?.UpdateScore(0);
		SceneUI?.UpdateCombo(0);
		SceneUI?.UpdateHP(Quirks.MaxHP, Quirks.MaxHP);
		SceneUI?.UpdateFeverProgress(0, 0);

		Character.Reset();

		SetScene(FirstScene);

		Stats.Reset();
		foreach (var entity in Entities) {
			if (entity is not DashEnemy entCD)
				continue;

			entCD.Reset();
		}

		Boss.Reset();
		ResetPathwaySpeeds();

		Combo = 0;
		Health = Quirks.MaxHP;
		InFever = false;
		WhenDidFeverStart = -1000000d;
		lastNoteHit = false;
		Score = 0;
		Fever = 0;
		Sustains.Reset();
		AutoPlayer.Reset();
		__whenjump = -2000000000000d;
		__whenHjump = -2000000000000d;
		DeathTime = -2000000d;
		ActiveEvents.Clear();
		HandledEvents.Clear();
		lastIFrameGivenTime = -10000d;
		Dead = false;
	}

	/// <summary>
	/// Produces a quirks snapshot for use with the current gameplay quirks
	/// </summary>
	protected MuseDash1GameplayQuirks.GameSnapshot ProduceSnapshot(DashEnemy? enemy = null, MuseDash1GameplayQuirks.Judgement judgement = 0) {
		MuseDash1GameplayQuirks.GameSnapshot snapshot = new() {
			Difficulty = gameParameters.Chart?.RatingNumber ?? 0,
			CurrentCombo = Combo,
			CurrentScore = Score,
			InFever = InFever,
			Health = Health,
			MaxHealth = Quirks.MaxHP,
			Judgement = judgement
		};

		if (enemy != null) {
			snapshot = snapshot with {
				EntityBlood = enemy.Blood ? 80 : 0,
				EntityType = enemy.Type,
				EntityWarning = enemy.Warns,
			};
		}

		return snapshot;
	}

	public void SeekTo(double time) {
		time = Math.Clamp(time, 0, audiosystem.GetPlaybackDuration(in Music));
		IsSeeking = true;

		Reset();

		if (time < 0.06f)
			audiosystem.RestartSound(Music);
		else
			audiosystem.SetSoundPlayhead(Music, time);

		if (time > 0) {
			foreach (var ev in Events) {
				switch (ev.TriggerType) {
					case EventTriggerType.AtTime:
						Conductor.ForceTimeTo(ev.Time);
						if (ev.Time < time)
							ActivateEvent(ev);

						Conductor.ForceTimeTo(ev.Time + ev.Length);
						if (ev.Time + ev.Length < time)
							DeactivateEvent(ev);

						break;
					case EventTriggerType.AtTimeMinusLength:
						Conductor.ForceTimeTo(ev.Time - ev.Length);
						if (ev.Time - ev.Length < time)
							ActivateEvent(ev);

						Conductor.ForceTimeTo(ev.Time);
						if (ev.Time < time)
							DeactivateEvent(ev);
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
			if (InFever) FeverFX?.Activate();

			// ALSO A HACK - but it solves some animation issues when mid-sustain.
			if (Sustains.IsSustaining())
				PlayCharacterAnimation(CharacterAnimationType.Press);
		}

		IsSeeking = false;
		SceneUI?.SetSeeking(false);
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
		if (!IsSeeking) {
			SceneUI?.StartMultiHitText();
			UpdateMashTextEffect();
		}
		InMashState = true;
		MashingEntity = ent;
		LastMasherRealHit = Conductor.Time;
		LastMasherAttemptedHit = Conductor.Time;
	}
	public void UpdateMashTextEffect() {
		if (!IValidatable.IsValid(MashingEntity)) return;
		SceneUI?.UpdateMultiHitText(MashingEntity.Hits);
	}
	/// <summary>
	/// Exits the mash state.
	/// </summary>
	public void ExitMashState() {
		SceneUI?.EndMultiHitText();
		InMashState = false;
		MashingEntity = null;
		LastMasherRealHit = double.NaN;
		LastMasherAttemptedHit = double.NaN;
	}

	MuseDash1GameplayQuirks Quirks = new();
	// Player input system
	InputState InputState;
	public ref readonly InputState GetInputState() => ref InputState;
	public List<ICloneDashInputSystem> InputReceivers { get; } = [];

	public AutoPlayer AutoPlayer { get; private set; } = null!;
	/// <summary>
	/// Timing system.
	/// </summary>
	public Conductor Conductor { get; private set; } = null!;
	public AudioPlaybackHandle Music;
	public IMuseDash1CharacterInstance Character { get; set; }
	// public ModelEntity Player { get; set; }
	// public ModelEntity HologramPlayer { get; set; }
	// public MD1_SpineActionController PlayerController { get; set; }
	// public MD1_SpineActionController HologramPlayerController { get; set; }
	public Boss Boss { get; set; } = null!;
	public Pathway TopPathway { get; set; } = null!;
	public Pathway BottomPathway { get; set; } = null!;

	/// <summary>
	/// Is the game currently paused
	/// </summary>
	public double UnpauseTime { get; private set; } = 0;
	public double DeltaUnpauseTime => Realtime - UnpauseTime;

	public bool SetPauseGuarded(bool paused) {
		if (IsDead())
			return false;

		Paused = paused;
		return true;
	}

	// WIP pausing
	// return false to not spawn the pause menu
	private bool startPause() {
		if (lastNoteHit)
			return false;
		if (Conductor.Time < 0)
			return false;

		if (SetPauseGuarded(true)) {
			audiosystem.PauseSound(in Music);
			UnpauseTime = 0;
			return true;
		}
		return false;
	}
	private void startUnpause() {
		if (HasActiveScene(out var scene))
			scene.PlaySound(SceneSound.Unpause, 0);
		UnpauseTime = Realtime;
		Timers.Simple(3, () => {
			fullUnpause();
		});
	}
	private void fullUnpause() {
		audiosystem.ResumeSound(in Music);
		SetPauseGuarded(false);
		UnpauseTime = 0;
	}

	public void ForcePause() {
		audiosystem.PauseSound(in Music);
		SetPauseGuarded(true);
	}
	public void ForceUnpause() {
		audiosystem.ResumeSound(in Music);
		SetPauseGuarded(false);
	}

	int attackP = 0;
	int failP = 0;

	private bool __deferringAsync = false;

	public StatisticsData Stats = null!;

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
					if (!IsSeeking)
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
	IMuseDash1FeverRuntime FeverFX;
	public bool HasSceneInitialized(ISceneDescriptor descriptor) {
		return sceneLUT.ContainsKey(descriptor.GetUUID().Hash());
	}
	IMuseDash1SceneUI? SceneUI;
	public override void Initialize(params object[] _) {
		ResetPathwaySpeeds();
		ResetScreenspaceEffects();

		Stats = new(gameParameters.Chart);
		using (StaticSequentialProfiler.StartStackFrame("CD_GameLevel.RichPresenceUpdate")) {
			RichPresenceSystem.SetPresence(new() {
				Details = "In Game",
				State = $"Muse Dash 1 - '{gameParameters.Chart?.Song?.Name ?? "<null>"}'"
			});
		}
		using (StaticSequentialProfiler.StartStackFrame("CD_GameLevel.PrepareShaders")) {
			Interlude.Spin(submessage: "Preparing shaders...");
			PrepareShaders();
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
				Quirks = MuseDash1GameplayQuirks.Default;
				charData.ApplyQuirks(ref Quirks);
				Character = charData.CreateInGame<IMuseDash1CharacterInstance>(this)!;
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
						if (sceneChangeInstance != null) {
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
				// The Scene UI never changes, it always inherits the original starting scene it seems
				SceneUI = FirstScene?.CreateUI();

				var feverFX = FeverMod.InstantiateCurrentFever(this);
				FeverFX = feverFX;
			}

			// Before loading the scene, load quirks, since some quirks might affect entity data
			// TODO: Elfin quirk mods
			MuseDash1GameplayQuirks.ApplyMods(ref Quirks);

			Interlude.Spin(submessage: "Initializing the scene...");
			using (StaticSequentialProfiler.StartStackFrame("Initialize Scene/Fever")) {
				foreach (var scene in GetAllScenes())
					scene.Initialize();
				FeverFX?.Initialize();
			}

			Interlude.Spin();

			Render3D = false;
			Health = Quirks.MaxHP;

			Interlude.Spin(submessage: "Initializing input...");
			using (StaticSequentialProfiler.StartStackFrame("Build Input Systems"))
				InputReceivers.AddRange(ICloneDashInputSystem.InstantiateAllInputSystems());

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

						Events.Sort((x, y) => x.Time.CompareTo(y.Time));
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

			SceneUI?.Initialize();

			SceneUI?.UpdateAllPerfect(true);
			SceneUI?.UpdateFullCombo(true);

			Interlude.Spin(submessage: "Ready!");

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

	public float GetPlayerY(bool secondary) {
		var height = EngineCore.GetWindowHeight();

		var bot = GetPathwayPosition(PathwaySide.Bottom);
		var character = secondary ? Character.GetSecondary() : Character.GetPrimary();
		if (!character.IsInAir())
			return bot.Y + -1f;

		var top = GetPathwayPosition(PathwaySide.Top);
		float ratio = (float)NMath.Remap(character.GetTimeToAnimationEnd(), character.GetAnimationDuration(), 0, 0, 1, clampOutput: true);
		return (float)NMath.Remap(NMath.Ease.InCirc(NMath.Ease.InExpo(ratio)), 0, 1, top.Y, bot.Y) + -1f; // TODO: re-evaluate
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

	public class PauseMenuButton : Button
	{
		Image? iconImage;
		public PauseMenuButton(Element parent, string image) : base(parent) {
			if (image != null) {
				iconImage = new Image(this);
				iconImage.SetTexture(Level.Textures.LoadTextureFromFile(image));
				iconImage.SetImageOrientation(ImageOrientation.Zoom);
				iconImage.SetImagePadding(new(4));
				iconImage.SetDock(Dock.Left);
			}
		}

		protected override void PerformLayout(float width, float height) {
			base.PerformLayout(width, height);
			if (iconImage != null) {
				iconImage.SetSize(new(height, height));
			}
		}

		public override void Paint(float width, float height) {
			var backpre = GetBgColor();

			var back = Element.MixColorBasedOnMouseState(this, backpre, new(0, 0.8f, 2.4f, 1f), new(0, 1.2f, 0.6f, 1f));
			var fore = Element.MixColorBasedOnMouseState(this, GetFgColor(), new(0, 0.8f, 1.8f, 1f), new(0, 1.2f, 0.6f, 1f));

			Graphics2D.SetDrawColor(back);
			Graphics2D.DrawRectangle(0, 0, width, height);
			var text = GetText();
			var tSize = Graphics2D.GetTextSize(text, GetFont(), GetTextSize());
			Graphics2D.SetDrawColor(255, 255, 255);
			Graphics2D.DrawText(new((width / 2) + (height / 4), height / 2), text, GetFont(), GetTextSize(), Anchor.Center);
		}
	}

	public override void Think(FrameState frameState) {
		ResetSceneSoundsPlayedThisFrame();

		if (Music.IsValid() && lastNoteHit && audiosystem.IsPlaybackComplete(Music) && gameParameters.Chart != null && SceneUI != null && !SceneUI.ShowingVictoryScreen()) {
			Stats.UploadScore(Score);
			SceneUI?.OpenVictory(Stats);
			audiosystem.PauseSound(Music);
			return;
		}

		if (IsDead() && ShowDeathTrigger()) {
			return;
		}

		if (ShouldExitFever && InFever)
			ExitFever();

		// Health drain
		if (Quirks.HealthLossPerSecond != null && Conductor.GetTime() >= 0) {
			double loss = 0;
			Quirks.HealthLossPerSecond(ProduceSnapshot(), ref loss);
			DrainHealth(loss * Conductor.TimeDelta);
		}

		InputState.Reset();
		if (!IsDead()) {
			if (AutoPlayer.Enabled) {
				AutoPlayer.Play(ref InputState);
				foreach (ICloneDashInputSystem playerInput in InputReceivers)
					playerInput.Poll(frameState, ref InputState, InputAction.PauseGame);
			}

			else if (!IValidatable.IsValid(RootPanel.GetKeyboardFocusedElement())) {
				foreach (ICloneDashInputSystem playerInput in InputReceivers)
					playerInput.Poll(frameState, ref InputState);
			}

			if (InMashState) {
				UpdateMashTextEffect();
				if (CheckMashHit())
					MashingEntity.Hit(PathwaySide.Bottom, 0);
			}

			if (InputState.PauseButton) {
				if (Music.IsValid() && audiosystem.IsPlaybackPaused(Music)) {
					startUnpause();
					if (IValidatable.IsValid(PauseWindow))
						PauseWindow.Remove();
				}
				else {
					if (startPause()) {
						PauseWindow = new Panel(this.RootPanel);
						PauseWindow.SetSize(new(300, 400));
						PauseWindow.Center();

						var flex = new FlexPanel(PauseWindow);
						flex.SetDock(Dock.Fill);
						flex.Direction = Axis.Vertical;
						flex.ChildrenResizingMode = FlexChildrenResizingMode.StretchToFit;
						flex.SetDockPadding(RectangleF.TLRB(4));

						var play = new PauseMenuButton(flex, "ui/pause_play.png");
						play.SetBorderSize(0);
						play.SetText("Return to Game");
						play.SetTextSize(24);
						play.OnButtonClick += delegate (Button self, ButtonCode clickedButton) {
							PauseWindow.Remove();
							startUnpause();
						};

						var restart = new PauseMenuButton(flex, "ui/pause_restart.png");
						restart.SetBorderSize(0);
						restart.SetText("Restart Level");
						restart.SetTextSize(24);
						restart.OnButtonClick += delegate (Button self, ButtonCode clickedButton) {
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

						var settings = new PauseMenuButton(flex, "ui/pause_settings.png");
						settings.SetBorderSize(0);
						settings.SetText("Open Preferences...");
						settings.SetTextSize(24);
						settings.OnButtonClick += delegate (Button self, ButtonCode clickedButton) {
							var panel = new Panel(RootPanel);
							panel.SetPaintBackgroundEnabled(false);
							panel.SetAnchor(Anchor.Center);
							panel.SetOrigin(Anchor.Center);
							panel.DynamicallySized = true;
							panel.SetSize(new(0.9f));

							var titlebar = new Titlebar(panel);
							titlebar.SetDock(Dock.Top);
							titlebar.MinimizeButton.SetVisible(false);
							titlebar.MaximizeButton.SetVisible(false);
							titlebar.CloseButton.OnButtonClick += (_, _) => {
								panel.Remove();
							};
							titlebar.SetText("Settings");

							var settings = new SettingsEditor(panel);
							settings.SetDock(Dock.Fill);
							settings.SetDockMargin(RectangleF.TLRB(0, 8, 8, 0));

							panel.MakePopup();
						};

						var back2menu = new PauseMenuButton(flex, "ui/pause_exit.png");
						back2menu.SetBorderSize(0);
						back2menu.SetText("Exit to Menu");
						back2menu.SetTextSize(24);
						back2menu.OnButtonClick += delegate (Button self, ButtonCode clickedButton) {
							LevelTransitions.LoadMainMenu();
						};
					}
				}
				return;
			}
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

		var playerY = yoff ?? GetPlayerY(false);

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
			-GetPlayerY(true)
		));
		Character.GetSecondary().SetScale(new(PlayerScale));

		Character.Think();

		var sceneUI = SceneUI;
		if (sceneUI != null) {
			sceneUI.UpdateCombo(Combo);
			if (InFever)
				sceneUI.UpdateInFever(FeverTimeLeft, Quirks.FeverDuration);

			if (MashingEntity != null)
				sceneUI.UpdateMultiHitText(MashingEntity.Hits);

			sceneUI.UpdateHP(Health, Quirks.MaxHP);
		}

		EnemyManager.RebuildVisibleEnemies(Conductor.Time);
		var visibleEnemies = EnemyManager.GetLastVisibleEnemies();
		var lastEntity = EnemyManager.GetLastEnemy();

		if (lastEntity != null && lastEntity.GetJudgementHitTime() + lastEntity.Length < Conductor.Time && !lastNoteHit) {
			lastNoteHit = true;
			if (Stats.CalculateFullCombo()) {
				Logs.Info("Full combo achieved.");
				PlaySceneSound(SceneSound.FullCombo, 0);
			}
		}

		// Sort the visible entities by their hit time

		IterateEvents();

		//LockEntityBuffer();

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
		foreach (var entity in visibleEnemies) {
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
							SceneUI?.UpdateFullCombo(false);
						}
					}
					break;
				case EntityInteractivity.SamePath:
					if (NMath.InRange(timeToHit, -entity.PreGreatRange, 0)) {
						PathwaySide pathCurrentCharacter = Pathway;
						if ((pathCurrentCharacter == PathwaySide.Both || pathCurrentCharacter == entity.Pathway) && entity.Hits == 0) {
							entity.Hit(pathCurrentCharacter, 0);
							PlaySceneSound(entity.Type switch {
								MuseDash1EntityType.Heart => SceneSound.GotHeart,
								MuseDash1EntityType.Score => SceneSound.GotScore,
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

					bool broken = false;
					if (Pathway == entity.Pathway && timeToHit < -entity.PrePerfectRange && !entity.DidRewardPlayer) {
						//entity.Hit(Game.PlayerController.Pathway);
						if (BreaksAvoids()) {
							if (entity.Hits == 0)
								entity.Hit(Pathway, 0);
							broken = true;
						}
						else
							entity.DamagePlayer();
					}

					// If the player is now avoiding the entity, then reward the player for missing it, and make it so they cant be damaged by it)
					if (!broken && Pathway != entity.Pathway && timeToHit < 0 && !entity.DidDamagePlayer) {
						entity.Pass();
					}

					break;
			}
			//entity.WhenVisible();
		}

		AddDebugString("HoldingTopPathwaySustain", Sustains.GetSustainsActiveCount(PathwaySide.Top));
		AddDebugString("HoldingBottomPathwaySustain", Sustains.GetSustainsActiveCount(PathwaySide.Top));

		if (HasActiveScene(out var scene)) {
			scene.Think(GetBgScrollSpeedMultiplier());
		}
		SceneUI?.Think(globals.CurTimeDelta);
		FeverFX?.Think();
	}


	public int EnemySortIndexCounter;

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
		var visibleEnemies = EnemyManager.GetLastVisibleEnemies();
		foreach (DashEnemy entity in visibleEnemies) {
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
							LastPollResult = PollResult.Create(entity, distance);
							return LastPollResult;
						}
					}
					break;
			}
		}

		LastPollResult = PollResult.Empty;
		return LastPollResult;
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

	public void ActivateEvent(DashEvent ev) {
		ActiveEvents.Add(ev);
		ev.Activate();
		if (!IsSeeking) {
			if (ev.Length == 0)
				Logs.Debug($"Triggering {ev.GetType().Name}");
			else
				Logs.Debug($"Activating {ev.GetType().Name}");
		}
	}
	public void DeactivateEvent(DashEvent ev) {
		HandledEvents.Add(ev);
		ActiveEvents.Remove(ev);
		ev.Deactivate();
		if (!IsSeeking) {
			if (ev.Length != 0)
				Logs.Debug($"Deactivating {ev.GetType().Name}");
		}
	}

	public void IterateEvents() {
		foreach (var ev in Events) {
			if (ActiveEvents.Contains(ev)) {
				// Determine if the event needs to be deactivated
				if (shouldDeactivateEvent(ev))
					DeactivateEvent(ev);
			}
			else if (!HandledEvents.Contains(ev)) {
				// Determine if the event needs to be activated
				if (shouldActivateEvent(ev))
					ActivateEvent(ev);
			}
			// The event has both been activated and deactivated, so its ignored
		}
	}

	readonly FCurve<float> flashbangIntensity = new();
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
		// This is a hack... whatever
		if (ev is FlashbangEffect flash)
			flashbangIntensity.AddKeyframe(new() { Time = flash.Time, Value = (float)flash.TargetValue, Interpolation = KeyframeInterpolation.Linear });
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
	public virtual DashEnemy? LoadEntity(MD1_SongChartEntity ChartEntity) {
		Interlude.Spin(submessage: "Loading entities...");

		if (!DashEnemy.TryCreateFromType(this, ChartEntity.Type, out DashEnemy? ent)) {
			Console.WriteLine("No load entity handler for type " + ChartEntity.Type);
			return null;
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
		EnemyManager.AddEnemy(ent);

		return ent;
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

	ComplexRenderTexture? renderTexture;
	ComplexRenderTexture? renderTexture2;

	public override void PreRender(FrameState frameState) {
		float width = EngineCore.GetWindowWidth(), height = EngineCore.GetWindowHeight();
		// Evaluate if complex render texture needs to be remade
		// This is only the case if null or bounds changed
		if (renderTexture == null || (renderTexture.Width != width || renderTexture.Height != height)) {
			renderTexture?.Dispose();
			renderTexture2?.Dispose();
			// TODO: If complex render textures are too slow for this (and they might be), then
			// comment out this line to remove it from the rendering pipeline here - you just won't get screenspace effects, 
			// when i have that working
			renderTexture = Textures.CreateComplexRenderTexture((int)width, (int)height);
			renderTexture2 = Textures.CreateComplexRenderTexture((int)width, (int)height);
		}

		renderTexture?.BeginDrawing();
		EngineCore.Window.ClearBackground(Color.Blank);
		base.PreRender(frameState);
		//Stopwatch test = Stopwatch.StartNew();
		if (HasActiveScene(out var scene))
			scene.RenderBackground();

		FeverFX?.Render();
		//Logs.Info(test.Elapsed.TotalMilliseconds);
	}

	public override void CalcView2D(FrameState frameState, ref Camera2D cam) {
		var zoomValue = MashZoomSOS.Update(InMashState ? 1 : 0) * .5f;
		cam.Zoom = ((frameState.WindowHeight / 900) * 120) + (zoomValue * 45) * 0.5f;
		cam.Rotation = 0.0f;
		cam.Offset = new(frameState.WindowWidth / 2, frameState.WindowHeight / 2);
		cam.Target = new(zoomValue * -5, 0);
		cam.Offset += cam.Target;

		//cam.Offset = new(frameState.WindowWidth * Game.Pathway.PATHWAY_LEFT_PERCENTAGE * .5f, frameState.WindowHeight * 0.5f);
		//cam.Target = cam.Offset;
	}

	public void ConditionallyRenderVisibleEntities(FrameState frameState, Predicate<DashEnemy> enemyPredicate, Span<DashEnemy> visibleEnemies) {
		DeadEntityVisibility deadVis = GameSettings.DeadEntityVisibility;
		foreach (DashEnemy ent in visibleEnemies) {
			if (!enemyPredicate(ent)) continue;

			if (ent.Dead) {
				switch (deadVis) {
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
	int render_visibleEntitiesCount;
	public override void Render(FrameState frameState) {
		Rlgl.DrawRenderBatchActive();

		Rlgl.DisableDepthTest();
		Rlgl.DisableDepthMask();
		Rlgl.DisableBackfaceCulling();

		//Raylib.DrawLineV(new(-100000, 0), new(100000, 0), Color.Red);
		//Raylib.DrawLineV(new(0, -100000), new(0, 100000), Color.Green);

		SceneUI?.PreRenderWorldspace();

		// Pathways
		TopPathway.Render();
		BottomPathway.Render();

		Span<DashEnemy> visibleEnemies = EnemyManager.GetLastVisibleEnemies();

		// Hold notes
		ConditionallyRenderVisibleEntities(frameState, static x => x.Type == MuseDash1EntityType.SustainBeam, visibleEnemies);

		// Boss
		Boss.Render();

		// The other entities, that aren't sustain beams, in order of top -> bottom pathway
		ConditionallyRenderVisibleEntities(frameState, static x => x.Type != MuseDash1EntityType.SustainBeam && x.Pathway == PathwaySide.Top, visibleEnemies);
		ConditionallyRenderVisibleEntities(frameState, static x => x.Type != MuseDash1EntityType.SustainBeam && x.Pathway == PathwaySide.Bottom, visibleEnemies);

		AddDebugString("Visible Entities", EnemyManager.GetLastVisibleEnemies().Length);

		Rlgl.DrawRenderBatchActive();
	}
	public override void PostRenderEntities(FrameState frameState) {
		base.PostRenderEntities(frameState);

		SceneUI?.PostRenderWorldspace();
		Rlgl.DrawRenderBatchActive();
	}
	public override void PostRender(FrameState frameState) {
		base.PostRender(frameState);

		SceneUI?.RenderUI();

		renderTexture?.EndDrawing();
		ScreenspaceDraw(frameState);
	}


	private double LastAttackTime;
	private PathwaySide LastAttackPathway;

	public void BroadcastEntitySignal(DashEnemy? entityFrom, EntitySignalType signalType, object? data = null) {
		foreach (var enemy in EnemyManager.GetAllEnemies())
			enemy.OnSignalReceived(entityFrom, signalType, data);
	}

	public void SendEntitySignal(DashEnemy? entityFrom, DashEnemy? entityTo, EntitySignalType signalType, object? data = null) {
		entityTo?.OnSignalReceived(entityFrom, signalType, data);
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

						bool showearlylate = GameSettings.gp_earlylate.GetBool();
						EarlyLate earlylate = EarlyLate.Perfect;
						if (showearlylate) {
							// TODO: tolerances for early/late..
						}

						// Mashers don't create perfects, they'll start the mash hit UI
						if (pollResult.HitEntity.Type != MuseDash1EntityType.Masher) {
							if (pollResult.IsPerfect)
								SceneUI?.CreatePerfectHitText(pollResult.Precision, pathway, InFever, earlylate);
							else {
								SceneUI?.CreateGreatHitText(pollResult.Precision, pathway, InFever, earlylate);
								SceneUI?.UpdateAllPerfect(false);
							}
							SceneUI?.UpdateHit();
						}

						PlaySceneSound(pollResult.HitEntity.Type switch {
							MuseDash1EntityType.Single => pollResult.HitEntity.Variant switch {
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
							MuseDash1EntityType.Hammer => SceneSound.HitHammer,
							MuseDash1EntityType.Double => SceneSound.HitGemini,
							MuseDash1EntityType.SustainBeam => SceneSound.StartedHold,
							MuseDash1EntityType.Raider => SceneSound.HitRaider,
							MuseDash1EntityType.Ghost => SceneSound.HitGhost,
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

	bool Dead;
	double DeathTime = -2000000;
	public bool IsDead() => Dead;

	void TriggerDeath() {
		if (IsDead()) return;
		if (Health <= 0) {
			Dead = true;
			DeathTime = Curtime;
			Character.GetPrimary().PlayAnimation(CharacterAnimationType.Die);
		}
	}

	bool ShowDeathTrigger() {
		if ((Curtime - DeathTime) > 5) {
			if (SceneUI != null && !SceneUI.ShowingFailureScreen())
				SceneUI.OpenFailure();
			// always return true if curtime - deathtime > 5
			return true;
		}
		return false;
	}

	private double lastIFrameGivenTime = -200000;

	/// <summary>
	/// Time since the last invincibility frame was given
	/// </summary>
	public double TimeSinceLastIFrame => Conductor.Time - lastIFrameGivenTime;
	/// <summary>
	/// Currently in an invincibility frame?
	/// </summary>
	public bool InIFrame => TimeSinceLastIFrame < Quirks.IFrameLength;
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
	private bool ShouldExitFever => (Conductor.Time - WhenDidFeverStart) >= Quirks.FeverDuration;
	/// <summary>
	/// How much fever time is left?
	/// </summary>
	public double FeverTimeLeft => Quirks.FeverDuration - (Conductor.Time - WhenDidFeverStart);
	/// <summary>
	/// Returns the fever time left as a value of 0-1, where 0 is the end and 1 is the start. Good for animation.
	/// </summary>
	private double FeverRatio => 1f - ((Conductor.Time - WhenDidFeverStart) / Quirks.FeverDuration);
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
	public bool InAir => (Conductor.Time - __whenjump) < Character.GetJumpDuration();

	public double AirTime => (Conductor.Time - __whenjump);
	public double TimeToAnimationEnds => Character.GetPrimary().GetAnimationDuration() - (Conductor.Time - __whenjump);

	public double Hologram_AirTime => (Conductor.Time - __whenHjump);
	public double Hologram_TimeToAnimationEnds => Character.GetSecondary().GetAnimationDuration() - (Conductor.Time - __whenHjump);

	public ISustainManager Sustains = new StackBasedSustainManager();

	/// <summary>
	/// Can the player jump right now?
	/// </summary>
	public bool CanJump => !InAir;

	private double __whenjump = -2000000000000d;
	private double __whenHjump = -2000000000000d;

	public void Heal(float health, DashEnemy? responsible = null) {
		Health = Math.Clamp(Health + health, 0, Quirks.MaxHP);
	}

	/// <summary>
	/// Drains health from the player, triggering death if the player has died, but does not call any other events unlike Damage
	/// </summary>
	/// <param name="damage"></param>
	/// <param name="responsible"></param>
	public void DrainHealth(double damage) {
		Health = Math.Clamp(Health - (float)damage, 0, Quirks.MaxHP);
		if (Health <= 0)
			TriggerDeath();
	}

	/// <summary>
	/// Damage the player.
	/// </summary>
	/// <param name="entity"></param>
	/// <param name="damage"></param>
	public void Damage(float damage, DashEnemy? responsible) {
		if (InFever && Quirks.InvincibleInFever)
			return;

		double damageD = (double)damage;
		if (Quirks.DamageModifier != null) Quirks.DamageModifier.Invoke(ProduceSnapshot(responsible), ref damageD);
		damage = (float)damageD;
		damage = Math.Max(0, damage);

		if (!InIFrame) {
			Health = Math.Clamp(Health - damage, 0, Quirks.MaxHP);
			SetIFrameTime();
			if (Health <= 0)
				TriggerDeath();
			if (InAir)
				PlayCharacterAnimation(CharacterAnimationType.JumpHurt);
			else
				PlayCharacterAnimation(CharacterAnimationType.Hurt);
		}

		ResetCombo();
		SceneUI?.UpdateFullCombo(false);
	}
	/// <summary>
	/// Adds to the players fever value, and automatically enters fever when the player has maxed out the fever bar.
	/// </summary>
	/// <param name="fever"></param>
	public void AddFever(float fever, DashEnemy? responsible = null) {
		if (InFever) return;

		double feverD = (double)fever;
		if (Quirks.FeverModifier != null) Quirks.FeverModifier.Invoke(ProduceSnapshot(responsible), ref feverD);
		fever = (float)feverD;

		Fever = Math.Clamp(Fever + fever, 0, Quirks.MaxFever);
		SceneUI?.UpdateFeverProgress(Fever, Quirks.MaxFever);
		if (Fever >= Quirks.MaxFever)
			EnterFever();
	}
	/// <summary>
	/// Enters fever.
	/// </summary>
	private void EnterFever() {
		InFever = true;
		WhenDidFeverStart = Conductor.Time;
		if (!IsSeeking) {
			FeverFX?.Activate();
			PlaySceneSound(SceneSound.Fever, 0);
		}
		SceneUI?.UpdateInFever(FeverTimeLeft, Quirks.FeverDuration);
	}
	/// <summary>
	/// Exits fever.
	/// </summary>
	private void ExitFever() {
		InFever = false;
		Fever = 0;
		WhenDidFeverStart = -1000000d;
		FeverFX?.Cancel();
	}
	/// <summary>
	/// Adds 1 to the players combo.
	/// </summary>
	public void AddCombo(DashEnemy? responsible = null) {
		Combo++;
		__lastCombo = Conductor.Time;
	}

	/// <summary>
	/// Resets the players combo.
	/// </summary>
	public void ResetCombo(DashEnemy? responsible = null) {
		Combo = 0;
	}

	/// <summary>
	/// Adds to the players score.
	/// </summary>
	/// <param name="score"></param>
	public void AddScore(int score, DashEnemy? responsible = null) {
		double s = (double)score;
		if (Quirks.ScoreModifier != null) Quirks.ScoreModifier.Invoke(ProduceSnapshot(responsible), ref s);
		score = (int)(float)s;

		Score += (int)(float)s;
		SceneUI?.UpdateScore(Score);
	}
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

	struct ScreenspaceEffectState
	{
		public double LastValue;
		public double CurrentValue;
		public double Length;
		public double Time;
	}

	delegate bool ExecuteShaderFn(IShader shader, ref ScreenspaceEffectState state);
	readonly ScreenspaceEffectState[] ScreenspaceEffectStates = new ScreenspaceEffectState[(int)ScreenspaceEffectType.Count];
	readonly IShader?[] ScreenspaceEffectShaders = new IShader?[(int)ScreenspaceEffectType.Count];
	readonly ExecuteShaderFn?[] ScreenspaceEffectShaderFns = new ExecuteShaderFn?[(int)ScreenspaceEffectType.Count];
	public void ResetScreenspaceEffects() {
		Array.Clear(ScreenspaceEffectStates);
	}
	public double GetBgScrollSpeedMultiplier() => 1 - GetCurrentInterpolatedValue(ref ScreenspaceEffectStates[(int)ScreenspaceEffectType.BgFreeze]);
	public bool ShouldFreezeNoteAnimations() => GetCurrentInterpolatedValue(ref ScreenspaceEffectStates[(int)ScreenspaceEffectType.NoteFreeze]) >= 1;

	public void TriggerScreenspaceEffectStart(ScreenspaceEffectType type, double effectParams, double length) {
		ref ScreenspaceEffectState state = ref ScreenspaceEffectStates[(int)type];

		state.LastValue = GetCurrentInterpolatedValue(ref state);
		state.CurrentValue = effectParams;
		state.Length = length;
		state.Time = Conductor.Time;
	}
	private double GetCurrentInterpolatedValue(ref ScreenspaceEffectState state) {
		if (state.Length <= 0) return state.CurrentValue;

		double t = Math.Clamp((Conductor.Time - state.Time) / state.Length, 0.0, 1.0);
		return double.Lerp(state.LastValue, state.CurrentValue, t);
	}

	double ScreenScrollProgress;
	double ScreenScrollRate;

	// TODO: Verify effect order...
	public void ScreenspaceDraw(FrameState frameState) {
		if (renderTexture == null || renderTexture2 == null)
			return;

		ComplexRenderTexture read = renderTexture;
		ComplexRenderTexture write = renderTexture2;
		DoOneEffect(ScreenspaceEffectType.Sepia, ref read, ref write);
		DoOneEffect(ScreenspaceEffectType.ChromaticAberration, ref read, ref write);
		DoOneEffect(ScreenspaceEffectType.Mosaic, ref read, ref write);
		DoOneEffect(ScreenspaceEffectType.Scanlines, ref read, ref write);
		DoOneEffect(ScreenspaceEffectType.FilmGrain, ref read, ref write);
		DoOneEffect(ScreenspaceEffectType.Vignette, ref read, ref write);

		// this draws the screen scroll effect, its done as a texture shift instead here
		double screenScrollDirection = ScreenspaceEffectStates[(int)ScreenspaceEffectType.ScreenScroll].CurrentValue;
		if (screenScrollDirection == -1)
			ScreenScrollRate = -0.7;
		else if (screenScrollDirection == 1)
			ScreenScrollRate = 1;

		if (ScreenScrollRate != 0 && !Paused) {
			ScreenScrollProgress += ScreenScrollRate * globals.CurTimeDelta * frameState.WindowHeight * 8;

			double windowH = frameState.WindowHeight;

			if (ScreenScrollRate > 0 && ScreenScrollProgress >= windowH) {
				ScreenScrollProgress = 0;
				if (screenScrollDirection == 0) ScreenScrollRate = 0;
			}
			else if (ScreenScrollRate < 0 && ScreenScrollProgress <= -windowH) {
				ScreenScrollProgress = 0;
				if (screenScrollDirection == 0) ScreenScrollRate = 0;
			}
		}

		DoOneEffect(ScreenspaceEffectType.TVStatic, ref read, ref write);

		if (ScreenScrollRate != 0) {
			float offset = (float)(ScreenScrollProgress % frameState.WindowHeight);
			float gap = frameState.WindowHeight * 0.02f;
			read.Draw(new(0, 0, frameState.WindowWidth, -frameState.WindowHeight), new(0, offset + gap), Color.White);
			read.Draw(new(0, 0, frameState.WindowWidth, -frameState.WindowHeight), new(0, offset - frameState.WindowHeight - gap), Color.White);
		}
		else {
			read.Draw(new(0, 0, frameState.WindowWidth, -frameState.WindowHeight), new(0, 0), Color.White);
		}

		// this draws flashbangs
		double flashbangBrightness = flashbangIntensity.DetermineValueAtTime(Conductor.GetTime());
		if (flashbangBrightness > 0) {
			Rlgl.DrawRenderBatchActive();
			Graphics2D.SetDrawColor(255, 255, 255, (int)(float)(255 * flashbangBrightness)); // todo: flashbang color interp
			Graphics2D.DrawRectangle(0, 0, frameState.WindowWidth, frameState.WindowHeight);
			Rlgl.DrawRenderBatchActive();
		}
	}

	public void DoOneEffect(ScreenspaceEffectType effect, ref ComplexRenderTexture read, ref ComplexRenderTexture write) {
		var shaderFn = ScreenspaceEffectShaderFns[(int)effect];
		ref ScreenspaceEffectState state = ref ScreenspaceEffectStates[(int)effect];
		var shader = ScreenspaceEffectShaders[(int)effect];
		if (shader == null) return;

		if (shaderFn == null || shaderFn(shader, ref state) == false)
			return;

		Rlgl.DrawRenderBatchActive();
		Rlgl.EnableFramebuffer(write.Framebuffer);
		Rlgl.ClearScreenBuffers();

		shader.Activate();
		Raylib.DrawTextureRec(
			read.Texture,
			new Rectangle(0, 0, read.Width, -read.Height),
			System.Numerics.Vector2.Zero,
			Color.White
		);
		shader.Deactivate();

		Rlgl.DrawRenderBatchActive();
		Rlgl.EnableFramebuffer(0);
		Rlgl.Viewport(0, 0, (int)EngineCore.Window.Size.W, (int)EngineCore.Window.Size.H);

		(read, write) = (write, read);
	}

	private void PrepareShader(ScreenspaceEffectType type, string shaderName, ExecuteShaderFn shaderFn) {
		ScreenspaceEffectShaders[(int)type] = Shaders.LoadFragmentShaderFromFile("shaders", $"{shaderName}.fs");
		ScreenspaceEffectShaderFns[(int)type] = shaderFn;
	}

	private void PrepareShaders() {
		PrepareShader(ScreenspaceEffectType.ChromaticAberration, "chromatic_aberration", PrepareChromaticAberration);
		PrepareShader(ScreenspaceEffectType.Vignette, "vignette", PrepareVignette);
		PrepareShader(ScreenspaceEffectType.Mosaic, "mosaic", PrepareMosaic);
		PrepareShader(ScreenspaceEffectType.Sepia, "sepia", PrepareSepia);
		PrepareShader(ScreenspaceEffectType.FilmGrain, "filmgrain", PrepareFilmGrain);
		PrepareShader(ScreenspaceEffectType.TVStatic, "tvstatic", PrepareTVStatic);
		PrepareShader(ScreenspaceEffectType.Scanlines, "scanlines", PrepareScanlines);
	}

	private bool PrepareFilmGrain(IShader shader, ref ScreenspaceEffectState state) {
		double value = GetCurrentInterpolatedValue(ref state);
		if (value <= 0.0) return false;

		shader.SetUniform("uTime", (float)Conductor.GetTime());
		shader.SetUniform("uStrength", (float)value * 1f);

		return true;
	}
	private bool PrepareTVStatic(IShader shader, ref ScreenspaceEffectState state) {
		double value = GetCurrentInterpolatedValue(ref state);
		if (value <= 0.0) return false;

		shader.SetUniform("uTime", (float)Conductor.GetTime());
		shader.SetUniform("uStrength", (float)value * 1f);

		return true;
	}
	private bool PrepareSepia(IShader shader, ref ScreenspaceEffectState state) {
		double value = GetCurrentInterpolatedValue(ref state);
		if (value <= 0.0) return false;

		shader.SetUniform("uStrength", (float)value * 1f);

		return true;
	}
	private bool PrepareMosaic(IShader shader, ref ScreenspaceEffectState state) {
		double value = GetCurrentInterpolatedValue(ref state);
		if (value <= 0.0) return false;

		shader.SetUniform("uStrength", (float)value * 0.05f);

		return true;
	}
	private bool PrepareScanlines(IShader shader, ref ScreenspaceEffectState state) {
		double value = GetCurrentInterpolatedValue(ref state);
		if (value <= 0.0) return false;

		shader.SetUniform("uTime", (float)Conductor.GetTime());
		shader.SetUniform("uStrength", 0.3f);

		return true;
	}

	private bool PrepareVignette(IShader shader, ref ScreenspaceEffectState state) {
		double value = GetCurrentInterpolatedValue(ref state);
		if (value <= 0.0) return false;

		shader.SetUniform("uStrength", (float)NMath.Remap(value, 0, 1, 1, 0.7f));
		shader.SetUniform("uSoftness", 0.7f);

		return true;
	}

	private bool PrepareChromaticAberration(IShader shader, ref ScreenspaceEffectState state) {
		double value = GetCurrentInterpolatedValue(ref state);
		if (value <= 0.0) return false;

		shader.SetUniform("uStrength", (float)value * 3);

		return true;
	}

	public IMuseDash1SceneUI? GetSceneUI() => SceneUI;

	public bool NeedsToHoldSustains() => !Quirks.AutoHoldsSustains;
	public bool BreaksAvoids() => Quirks.BreaksAvoids;

	/// <summary>
	/// Current combo of the player (how many successful hits/avoids in a row)
	/// </summary>
	public int Combo { get; private set; } = 0;
	private double __lastCombo = -2000; // Last time a combo occured in game-time

	public double LastCombo => __lastCombo;
}