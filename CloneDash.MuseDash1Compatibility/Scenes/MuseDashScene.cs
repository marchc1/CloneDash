using AssetStudio;
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
using CloneDash.Compatibility.Unity;
using CloneDash.Game;
using CloneDash.Game.Entities;
using CloneDash.Game.Statistics;
using CloneDash.Settings;
using DiscordRPC;
using DiscordRPC.Registry;
using NAudio.CoreAudioApi;
using Nucleus;
using Nucleus.Audio;
using Nucleus.Common.Audio;
using Nucleus.Common.Graphics;
using Nucleus.Core;
using Nucleus.Engine;
using Nucleus.ManagedMemory;
using Nucleus.Models.Runtime;
using Nucleus.Types;
using Nucleus.UI;
using Nucleus.Util;
using OggVorbisEncoder;
using Raylib_cs;
using SevenZip.CommandLineParser;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Color = Nucleus.Common.Types.Color;
using Texture = Nucleus.ManagedMemory.Texture;
using Texture2D = AssetStudio.Texture2D;
using Transform = AssetStudio.Transform;

namespace CloneDash.Scenes;

public struct MuseDash1SceneSounds
{
	public string? Begin;
	public string? Fever;
	public string? Unpause;
	public string? FullCombo;
	public string? Block;
	public string? Crystal;
	public string? FailBgm;
	public string? Forte2;
	public string? Forte3;
	public string? Ghost;
	public string? Hp;
	public string? Jump;
	public string? Mezzo1;
	public string? Mezzo3;
	public string? Piano2;
	public string? PressIdle;
	public string? PressTop;
	public string? Score;
	public string? VictoryBgm;
	public Nucleus.Util.InlineArray16<string?> HitSounds;


	public static readonly MuseDash1SceneSounds Default = new MuseDash1SceneSounds() {
		Begin = "sfx_readygo",
		Fever = "char_common_fever",
		Unpause = "sfx_pause321",
		FullCombo = "sfx_full_combo",
		PressIdle = "sfx_press",
		PressTop = "sfx_press_top",
		Block = "sfx_block",
		Crystal = "sfx_crystal",
		FailBgm = "sfx_fail_bgm",
		Forte2 = "sfx_forte_2",
		Forte3 = "sfx_forte_3",
		Ghost = "sfx_mezzo_3",
		Hp = "sfx_hp",
		Jump = "sfx_jump",
		Mezzo1 = "sfx_mezzo_1",
		Mezzo3 = "sfx_mezzo_3",
		Piano2 = "sfx_piano_2",
		Score = "sfx_score",
		VictoryBgm = "sfx_victory_bgm",
	}.AutoGenHitSounds();

	public MuseDash1SceneSounds AutoGenHitSounds() {
		for (int i = 0; i < ((Span<string?>)HitSounds).Length; i++) {
			HitSounds[i] = $"hitsound_{i:000}";
		}
		return this;
	}
	public static MuseDash1SceneSounds operator +(MuseDash1SceneSounds a, MuseDash1SceneSounds b) {
		if (b.Begin != null) a.Begin = b.Begin;
		if (b.Fever != null) a.Fever = b.Fever;
		if (b.Unpause != null) a.Unpause = b.Unpause;
		if (b.FullCombo != null) a.FullCombo = b.FullCombo;
		if (b.Block != null) a.Block = b.Block;
		if (b.Crystal != null) a.Crystal = b.Crystal;
		if (b.FailBgm != null) a.FailBgm = b.FailBgm;
		if (b.Forte2 != null) a.Forte2 = b.Forte2;
		if (b.Forte3 != null) a.Forte3 = b.Forte3;
		if (b.Ghost != null) a.Ghost = b.Ghost;
		if (b.Hp != null) a.Hp = b.Hp;
		if (b.Jump != null) a.Jump = b.Jump;
		if (b.Mezzo1 != null) a.Mezzo1 = b.Mezzo1;
		if (b.Mezzo3 != null) a.Mezzo3 = b.Mezzo3;
		if (b.Piano2 != null) a.Piano2 = b.Piano2;
		if (b.PressIdle != null) a.PressIdle = b.PressIdle;
		if (b.PressTop != null) a.PressTop = b.PressTop;
		if (b.Score != null) a.Score = b.Score;
		if (b.VictoryBgm != null) a.VictoryBgm = b.VictoryBgm;
		return a;
	}
}

public class MD1_Animations3Speed
{
	public readonly MD1_SpineActionControllerData[][] Speeds = [
		[null!, null!, null!],
		[null!, null!, null!],
		[null!, null!, null!]
	];

	public MD1_SpineActionControllerData GetSpeed(int speed, EntityEnterDirection dir = EntityEnterDirection.RightSide) {
		Debug.Assert(speed >= 1);
		Debug.Assert(speed <= 3);
		return Speeds[speed - 1][(int)dir] ?? Speeds[speed - 1][0]; // Default to rightside
	}

	public ref MD1_SpineActionControllerData GetSpeedForEdit(int speed, EntityEnterDirection dir = EntityEnterDirection.RightSide) {
		Debug.Assert(speed >= 1);
		Debug.Assert(speed <= 3);
		return ref Speeds[speed - 1][(int)dir];
	}
}

public static class MuseDash1SceneEnemyInfo
{
	public const string CODE_BOSS = "01";
	public const string CODE_SUSTAIN = "02";
	public const string CODE_GEARS = "03";
	public const string CODE_MASHERS = "04";
	public const string CODE_DOUBLES = "05";
	public const string CODE_BOSS1 = "06";
	public const string CODE_BOSS2 = "07";
	public const string CODE_BOSS3 = "08";
	public const string CODE_BOSSGEARS = "09";
	public const string CODE_SMALL = "10";
	public const string CODE_MEDIUM1 = "11";
	public const string CODE_MEDIUM2 = "12";
	public const string CODE_LARGE1 = "13";
	public const string CODE_LARGE2 = "14";
	public const string CODE_HAMMER = "15";
	public const string CODE_RAIDER = "16";
	public const string CODE_GHOST = "17";

	public const string PATHWAY_AIR = "air";
	public const string PATHWAY_GROUND = "road";

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static string PATHWAY(PathwaySide side) => side == PathwaySide.Top ? PATHWAY_AIR : PATHWAY_GROUND;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static string DIRECTION(EntityEnterDirection dir) => dir switch {
		EntityEnterDirection.RightSide => "nor",
		EntityEnterDirection.TopDown => "down",
		EntityEnterDirection.BottomUp => "up",
		_ => throw new ArgumentOutOfRangeException()
	};
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static int SPEED(int speed) => Math.Clamp(speed, 1, 3);

	public static string GetBoss(MuseDash1SceneInfo scene)
		=> $"{scene.MapIdx:00}{CODE_BOSS}_boss";

	public static string GetSustainTop(MuseDash1SceneInfo scene, PathwaySide side)
		=> $"{scene.MapIdx:00}{CODE_SUSTAIN}_{PATHWAY(side)}_top";
	public static string GetSustainBody(MuseDash1SceneInfo scene, PathwaySide side)
		=> $"{scene.MapIdx:00}{CODE_SUSTAIN}_{PATHWAY(side)}_body";
	public static string GetSustainNoteUp(MuseDash1SceneInfo scene, PathwaySide side)
		=> $"{scene.MapIdx:00}{CODE_SUSTAIN}_{PATHWAY(side)}_note_up";
	public static string GetSustainNoteDown(MuseDash1SceneInfo scene, PathwaySide side)
		=> $"{scene.MapIdx:00}{CODE_SUSTAIN}_{PATHWAY(side)}_note_down";

	public static string GetGear(MuseDash1SceneInfo scene, PathwaySide side, int speed)
		=> $"{scene.MapIdx:00}{CODE_GEARS}{speed + (side == PathwaySide.Top ? 3 : 0):00}_{PATHWAY(side)}_nor_{SPEED(speed)}";

	public static string GetMasher(MuseDash1SceneInfo scene, EntityEnterDirection direction, int speed)
		=> $"{scene.MapIdx:00}{CODE_MASHERS}{speed + (direction == EntityEnterDirection.TopDown ? 3 : 0):00}_{DIRECTION(direction)}_{SPEED(speed)}";

	public static string GetDouble(MuseDash1SceneInfo scene, PathwaySide side, int speed)
		=> $"{scene.MapIdx:00}{CODE_DOUBLES}{speed + (side == PathwaySide.Top ? 3 : 0):00}_{PATHWAY(side)}_nor_{SPEED(speed)}";

	public static string GetBoss1(MuseDash1SceneInfo scene, PathwaySide side, int speed)
			=> $"{scene.MapIdx:00}{CODE_BOSS1}{speed + (side == PathwaySide.Top ? 3 : 0):00}_{PATHWAY(side)}_nor_{SPEED(speed)}";
	public static string GetBoss2(MuseDash1SceneInfo scene, PathwaySide side, int speed)
		=> $"{scene.MapIdx:00}{CODE_BOSS2}{speed + (side == PathwaySide.Top ? 3 : 0):00}_{PATHWAY(side)}_nor_{SPEED(speed)}";
	public static string GetBoss3(MuseDash1SceneInfo scene, PathwaySide side, int speed)
		=> $"{scene.MapIdx:00}{CODE_BOSS3}{speed + (side == PathwaySide.Top ? 3 : 0):00}_{PATHWAY(side)}_nor_{SPEED(speed)}";

	public static string GetBossGear(MuseDash1SceneInfo scene, PathwaySide side, int speed, bool second)
			=> $"{scene.MapIdx:00}{CODE_BOSSGEARS}{speed + (second ? 6 : 0) + (side == PathwaySide.Top ? 3 : 0):00}_{PATHWAY(side)}_{(second ? 2 : 1)}_nor_{SPEED(speed)}";

	public static string GetSmall(MuseDash1SceneInfo scene, PathwaySide side, EntityEnterDirection dir, int speed)
		=> $"{scene.MapIdx:00}{CODE_SMALL}{speed + (dir switch { EntityEnterDirection.RightSide => 0, EntityEnterDirection.TopDown => 12, EntityEnterDirection.BottomUp => 6, _ => throw new NotImplementedException() }) + (side == PathwaySide.Top ? 3 : 0):00}_{PATHWAY(side)}_{DIRECTION(dir)}_{SPEED(speed)}";

