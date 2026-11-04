using CloneDash.Characters;
using CloneDash.Common;
using CloneDash.Common.Game;
using CloneDash.Common.Gamemodes;
using CloneDash.Common.Gamemodes.MuseDash;
using CloneDash.Common.Gamemodes.MuseDash.V1;
using CloneDash.Common.Gamemodes.MuseDash.V1.Data;
using CloneDash.Common.Scenes;
using CloneDash.Common.Songs;
using CloneDash.Common.Systems;
using CloneDash.Common.Systems.Discord;
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
using Nucleus;
using Nucleus.Commands;
using Nucleus.Common.Audio;
using Nucleus.Common.Commands;
using Nucleus.Common.Graphics;
using Nucleus.Common.Input;
using Nucleus.Core;
using Nucleus.Engine;
using Nucleus.ManagedMemory;
using Nucleus.Models;
using Nucleus.Models.Runtime;
using Nucleus.Types;
using Nucleus.UI;
using Nucleus.UI.Elements;
using Nucleus.Util;
using Raylib_cs;
using System.Diagnostics.CodeAnalysis;

using Color = Nucleus.Common.Types.Color;
using Image = Nucleus.UI.Elements.Image;

namespace CloneDash.Game;

public struct DashGameParams
{
	public bool Autoplay;
	public ISongChart? Chart;
	public int Measure;

	public DashGameParams(ISongChart sheet) {
		Chart = sheet;
	}
}

[Nucleus.MarkForStaticConstruction]
public partial class MuseDash1Gamemode : IGamemodeDescriptor
{
	public const string UUID = "gamemode/musedash1/standard";

	public ReadOnlySpan<char> GetUUID() => UUID;

	public IGame Load(ISongChart chart, in GameLoadGenericParameters parms) {
		var game = new MuseDash1Game(new(chart) {
			Autoplay = parms.Autoplay,
			Measure = parms.StartMeasure ?? 0
		});
		EngineCore.LoadLevel(game);
		return game;
	}
}

[MarkForStaticConstruction]
public partial class MuseDash1Game(DashGameParams gameParameters) : Level, IGame
{
	public const float PLAYER_OFFSET_HIT_Y = -0.267f;
	public const float PLAYER_OFFSET_X = 0.25f;
	public const float PLAYER_OFFSET_Y = 0.775f;
	public const string STRING_COMBO = "COMBO";
	public const string STRING_FEVERN = "FEVER: {0}/{1}";
	public const string STRING_FEVERY = "FEVER! {0}s";
	public const string STRING_HP = "HP: {0}";
	public const string STRING_SCORE = "SCORE";
	public static readonly ConVar md1_debugevents = new ConVar(nameof(md1_debugevents), "0", 0, "Prints event debugging to console.");
	public static readonly ConVar musicspeed = new(nameof(musicspeed), "1", FCvar.None, "Sets the music speed for the game.", 0.1, 4, (cv, _, _) => {
		if (EngineCore.Level is MuseDash1Game game)
			game.InitSpeedFromCvar();
	});

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

	public static ConCommand md1level = new(nameof(md1level), clonedash_openmdlevel_execute, clonedash_openmdlevel_autocomplete, "Opens a Muse Dash 1 level.");
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

	public static ConVar profilegameload = new(nameof(profilegameload), "0", FCvar.None, "Profiles the game during loading, then triggers an engine interrupt afterwards to tell you how long each individual component took.");
	public readonly MuseDash1EnemyManager EnemyManager = new();
	public readonly MuseDash1EventManager EventManager = new();
	public HashSet<DashEvent> ActiveEvents = [];
	public ComboComponent Combo = null!;
	public ScreenEffectsComponent Effects = null!;

	public int EnemySortIndexCounter;
	public readonly List<DashEvent> Events = [];
	public int EventSortIndexCounter;
	public FeverComponent Fever = null!;
	public readonly GameEventManager GameEvents = new();
	public readonly int[] PathwaySpeeds = new int[PathwayLayout.MAX_PATHWAYS];
	public readonly HashSet<DashEvent> HandledEvents = [];
	public HealthComponent Health = null!;
	public readonly List<ICloneDashInputSystem> InputReceivers = [];
	public PollResult LastPollResult = PollResult.Empty;
	public DashEnemy? MashingEntity;
	public readonly MusicComponent Music = new();
	public PauseComponent Pause = null!;

	public ResultComponent Results = null!;
	public readonly ScoreComponent Score = new();
	public StatisticsData Stats = null!;
	public ISustainManager Sustains = new StackBasedSustainManager();

	const double TIME_BETWEEN_MASH_HITS = (1d / Masher.MASHER_PLAYER_MAX_HITS_PER_SECOND);
	// This helps with doubles/sustains played in the same frame...
	// probably a better way to handle it, but this works
	readonly bool[] PlayedSceneSoundThisFrame = new bool[(int)SceneSound.Count];
	readonly List<SceneChange> sceneChanges = [];
	readonly Dictionary<UtlSymId_t, IMuseDash1SceneInstance> sceneLUT = [];
	readonly List<IMuseDash1SceneInstance> scenes = [];

	IMuseDash1SceneInstance? ActiveScene;
	int AttackP = 0;
	bool CanSceneChange = false;
	bool DeferringAsync = false;
	int FailP = 0;
	IMuseDash1FeverRuntime? FeverFX;
	IMuseDash1SceneInstance? FirstScene;
	// Player input system
	InputState InputState;
	PathwayId LastAttackPathway;
	double LastAttackTime;
	double LastHologramJumpTIme = -2000000000000d;
	double LastJumpTime = -2000000000000d;
	double LastMasherAttemptedHit;
	double LastMasherRealHit;
	readonly SecondOrderSystem MashZoomSOS = new(1.1f, 0.9f, 2f, 0);
	MuseDash1GameplayQuirks Quirks = new();
	readonly List<DashEnemy> ReadyToBuildEntities = [];
	readonly List<DashEvent> ReadyToBuildEvents = [];
	IMuseDash1SceneUI? SceneUI;
	SecondOrderSystem? YOffsetSOS;