	public static string GetMedium1(MuseDash1SceneInfo scene, PathwaySide side, EntityEnterDirection dir, int speed)
		=> $"{scene.MapIdx:00}{CODE_MEDIUM1}{speed + (dir switch { EntityEnterDirection.RightSide => 0, EntityEnterDirection.TopDown => 12, EntityEnterDirection.BottomUp => 6, _ => throw new NotImplementedException() }) + (side == PathwaySide.Top ? 3 : 0):00}_{PATHWAY(side)}_{DIRECTION(dir)}_{SPEED(speed)}";

	public static string GetMedium2(MuseDash1SceneInfo scene, PathwaySide side, EntityEnterDirection dir, int speed)
		=> $"{scene.MapIdx:00}{CODE_MEDIUM2}{speed + (dir switch { EntityEnterDirection.RightSide => 0, EntityEnterDirection.TopDown => 12, EntityEnterDirection.BottomUp => 6, _ => throw new NotImplementedException() }) + (side == PathwaySide.Top ? 3 : 0):00}_{PATHWAY(side)}_{DIRECTION(dir)}_{SPEED(speed)}";

	public static string GetLarge1(MuseDash1SceneInfo scene, PathwaySide side, int speed)
		=> $"{scene.MapIdx:00}{CODE_LARGE1}{speed + (side == PathwaySide.Top ? 3 : 0):00}_{PATHWAY(side)}_nor_{SPEED(speed)}";

	public static string GetLarge2(MuseDash1SceneInfo scene, PathwaySide side, int speed)
		=> $"{scene.MapIdx:00}{CODE_LARGE2}{speed + (side == PathwaySide.Top ? 3 : 0):00}_{PATHWAY(side)}_nor_{SPEED(speed)}";

	public static string GetHammer(MuseDash1SceneInfo scene, PathwaySide side, int speed, bool reversed)
		=> $"{scene.MapIdx:00}{CODE_HAMMER}{speed + (reversed ? 6 : 0) + (side == PathwaySide.Top ? 3 : 0):00}_{PATHWAY(side)}_{(reversed ? "up" : "down")}_{SPEED(speed)}";

	public static string GetRaider(MuseDash1SceneInfo scene, PathwaySide side, int speed, bool reversed)
		=> $"{scene.MapIdx:00}{CODE_RAIDER}{speed + (reversed ? 6 : 0) + (side == PathwaySide.Top ? 3 : 0):00}_{PATHWAY(side)}_{(reversed ? "down" : "up")}_{SPEED(speed)}";

	public static string GetGhost(MuseDash1SceneInfo scene, PathwaySide side, int speed)
		=> $"{scene.MapIdx:00}{CODE_GHOST}{speed + (side == PathwaySide.Top ? 3 : 0):00}_{PATHWAY(side)}_nor_{SPEED(speed)}";

	internal static string GetHeart(PathwaySide path, int speed)
		=> $"0002{speed + (path == PathwaySide.Top ? 3 : 0):00}_{PATHWAY(path)}_nor_{SPEED(speed)}";

	internal static string GetScore(PathwaySide path, int speed)
		=> $"0003{speed + (path == PathwaySide.Top ? 3 : 0):00}_{PATHWAY(path)}_nor_{SPEED(speed)}";
}

/// <summary>
/// Some hardcoded Muse Dash scene information.
/// I think this info is hardcoded in basegame anyway
/// </summary>
public record class MuseDash1SceneInfo
{
	readonly static Dictionary<ulong, MuseDash1SceneInfo> scenes = [];

	public readonly string MapName;
	public readonly string OfficialName;
	public readonly int MapIdx;

	public MuseDash1SceneSounds Sounds = MuseDash1SceneSounds.Default;
	public bool Unusable;
	public MuseDash1SceneInfo MarkUnusable() {
		Unusable = true;
		return this;
	}

	public MuseDash1SceneInfo WithUI(UIFactoryFn uiFactory) {
		UIFactory = uiFactory;
		return this;
	}

	public MuseDash1SceneInfo WithSounds(MuseDash1SceneSounds sounds) {
		Sounds += sounds;
		return this;
	}
	public static IEnumerable<MuseDash1SceneInfo> GetScenes() => scenes.Values;

	public MuseDash1SceneInfo(int idx, string officialName, [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format = "scene_{0:00}") {
		MapIdx = idx;
		MapName = string.Format(format, idx);
		OfficialName = officialName;

		scenes[MapName.Hash()] = this;
	}

	public static MuseDash1SceneInfo? GetSceneInfo(ReadOnlySpan<char> name) {
		ulong hash = name.Hash();
		if (scenes.TryGetValue(hash, out var ret))
			return ret;
		return null;
	}

	// TODO: Verify these names properly
	// Wiki is probably a horrible source for ~50% of these
	public static readonly MuseDash1SceneInfo SpaceStation = new MuseDash1SceneInfo(1, "Space Station");
	public static readonly MuseDash1SceneInfo RetroCity = new MuseDash1SceneInfo(2, "Retro City");
	public static readonly MuseDash1SceneInfo Castle = new MuseDash1SceneInfo(3, "Castle");
	public static readonly MuseDash1SceneInfo RainyNight = new MuseDash1SceneInfo(4, "Rainy Night");
	public static readonly MuseDash1SceneInfo Candyland = new MuseDash1SceneInfo(5, "Candyland");
	public static readonly MuseDash1SceneInfo Oriental = new MuseDash1SceneInfo(6, "Oriental");
	public static readonly MuseDash1SceneInfo GrooveCoaster = new MuseDash1SceneInfo(7, "Groove Coaster")
												.WithSounds(new MuseDash1SceneSounds {
													Begin = "sfx_readygo_gc",
													Ghost = "sfx_ghost_gc"
												});
	public static readonly MuseDash1SceneInfo Gensokyo = new MuseDash1SceneInfo(8, "Gensokyo");
	public static readonly MuseDash1SceneInfo GameGraveyard = new MuseDash1SceneInfo(9, "Game Graveyard");
	public static readonly MuseDash1SceneInfo Museland = new MuseDash1SceneInfo(10, "Museland", "scene_{0:00}_miku");
	public static readonly MuseDash1SceneInfo Mirrorland = new MuseDash1SceneInfo(10, "Mirrorland", "scene_{0:00}_rin_len");
	public static readonly MuseDash1SceneInfo Warriorland = new MuseDash1SceneInfo(11, "Warriorland")
												.MarkUnusable();
	public static readonly MuseDash1SceneInfo JadeTemple = new MuseDash1SceneInfo(12, "Jade Temple")
												.MarkUnusable();

	public UIFactoryFn UIFactory = scene => new CloneDashSceneUI(scene);
}
public delegate IMuseDash1SceneUI UIFactoryFn(IMuseDash1SceneInstance scene);
public class MuseDash1SceneDescriptor : IMuseDash1SceneDescriptor
{
	public readonly MuseDash1SceneInfo SceneInfo;

	public MuseDash1SceneDescriptor(MuseDash1SceneInfo info) {
		SceneInfo = info;
	}

	public static MuseDash1SceneDescriptor? GetScene(ReadOnlySpan<char> name) {
		var sceneInfo = MuseDash1SceneInfo.GetSceneInfo(name);
		if (sceneInfo == null)
			return null;

		if (sceneInfo.Unusable) {
			Logs.Warn($"The scene '{sceneInfo.OfficialName}' is currently broken, so 'Space Station' will be selected as a fallback for this scene.");
			return GetScene("scene_01"); // Fall back to Space Station...
		}

		return new(sceneInfo);
	}

	public T? CreateInGame<T>(IGame game) where T : ISceneInstance {
		var uuid = game.GetGamemode().GetUUID();

		switch (uuid) {
			case "gamemode/musedash1/standard":
				return (T)(object)(new MuseDash1SceneRuntime(this, (MuseDash1Game)game));
		}

		return default;
	}

	public SceneMetadata FetchMetadata(in HumanLanguage desiredLanguage) {
		return new() {
			Name = SceneInfo.OfficialName,
			Artists = "PeroPeroGames",
			Language = HumanLanguage.English,
		};
	}

	public ReadOnlySpan<char> GetUUID() => ISceneDescriptor.ConstructUUID("musedash1", SceneInfo.MapName);

	public bool SupportsGamemode(IGamemodeDescriptor gamemodeDescriptor) {
		var uuid = gamemodeDescriptor.GetUUID();
		switch (uuid) {
			case "gamemode/musedash1/standard":
				return true;
		}
		return false;
	}
}

public class CloneDashSceneUI(IMuseDash1SceneInstance scene) : IMuseDash1SceneUI
{
	StatisticsPanel? CurrentStatisticsPanel;
	readonly List<(SceneObject obj, double expiry)> timedObjects = [];
	double time;

	public virtual void Initialize() {
		
	}
	public void CreateGreatHitText(double precision, PathwaySide pathway, bool inFever, EarlyLate earlylate) {

	}

	public void CreateHealthText(float healthGiven) {

	}

	public void CreatePassText(double precision, PathwaySide pathway) {

	}

	public void CreatePerfectHitText(double precision, PathwaySide pathway, bool inFever, EarlyLate earlylate) {
		
	}

	public void CreateScoreText(int scoreGiven) {

	}

	public void EndMultiHitText() {

	}

	public void EndWarning() {

	}

	public void OpenVictory(StatisticsData stats) {

	}

	public void CloseVictory(){
		CurrentStatisticsPanel?.Remove();
		CurrentStatisticsPanel = null;
	}

	public void RenderWorldspace() {

	}

	public void Think(double dt) {
		time = scene.GetGame().GetConductor().GetTime();

	}

	public bool ShowingVictoryScreen() => IValidatable.IsValid(CurrentStatisticsPanel);


	public void StartMultiHitText() {

	}

	public void StartWarning() {

	}

	public void UpdateAllPerfect(bool allPerfect) {

	}

	public void UpdateCombo(int currentCombo) {

	}

	public void UpdateFeverProgress(double fever, double maxFever) {

	}

	public void UpdateFullCombo(bool fullCombo) {

	}

	public void UpdateHP(double hp, double maxHP) {

	}

	public void UpdateInFever(double feverRemainingTime, double feverTotalTime) {

	}

	public void UpdateMultiHitText(int hits) {

	}

	public void UpdateScore(double score) {

	}
}

public class MuseDash1SceneRuntime : BaseMuseDash1UnitySimScene, IMuseDash1SceneInstance
{
	readonly PathwayInformation[] pathwayInfo = new PathwayInformation[4];
	public readonly MuseDash1SceneDescriptor Descriptor;
	public readonly MuseDash1Game Game;
	public MuseDash1SceneInfo SceneInfo => Descriptor.SceneInfo;


	public MuseDash1SceneRuntime(MuseDash1SceneDescriptor descriptor, MuseDash1Game game) {
		Descriptor = descriptor;
		Game = game;

		var sceneGameObject = MuseDash1Compatibility.StreamingAssets.FindAssetByName<GameObject>(SceneInfo.MapName)!;
		var sceneSubControl = new MonoBehaviourReader(
			sceneGameObject.GetComponentByName<MonoBehaviour>("SceneSubControl")
			?? throw new NullReferenceException("No scene control?"));

		var scenePoint = sceneSubControl.Get<GameObject>("scenePoint");
		var transform = scenePoint!.GetFirstComponent<Transform>()!;

		var pathwaysObject = ImportGameObject(scenePoint, null);

		var pathwayChildren = new List<(SceneObject obj, Vector3 pos)>();
		foreach (var child in pathwaysObject.Transform.Children) {
			child.Object.Transform.ComputeGlobalTransform(out var pos, out _);
			pathwayChildren.Add((child.Object, pos));
		}

		if (pathwayChildren.Count >= 2) {
			pathwayChildren.Sort((a, b) => b.pos.Y.CompareTo(a.pos.Y));
			AssignPathway(PathwaySide.Top, pathwayChildren[0].obj, pathwayChildren[0].pos);
			AssignPathway(PathwaySide.Bottom, pathwayChildren[1].obj, pathwayChildren[1].pos);
		}
		else if (pathwayChildren.Count == 1) {
			AssignPathway(PathwaySide.Bottom, pathwayChildren[0].obj, pathwayChildren[0].pos);
		}

		var rootTransform = sceneGameObject.GetFirstComponent<Transform>()!;
		root = ImportGameObject(rootTransform.GetGameObject()!, null);

		foreach (var obj in allObjects) obj.Awake();

		foreach (var obj in allObjects)
			foreach (var anim in obj.GetComponents<SceneAnimator>())
				animators.Add(anim);

		BuildRenderOrder();

		pathwayInfo[(int)PathwaySide.Both] = new() {
			Position = (pathwayInfo[(int)PathwaySide.Top].Position +
						pathwayInfo[(int)PathwaySide.Bottom].Position) / 2,
			Color = PathwayExts.PATHWAY_DUAL_COLOR
		};
		pathwayInfo[(int)PathwaySide.Top].Color = PathwayExts.PATHWAY_TOP_COLOR;
		pathwayInfo[(int)PathwaySide.Bottom].Color = PathwayExts.PATHWAY_BOTTOM_COLOR;
	}

	private void AssignPathway(PathwaySide side, SceneObject obj, Vector3 pos) {
		pathwayInfo[(int)side] = new(pos.X, pos.Y, obj);
	}
	delegate T? ProducerFn<T>();
	class NullLazyLoad<T>(ProducerFn<T> producer) where T : class
	{
		bool triedToLoad = false;
		T? value { get; set; }
		public T? TryLoad() {
			if (triedToLoad)
				return value;

			triedToLoad = true;
			value = producer();
			return value;
		}
		public static implicit operator NullLazyLoad<T>(ProducerFn<T> v) => new(v);
		public static implicit operator T?(NullLazyLoad<T> v) => v.TryLoad();
	}

	NullLazyLoad<ModelData> BossModel;
	NullLazyLoad<ModelData> AirGearModel, RoadGearModel;
	NullLazyLoad<ModelData> MasherModel;
	NullLazyLoad<ModelData> AirHeartModel, RoadHeartModel;
	NullLazyLoad<ModelData> AirScoreModel, RoadScoreModel;
	NullLazyLoad<ModelData> AirDoubleModel, RoadDoubleModel;
	NullLazyLoad<ModelData> AirBoss1Model, RoadBoss1Model;
	NullLazyLoad<ModelData> AirBoss2Model, RoadBoss2Model;
	NullLazyLoad<ModelData> AirBoss3Model, RoadBoss3Model;
	NullLazyLoad<ModelData> AirBossGearModel, RoadBossGearModel;
	NullLazyLoad<ModelData> AirSmallModel, RoadSmallModel;
	NullLazyLoad<ModelData> AirMedium1Model, RoadMedium1Model;
	NullLazyLoad<ModelData> AirMedium2Model, RoadMedium2Model;
	NullLazyLoad<ModelData> AirLarge1Model, RoadLarge1Model;
	NullLazyLoad<ModelData> AirLarge2Model, RoadLarge2Model;
	NullLazyLoad<ModelData> AirHammerModel, RoadHammerModel, AirHammerBModel, RoadHammerBModel;
	NullLazyLoad<ModelData> AirRaiderModel, RoadRaiderModel, AirRaiderBModel, RoadRaiderBModel;
	NullLazyLoad<ModelData> AirGhostModel, RoadGhostModel;

	NullLazyLoad<ModelData> HpMountModel;

	IAudioClip? BeginSound;
	IAudioClip? FeverSound;
	IAudioClip? UnpauseSound;
	IAudioClip? FullComboSound;
	IAudioClip? BlockSound;
	IAudioClip? CrystalSound;
	IAudioClip? FailBgmSound;
	IAudioClip? Forte2Sound;
	IAudioClip? Forte3Sound;
	IAudioClip? GhostSound;
	IAudioClip? HpSound;
	IAudioClip? JumpSound;
	IAudioClip? Mezzo1Sound;
	IAudioClip? Mezzo3Sound;
	IAudioClip? Piano2Sound;
	IAudioClip? PressIdleSound;
	IAudioClip? PressTopSound;
	IAudioClip? ScoreSound;
	IAudioClip?[]? HitSounds;
	IAudioClip? VictoryBgmSound;

	MD1_SpineActionControllerData BossAnims = null!;

	MD1_Animations3Speed AirGearAnims = new(), RoadGearAnims = new();
	MD1_Animations3Speed MasherAnims = new();
	MD1_Animations3Speed AirHeartAnims = new(), RoadHeartAnims = new();
	MD1_Animations3Speed AirScoreAnims = new(), RoadScoreAnims = new();
	MD1_Animations3Speed AirDoubleAnims = new(), RoadDoubleAnims = new();
	MD1_Animations3Speed AirBoss1Anims = new(), RoadBoss1Anims = new();
	MD1_Animations3Speed AirBoss2Anims = new(), RoadBoss2Anims = new();
	MD1_Animations3Speed AirBoss3Anims = new(), RoadBoss3Anims = new();
	MD1_Animations3Speed AirBossGearA_Anims = new(), RoadBossGearA_Anims = new();
	MD1_Animations3Speed AirBossGearB_Anims = new(), RoadBossGearB_Anims = new();
	MD1_Animations3Speed AirSmallAnims = new(), RoadSmallAnims = new();
	MD1_Animations3Speed AirMedium1Anims = new(), RoadMedium1Anims = new();
	MD1_Animations3Speed AirMedium2Anims = new(), RoadMedium2Anims = new();
	MD1_Animations3Speed AirLarge1Anims = new(), RoadLarge1Anims = new();
	MD1_Animations3Speed AirLarge2Anims = new(), RoadLarge2Anims = new();
	MD1_Animations3Speed AirHammerA_Anims = new(), RoadHammerA_Anims = new();
	MD1_Animations3Speed AirHammerB_Anims = new(), RoadHammerB_Anims = new();
	MD1_Animations3Speed AirRaiderA_Anims = new(), RoadRaiderA_Anims = new();
	MD1_Animations3Speed AirRaiderB_Anims = new(), RoadRaiderB_Anims = new();
	MD1_Animations3Speed AirGhostAnims = new(), RoadGhostAnims = new();

	ITexture? AirStartSustainTexture, AirEndSustainTexture, AirBodySustainTexture, AirUpSustainTexture, AirDownSustainTexture;
	ITexture? RoadStartSustainTexture, RoadEndSustainTexture, RoadBodySustainTexture, RoadUpSustainTexture, RoadDownSustainTexture;

	AudioPlaybackHandle pressIdle;
	int arrayIndex;