	public delegate void AttackEvent(MuseDash1Game game, PathwayId pathway);

	public event AttackEvent? OnAirAttack;
	public event AttackEvent? OnGroundAttack;

	public static float GlobalScale => 1 / 200f;

	public PathwaySet AirSidePathways {
		get {
			var set = PathwaySet.None;
			for (int i = 0; i < Layout.Count; i++)
				if (Layout.IsAirSide(Layout.PathwayAt(i)))
					set = set.With(Layout.PathwayAt(i));
			return set;
		}
	}

	public AutoPlayer AutoPlayer { get; private set; } = null!;

	// public ModelEntity Player { get; set; }
	// public ModelEntity HologramPlayer { get; set; }
	// public MD1_SpineActionController PlayerController { get; set; }
	// public MD1_SpineActionController HologramPlayerController { get; set; }
	public Boss Boss { get; set; } = null!;

	/// <summary>
	/// Can the player jump right now?
	/// </summary>
	public bool CanJump => !InAir;

	public IMuseDash1CharacterInstance Character { get; set; } = null!;

	/// <summary>
	/// Gets the current pathway the player is on. Returns Top if jumping, else bottom.
	/// </summary>
	public PathwaySet CharacterPathways {
		get {
			var state = Sustains.GetSustainState();
			if (state.IsEmpty) return (InAir ? JumpPathway : GroundPathway).AsSet;
			return state;
		}
	}

	/// <summary>
	/// Timing system.
	/// </summary>
	public Conductor Conductor { get; private set; } = null!;

	public bool Debug { get; set; } = true;

	public PathwayId GroundPathway => Layout.PathwayAt(Layout.Count - 1);

	public PathwaySet GroundSidePathways => Layout.All & PathwaySet.FromBits(~AirSidePathways.Bits);

	public double Hologram_AirTime => (Conductor.Time - LastHologramJumpTIme);

	public double Hologram_TimeToAnimationEnds => Character.GetSecondary().GetAnimationDuration() - (Conductor.Time - LastHologramJumpTIme);

	/// <summary>
	/// Is the player in the air right now?
	/// </summary>
	public bool InAir => (Conductor.Time - LastJumpTime) < Character.GetJumpDuration();

	public bool InHit { get; private set; } = false;

	[MemberNotNullWhen(true, nameof(MashingEntity))] public bool InMashState { get; private set; }

	public override bool IsInGame => true;

	/// <summary>
	/// Is the player currently dragging the time-seek bar?
	/// </summary>
	public bool IsSeeking { get; private set; } = false;

	public PathwayId JumpPathway => Layout.PathwayAt(0);

	public PathwayLayout Layout { get; private set; } = PathwayLayout.MuseDash;

	public Pathway[] Pathways { get; private set; } = [];

	public float PlayerScale => 1 / 200f;

	public float PlayScale => 1 / 200f;

	public bool SuppressHitMessages { get; set; }

	/// <summary>
	/// Which entity is being held on the top pathway
	/// </summary>
	//public CD_BaseMEntity? HoldingTopPathwaySustain { get; private set; } = null;
	/// <summary>
	/// Which entity is being held on the bottom pathway
	/// </summary>
	//public CD_BaseMEntity? HoldingBottomPathwaySustain { get; private set; } = null;
	public double TimeToAnimationEnds => Character.GetPrimary().GetAnimationDuration() - (Conductor.Time - LastJumpTime);


	// todo: confirm this is the right behavior
	static bool PathwayTransitionAnimation => true;

	static float TEMP_PLAYER_OFFSET => 0;

	public void ActivateEvent(DashEvent ev) {
		ActiveEvents.Add(ev);
		ev.Activate();
		if (md1_debugevents.GetBool() && !IsSeeking && ev.ShouldDebug()) {
			if (ev.Length == 0)
				Logs.Debug($"Triggering {ev.ToString()}");
			else
				Logs.Debug($"Activating {ev.ToString()}");
		}
	}

	/// <summary>
	/// Adds to the players fever value, and automatically enters fever when the player has maxed out the fever bar.
	/// </summary>
	/// <param name="fever"></param>
	public void AddFever(float fever, DashEnemy? responsible = null) {
		if (Fever.InFever) return;

		double feverD = (double)fever;
		if (Quirks.FeverModifier != null) Quirks.FeverModifier.Invoke(ProduceSnapshot(responsible), ref feverD);
		fever = (float)feverD;

		Fever.Add(fever);
	}

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

	public void AddScene(IMuseDash1SceneInstance instance) {
		scenes.Add(instance);
		sceneLUT[instance.GetScene().GetUUID().Hash()] = instance;
		instance.SetSceneArrayIndex(scenes.Count - 1);
	}

	/// <summary>
	/// Adds to the players score.
	/// </summary>
	/// <param name="score"></param>
	public void AddScore(int score, DashEnemy? responsible = null) {
		double s = (double)score;
		if (responsible != null && responsible.Type != 0) s *= Quirks.ScoreMultipliers[responsible.Type.GetBit()];
		if (Quirks.ScoreModifier != null) Quirks.ScoreModifier.Invoke(ProduceSnapshot(responsible), ref s);
		score = (int)(float)s;

		Score.Add((int)(float)s);
		SceneUI?.UpdateScore(Score.Value);
	}