	public void Initialize() {
		var game = this.Game;

		BeginSound = MuseDash1Compatibility.LoadSoundFromName(game, SceneInfo.Sounds.Begin ?? throw new NullReferenceException());
		FeverSound = MuseDash1Compatibility.LoadSoundFromName(game, SceneInfo.Sounds.Fever ?? throw new NullReferenceException());
		UnpauseSound = MuseDash1Compatibility.LoadSoundFromName(game, SceneInfo.Sounds.Unpause ?? throw new NullReferenceException());
		FullComboSound = MuseDash1Compatibility.LoadSoundFromName(game, SceneInfo.Sounds.FullCombo ?? throw new NullReferenceException());
		BlockSound = MuseDash1Compatibility.LoadSoundFromName(game, SceneInfo.Sounds.Block ?? throw new NullReferenceException());
		CrystalSound = MuseDash1Compatibility.LoadSoundFromName(game, SceneInfo.Sounds.Crystal ?? throw new NullReferenceException());
		FailBgmSound = MuseDash1Compatibility.LoadMusicFromName(game, SceneInfo.Sounds.FailBgm ?? throw new NullReferenceException());
		Forte2Sound = MuseDash1Compatibility.LoadSoundFromName(game, SceneInfo.Sounds.Forte2 ?? throw new NullReferenceException());
		Forte3Sound = MuseDash1Compatibility.LoadSoundFromName(game, SceneInfo.Sounds.Forte3 ?? throw new NullReferenceException());
		GhostSound = MuseDash1Compatibility.LoadSoundFromName(game, SceneInfo.Sounds.Ghost ?? throw new NullReferenceException());
		HpSound = MuseDash1Compatibility.LoadSoundFromName(game, SceneInfo.Sounds.Hp ?? throw new NullReferenceException());
		JumpSound = MuseDash1Compatibility.LoadSoundFromName(game, SceneInfo.Sounds.Jump ?? throw new NullReferenceException());
		Mezzo1Sound = MuseDash1Compatibility.LoadSoundFromName(game, SceneInfo.Sounds.Mezzo1 ?? throw new NullReferenceException());
		Mezzo3Sound = MuseDash1Compatibility.LoadSoundFromName(game, SceneInfo.Sounds.Mezzo3 ?? throw new NullReferenceException());
		Piano2Sound = MuseDash1Compatibility.LoadSoundFromName(game, SceneInfo.Sounds.Piano2 ?? throw new NullReferenceException());
		PressIdleSound = MuseDash1Compatibility.LoadMusicFromName(game, SceneInfo.Sounds.PressIdle ?? throw new NullReferenceException());
		PressTopSound = MuseDash1Compatibility.LoadSoundFromName(game, SceneInfo.Sounds.PressTop ?? throw new NullReferenceException());
		ScoreSound = MuseDash1Compatibility.LoadSoundFromName(game, SceneInfo.Sounds.Score ?? throw new NullReferenceException());
		VictoryBgmSound = MuseDash1Compatibility.LoadMusicFromName(game, SceneInfo.Sounds.VictoryBgm ?? throw new NullReferenceException());

		HitSounds = new IAudioClip?[16];
		for (int i = 0; i < 16; i++) {
			HitSounds[i] = MuseDash1Compatibility.LoadSoundFromName(game, SceneInfo.Sounds.HitSounds[i] ?? throw new NullReferenceException());
			HitSounds[i]?.BindVolumeToConVar(AudioSettings.snd_hitvolume);
		}

		BeginSound?.BindVolumeToConVar(AudioSettings.snd_voicevolume);
		FeverSound?.BindVolumeToConVar(AudioSettings.snd_voicevolume);
		UnpauseSound?.BindVolumeToConVar(AudioSettings.snd_voicevolume);
		FullComboSound?.BindVolumeToConVar(AudioSettings.snd_voicevolume);

		BlockSound?.BindVolumeToConVar(AudioSettings.snd_hitvolume);
		CrystalSound?.BindVolumeToConVar(AudioSettings.snd_hitvolume);
		Forte2Sound?.BindVolumeToConVar(AudioSettings.snd_hitvolume);
		Forte3Sound?.BindVolumeToConVar(AudioSettings.snd_hitvolume);
		GhostSound?.BindVolumeToConVar(AudioSettings.snd_hitvolume);
		HpSound?.BindVolumeToConVar(AudioSettings.snd_hitvolume);
		JumpSound?.BindVolumeToConVar(AudioSettings.snd_hitvolume);
		Mezzo1Sound?.BindVolumeToConVar(AudioSettings.snd_hitvolume);
		Mezzo3Sound?.BindVolumeToConVar(AudioSettings.snd_hitvolume);
		Piano2Sound?.BindVolumeToConVar(AudioSettings.snd_hitvolume);
		PressIdleSound?.BindVolumeToConVar(AudioSettings.snd_hitvolume);
		PressTopSound?.BindVolumeToConVar(AudioSettings.snd_hitvolume);
		ScoreSound?.BindVolumeToConVar(AudioSettings.snd_hitvolume);

		FailBgmSound?.BindVolumeToConVar(AudioSettings.snd_musicvolume);
		VictoryBgmSound?.BindVolumeToConVar(AudioSettings.snd_musicvolume);

		var assets = MuseDash1Compatibility.StreamingAssets;

		string sustainID = $"{SceneInfo.MapIdx:00}{MuseDash1SceneEnemyInfo.CODE_SUSTAIN}";
		AirStartSustainTexture = LoadTexture(assets.FindAssetByName<Texture2D>($"{sustainID}_air_top"));
		AirEndSustainTexture = LoadTexture(assets.FindAssetByName<Texture2D>($"{sustainID}_air_top"));
		AirBodySustainTexture = LoadTexture(assets.FindAssetByName<Texture2D>($"{sustainID}_air_body"));
		AirUpSustainTexture = LoadTexture(assets.FindAssetByName<Texture2D>($"{sustainID}_air_note_up"));
		AirDownSustainTexture = LoadTexture(assets.FindAssetByName<Texture2D>($"{sustainID}_air_note_down"));

		RoadStartSustainTexture = LoadTexture(assets.FindAssetByName<Texture2D>($"{sustainID}_road_top"));
		RoadEndSustainTexture = LoadTexture(assets.FindAssetByName<Texture2D>($"{sustainID}_road_top"));
		RoadBodySustainTexture = LoadTexture(assets.FindAssetByName<Texture2D>($"{sustainID}_road_body"));
		RoadUpSustainTexture = LoadTexture(assets.FindAssetByName<Texture2D>($"{sustainID}_road_note_up"));
		RoadDownSustainTexture = LoadTexture(assets.FindAssetByName<Texture2D>($"{sustainID}_road_note_down"));

		string bossID = $"{SceneInfo.MapIdx:00}{MuseDash1SceneEnemyInfo.CODE_BOSS}";
		string gearAirID = $"{SceneInfo.MapIdx:00}{MuseDash1SceneEnemyInfo.CODE_GEARS}_air";
		string gearRoadID = SceneInfo.MapIdx switch {
			// :(
			3 => $"{SceneInfo.MapIdx:00}{MuseDash1SceneEnemyInfo.CODE_GEARS}_road",
			8 => $"{SceneInfo.MapIdx:00}{MuseDash1SceneEnemyInfo.CODE_GEARS}_road",
			10 => $"{SceneInfo.MapIdx:00}{MuseDash1SceneEnemyInfo.CODE_GEARS}_road",
			_ => $"{SceneInfo.MapIdx:00}{MuseDash1SceneEnemyInfo.CODE_GEARS}"
		};
		string masherID = $"{SceneInfo.MapIdx:00}{MuseDash1SceneEnemyInfo.CODE_MASHERS}";
		string doubleAirID = $"{SceneInfo.MapIdx:00}{MuseDash1SceneEnemyInfo.CODE_DOUBLES}_air";
		string doubleRoadID = $"{SceneInfo.MapIdx:00}{MuseDash1SceneEnemyInfo.CODE_DOUBLES}_road";
		string boss1AirID = $"{SceneInfo.MapIdx:00}{MuseDash1SceneEnemyInfo.CODE_BOSS1}_air";
		string boss1RoadID = $"{SceneInfo.MapIdx:00}{MuseDash1SceneEnemyInfo.CODE_BOSS1}_road";
		string boss2AirID = $"{SceneInfo.MapIdx:00}{MuseDash1SceneEnemyInfo.CODE_BOSS2}_air";
		string boss2RoadID = $"{SceneInfo.MapIdx:00}{MuseDash1SceneEnemyInfo.CODE_BOSS2}_road";
		string boss3AirID = $"{SceneInfo.MapIdx:00}{MuseDash1SceneEnemyInfo.CODE_BOSS3}_air";
		string boss3RoadID = $"{SceneInfo.MapIdx:00}{MuseDash1SceneEnemyInfo.CODE_BOSS3}_road";
		string bossGearAirID = $"{SceneInfo.MapIdx:00}{MuseDash1SceneEnemyInfo.CODE_BOSSGEARS}_air";
		string bossGearRoadID = $"{SceneInfo.MapIdx:00}{MuseDash1SceneEnemyInfo.CODE_BOSSGEARS}_road";
		string smallAirID = $"{SceneInfo.MapIdx:00}{MuseDash1SceneEnemyInfo.CODE_SMALL}_air";
		string smallRoadID = $"{SceneInfo.MapIdx:00}{MuseDash1SceneEnemyInfo.CODE_SMALL}_road";
		string medium1AirID = $"{SceneInfo.MapIdx:00}{MuseDash1SceneEnemyInfo.CODE_MEDIUM1}_air";
		string medium1RoadID = $"{SceneInfo.MapIdx:00}{MuseDash1SceneEnemyInfo.CODE_MEDIUM1}_road";
		string medium2AirID = $"{SceneInfo.MapIdx:00}{MuseDash1SceneEnemyInfo.CODE_MEDIUM2}_air";
		string medium2RoadID = $"{SceneInfo.MapIdx:00}{MuseDash1SceneEnemyInfo.CODE_MEDIUM2}_road";
		string large1AirID = $"{SceneInfo.MapIdx:00}{MuseDash1SceneEnemyInfo.CODE_LARGE1}_air";
		string large1RoadID = $"{SceneInfo.MapIdx:00}{MuseDash1SceneEnemyInfo.CODE_LARGE1}_road";
		string large2AirID = $"{SceneInfo.MapIdx:00}{MuseDash1SceneEnemyInfo.CODE_LARGE2}_air";
		string large2RoadID = $"{SceneInfo.MapIdx:00}{MuseDash1SceneEnemyInfo.CODE_LARGE2}_road";
		string hammerAirID = $"{SceneInfo.MapIdx:00}{MuseDash1SceneEnemyInfo.CODE_HAMMER}_air";
		string hammerRoadID = $"{SceneInfo.MapIdx:00}{MuseDash1SceneEnemyInfo.CODE_HAMMER}_road";
		string hammerAirBID = $"{SceneInfo.MapIdx:00}{MuseDash1SceneEnemyInfo.CODE_HAMMER}_air_b";
		string hammerRoadBID = $"{SceneInfo.MapIdx:00}{MuseDash1SceneEnemyInfo.CODE_HAMMER}_road_b";
		string raiderAirID = $"{SceneInfo.MapIdx:00}{MuseDash1SceneEnemyInfo.CODE_RAIDER}_air";
		string raiderRoadID = $"{SceneInfo.MapIdx:00}{MuseDash1SceneEnemyInfo.CODE_RAIDER}_road";
		string raiderAirBID = $"{SceneInfo.MapIdx:00}{MuseDash1SceneEnemyInfo.CODE_RAIDER}_air_b";
		string raiderRoadBID = $"{SceneInfo.MapIdx:00}{MuseDash1SceneEnemyInfo.CODE_RAIDER}_road_b";
		string ghostAirID = $"{SceneInfo.MapIdx:00}{MuseDash1SceneEnemyInfo.CODE_GHOST}_air";
		string ghostRoadID = $"{SceneInfo.MapIdx:00}{MuseDash1SceneEnemyInfo.CODE_GHOST}_road";

		// Populate models
		BossModel = new ProducerFn<ModelData>(() => LoadModel(assets.FindAssetByName<MonoBehaviour>($"{bossID}_SkeletonData")!));
		AirHeartModel = new ProducerFn<ModelData>(() => LoadModel(assets.FindAssetByName<MonoBehaviour>("0002_hp_SkeletonData")!));
		RoadHeartModel = new ProducerFn<ModelData>(() => LoadModel(assets.FindAssetByName<MonoBehaviour>("0002_hp_SkeletonData")!));
		AirScoreModel = new ProducerFn<ModelData>(() => LoadModel(assets.FindAssetByName<MonoBehaviour>("0003_score_SkeletonData")!));
		RoadScoreModel = new ProducerFn<ModelData>(() => LoadModel(assets.FindAssetByName<MonoBehaviour>("0003_score_SkeletonData")!));
		AirGearModel = new ProducerFn<ModelData>(() => LoadModel(assets.FindAssetByName<MonoBehaviour>($"{gearAirID}_SkeletonData")!));
		RoadGearModel = new ProducerFn<ModelData>(() => LoadModel(assets.FindAssetByName<MonoBehaviour>($"{gearRoadID}_SkeletonData")!));
		MasherModel = new ProducerFn<ModelData>(() => LoadModel(assets.FindAssetByName<MonoBehaviour>($"{masherID}_SkeletonData")!));
		AirDoubleModel = new ProducerFn<ModelData>(() => LoadModel(assets.FindAssetByName<MonoBehaviour>($"{doubleAirID}_SkeletonData")!));
		RoadDoubleModel = new ProducerFn<ModelData>(() => LoadModel(assets.FindAssetByName<MonoBehaviour>($"{doubleRoadID}_SkeletonData")!));
		AirBoss1Model = new ProducerFn<ModelData>(() => LoadModel(assets.FindAssetByName<MonoBehaviour>($"{boss1AirID}_SkeletonData")!));
		RoadBoss1Model = new ProducerFn<ModelData>(() => LoadModel(assets.FindAssetByName<MonoBehaviour>($"{boss1RoadID}_SkeletonData")!));
		AirBoss2Model = new ProducerFn<ModelData>(() => LoadModel(assets.FindAssetByName<MonoBehaviour>($"{boss2AirID}_SkeletonData")!));
		RoadBoss2Model = new ProducerFn<ModelData>(() => LoadModel(assets.FindAssetByName<MonoBehaviour>($"{boss2RoadID}_SkeletonData")!));
		AirBoss3Model = new ProducerFn<ModelData>(() => LoadModel(assets.FindAssetByName<MonoBehaviour>($"{boss3AirID}_SkeletonData")!));
		RoadBoss3Model = new ProducerFn<ModelData>(() => LoadModel(assets.FindAssetByName<MonoBehaviour>($"{boss3RoadID}_SkeletonData")!));
		AirBossGearModel = new ProducerFn<ModelData>(() => LoadModel(assets.FindAssetByName<MonoBehaviour>($"{bossGearAirID}_SkeletonData")!));
		RoadBossGearModel = new ProducerFn<ModelData>(() => LoadModel(assets.FindAssetByName<MonoBehaviour>($"{bossGearRoadID}_SkeletonData")!));
		AirSmallModel = new ProducerFn<ModelData>(() => LoadModel(assets.FindAssetByName<MonoBehaviour>($"{smallAirID}_SkeletonData")!));
		RoadSmallModel = new ProducerFn<ModelData>(() => LoadModel(assets.FindAssetByName<MonoBehaviour>($"{smallRoadID}_SkeletonData")!));
		AirMedium1Model = new ProducerFn<ModelData>(() => LoadModel(assets.FindAssetByName<MonoBehaviour>($"{medium1AirID}_SkeletonData")!));
		RoadMedium1Model = new ProducerFn<ModelData>(() => LoadModel(assets.FindAssetByName<MonoBehaviour>($"{medium1RoadID}_SkeletonData")!));
		AirMedium2Model = new ProducerFn<ModelData>(() => LoadModel(assets.FindAssetByName<MonoBehaviour>($"{medium2AirID}_SkeletonData")!));
		RoadMedium2Model = new ProducerFn<ModelData>(() => LoadModel(assets.FindAssetByName<MonoBehaviour>($"{medium2RoadID}_SkeletonData")!));
		AirLarge1Model = new ProducerFn<ModelData>(() => LoadModel(assets.FindAssetByName<MonoBehaviour>($"{large1AirID}_SkeletonData")!));
		RoadLarge1Model = new ProducerFn<ModelData>(() => LoadModel(assets.FindAssetByName<MonoBehaviour>($"{large1RoadID}_SkeletonData")!));
		AirLarge2Model = new ProducerFn<ModelData>(() => LoadModel(assets.FindAssetByName<MonoBehaviour>($"{large2AirID}_SkeletonData")!));
		RoadLarge2Model = new ProducerFn<ModelData>(() => LoadModel(assets.FindAssetByName<MonoBehaviour>($"{large2RoadID}_SkeletonData")!));
		AirHammerModel = new ProducerFn<ModelData>(() => LoadModel(assets.FindAssetByName<MonoBehaviour>($"{hammerAirID}_SkeletonData")!));
		RoadHammerModel = new ProducerFn<ModelData>(() => LoadModel(assets.FindAssetByName<MonoBehaviour>($"{hammerRoadID}_SkeletonData")!));
		AirHammerBModel = new ProducerFn<ModelData>(() => LoadModel(assets.FindAssetByName<MonoBehaviour>($"{hammerAirBID}_SkeletonData")!));
		RoadHammerBModel = new ProducerFn<ModelData>(() => LoadModel(assets.FindAssetByName<MonoBehaviour>($"{hammerRoadBID}_SkeletonData")!));
		AirRaiderModel = new ProducerFn<ModelData>(() => LoadModel(assets.FindAssetByName<MonoBehaviour>($"{raiderAirID}_SkeletonData")!));
		RoadRaiderModel = new ProducerFn<ModelData>(() => LoadModel(assets.FindAssetByName<MonoBehaviour>($"{raiderRoadID}_SkeletonData")!));
		AirRaiderBModel = new ProducerFn<ModelData>(() => LoadModel(assets.FindAssetByName<MonoBehaviour>($"{raiderAirBID}_SkeletonData")!));
		RoadRaiderBModel = new ProducerFn<ModelData>(() => LoadModel(assets.FindAssetByName<MonoBehaviour>($"{raiderRoadBID}_SkeletonData")!));
		AirGhostModel = new ProducerFn<ModelData>(() => LoadModel(assets.FindAssetByName<MonoBehaviour>($"{ghostAirID}_SkeletonData")!));
		RoadGhostModel = new ProducerFn<ModelData>(() => LoadModel(assets.FindAssetByName<MonoBehaviour>($"{ghostRoadID}_SkeletonData")!));
		HpMountModel = new ProducerFn<ModelData>(() => LoadModel(assets.FindAssetByName<MonoBehaviour>($"0002_hp_SkeletonData")!));

		// Populate animations

		BossAnims = new(getSpineController(MuseDash1SceneEnemyInfo.GetBoss(SceneInfo)));
		PopulateThreeSpeedPathwayAnimations(AirHeartAnims, RoadHeartAnims, static (in req) => MuseDash1SceneEnemyInfo.GetHeart(req.path, req.speed));
		PopulateThreeSpeedPathwayAnimations(AirScoreAnims, RoadScoreAnims, static (in req) => MuseDash1SceneEnemyInfo.GetScore(req.path, req.speed));
		PopulateThreeSpeedAnimations(MasherAnims, [EntityEnterDirection.RightSide, EntityEnterDirection.TopDown], static (in req) => MuseDash1SceneEnemyInfo.GetMasher(req.scene, req.dir, req.speed));
		PopulateThreeSpeedPathwayAnimations(AirGearAnims, RoadGearAnims, static (in req) => MuseDash1SceneEnemyInfo.GetGear(req.scene, req.path, req.speed));
		PopulateThreeSpeedPathwayAnimations(AirDoubleAnims, RoadDoubleAnims, static (in req) => MuseDash1SceneEnemyInfo.GetDouble(req.scene, req.path, req.speed));
		PopulateThreeSpeedPathwayAnimations(AirBoss1Anims, RoadBoss1Anims, static (in req) => MuseDash1SceneEnemyInfo.GetBoss1(req.scene, req.path, req.speed));
		PopulateThreeSpeedPathwayAnimations(AirBoss2Anims, RoadBoss2Anims, static (in req) => MuseDash1SceneEnemyInfo.GetBoss2(req.scene, req.path, req.speed));
		PopulateThreeSpeedPathwayAnimations(AirBoss3Anims, RoadBoss3Anims, static (in req) => MuseDash1SceneEnemyInfo.GetBoss3(req.scene, req.path, req.speed));
		PopulateThreeSpeedPathwayAnimations(AirBoss3Anims, RoadBoss3Anims, static (in req) => MuseDash1SceneEnemyInfo.GetBoss3(req.scene, req.path, req.speed));
		PopulateThreeSpeedPathwayAnimations(AirBossGearA_Anims, RoadBossGearA_Anims, static (in req) => MuseDash1SceneEnemyInfo.GetBossGear(req.scene, req.path, req.speed, false));
		PopulateThreeSpeedPathwayAnimations(AirBossGearB_Anims, RoadBossGearB_Anims, static (in req) => MuseDash1SceneEnemyInfo.GetBossGear(req.scene, req.path, req.speed, true));
		PopulateThreeSpeedAllDirsAnimations(AirSmallAnims, RoadSmallAnims, static (in req) => MuseDash1SceneEnemyInfo.GetSmall(req.scene, req.path, req.dir, req.speed));
		PopulateThreeSpeedAllDirsAnimations(AirMedium1Anims, RoadMedium1Anims, static (in req) => MuseDash1SceneEnemyInfo.GetMedium1(req.scene, req.path, req.dir, req.speed));
		PopulateThreeSpeedAllDirsAnimations(AirMedium2Anims, RoadMedium2Anims, static (in req) => MuseDash1SceneEnemyInfo.GetMedium2(req.scene, req.path, req.dir, req.speed));
		PopulateThreeSpeedPathwayAnimations(AirLarge1Anims, RoadLarge1Anims, static (in req) => MuseDash1SceneEnemyInfo.GetLarge1(req.scene, req.path, req.speed));
		PopulateThreeSpeedPathwayAnimations(AirLarge2Anims, RoadLarge2Anims, static (in req) => MuseDash1SceneEnemyInfo.GetLarge2(req.scene, req.path, req.speed));
		PopulateThreeSpeedPathwayAnimations(AirHammerA_Anims, RoadHammerA_Anims, static (in req) => MuseDash1SceneEnemyInfo.GetHammer(req.scene, req.path, req.speed, false));
		PopulateThreeSpeedPathwayAnimations(AirHammerB_Anims, RoadHammerB_Anims, static (in req) => MuseDash1SceneEnemyInfo.GetHammer(req.scene, req.path, req.speed, true));
		PopulateThreeSpeedPathwayAnimations(AirRaiderA_Anims, RoadRaiderA_Anims, static (in req) => MuseDash1SceneEnemyInfo.GetRaider(req.scene, req.path, req.speed, false));
		PopulateThreeSpeedPathwayAnimations(AirRaiderB_Anims, RoadRaiderB_Anims, static (in req) => MuseDash1SceneEnemyInfo.GetRaider(req.scene, req.path, req.speed, true));
		PopulateThreeSpeedPathwayAnimations(AirGhostAnims, RoadGhostAnims, static (in req) => MuseDash1SceneEnemyInfo.GetGhost(req.scene, req.path, req.speed));
	}