	public bool AttackAir(PathwayId pathway, in PollResult result) {
		if (InMashState) {
			PlayCharacterAnimation(CharacterAnimationType.JumpHit);

			OnAirAttack?.Invoke(this, pathway);
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

			OnAirAttack?.Invoke(this, pathway);

			if (Sustains.IsSustaining())
				LastHologramJumpTIme = Conductor.Time;
			else
				LastJumpTime = Conductor.Time;

			return true;
		}
		else if (CanJump) {
			if (!Sustains.IsSustaining())
				PlayCharacterAnimation(CharacterAnimationType.Jump);
			OnAirAttack?.Invoke(this, pathway);

			if (Sustains.IsSustaining())
				LastHologramJumpTIme = Conductor.Time;
			else
				LastJumpTime = Conductor.Time;

			return true;
		}

		return false;
	}

	public void AttackGround(PathwayId pathway, PollResult result) {
		if (InMashState) {
			PlayCharacterAnimation(CharacterAnimationType.AttackPerfect);
			OnGroundAttack?.Invoke(this, pathway);
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

		LastJumpTime = -2000000000000d;
		LastHologramJumpTIme = -2000000000000d;
		OnGroundAttack?.Invoke(this, pathway);
	}

	public bool BreaksAvoids() => Quirks.BreaksAvoids;

	public void BroadcastEntitySignal(DashEnemy? entityFrom, EntitySignalType signalType, object? data = null) {
		foreach (var enemy in EnemyManager.GetAll())
			enemy.OnSignalReceived(entityFrom, signalType, data);
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

	public bool CanHit(PathwayId pathway) {
		if (Sustains.IsSustaining(pathway))
			return false;

		if (Layout.IsAirSide(pathway) && InAir)
			return false;

		return true;
	}

	public void ConditionallyRenderVisibleEntities(FrameState frameState, Predicate<DashEnemy> enemyPredicate, Span<DashEnemy> visibleEnemies) {
		DeadEntityVisibility deadVis = GameSettings.DeadEntityVisibility;
		foreach (DashEnemy ent in visibleEnemies) {
			if (!enemyPredicate(ent)) continue;
			RenderVisibleEntity(frameState, ent, deadVis);
		}
	}

	public void ConditionallyRenderVisibleEntities(FrameState frameState, PathwaySet exactPathways, Span<DashEnemy> visibleEnemies) {
		DeadEntityVisibility deadVis = GameSettings.DeadEntityVisibility;
		foreach (DashEnemy ent in visibleEnemies) {
			if (ent.Type == MuseDash1EntityType.SustainBeam || ent.Pathway != exactPathways) continue;
			RenderVisibleEntity(frameState, ent, deadVis);
		}
	}

	/// <summary>
	/// Damage the player.
	/// </summary>
	/// <param name="entity"></param>
	/// <param name="damage"></param>
	public void Damage(float damage, DashEnemy? responsible) {
		if (Fever.InFever && Quirks.InvincibleInFever)
			return;

		double damageD = (double)damage;
		if (Quirks.DamageModifier != null) Quirks.DamageModifier.Invoke(ProduceSnapshot(responsible), ref damageD);
		damage = (float)damageD;
		damage = Math.Max(0, damage);

		if (Quirks.ConsumeFeverBeforeHealth && !Health.Invincible)
			damage -= Fever.Consume(damage);

		if (Health.Damage(damage)) {
			if (InAir)
				PlayCharacterAnimation(CharacterAnimationType.JumpHurt);
			else
				PlayCharacterAnimation(CharacterAnimationType.Hurt);
		}

		Combo.Break();
		SceneUI?.UpdateFullCombo(false);
	}

	public void DeactivateEvent(DashEvent ev) {
		HandledEvents.Add(ev);
		ActiveEvents.Remove(ev);
		ev.Deactivate();
		if (md1_debugevents.GetBool() && !IsSeeking && ev.ShouldDebug()) {
			if (ev.Length != 0)
				Logs.Debug($"Deactivating {ev.GetType().Name}");
		}
	}

	public void EnterHitState() {
		InHit = true;
		SuppressHitMessages = false;
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

	public void ExitHitState() {
		InHit = false;
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

	public IMuseDash1SceneInstance? GetActiveScene() => ActiveScene;

	public int GetActiveSceneIdx() => ActiveScene?.GetSceneArrayIndex() ?? -1;

	public IReadOnlyList<IMuseDash1SceneInstance> GetAllScenes() => scenes;

	public double GetBgScrollSpeedMultiplier() => 1 - Effects.Get<BgFreezeScreenEffect>().Value;

	public Vector2F GetCenterPosition() => HasActiveScene(out var scene) ? scene.GetCenterPosition() : default;

	public ISongChart? GetChart() => gameParameters.Chart;

	public Vector2F GetEntityPosition(PathwaySet pathways) => Layout.Clamp(pathways).Count == 1 ? GetPathwayPosition(pathways.First) : GetCenterPosition();
	IConductor IGame.GetConductor() => Conductor;

	public virtual IGamemodeDescriptor GetGamemode() => GamemodeMod.GetGamemode(MuseDash1Gamemode.UUID)!;

	object? IGame.GetGamemodeData() => gameParameters.Chart?.GetGamemodeData();

	public ref readonly InputState GetInputState() => ref InputState;

	public int GetNumScenes() => scenes.Count;

	public Pathway GetPathway(PathwayId pathway) => Pathways[pathway.Index];

	public Pathway GetPathway(DashEnemy ent) => GetPathway(PrimaryPathway(ent.Pathway));

	// Its own function in case we have player-specific overrides (not sure if this exists yet)
	public Vector2F GetPathwayPosition(PathwayId pathway) => HasActiveScene(out var scene) ? scene.GetPathwayPosition(pathway) : default;


	public float GetPlayerY(bool secondary) {
		var height = EngineCore.GetWindowHeight();

		var bot = GetPathwayPosition(GroundPathway);
		var character = secondary ? Character.GetSecondary() : Character.GetPrimary();
		if (!character.IsInAir())
			return bot.Y + -1f;

		var top = GetPathwayPosition(JumpPathway);
		float ratio = (float)NMath.Remap(character.GetTimeToAnimationEnd(), character.GetAnimationDuration(), 0, 0, 1, clampOutput: true);
		return (float)NMath.Remap(NMath.Ease.InCirc(NMath.Ease.InExpo(ratio)), 0, 1, top.Y, bot.Y) + -1f; // TODO: re-evaluate
	}

	public IMuseDash1SceneInstance GetSceneAtTime(double time) {
		for (int i = sceneChanges.Count - 1; i >= 0; i--) {
			var sceneChange = sceneChanges[i];
			if (time >= sceneChange.Time)
				return scenes[sceneChange.ArrayIdx];
		}
		return scenes[0];
	}

	public IMuseDash1SceneUI? GetSceneUI() => SceneUI;

	ISong? IGame.GetSong() => gameParameters.Chart?.GetSong();

	ISongChart? IGame.GetSongChart() => gameParameters.Chart;

	public bool HasActiveScene([NotNullWhen(true)] out IMuseDash1SceneInstance? scene) => (scene = ActiveScene) != null;

	public bool HasSceneInitialized(ISceneDescriptor descriptor) {
		return sceneLUT.ContainsKey(descriptor.GetUUID().Hash());
	}

	ITexture? backgroundOverride;
	float backgroundOverrideOpacity;

	public override void Initialize(params object[] _) {
		ResetPathwaySpeeds();
		Effects = new(this, this);

		Stats = new(gameParameters.Chart);
		using (StaticSequentialProfiler.StartStackFrame("CD_GameLevel.RichPresenceUpdate")) {
			RichPresenceSystem.SetPresence(new() {
				Details = "In Game",
				State = $"Muse Dash 1 - '{(gameParameters.Chart == null ? " <null>" : gameParameters.Chart.GetSong().FetchMetadata().Name)}'"
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

			backgroundOverride = gamemodeData.BackgroundTextureOverride;
			Layout = gamemodeData.Layout;
			backgroundOverrideOpacity = gamemodeData.BackgroundTextureOpacity;

			using (StaticSequentialProfiler.StartStackFrame("Get Descriptors")) {
				var charData = CharacterMod.Instance.GetActive();
				if (charData == null) throw new ArgumentNullException(nameof(charData));
				Quirks = MuseDash1GameplayQuirks.Default;
				charData.ApplyQuirks(ref Quirks);
				Character = charData.CreateInGame<IMuseDash1CharacterInstance>(this)!;
				if (Character == null)
					throw new Exception("The character isn't supported for Muse Dash 1");

				var sceneData = SceneMod.Instance.GetActive();
				IMuseDash1SceneInstance? sceneToActivate = null;
				if (sceneData != null) {
					// Ignore scene changes. Only use this scene.
					var scene = AddOrGetScene(sceneData);
					if (scene != null)
						sceneToActivate = scene;
					CanSceneChange = false;
				}
				else {
					sceneData = SceneMod.Instance.GetDescriptorByUID(gamemodeData.InitialScene);
					if (sceneData == null)
						throw new ArgumentNullException(nameof(sceneData));

					var scene = AddOrGetScene(sceneData);
					if (scene != null)
						sceneToActivate = scene;

					// Process scene changes
					CanSceneChange = true;
					foreach (var sceneChange in gamemodeData.SceneChanges) {
						ISceneDescriptor? sceneDescToChangeTo = SceneMod.Instance.GetDescriptorByUID(sceneChange.SceneUID);

						var sceneChangeInstance = AddOrGetScene(sceneDescToChangeTo);
						if (sceneChangeInstance != null) {
							var ev = new SceneChange(this, sceneChangeInstance.GetSceneArrayIndex());
							Events.Add(ev);
							ReadyToBuildEvents.Add(ev);
							EventManager.Add(ev);
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
				Pathways = new Pathway[Layout.Count];
				for (int i = 0; i < Layout.Count; i++)
					Pathways[i] = Add<Pathway>(Layout.PathwayAt(i));
				Interlude.Spin();

				Conductor = Add<Conductor>();
				Combo = new(Conductor);
				Combo.MissForgiveness = Quirks.MissComboForgiveness;
				Health = new(Conductor, GameEvents);
				Health.Max = Quirks.MaxHP;
				Health.IFrameLength = Quirks.IFrameLength;
				Health.BonusGranted = Quirks.HealthBonusGranted;
				Health.BonusThreshold = Quirks.HealthBonusThreshold;
				Health.BonusTimes = Quirks.HealthBonusTimes;
				Health.DyingSeconds = Quirks.DyingSeconds;
				GameEvents.Listen<PlayerDeathGameEvent>(TriggerDeath);
				Health.Reset();
				Fever = new(Conductor, GameEvents);
				Fever.Max = Quirks.MaxFever;
				Fever.Duration = Quirks.FeverDuration;
				Fever.DismissalTime = Quirks.PostFeverDismissalTime;
				GameEvents.Listen<FeverChangedGameEvent>(UpdateFeverProgress);
				GameEvents.Listen<FeverStartGameEvent>(EnterFever);
				GameEvents.Listen<FeverEndGameEvent>(ExitFever);
				Pause = new(this, this, Music, GameEvents);
				GameEvents.Listen<PauseRequestGameEvent>(GuardPause);
				GameEvents.Listen<UnpauseStartGameEvent>(PlayUnpauseSound);
				Results = new(this, Music, GameEvents);
				GameEvents.Listen<SongClearedGameEvent>(ShowVictory);
				GameEvents.Listen<SongFailedGameEvent>(ShowFailure);
				Interlude.Spin();
			}

			using (StaticSequentialProfiler.StartStackFrame("Load Enemies")) {
				Boss.PreBuildVisuals(this);
				foreach (var scene in GetAllScenes())
					Boss.BuildForScene(scene);

				if (chart != null) {
					if (!DeferringAsync) {
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
					Music.Load(gameParameters.Chart.GetAudioTrack());
					InitSpeedFromCvar();
					if (gameParameters.Measure != 0)
						SeekTo(Conductor.MeasureToSeconds(gameParameters.Measure));
				}
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
			PanicSystem.Interrupt(() => {
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
		MuseDash1Compatibility.StreamingAssets?.UnloadAll();
		MainThread.RunASAP(Interlude.End, ThreadExecutionTime.AfterFrame);
	}

	public void InitSpeedFromCvar() {
		Music.SetSpeed(musicspeed.GetDouble());
	}

	public bool IsCharacterOn(PathwaySet pathways) => CharacterPathways == Layout.Clamp(pathways);

	public bool IsDead() => Health.Dead && !Quirks.PlayableAfterDeath;

	public void IterateEvents() {
		foreach (var ev in EventManager.GetLastVisible()) {
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
		ent.Dt = ChartEntity.Dt;
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
		ReadyToBuildEntities.Add(ent);
		EnemyManager.Add(ent);

		return ent;
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
		ReadyToBuildEvents.Add(ev);
		EventManager.Add(ev);
		// This is a hack... whatever
		if (ev is FlashbangEffect flash)
			Effects.Get<FlashbangScreenEffect>().AddFlashbang(flash);
	}

	public bool NeedsToHoldSustains() => !Quirks.AutoHoldsSustains;

	/// <summary>
	/// This is a callback for <see cref="ISustainManager"/> implementations. It expects wasSustaining, inSustaining, and sustainCount to be relative to the current pathway.
	/// </summary>
	/// <param name="sustain"></param>
	/// <param name="pathway"></param>
	/// <param name="wasSustainingBefore"></param>
	/// <param name="isSustainingNow"></param>
	/// <param name="sustainCount"></param>
	public void OnSustainCallback(SustainBeam sustain, PathwayId pathway, bool wasSustainingBefore, bool isSustainingNow, int sustainCount) {
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
			if (Layout.IsAirSide(pathway))
				PlayCharacterAnimation(CharacterAnimationType.UpPressEnd);
			else
				PlayCharacterAnimation(CharacterAnimationType.Run);
		}

		if (HasActiveScene(out var scene))
			scene.OnPressStateChange(nowInsustain, wasSustainingBefore);
	}

	public override void OnUnload() {
		GameEvents.Clear();
		SceneUI?.Dispose();
		SceneUI = null;

		if (backgroundOverride != null) {
			backgroundOverride.Dispose();
		}

		MuseDash1Compatibility.StreamingAssets?.UnloadAll();
	}

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

	public void PlaySceneSound(SceneSound sound, int hits = 0) {
		if (IsSeeking)
			return;
		if (PlayedSceneSoundThisFrame[(int)sound])
			return;
		PlayedSceneSoundThisFrame[(int)sound] = true;
		if (HasActiveScene(out var scene))
			scene.PlaySound(sound, hits);
	}

	/// <summary>
	/// Polling function which figures out the closest, potentially-hit entity and returns the result.
	/// </summary>
	/// <param name="pathway"></param>
	/// <returns>A <see cref="PollResult"/>, if it hit something, Hit is true, and vice versa.</returns>
	public PollResult Poll(in PollParams parms) {
		var visibleEnemies = EnemyManager.GetLastVisible();
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
					if (isValidHit && entity.Pathway.Contains(parms.Pathway)) {
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

	public override void PostRender(FrameState frameState) {
		base.PostRender(frameState);

		SceneUI?.RenderUI();

		Effects.End();
		Effects.Draw(frameState);
	}

	public override void PostRenderEntities(FrameState frameState) {
		base.PostRenderEntities(frameState);

		SceneUI?.PostRenderWorldspace();
		Rlgl.DrawRenderBatchActive();
	}

	public override void PreRender(FrameState frameState) {
		float width = EngineCore.GetWindowWidth(), height = EngineCore.GetWindowHeight();
		Effects.Begin(width, height);
		EngineCore.Window.ClearBackground(Color.Blank);
		base.PreRender(frameState);
		//Stopwatch test = Stopwatch.StartNew();
		if (Effects.Get<BackgroundVisibilityScreenEffect>().Visible) {
			if (backgroundOverride != null) {
				backgroundOverride.Download(); // call regenerator?
				Graphics2D.SetTexture(backgroundOverride);
				Graphics2D.SetDrawColor(255, 255, 255, (int)(255 * backgroundOverrideOpacity));
				float texWidth = width * GlobalScale * 1.5f;
				float texHeight = height * GlobalScale * 1.5f;
				Graphics2D.DrawTexturedRectangle(texWidth / -2, texHeight / -2, texWidth, texHeight);
			}
			else if (HasActiveScene(out var scene))
				scene.RenderBackground();
		}
		FeverFX?.Render();
		//Logs.Info(test.Elapsed.TotalMilliseconds);
	}

	public override void PreRenderBackground(FrameState frameState) {
		Boss.Position = new(0, 2.25f);
	}

	public PathwayId PrimaryPathway(PathwaySet pathways) {
		var clamped = Layout.Clamp(pathways);
		return clamped.Count == 1 ? clamped.First : GroundPathway;
	}

	public override void Render(FrameState frameState) {
		Rlgl.DrawRenderBatchActive();

		Rlgl.DisableDepthTest();
		Rlgl.DisableDepthMask();
		Rlgl.DisableBackfaceCulling();

		//Raylib.DrawLineV(new(-100000, 0), new(100000, 0), Color.Red);
		//Raylib.DrawLineV(new(0, -100000), new(0, 100000), Color.Green);

		SceneUI?.PreRenderWorldspace();

		// Pathways
		foreach (var pathway in Pathways)
			pathway.Render();

		if (Effects.Get<NoteVisibilityScreenEffect>().Visible) {
			Span<DashEnemy> visibleEnemies = EnemyManager.GetLastVisible();

			// Hold notes
			ConditionallyRenderVisibleEntities(frameState, static x => x.Type == MuseDash1EntityType.SustainBeam, visibleEnemies);

			// Boss
			Boss.Render();

			// The other entities, that aren't sustain beams, in order of top -> bottom pathway
			for (int i = 0; i < Layout.Count; i++)
				ConditionallyRenderVisibleEntities(frameState, Layout.PathwayAt(i).AsSet, visibleEnemies);
		}

		AddDebugString("Visible Entities", EnemyManager.GetLastVisible().Length);
		AddDebugString("Active Effects", Effects.GetActiveEffects());

		Rlgl.DrawRenderBatchActive();
	}

	/// <summary>
	/// Resets the entire game state and variables.
	/// </summary>
	public virtual void Reset() {
		ExitMashState();
		Effects.Reset();
		FeverFX?.Reset();

		if (Sustains.IsSustaining() && HasActiveScene(out var scene))
			scene.OnPressStateChange(false, true);

		SceneUI?.UpdateAllPerfect(true);
		SceneUI?.UpdateFullCombo(true);
		SceneUI?.Reset();
		SceneUI?.SetSeeking(true);
		SceneUI?.UpdateScore(0);
		SceneUI?.UpdateCombo(0);
		SceneUI?.UpdateHP(Health.Max, Health.Max);
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

		Combo.Reset();
		Health.Reset();
		Fever.Reset();
		Results.Reset();
		Score.Reset();
		Sustains.Reset();
		AutoPlayer.Reset();
		LastJumpTime = -2000000000000d;
		LastHologramJumpTIme = -2000000000000d;
		ActiveEvents.Clear();
		HandledEvents.Clear();
	}

	public void ResetPathwaySpeeds() {
		Array.Fill(PathwaySpeeds, 1);
	}

	public float? ResolveCharacterY(PathwaySet held) {
		if (held.Count >= 2 || InMashState)
			return GetCenterPosition().Y;
		if (held.Count == 1)
			return GetPathwayPosition(held.First).Y;
		return null;
	}

	public void Restart() => SeekTo(0);

	public void SeekTo(double time) {
		time = Math.Clamp(time, 0, Music.Duration);
		IsSeeking = true;

		Reset();

		Music.SeekTo(time);

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
					if (Fever.ShouldExit && Fever.InFever)
						Fever.Exit();

					switch (mEnt) {
						case Masher masher: // Mashers need a bit more trickery
							EnterMashState(masher);
							for (int i = 0; i < masher.MaxHits; i++)
								masher.Hit(PrimaryPathway(mEnt.Pathway), 0);
							ExitMashState();
							masher.RewardPlayer();
							AutoPlayer.MarkEntityAsPassed(mEnt);
							break;
						case SustainBeam sustain:
							sustain.Hit(PrimaryPathway(sustain.Pathway), 0);
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
									mEnt.Hit(PrimaryPathway(mEnt.Pathway), 0);
									mEnt.RewardPlayer();
									break;
								case EntityInteractivity.SamePath:
									mEnt.Hit(PrimaryPathway(mEnt.Pathway), 0);
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
			if (Fever.InFever) FeverFX?.Activate();

			// ALSO A HACK - but it solves some animation issues when mid-sustain.
			if (Sustains.IsSustaining())
				PlayCharacterAnimation(CharacterAnimationType.Press);
		}

		IsSeeking = false;
		SceneUI?.SetSeeking(false);
		Conductor.InvalidateTime();
	}

	public void SendEntitySignal(DashEnemy? entityFrom, DashEnemy? entityTo, EntitySignalType signalType, object? data = null) {
		entityTo?.OnSignalReceived(entityFrom, signalType, data);
	}

	public void SetPathwaySpeed(PathwaySet pathways, int speed) {
		System.Diagnostics.Debug.Assert(speed >= 1);
		System.Diagnostics.Debug.Assert(speed <= 3);

		for (int i = 0; i < Layout.Count; i++)
			if (pathways.Contains(Layout.PathwayAt(i)))
				PathwaySpeeds[i] = speed;
	}

	public bool SetScene(IMuseDash1SceneInstance? scene) {
		ActiveScene?.Deactivate(scene);
		var oldScene = ActiveScene;
		ActiveScene = scene;
		scene?.Activate(oldScene);
		BroadcastEntitySignal(null, EntitySignalType.SceneChange, (oldScene, scene));
		return true;
	}

	public bool SetScene(int sceneIdx) {
		return SetScene(scenes[sceneIdx]);
	}

	public void SetScene(ReadOnlySpan<char> scene) {
		if (sceneLUT.TryGetValue(scene.SliceNullTerminatedString().Hash(), out var instance))
			SetScene(instance);
	}

	public bool ShouldFreezeNoteAnimations() => Effects.Get<NoteFreezeScreenEffect>().Value >= 1;

	public override void Think(FrameState frameState) {
		ResetSceneSoundsPlayedThisFrame();

		if (gameParameters.Chart != null && SceneUI != null && !SceneUI.ShowingVictoryScreen() && Results.TryClear()) {
			return;
		}

		if (Results.TryFail(IsDead())) {
			return;
		}

		if (Fever.ShouldExit && Fever.InFever)
			Fever.Exit();

		// Health drain
		if (Quirks.HealthLossPerSecond != null && Conductor.GetTime() >= 0) {
			double loss = 0;
			Quirks.HealthLossPerSecond(ProduceSnapshot(), ref loss);
			Health.Drain(loss * Conductor.TimeDelta);
		}

		if (Quirks.FeverGainPerSecond != null && Conductor.GetTime() >= 0) {
			double gain = 0;
			Quirks.FeverGainPerSecond(ProduceSnapshot(), ref gain);
			Fever.Add((float)(gain * Conductor.TimeDelta));
		}

		EnemyManager.Rebuild(Conductor.Time);
		EventManager.Rebuild(Conductor.Time);

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
					MashingEntity.Hit(GroundPathway, 0);
			}

			if (InputState.PauseButton) {
				Pause.TogglePauseMenu();
				return;
			}
		}

		float? yoff = null;

		yoff = ResolveCharacterY(Sustains.GetSustainState());

		if (yoff.HasValue) {
			if (YOffsetSOS == null)
				YOffsetSOS = new(15, 1, 1, yoff.Value);

			yoff = yoff.Value + -1f;
		}
		else
			YOffsetSOS = null;

		var playerY = yoff ?? GetPlayerY(false);

		float conductorInTime = Conductor.PreStartTime <= 0 ? 1 : (float)NMath.Remap(Conductor.Time, -Conductor.PreStartTime, -Conductor.PreStartTime / 1.5f, 0, 1, clampInput: true);
		conductorInTime = 1 - NMath.Ease.OutQuad(conductorInTime);

		// This sucks... todo, figure out how to get this proper
		var leftPlayer = GetCenterPosition().X;

		Character.GetPrimary().SetPos(new Vector2F(
			(leftPlayer - 1) - (conductorInTime * 1),
			-(YOffsetSOS?.Update(playerY) ?? playerY)
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
			sceneUI.UpdateCombo(Combo.Value);
			if (Fever.InFever)
				sceneUI.UpdateInFever(Fever.TimeLeft, Fever.Duration);

			if (MashingEntity != null)
				sceneUI.UpdateMultiHitText(MashingEntity.Hits);

			sceneUI.UpdateHP(Health.Value, Health.Max);
		}

		var visibleEnemies = EnemyManager.GetLastVisible();
		var lastEntity = EnemyManager.GetLast();

		if (lastEntity != null && lastEntity.GetJudgementHitTime() + lastEntity.Length < Conductor.Time && !Results.LastNoteHit) {
			Results.MarkLastNoteHit();
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
		for (int pathway = Layout.Count - 1; pathway >= 0; pathway--)
			HitLogic(Layout.PathwayAt(pathway));

		// This loop is mostly for per-tick polls that need to occur, ie. when entities have been fully missed.
		// It is ran after input processing.
		foreach (var entity in visibleEnemies) {
			var timeToHit = entity.GetJudgementTimeUntilHit();
			switch (entity.Interactivity) {
				case EntityInteractivity.Hit:
				case EntityInteractivity.Sustain:
					if (!entity.Dead) {
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
						PathwaySet characterPathways = CharacterPathways;
						if ((characterPathways.Count >= 2 || IsCharacterOn(entity.Pathway)) && entity.Hits == 0) {
							entity.Hit(characterPathways.Count >= 2 ? PrimaryPathway(entity.Pathway) : characterPathways.First, 0);
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
					if (Sustains.IsSustaining(Layout.Clamp(entity.Pathway)))
						break;

					// Checks if the player has completely failed to avoid the entity, and if so, damages the player.

					bool broken = false;
					if (IsCharacterOn(entity.Pathway) && timeToHit < -entity.PrePerfectRange && !entity.DidRewardPlayer) {
						//entity.Hit(Game.PlayerController.Pathway);
						if (BreaksAvoids()) {
							if (entity.Hits == 0)
								entity.Hit(PrimaryPathway(entity.Pathway), 0);
							broken = true;
						}
						else
							entity.DamagePlayer();
					}

					// If the player is now avoiding the entity, then reward the player for missing it, and make it so they cant be damaged by it)
					if (!broken && !IsCharacterOn(entity.Pathway) && timeToHit < 0 && !entity.DidDamagePlayer) {
						entity.Pass();
					}

					break;
			}
			//entity.WhenVisible();
		}

		AddDebugString("HoldingTopPathwaySustain", Sustains.GetSustainsActiveCount(JumpPathway));
		AddDebugString("HoldingBottomPathwaySustain", Sustains.GetSustainsActiveCount(GroundPathway));

		if (HasActiveScene(out var scene)) {
			scene.Think(GetBgScrollSpeedMultiplier());
		}
		SceneUI?.Think(globals.CurTimeDelta);
		FeverFX?.Think();
	}

	public void UpdateMashTextEffect() {
		if (!IValidatable.IsValid(MashingEntity)) return;
		SceneUI?.UpdateMultiHitText(MashingEntity.Hits);
	}

	internal void SetEnemyKilledPosition(DashEnemy ent) {
		var pos = GetEntityPosition(ent.Pathway);
		ent.Position = new(pos.X, -pos.Y);
	}

	internal void SetEnemyPosition(DashEnemy ent) {
		ent.Position = new(0, 2.25f);
	}

	/// <summary>
	/// Produces a quirks snapshot for use with the current gameplay quirks
	/// </summary>
	protected MuseDash1GameplayQuirks.GameSnapshot ProduceSnapshot(DashEnemy? enemy = null, MuseDash1GameplayQuirks.Judgement judgement = 0) {
		MuseDash1GameplayQuirks.GameSnapshot snapshot = new() {
			Difficulty = gameParameters.Chart?.GetRatingNumber() ?? 0,
			CurrentCombo = Combo.Value,
			CurrentScore = Score.Value,
			InFever = Fever.InFever,
			Health = Health.Value,
			MaxHealth = Health.Max,
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

	private static void clonedash_openmdlevel_autocomplete(ConCommandBase cmd, string argsStr, TokenizedCommand args, int curArgPos, ref string[] returns, ref string[]? returnHelp) {
		if (curArgPos == 1) {
			var songs = MuseDash1Compatibility.FindSongsStartingWith(args.Arg(1));
			returns = [.. songs.Select(s => s.BaseName)];
			returnHelp = [.. songs.Select(s => $" '{s.FetchMetadata().Name}'")];
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
				Logs.Print($"    {s.FetchMetadata().Name} ({s.BaseName})");
			return;
		}

		LevelTransitions.LoadSongChart($"Loading '{song.FetchMetadata().Name}'...", song.GetSheet(map), new() {
			Autoplay = args.Arg(3, 0) == 1
		});
	}
	void BuildQueues() {
		// This prepares the arrays...
		foreach (var ent in ReadyToBuildEntities) {
			if (ent is DashEnemy dashEnemy)
				dashEnemy.PreBuildVisuals(this);
		}

		// ...and then this builds the visuals in those arrays
		foreach (var scene in GetAllScenes()) {
			foreach (var ent in ReadyToBuildEntities) {
				if (ent is DashEnemy dashEnemy)
					dashEnemy.BuildForScene(scene);
			}
		}

		ReadyToBuildEntities.Clear();
		foreach (var ev in ReadyToBuildEvents)
			ev.Build();

		ReadyToBuildEvents.Clear();

		// If new entities exist, build those then (events might create entities)
		if (ReadyToBuildEntities.Count != 0)
			BuildQueues();
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

	private void EnterFever(ref FeverStartGameEvent ev) {
		if (!IsSeeking) {
			FeverFX?.Activate();
			PlaySceneSound(SceneSound.Fever, 0);
		}
		SceneUI?.UpdateInFever(Fever.TimeLeft, ev.Duration);
	}

	private void ExitFever(ref FeverEndGameEvent ev) {
		FeverFX?.Cancel();
	}

	private void GuardPause(ref PauseRequestGameEvent ev) {
		if (ev.FromMenu && (Results.LastNoteHit || Conductor.Time < 0))
			ev.Cancel = true;
		if (IsDead())
			ev.Cancel = true;
	}

	private void HitLogic(PathwayId pathway) {
		int amountOfTimesHit = InputState.ClickCount(Layout[pathway].Action);
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
								SceneUI?.CreatePerfectHitText(pollResult.Precision, pathway.AsSet, Fever.InFever, earlylate);
							else {
								SceneUI?.CreateGreatHitText(pollResult.Precision, pathway.AsSet, Fever.InFever, earlylate);
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
			if (Layout.IsAirSide(pathway))
				AttackAir(pathway, pollResult);
			else
				AttackGround(pathway, pollResult);

			ExitHitState();

			pollParams.HitsRemaining--;
		}
		//if (Debug)
		//Console.WriteLine($"poll.Hit = {hitSomething}, entity = {((pollResult.HasValue && pollResult.Value.Hit) ? pollResult.Value.HitEntity.ToString() : "NULL")}");
	}

	private void PlayUnpauseSound(ref UnpauseStartGameEvent ev) {
		if (HasActiveScene(out var scene))
			scene.PlaySound(SceneSound.Unpause, 0);
	}

	private void PrepareShaders() {
		// TODO: Verify effect order...
		Effects.Add(new SepiaScreenEffect());
		Effects.Add(new ChromaticAberrationScreenEffect());
		Effects.Add(new MosaicScreenEffect());
		Effects.Add(new ScanlinesScreenEffect());
		Effects.Add(new FilmGrainScreenEffect());
		Effects.Add(new VignetteScreenEffect());
		Effects.Add(new ScreenScrollScreenEffect());
		Effects.Add(new TVStaticScreenEffect());
		Effects.Add(new FlashbangScreenEffect());
		Effects.Add(new NoteFreezeScreenEffect());
		Effects.Add(new BgFreezeScreenEffect());
		Effects.Add(new FocusLinesScreenEffect());
		Effects.Add(new NoteVisibilityScreenEffect());
		Effects.Add(new BackgroundVisibilityScreenEffect());
		Effects.Initialize();
	}

	private void RenderVisibleEntity(FrameState frameState, DashEnemy ent, DeadEntityVisibility deadVis) {
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
					return;
			}
		}

		ent.Render(frameState);
		Rlgl.DrawRenderBatchActive();
		Model4System.PopRenderBlend();
	}

	private void ResetSceneSoundsPlayedThisFrame() {
		for (int i = 0; i < PlayedSceneSoundThisFrame.Length; i++)
			PlayedSceneSoundThisFrame[i] = false;
	}

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

	void ShowFailure(ref SongFailedGameEvent ev) {
		if (SceneUI != null && !SceneUI.ShowingFailureScreen())
			SceneUI.OpenFailure();
	}

	void ShowVictory(ref SongClearedGameEvent ev) {
		if (!Health.Dead)
			Stats.UploadScore(Score.Value);
		SceneUI?.OpenVictory(Stats);
		Music.Pause();
	}

	private void SubmitMashHit() {
		if (!InMashState)
			return;
		LastMasherAttemptedHit = Conductor.Time;
	}
	void TriggerDeath(ref PlayerDeathGameEvent ev) {
		if (Quirks.PlayableAfterDeath)
			return;

		Character.GetPrimary().PlayAnimation(CharacterAnimationType.Die);
	}

	void UpdateFeverProgress(ref FeverChangedGameEvent ev) {
		SceneUI?.UpdateFeverProgress(ev.Value, ev.Max);
	}

	/// <summary>
	/// Creates an event from an EventType enumeration, and adds it to <see cref="GameplayManager.Events"/>.
	/// </summary>
	/// <param name="t"></param>
	/// <returns></returns>
	/*public CD_BaseEvent AddEvent(EventType t) {
            CD_BaseEvent e = CD_BaseEvent.CreateFromType(this.Game, t);
            return e;
        }*/
	// Last time a combo occured in game-time
}