	public struct RequestInfo
	{
		public MuseDash1SceneInfo scene;
		public PathwaySide path;
		public EntityEnterDirection dir;
		public int speed;
	}

	MonoBehaviourReader getSpineController(string name) => new(MuseDash1Compatibility.StreamingAssets.FindAssetByName<GameObject>(name)!.GetComponentByName<MonoBehaviour>("SpineActionController")!);

	public delegate string ResolverFn(in RequestInfo info);

	void ProcessThreeSpeedAnimations(MD1_Animations3Speed table, in RequestInfo req, MonoBehaviourReader reader) {
		ref MD1_SpineActionControllerData speedToEdit = ref table.GetSpeedForEdit(req.speed, req.dir);
		speedToEdit = new(reader);
	}

	void PopulateThreeSpeedAnimations(MD1_Animations3Speed table, ResolverFn resolver) {
		RequestInfo req = new() {
			scene = SceneInfo
		};
		for (int i = 0; i < 3; i++) {
			req.speed = i + 1;
			string name = resolver(in req);
			var spine = getSpineController(name);
			ProcessThreeSpeedAnimations(table, in req, spine);
		}
	}
	void PopulateThreeSpeedAllDirsAnimations(MD1_Animations3Speed table, ResolverFn resolver) => PopulateThreeSpeedAnimations(table, [EntityEnterDirection.RightSide, EntityEnterDirection.TopDown, EntityEnterDirection.BottomUp], resolver);
	void PopulateThreeSpeedAllDirsAnimations(MD1_Animations3Speed top, MD1_Animations3Speed bottom, ResolverFn resolver) => PopulateThreeSpeedAnimations(top, bottom, [EntityEnterDirection.RightSide, EntityEnterDirection.TopDown, EntityEnterDirection.BottomUp], resolver);
	void PopulateThreeSpeedAnimations(MD1_Animations3Speed table, ReadOnlySpan<EntityEnterDirection> dirs, ResolverFn resolver) {
		foreach (var dir in dirs) {
			RequestInfo req = new() {
				scene = SceneInfo,
				dir = dir
			};

			for (int i = 0; i < 3; i++) {
				req.speed = i + 1;
				string name = resolver(in req);
				var spine = getSpineController(name);
				ProcessThreeSpeedAnimations(table, in req, spine);
			}
		}
	}

	void PopulateThreeSpeedAnimations(MD1_Animations3Speed top, MD1_Animations3Speed bottom, ReadOnlySpan<EntityEnterDirection> dirs, ResolverFn resolver) {
		foreach (var dir in dirs) {
			RequestInfo req = new() {
				scene = SceneInfo,
				dir = dir
			};

			req.path = PathwaySide.Top;
			for (int i = 0; i < 3; i++) {
				req.speed = i + 1;
				string name = resolver(in req);
				var spine = getSpineController(name);
				ProcessThreeSpeedAnimations(top, in req, spine);
			}

			req.path = PathwaySide.Bottom;
			for (int i = 0; i < 3; i++) {
				req.speed = i + 1;
				string name = resolver(in req);
				var spine = getSpineController(name);
				ProcessThreeSpeedAnimations(bottom, in req, spine);
			}
		}
	}
	void PopulateThreeSpeedPathwayAnimations(MD1_Animations3Speed top, MD1_Animations3Speed bottom, ResolverFn resolver) {
		RequestInfo req = new() {
			scene = SceneInfo
		};

		req.path = PathwaySide.Top;
		for (int i = 0; i < 3; i++) {
			req.speed = i + 1;
			string name = resolver(in req);
			var spine = getSpineController(name);
			ProcessThreeSpeedAnimations(top, in req, spine);
		}

		req.path = PathwaySide.Bottom;
		for (int i = 0; i < 3; i++) {
			req.speed = i + 1;
			string name = resolver(in req);
			var spine = getSpineController(name);
			ProcessThreeSpeedAnimations(bottom, in req, spine);
		}
	}

	public void RenderBackground() {
		Rlgl.PushMatrix();
		foreach (var renderer in sortedRenderers) renderer.Render(this);
		Rlgl.PopMatrix();
	}

	public void RenderPathway(PathwaySide side, float alpha, float size, float rotation) {
		var obj = ((SceneObject)pathwayInfo[(int)side].UserData!);
		var transform = obj.Transform;
		transform.LocalRotationX = 0; transform.LocalRotationY = 0;
		transform.LocalRotationZ = NMath.Remap(rotation, 0, 1, -1, 1);
		transform.LocalRotationW = 1;
		transform.LocalScaleX = size; transform.LocalScaleY = size;
		obj.Color.W = alpha / 255f;
	}

	public void Think(double scrollSpeed) {
		RunThinkFuncs(globals.CurTimeDelta * scrollSpeed);

		if (!Game.Paused && IValidatable.IsValid(pressIdle))
			audiosystem.UpdatePlayback(pressIdle);
	}

	public void Refresh() { }
	public void PlaySound(SceneSound sound, int hits) {
		switch (sound) {
			case SceneSound.Begin: audiosystem.PlaySound(BeginSound, in AudioPlaybackSettings.Unaltered); break;
			case SceneSound.Fever: audiosystem.PlaySound(FeverSound, in AudioPlaybackSettings.Unaltered); break;
			case SceneSound.Unpause: audiosystem.PlaySound(UnpauseSound, in AudioPlaybackSettings.Unaltered); break;
			case SceneSound.FullCombo: audiosystem.PlaySound(FullComboSound, in AudioPlaybackSettings.Unaltered); break;

			case SceneSound.HitSmall: audiosystem.PlaySound(Mezzo1Sound, in AudioPlaybackSettings.Unaltered); break;
			case SceneSound.HitMedium1: audiosystem.PlaySound(Mezzo1Sound, in AudioPlaybackSettings.Unaltered); break;
			case SceneSound.HitMedium2: audiosystem.PlaySound(Mezzo1Sound, in AudioPlaybackSettings.Unaltered); break;
			case SceneSound.HitLarge1: audiosystem.PlaySound(Piano2Sound, in AudioPlaybackSettings.Unaltered); break;
			case SceneSound.HitLarge2: audiosystem.PlaySound(Forte2Sound, in AudioPlaybackSettings.Unaltered); break;
			case SceneSound.HitRaider: audiosystem.PlaySound(Piano2Sound, in AudioPlaybackSettings.Unaltered); break;
			case SceneSound.HitHammer: audiosystem.PlaySound(Forte3Sound, in AudioPlaybackSettings.Unaltered); break;
			case SceneSound.HitGemini: audiosystem.PlaySound(Mezzo1Sound, in AudioPlaybackSettings.Unaltered); break;
			case SceneSound.StartedHold: audiosystem.PlaySound(PressTopSound, in AudioPlaybackSettings.Unaltered); break;
			case SceneSound.HitMasher: audiosystem.PlaySound(HitSounds![Math.Min(hits, HitSounds!.Length - 1)], in AudioPlaybackSettings.Unaltered); break;
			case SceneSound.HitBoss1: audiosystem.PlaySound(Mezzo1Sound, in AudioPlaybackSettings.Unaltered); break;
			case SceneSound.HitBoss2: audiosystem.PlaySound(Mezzo1Sound, in AudioPlaybackSettings.Unaltered); break;
			case SceneSound.HitBoss3: audiosystem.PlaySound(Mezzo1Sound, in AudioPlaybackSettings.Unaltered); break;
			case SceneSound.HitGhost: audiosystem.PlaySound(GhostSound, in AudioPlaybackSettings.Unaltered); break;
			case SceneSound.GotHeart: audiosystem.PlaySound(HpSound, in AudioPlaybackSettings.Unaltered); break;
			case SceneSound.GotScore: audiosystem.PlaySound(ScoreSound, in AudioPlaybackSettings.Unaltered); break;
			case SceneSound.HitBossFast: audiosystem.PlaySound(Forte2Sound, in AudioPlaybackSettings.Unaltered); break;
			case SceneSound.HitBossSlow: audiosystem.PlaySound(Forte2Sound, in AudioPlaybackSettings.Unaltered); break;
			case SceneSound.Victory: audiosystem.PlaySound(VictoryBgmSound, in AudioPlaybackSettings.Unaltered); break;
		}
	}

	public double GetBossAnimationTime(BossAnimationType type, AnimationHandler anim) {
		MD_ActionData? actions = GetBossAnimation(type);

		string? animation = actions?.ActionIdx?.FirstOrDefault() ?? type switch {
			BossAnimationType.MultiAttack => ActionKeys.BOSS_MULTI_ATK,
			BossAnimationType.MultiAttackEnd => ActionKeys.BOSS_MULTI_ATK_END,
			BossAnimationType.MultiAttackHurt => ActionKeys.BOSS_MULTI_HURT,
			BossAnimationType.MultiAttackHurtEnd => ActionKeys.BOSS_MULTI_ATK_END,
			_ => null
		};

		if (animation == null)
			return 0;

		var animObj = anim.GetModelData()?.FindAnimation(animation);
		// This is a REALLY dumb way of doing it. There *has* to be a better way.
		// I don't think these frame times are standardized. I also don't know where they're stored.
		// if you run into this code and know, let me know =)
		if (TryFindFirstTwoDigitNumber(animObj?.Name, out int value, out _))
			// Muse Dash uses 30fps as a reference
			return value / 30d;
		return 0;
	}

	public double PlayBossAnimation(int channel, BossAnimationType type, AnimationHandler anim) {
		MD_ActionData? actions = GetBossAnimation(type);
		if (actions == null) {
			// This is a hack... figure out why this happens
			string? animation = type switch {
				BossAnimationType.MultiAttack => ActionKeys.BOSS_MULTI_ATK,
				BossAnimationType.MultiAttackEnd => ActionKeys.BOSS_MULTI_ATK_END,
				BossAnimationType.MultiAttackHurt => ActionKeys.BOSS_MULTI_HURT,
				BossAnimationType.MultiAttackHurtEnd => ActionKeys.BOSS_MULTI_ATK_END,
				_ => null
			};

			if (animation == null)
				return 0;

			anim.SetAnimation(channel, animation);
			return anim.GetModelData()?.FindAnimation(animation)?.Duration ?? 0;
		}
		anim.ClearAnimation(channel);
		for (int i = 0; i < actions.ActionIdx.Length; i++)
			if (i == 0)
				anim.SetAnimation(channel, actions.ActionIdx[i], i == (actions.ActionIdx.Length - 1));
			else
				anim.AddAnimation(channel, actions.ActionIdx[i], i == (actions.ActionIdx.Length - 1));

		return anim.GetModelData()?.FindAnimation(actions?.ActionIdx?.FirstOrDefault())?.Duration ?? 0;
	}

	public MD_ActionData? GetBossAnimation(BossAnimationType type) {
		MD_ActionData? actions = null;
		switch (type) {
			case BossAnimationType.Standby0: actions = BossAnims.Get(ActionKeys.STAND); break;
			case BossAnimationType.In: actions = BossAnims.Get(ActionKeys.COMEIN); break;
			case BossAnimationType.Out: actions = BossAnims.Get(ActionKeys.COMEOUT); break;
			case BossAnimationType.CloseAttackSlow: actions = BossAnims.Get(ActionKeys.NEAR_ATTACK_1); break;
			case BossAnimationType.CloseAttackFast: actions = BossAnims.Get(ActionKeys.NEAR_ATTACK_2); break;
			case BossAnimationType.Hurt: actions = BossAnims.Get(ActionKeys.BOSS_HURT); break;
			case BossAnimationType.From0To1: actions = BossAnims.Get(ActionKeys.FAR_ATTACK1_START); break;
			case BossAnimationType.AttackAir1: actions = BossAnims.Get(ActionKeys.FAR_ATTACK1_LEFT); break;
			case BossAnimationType.AttackGround1: actions = BossAnims.Get(ActionKeys.FAR_ATTACK1_RIGHT); break;
			case BossAnimationType.From1To0: actions = BossAnims.Get(ActionKeys.FAR_ATTACK1_END); break;
			case BossAnimationType.From0To2: actions = BossAnims.Get(ActionKeys.FAR_ATTACK2_START); break;
			case BossAnimationType.AttackAir2: actions = BossAnims.Get(ActionKeys.FAR_ATTACK2); break;
			case BossAnimationType.AttackGround2: actions = BossAnims.Get(ActionKeys.FAR_ATTACK2); break;
			case BossAnimationType.From2To0: actions = BossAnims.Get(ActionKeys.FAR_ATTACK2_END); break;
			case BossAnimationType.From1To2: actions = BossAnims.Get(ActionKeys.BOSS_ATK_1_TO_2); break;
			case BossAnimationType.From2To1: actions = BossAnims.Get(ActionKeys.BOSS_ATK_2_TO_1); break;
		}
		return actions;
	}

	public string? GetEnemyApproachAnimation(DashEnemy enemy, out double time) {
		time = 0;

		MD1_SpineActionControllerData? anim = null;

		switch (enemy.Type) {
			case EntityType.Single: {
					anim = enemy.Variant switch {
						EntityVariant.Boss1 => Pathway.ValueDependantOnPathway(enemy.Pathway, AirBoss1Anims, RoadBoss1Anims).GetSpeed(enemy.Speed),
						EntityVariant.Boss2 => Pathway.ValueDependantOnPathway(enemy.Pathway, AirBoss2Anims, RoadBoss2Anims).GetSpeed(enemy.Speed),
						EntityVariant.Boss3 => Pathway.ValueDependantOnPathway(enemy.Pathway, AirBoss3Anims, RoadBoss3Anims).GetSpeed(enemy.Speed),

						EntityVariant.Small => Pathway.ValueDependantOnPathway(enemy.Pathway, AirSmallAnims, RoadSmallAnims).GetSpeed(enemy.Speed, enemy.EnterDirection),

						EntityVariant.Medium1 => Pathway.ValueDependantOnPathway(enemy.Pathway, AirMedium1Anims, RoadMedium1Anims).GetSpeed(enemy.Speed, enemy.EnterDirection),
						EntityVariant.Medium2 => Pathway.ValueDependantOnPathway(enemy.Pathway, AirMedium2Anims, RoadMedium2Anims).GetSpeed(enemy.Speed, enemy.EnterDirection),

						EntityVariant.Large1 => Pathway.ValueDependantOnPathway(enemy.Pathway, AirLarge1Anims, RoadLarge1Anims).GetSpeed(enemy.Speed),
						EntityVariant.Large2 => Pathway.ValueDependantOnPathway(enemy.Pathway, AirLarge2Anims, RoadLarge2Anims).GetSpeed(enemy.Speed),

						_ => null
					};
					break;
				}
			case EntityType.Gear: {
					anim = enemy.Variant switch {
						EntityVariant.Boss1 => Pathway.ValueDependantOnPathway(enemy.Pathway, AirBossGearA_Anims, RoadBossGearA_Anims).GetSpeed(enemy.Speed),
						EntityVariant.Boss2 => Pathway.ValueDependantOnPathway(enemy.Pathway, AirBossGearB_Anims, RoadBossGearB_Anims).GetSpeed(enemy.Speed),
						_ => Pathway.ValueDependantOnPathway(enemy.Pathway, AirGearAnims, RoadGearAnims).GetSpeed(enemy.Speed)
					};
					break;
				}
			case EntityType.Masher: {
					anim = MasherAnims.GetSpeed(enemy.Speed, enemy.EnterDirection);
					break;
				}
			case EntityType.Double: anim = Pathway.ValueDependantOnPathway(enemy.Pathway, AirDoubleAnims, RoadDoubleAnims).GetSpeed(enemy.Speed); break;
			case EntityType.Heart: anim = Pathway.ValueDependantOnPathway(enemy.Pathway, AirHeartAnims, RoadHeartAnims).GetSpeed(enemy.Speed); break;
			case EntityType.Score: anim = Pathway.ValueDependantOnPathway(enemy.Pathway, AirScoreAnims, RoadScoreAnims).GetSpeed(enemy.Speed); break;
			case EntityType.Ghost: anim = Pathway.ValueDependantOnPathway(enemy.Pathway, AirGhostAnims, RoadGhostAnims).GetSpeed(enemy.Speed); break;
			case EntityType.Hammer:
				if (enemy.Flipped)
					anim = Pathway.ValueDependantOnPathway(enemy.Pathway, AirHammerB_Anims, RoadHammerB_Anims).GetSpeed(enemy.Speed, enemy.EnterDirection);
				else
					anim = Pathway.ValueDependantOnPathway(enemy.Pathway, AirHammerA_Anims, RoadHammerA_Anims).GetSpeed(enemy.Speed, enemy.EnterDirection);
				break;
			case EntityType.Raider:
				if (enemy.Flipped)
					anim = Pathway.ValueDependantOnPathway(enemy.Pathway, AirRaiderB_Anims, RoadRaiderB_Anims).GetSpeed(enemy.Speed, enemy.EnterDirection);
				else
					anim = Pathway.ValueDependantOnPathway(enemy.Pathway, AirRaiderA_Anims, RoadRaiderA_Anims).GetSpeed(enemy.Speed, enemy.EnterDirection);
				break;
				// default: throw new NotImplementedException($"{enemy.Type} isn't implemented yet");
		}

		if (anim == null)
			return null;

		string? a = anim.Get("in")?.ActionIdx?.FirstOrDefault();
		// This is a REALLY dumb way of doing it. There *has* to be a better way.
		// I don't think these frame times are standardized. I also don't know where they're stored.
		// if you run into this code and know, let me know =)
		if (TryFindFirstTwoDigitNumber(a, out int value, out _))
			// Muse Dash uses 30fps as a reference
			time = value / 30d;

		return a;
	}
	public static bool TryFindFirstTwoDigitNumber(ReadOnlySpan<char> span, out int value, out int index) {
		for (int i = 0; i < span.Length - 1; i++) {
			char c1 = span[i];
			char c2 = span[i + 1];

			if ((uint)(c1 - '0') <= 9 && (uint)(c2 - '0') <= 9) {
				value = (c1 - '0') * 10 + (c2 - '0');
				index = i;
				return true;
			}
		}

		value = 0;
		index = -1;
		return false;
	}

	private MD1_Animations3Speed fromVariantSHE(EntityVariant variant, PathwaySide pathway) => variant switch {
		EntityVariant.Boss1 => pathway == PathwaySide.Top ? AirBoss1Anims : RoadBoss1Anims,
		EntityVariant.Boss2 => pathway == PathwaySide.Top ? AirBoss2Anims : RoadBoss2Anims,
		EntityVariant.Boss3 => pathway == PathwaySide.Top ? AirBoss3Anims : RoadBoss3Anims,

		EntityVariant.Small => pathway == PathwaySide.Top ? AirSmallAnims : RoadSmallAnims,

		EntityVariant.Medium1 => pathway == PathwaySide.Top ? AirMedium1Anims : RoadMedium1Anims,
		EntityVariant.Medium2 => pathway == PathwaySide.Top ? AirMedium2Anims : RoadMedium2Anims,

		EntityVariant.Large1 => pathway == PathwaySide.Top ? AirLarge1Anims : RoadLarge1Anims,
		EntityVariant.Large2 => pathway == PathwaySide.Top ? AirLarge2Anims : RoadLarge2Anims,

		_ => throw new Exception()
	};


	public string? GetEnemyHitAnimation(DashEnemy enemy, HitAnimationType type) {
		string request = type == HitAnimationType.Great ? ActionKeys.COMEOUT2 : ActionKeys.COMEOUT3;
		MD_ActionData? anim = null;
		switch (enemy.Type) {
			case EntityType.Single: anim = fromVariantSHE(enemy.Variant, enemy.Pathway).GetSpeed(enemy.Speed, enemy.EnterDirection).Get(request); break;
			case EntityType.Double: anim = Pathway.ValueDependantOnPathway(enemy.Pathway, AirDoubleAnims, RoadDoubleAnims).GetSpeed(enemy.Speed, enemy.EnterDirection).Get(request); break;
			case EntityType.Masher: anim = MasherAnims.GetSpeed(enemy.Speed, enemy.EnterDirection).Get(request); break;
			case EntityType.Ghost: anim = Pathway.ValueDependantOnPathway(enemy.Pathway, AirGhostAnims, RoadGhostAnims).GetSpeed(enemy.Speed, enemy.EnterDirection).Get(request); break;
			case EntityType.Hammer:
				if (enemy.Flipped)
					anim = Pathway.ValueDependantOnPathway(enemy.Pathway, AirHammerB_Anims, RoadHammerB_Anims).GetSpeed(enemy.Speed, enemy.EnterDirection).Get(request);
				else
					anim = Pathway.ValueDependantOnPathway(enemy.Pathway, AirHammerA_Anims, RoadHammerA_Anims).GetSpeed(enemy.Speed, enemy.EnterDirection).Get(request);
				break;
			case EntityType.Heart:
			case EntityType.Score:
				return "out"; // todo
			case EntityType.Raider:
				if (enemy.Flipped)
					anim = Pathway.ValueDependantOnPathway(enemy.Pathway, AirRaiderB_Anims, RoadRaiderB_Anims).GetSpeed(enemy.Speed, enemy.EnterDirection).Get(request);
				else
					anim = Pathway.ValueDependantOnPathway(enemy.Pathway, AirRaiderA_Anims, RoadRaiderA_Anims).GetSpeed(enemy.Speed, enemy.EnterDirection).Get(request);
				break;
		}

		if (anim == null)
			return null;

		return anim.ActionIdx.FirstOrDefault();
	}

	public ModelData? GetEnemyModel(DashEnemy enemy) {
		switch (enemy.Type) {
			case EntityType.Boss: return BossModel;
			case EntityType.Single:
				return enemy.Variant switch {
					EntityVariant.Boss1 => enemy.Pathway == PathwaySide.Top ? AirBoss1Model : RoadBoss1Model,
					EntityVariant.Boss2 => enemy.Pathway == PathwaySide.Top ? AirBoss2Model : RoadBoss2Model,
					EntityVariant.Boss3 => enemy.Pathway == PathwaySide.Top ? AirBoss3Model : RoadBoss3Model,
					EntityVariant.Small => enemy.Pathway == PathwaySide.Top ? AirSmallModel : RoadSmallModel,
					EntityVariant.Medium1 => enemy.Pathway == PathwaySide.Top ? AirMedium1Model : RoadMedium1Model,
					EntityVariant.Medium2 => enemy.Pathway == PathwaySide.Top ? AirMedium2Model : RoadMedium2Model,
					EntityVariant.Large1 => enemy.Pathway == PathwaySide.Top ? AirLarge1Model : RoadLarge1Model,
					EntityVariant.Large2 => enemy.Pathway == PathwaySide.Top ? AirLarge2Model : RoadLarge2Model,
					_ => throw new NotImplementedException()
				};
			case EntityType.Gear:
				return enemy.Variant switch {
					EntityVariant.Boss1 => enemy.Pathway == PathwaySide.Top ? AirBossGearModel : RoadBossGearModel,
					EntityVariant.Boss2 => enemy.Pathway == PathwaySide.Top ? AirBossGearModel : RoadBossGearModel,
					_ => enemy.Pathway == PathwaySide.Top ? AirGearModel : RoadGearModel,
				};
			case EntityType.Double: return enemy.Pathway == PathwaySide.Top ? AirDoubleModel : RoadDoubleModel;
			case EntityType.Ghost: return enemy.Pathway == PathwaySide.Top ? AirGhostModel : RoadGhostModel;
			case EntityType.Hammer: return enemy.Flipped ? (enemy.Pathway == PathwaySide.Top ? AirHammerBModel : RoadHammerBModel) : (enemy.Pathway == PathwaySide.Top ? AirHammerModel : RoadHammerModel);
			case EntityType.Masher: return MasherModel;
			case EntityType.Raider: return enemy.Flipped ? (enemy.Pathway == PathwaySide.Top ? AirRaiderBModel : RoadRaiderBModel) : (enemy.Pathway == PathwaySide.Top ? AirRaiderModel : RoadRaiderModel);
			case EntityType.Heart: return enemy.Pathway == PathwaySide.Top ? AirHeartModel : RoadHeartModel;
			case EntityType.Score: return enemy.Pathway == PathwaySide.Top ? AirScoreModel : RoadScoreModel;
			default: throw new NotImplementedException();
		}
	}
	public ModelData? GetHP(out string? mountAnimation) {
		if (HpMountModel == null)
			mountAnimation = null;
		else
			mountAnimation = "in_mount"; // TODO - probably not consistent
		return HpMountModel;
	}
	public BoneInstance? GetHPMount(DashEnemyVisuals enemy) => enemy.Model?.FindBone("hp");
	public string GetMasherHitAnimation(int speed, EntityEnterDirection dir) {
		var s = MasherAnims.GetSpeed(speed, dir).Get(ActionKeys.MUL_HURT)?.ActionIdx;
		return s?[Random.Shared.Next(0, s.Length)] ?? "";
	}
	public ref readonly PathwayInformation GetPathwayInformation(PathwaySide pathway) => ref pathwayInfo[(int)pathway];
	public Color GetPathwayColor(PathwaySide side) => GetPathwayInformation(side).Color;
	public Vector2F GetPathwayPosition(PathwaySide side) => GetPathwayInformation(side).Position;
	public IAudioClip? GetPressIdleSound() => PressIdleSound;
	public void GetSustainResources(PathwaySide pathway, out ITexture? start, out ITexture? end, out ITexture? body, out ITexture? up, out ITexture? down, out float rotationDegsPerSecond) {
		rotationDegsPerSecond = 120;
		switch (pathway) {
			case PathwaySide.Top:
				start = AirStartSustainTexture;
				end = AirEndSustainTexture;
				body = AirBodySustainTexture;
				up = AirUpSustainTexture;
				down = AirDownSustainTexture;
				break;
			case PathwaySide.Bottom:
				start = RoadStartSustainTexture;
				end = RoadEndSustainTexture;
				body = RoadBodySustainTexture;
				up = RoadUpSustainTexture;
				down = RoadDownSustainTexture;
				break;
			default:
				throw new Exception();
		}
	}

	public void OnPressStateChange(bool startSustaining, bool wasSustaining) {
		if (startSustaining != wasSustaining) {
			var clip = GetPressIdleSound();
			if (IValidatable.IsValid(clip)) {
				if (audiosystem.IsPlaybackActive(pressIdle) && !startSustaining)
					audiosystem.DestroyPlayback(pressIdle);
				else if (!audiosystem.IsPlaybackActive(pressIdle) && startSustaining) {
					pressIdle = audiosystem.CreatePlayback(clip, AudioPlaybackSettings.Unaltered with { Stream = true, Looping = true, ManuallyUpdate = true });
					audiosystem.PlaySound(pressIdle);
				}
			}
		}
	}

	public void Activate(IMuseDash1SceneInstance? transitioningTo) { }
	public void Deactivate(IMuseDash1SceneInstance? transitioningFrom) { }
	public int GetSceneArrayIndex() => arrayIndex;
	public void SetSceneArrayIndex(int idx) => arrayIndex = idx;
	public IGame GetGame() => Game;
	public ISceneDescriptor GetScene() => Descriptor;

	public void RenderOverlay() {
		Conductor conductor = Game.Conductor;
		var time = conductor.Time;


	}

	public IMuseDash1SceneUI CreateUI() => SceneInfo.UIFactory(this);
}

class StatisticsPanel(IGame game, StatisticsData stats) : Panel()
{
	ICharacterVictoryInstance victory = null!;
	ISongChart? chart;
	double start = 0;
	double Time() => globals.CurTime - start;

	protected override void Initialize() {
		chart = game.GetSongChart();
		if (chart == null) return;
		start = globals.CurTime;

		ICharacterDescriptor? character = CharacterMod.GetCharacterData();
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

		var restart = bottom.Add<Nucleus.UI.Button>();
		restart.DynamicallySized = true;
		restart.Size = new(.2f);
		restart.Text = "Restart";
		restart.Dock = Dock.Left;
		restart.MouseReleaseEvent += (_, _, _) => {
			// TODO: Probably should just hard restart it...
			// Maybe seeking is stable enough now to justify this though?
			game.Restart();
			this.Remove();
		};

		var back = bottom.Add<Nucleus.UI.Button>();
		back.DynamicallySized = true;
		back.Size = new(.2f);
		back.Text = "Main Menu";
		back.Dock = Dock.Right;
		back.MouseReleaseEvent += (_, _, _) => LevelTransitions.LoadMainMenu();

		BorderSize = 0;
	}
	void RenderOneLine(ReadOnlySpan<char> line, int fs, ref int y) {
		Graphics2D.DrawText(16, 16 + y, line, Graphics2D.UI_FONT_NAME, fs);
		y += fs + 4;
	}
	public override void Paint(float width, float height) {
		BackgroundColor = new(0, 0, 0, (int)(220 * (float)NMath.Ease.OutQuad(NMath.Remap(Time(), 0, 0.5, 0, 1, true))));
		base.Paint(width, height);

		Vector2F position = new(width / 2, (1 - (float)NMath.Ease.OutElastic(Math.Clamp(Time() * 0.2, 0, 1))) * (height));
		EngineCore.Window.BeginMode2D(new() {
			Zoom = height / 900 / 2.4f,
			Offset = (new Vector2F(width / 2, height / 1)).ToNumerics()
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