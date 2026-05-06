using AssetStudio;
using CloneDash.Common.Gamemodes.MuseDash;
using CloneDash.Common.Gamemodes.MuseDash.V1;
using CloneDash.Compatibility.MuseDash;
using CloneDash.Compatibility.Unity;
using CloneDash.Game;
using CloneDash.Game.Entities;
using CloneDash.Settings;
using DiscordRPC;
using NAudio.CoreAudioApi;
using Nucleus;
using Nucleus.Audio;
using Nucleus.Common.Audio;
using Nucleus.Common.Graphics;
using Nucleus.Engine;
using Nucleus.ManagedMemory;
using Nucleus.Models.Runtime;
using Nucleus.Types;
using Nucleus.Util;
using OggVorbisEncoder;
using Raylib_cs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Color = Nucleus.Common.Types.Color;
using Texture = Nucleus.ManagedMemory.Texture;
using Texture2D = AssetStudio.Texture2D;
using Transform = AssetStudio.Transform;

namespace CloneDash.Scenes;

public struct MuseDashSceneSounds
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


	public static readonly MuseDashSceneSounds Default = new MuseDashSceneSounds() {
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

	public MuseDashSceneSounds AutoGenHitSounds() {
		for (int i = 0; i < ((Span<string?>)HitSounds).Length; i++) {
			HitSounds[i] = $"hitsound_{i:000}";
		}
		return this;
	}
	public static MuseDashSceneSounds operator +(MuseDashSceneSounds a, MuseDashSceneSounds b) {
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

public class MD_Animations3Speed
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

public static class MuseDashSceneEnemyInfo
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

	public static string GetBoss(MuseDashSceneInfo scene)
		=> $"{scene.MapIdx:00}{CODE_BOSS}_boss";

	public static string GetSustainTop(MuseDashSceneInfo scene, PathwaySide side)
		=> $"{scene.MapIdx:00}{CODE_SUSTAIN}_{PATHWAY(side)}_top";
	public static string GetSustainBody(MuseDashSceneInfo scene, PathwaySide side)
		=> $"{scene.MapIdx:00}{CODE_SUSTAIN}_{PATHWAY(side)}_body";
	public static string GetSustainNoteUp(MuseDashSceneInfo scene, PathwaySide side)
		=> $"{scene.MapIdx:00}{CODE_SUSTAIN}_{PATHWAY(side)}_note_up";
	public static string GetSustainNoteDown(MuseDashSceneInfo scene, PathwaySide side)
		=> $"{scene.MapIdx:00}{CODE_SUSTAIN}_{PATHWAY(side)}_note_down";

	public static string GetGear(MuseDashSceneInfo scene, PathwaySide side, int speed)
		=> $"{scene.MapIdx:00}{CODE_GEARS}{speed + (side == PathwaySide.Top ? 3 : 0):00}_{PATHWAY(side)}_nor_{SPEED(speed)}";

	public static string GetMasher(MuseDashSceneInfo scene, EntityEnterDirection direction, int speed)
		=> $"{scene.MapIdx:00}{CODE_MASHERS}{speed + (direction == EntityEnterDirection.TopDown ? 3 : 0):00}_{DIRECTION(direction)}_{SPEED(speed)}";

	public static string GetDouble(MuseDashSceneInfo scene, PathwaySide side, int speed)
		=> $"{scene.MapIdx:00}{CODE_DOUBLES}{speed + (side == PathwaySide.Top ? 3 : 0):00}_{PATHWAY(side)}_nor_{SPEED(speed)}";

	public static string GetBoss1(MuseDashSceneInfo scene, PathwaySide side, int speed)
			=> $"{scene.MapIdx:00}{CODE_BOSS1}{speed + (side == PathwaySide.Top ? 3 : 0):00}_{PATHWAY(side)}_nor_{SPEED(speed)}";
	public static string GetBoss2(MuseDashSceneInfo scene, PathwaySide side, int speed)
		=> $"{scene.MapIdx:00}{CODE_BOSS2}{speed + (side == PathwaySide.Top ? 3 : 0):00}_{PATHWAY(side)}_nor_{SPEED(speed)}";
	public static string GetBoss3(MuseDashSceneInfo scene, PathwaySide side, int speed)
		=> $"{scene.MapIdx:00}{CODE_BOSS3}{speed + (side == PathwaySide.Top ? 3 : 0):00}_{PATHWAY(side)}_nor_{SPEED(speed)}";

	public static string GetBossGear(MuseDashSceneInfo scene, PathwaySide side, int speed, bool second)
			=> $"{scene.MapIdx:00}{CODE_BOSSGEARS}{speed + (second ? 6 : 0) + (side == PathwaySide.Top ? 3 : 0):00}_{PATHWAY(side)}_{(second ? 2 : 1)}_nor_{SPEED(speed)}";

	public static string GetSmall(MuseDashSceneInfo scene, PathwaySide side, EntityEnterDirection dir, int speed)
		=> $"{scene.MapIdx:00}{CODE_SMALL}{speed + (dir switch { EntityEnterDirection.RightSide => 0, EntityEnterDirection.TopDown => 12, EntityEnterDirection.BottomUp => 6, _ => throw new NotImplementedException() }) + (side == PathwaySide.Top ? 3 : 0):00}_{PATHWAY(side)}_{DIRECTION(dir)}_{SPEED(speed)}";

	public static string GetMedium1(MuseDashSceneInfo scene, PathwaySide side, EntityEnterDirection dir, int speed)
		=> $"{scene.MapIdx:00}{CODE_MEDIUM1}{speed + (dir switch { EntityEnterDirection.RightSide => 0, EntityEnterDirection.TopDown => 12, EntityEnterDirection.BottomUp => 6, _ => throw new NotImplementedException() }) + (side == PathwaySide.Top ? 3 : 0):00}_{PATHWAY(side)}_{DIRECTION(dir)}_{SPEED(speed)}";

	public static string GetMedium2(MuseDashSceneInfo scene, PathwaySide side, EntityEnterDirection dir, int speed)
		=> $"{scene.MapIdx:00}{CODE_MEDIUM2}{speed + (dir switch { EntityEnterDirection.RightSide => 0, EntityEnterDirection.TopDown => 12, EntityEnterDirection.BottomUp => 6, _ => throw new NotImplementedException() }) + (side == PathwaySide.Top ? 3 : 0):00}_{PATHWAY(side)}_{DIRECTION(dir)}_{SPEED(speed)}";

	public static string GetLarge1(MuseDashSceneInfo scene, PathwaySide side, int speed)
		=> $"{scene.MapIdx:00}{CODE_LARGE1}{speed + (side == PathwaySide.Top ? 3 : 0):00}_{PATHWAY(side)}_nor_{SPEED(speed)}";

	public static string GetLarge2(MuseDashSceneInfo scene, PathwaySide side, int speed)
		=> $"{scene.MapIdx:00}{CODE_LARGE2}{speed + (side == PathwaySide.Top ? 3 : 0):00}_{PATHWAY(side)}_nor_{SPEED(speed)}";

	public static string GetHammer(MuseDashSceneInfo scene, PathwaySide side, int speed, bool reversed)
		=> $"{scene.MapIdx:00}{CODE_HAMMER}{speed + (reversed ? 6 : 0) + (side == PathwaySide.Top ? 3 : 0):00}_{PATHWAY(side)}_{(reversed ? "up" : "down")}_{SPEED(speed)}";

	public static string GetRaider(MuseDashSceneInfo scene, PathwaySide side, int speed, bool reversed)
		=> $"{scene.MapIdx:00}{CODE_RAIDER}{speed + (reversed ? 6 : 0) + (side == PathwaySide.Top ? 3 : 0):00}_{PATHWAY(side)}_{(reversed ? "down" : "up")}_{SPEED(speed)}";

	public static string GetGhost(MuseDashSceneInfo scene, PathwaySide side, int speed)
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
public record class MuseDashSceneInfo
{
	readonly static Dictionary<ulong, MuseDashSceneInfo> scenes = [];

	public readonly string MapName;
	public readonly string OfficialName;
	public readonly int MapIdx;

	public MuseDashSceneSounds Sounds = MuseDashSceneSounds.Default;
	public bool Unusable;
	public MuseDashSceneInfo MarkUnusable() {
		Unusable = true;
		return this;
	}
	public MuseDashSceneInfo WithSounds(MuseDashSceneSounds sounds) {
		Sounds += sounds;
		return this;
	}
	public static IEnumerable<MuseDashSceneInfo> GetScenes() => scenes.Values;

	public MuseDashSceneInfo(int idx, string officialName, [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format = "scene_{0:00}") {
		MapIdx = idx;
		MapName = string.Format(format, idx);
		OfficialName = officialName;

		scenes[MapName.Hash()] = this;
	}

	public static MuseDashSceneInfo? GetSceneInfo(ReadOnlySpan<char> name) {
		ulong hash = name.Hash();
		if (scenes.TryGetValue(hash, out var ret))
			return ret;
		return null;
	}

	// TODO: Verify these names properly
	// Wiki is probably a horrible source for ~50% of these
	public static readonly MuseDashSceneInfo SpaceStation = new MuseDashSceneInfo(1, "Space Station");
	public static readonly MuseDashSceneInfo RetroCity = new MuseDashSceneInfo(2, "Retro City");
	public static readonly MuseDashSceneInfo Castle = new MuseDashSceneInfo(3, "Castle");
	public static readonly MuseDashSceneInfo RainyNight = new MuseDashSceneInfo(4, "Rainy Night");
	public static readonly MuseDashSceneInfo Candyland = new MuseDashSceneInfo(5, "Candyland");
	public static readonly MuseDashSceneInfo Oriental = new MuseDashSceneInfo(6, "Oriental");
	public static readonly MuseDashSceneInfo GrooveCoaster = new MuseDashSceneInfo(7, "Groove Coaster")
												.WithSounds(new MuseDashSceneSounds {
													Begin = "sfx_readygo_gc",
													Ghost = "sfx_ghost_gc"
												});
	public static readonly MuseDashSceneInfo Gensokyo = new MuseDashSceneInfo(8, "Gensokyo");
	public static readonly MuseDashSceneInfo GameGraveyard = new MuseDashSceneInfo(9, "Game Graveyard");
	public static readonly MuseDashSceneInfo Museland = new MuseDashSceneInfo(10, "Museland", "scene_{0:00}_miku");
	public static readonly MuseDashSceneInfo Mirrorland = new MuseDashSceneInfo(10, "Mirrorland", "scene_{0:00}_rin_len");
	public static readonly MuseDashSceneInfo Warriorland = new MuseDashSceneInfo(11, "Warriorland")
												.MarkUnusable();
	public static readonly MuseDashSceneInfo JadeTemple = new MuseDashSceneInfo(12, "Jade Temple")
												.MarkUnusable();
}

public class MuseDashScene : BaseMuseDash1UnitySimScene, ISceneDescriptor
{
	public const float MUSEDASH_MULTIPLIER_POSITIONS = 1;
	readonly PathwayInformation[] pathwayInfo = new PathwayInformation[4];
	public readonly MuseDashSceneInfo SceneInfo;

	public MuseDashScene(MuseDashSceneInfo info) {
		SceneInfo = info;
	}

	public static MuseDashScene? GetScene(ReadOnlySpan<char> name) {
		var sceneInfo = MuseDashSceneInfo.GetSceneInfo(name);
		if (sceneInfo == null)
			return null;

		if (sceneInfo.Unusable) {
			Logs.Warn($"The scene '{sceneInfo.OfficialName}' is currently broken, so 'Space Station' will be selected as a fallback for this scene.");
			return GetScene("scene_01"); // Fall back to Space Station...
		}

		var sceneGameObject = MuseDash1Compatibility.StreamingAssets.FindAssetByName<GameObject>(sceneInfo.MapName)!;
		var sceneSubControl = new MonoBehaviourReader(
			sceneGameObject.GetComponentByName<MonoBehaviour>("SceneSubControl")
			?? throw new NullReferenceException("No scene control?"));

		var scenePoint = sceneSubControl.Get<GameObject>("scenePoint");
		var transform = scenePoint!.GetFirstComponent<Transform>()!;

		var scene = new MuseDashScene(sceneInfo);
		var pathwaysObject = scene.ImportGameObject(scenePoint, null);

		var pathwayChildren = new List<(SceneObject obj, Vector3 pos)>();
		foreach (var child in pathwaysObject.Transform.Children) {
			child.Object.Transform.ComputeGlobalTransform(out var pos, out _);
			pathwayChildren.Add((child.Object, pos));
		}

		if (pathwayChildren.Count >= 2) {
			pathwayChildren.Sort((a, b) => b.pos.Y.CompareTo(a.pos.Y));
			scene.AssignPathway(PathwaySide.Top, pathwayChildren[0].obj, pathwayChildren[0].pos);
			scene.AssignPathway(PathwaySide.Bottom, pathwayChildren[1].obj, pathwayChildren[1].pos);
		}
		else if (pathwayChildren.Count == 1) {
			scene.AssignPathway(PathwaySide.Bottom, pathwayChildren[0].obj, pathwayChildren[0].pos);
		}

		var rootTransform = sceneGameObject.GetFirstComponent<Transform>()!;
		scene.root = scene.ImportGameObject(rootTransform.GetGameObject()!, null);

		foreach (var obj in scene.allObjects) obj.Awake();

		foreach (var obj in scene.allObjects)
			foreach (var anim in obj.GetComponents<SceneAnimator>())
				scene.animators.Add(anim);

		scene.BuildRenderOrder();

		scene.pathwayInfo[(int)PathwaySide.Both] = new() {
			Position = (scene.pathwayInfo[(int)PathwaySide.Top].Position +
						scene.pathwayInfo[(int)PathwaySide.Bottom].Position) / 2,
			Color = Pathway.PATHWAY_DUAL_COLOR
		};
		scene.pathwayInfo[(int)PathwaySide.Top].Color = Pathway.PATHWAY_TOP_COLOR;
		scene.pathwayInfo[(int)PathwaySide.Bottom].Color = Pathway.PATHWAY_BOTTOM_COLOR;
		return scene;
	}

	private void AssignPathway(PathwaySide side, SceneObject obj, Vector3 pos) {
		pathwayInfo[(int)side] = new(pos.X * MUSEDASH_MULTIPLIER_POSITIONS, pos.Y * MUSEDASH_MULTIPLIER_POSITIONS, obj);
	}


	ModelData? BossModel;
	ModelData? AirGearModel, RoadGearModel;
	ModelData? MasherModel;
	ModelData? AirHeartModel, RoadHeartModel;
	ModelData? AirScoreModel, RoadScoreModel;
	ModelData? AirDoubleModel, RoadDoubleModel;

	ModelData? AirBoss1Model, RoadBoss1Model;
	ModelData? AirBoss2Model, RoadBoss2Model;
	ModelData? AirBoss3Model, RoadBoss3Model;

	ModelData? AirBossGearModel, RoadBossGearModel;

	ModelData? AirSmallModel, RoadSmallModel;
	ModelData? AirMedium1Model, RoadMedium1Model;
	ModelData? AirMedium2Model, RoadMedium2Model;
	ModelData? AirLarge1Model, RoadLarge1Model;
	ModelData? AirLarge2Model, RoadLarge2Model;
	ModelData? AirHammerModel, RoadHammerModel, AirHammerBModel, RoadHammerBModel;
	ModelData? AirRaiderModel, RoadRaiderModel, AirRaiderBModel, RoadRaiderBModel;
	ModelData? AirGhostModel, RoadGhostModel;

	ModelData? HpMountModel;

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

	MD_Animations3Speed AirGearAnims = new(), RoadGearAnims = new();
	MD_Animations3Speed MasherAnims = new();
	MD_Animations3Speed AirHeartAnims = new(), RoadHeartAnims = new();
	MD_Animations3Speed AirScoreAnims = new(), RoadScoreAnims = new();
	MD_Animations3Speed AirDoubleAnims = new(), RoadDoubleAnims = new();
	MD_Animations3Speed AirBoss1Anims = new(), RoadBoss1Anims = new();
	MD_Animations3Speed AirBoss2Anims = new(), RoadBoss2Anims = new();
	MD_Animations3Speed AirBoss3Anims = new(), RoadBoss3Anims = new();
	MD_Animations3Speed AirBossGearA_Anims = new(), RoadBossGearA_Anims = new();
	MD_Animations3Speed AirBossGearB_Anims = new(), RoadBossGearB_Anims = new();
	MD_Animations3Speed AirSmallAnims = new(), RoadSmallAnims = new();
	MD_Animations3Speed AirMedium1Anims = new(), RoadMedium1Anims = new();
	MD_Animations3Speed AirMedium2Anims = new(), RoadMedium2Anims = new();
	MD_Animations3Speed AirLarge1Anims = new(), RoadLarge1Anims = new();
	MD_Animations3Speed AirLarge2Anims = new(), RoadLarge2Anims = new();
	MD_Animations3Speed AirHammerA_Anims = new(), RoadHammerA_Anims = new();
	MD_Animations3Speed AirHammerB_Anims = new(), RoadHammerB_Anims = new();
	MD_Animations3Speed AirRaiderA_Anims = new(), RoadRaiderA_Anims = new();
	MD_Animations3Speed AirRaiderB_Anims = new(), RoadRaiderB_Anims = new();
	MD_Animations3Speed AirGhostAnims = new(), RoadGhostAnims = new();

	ITexture? AirStartSustainTexture, AirEndSustainTexture, AirBodySustainTexture, AirUpSustainTexture, AirDownSustainTexture;
	ITexture? RoadStartSustainTexture, RoadEndSustainTexture, RoadBodySustainTexture, RoadUpSustainTexture, RoadDownSustainTexture;

	public void Initialize(MuseDash1Game game) {
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

		BeginSound.BindVolumeToConVar(AudioSettings.snd_voicevolume);
		FeverSound.BindVolumeToConVar(AudioSettings.snd_voicevolume);
		UnpauseSound.BindVolumeToConVar(AudioSettings.snd_voicevolume);
		FullComboSound.BindVolumeToConVar(AudioSettings.snd_voicevolume);

		BlockSound.BindVolumeToConVar(AudioSettings.snd_hitvolume);
		CrystalSound.BindVolumeToConVar(AudioSettings.snd_hitvolume);
		Forte2Sound.BindVolumeToConVar(AudioSettings.snd_hitvolume);
		Forte3Sound.BindVolumeToConVar(AudioSettings.snd_hitvolume);
		GhostSound.BindVolumeToConVar(AudioSettings.snd_hitvolume);
		HpSound.BindVolumeToConVar(AudioSettings.snd_hitvolume);
		JumpSound.BindVolumeToConVar(AudioSettings.snd_hitvolume);
		Mezzo1Sound.BindVolumeToConVar(AudioSettings.snd_hitvolume);
		Mezzo3Sound.BindVolumeToConVar(AudioSettings.snd_hitvolume);
		Piano2Sound.BindVolumeToConVar(AudioSettings.snd_hitvolume);
		PressIdleSound.BindVolumeToConVar(AudioSettings.snd_hitvolume);
		PressTopSound.BindVolumeToConVar(AudioSettings.snd_hitvolume);
		ScoreSound.BindVolumeToConVar(AudioSettings.snd_hitvolume);

		FailBgmSound.BindVolumeToConVar(AudioSettings.snd_musicvolume);
		VictoryBgmSound.BindVolumeToConVar(AudioSettings.snd_musicvolume);

		var assets = MuseDash1Compatibility.StreamingAssets;

		string sustainID = $"{SceneInfo.MapIdx:00}{MuseDashSceneEnemyInfo.CODE_SUSTAIN}";
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

		string bossID = $"{SceneInfo.MapIdx:00}{MuseDashSceneEnemyInfo.CODE_BOSS}";
		string gearAirID = $"{SceneInfo.MapIdx:00}{MuseDashSceneEnemyInfo.CODE_GEARS}_air";
		string gearRoadID = SceneInfo.MapIdx switch {
			// :(
			3 => $"{SceneInfo.MapIdx:00}{MuseDashSceneEnemyInfo.CODE_GEARS}_road",
			8 => $"{SceneInfo.MapIdx:00}{MuseDashSceneEnemyInfo.CODE_GEARS}_road",
			10 => $"{SceneInfo.MapIdx:00}{MuseDashSceneEnemyInfo.CODE_GEARS}_road",
			_ => $"{SceneInfo.MapIdx:00}{MuseDashSceneEnemyInfo.CODE_GEARS}"
		};
		string masherID = $"{SceneInfo.MapIdx:00}{MuseDashSceneEnemyInfo.CODE_MASHERS}";
		string doubleAirID = $"{SceneInfo.MapIdx:00}{MuseDashSceneEnemyInfo.CODE_DOUBLES}_air";
		string doubleRoadID = $"{SceneInfo.MapIdx:00}{MuseDashSceneEnemyInfo.CODE_DOUBLES}_road";
		string boss1AirID = $"{SceneInfo.MapIdx:00}{MuseDashSceneEnemyInfo.CODE_BOSS1}_air";
		string boss1RoadID = $"{SceneInfo.MapIdx:00}{MuseDashSceneEnemyInfo.CODE_BOSS1}_road";
		string boss2AirID = $"{SceneInfo.MapIdx:00}{MuseDashSceneEnemyInfo.CODE_BOSS2}_air";
		string boss2RoadID = $"{SceneInfo.MapIdx:00}{MuseDashSceneEnemyInfo.CODE_BOSS2}_road";
		string boss3AirID = $"{SceneInfo.MapIdx:00}{MuseDashSceneEnemyInfo.CODE_BOSS3}_air";
		string boss3RoadID = $"{SceneInfo.MapIdx:00}{MuseDashSceneEnemyInfo.CODE_BOSS3}_road";
		string bossGearAirID = $"{SceneInfo.MapIdx:00}{MuseDashSceneEnemyInfo.CODE_BOSSGEARS}_air";
		string bossGearRoadID = $"{SceneInfo.MapIdx:00}{MuseDashSceneEnemyInfo.CODE_BOSSGEARS}_road";
		string smallAirID = $"{SceneInfo.MapIdx:00}{MuseDashSceneEnemyInfo.CODE_SMALL}_air";
		string smallRoadID = $"{SceneInfo.MapIdx:00}{MuseDashSceneEnemyInfo.CODE_SMALL}_road";
		string medium1AirID = $"{SceneInfo.MapIdx:00}{MuseDashSceneEnemyInfo.CODE_MEDIUM1}_air";
		string medium1RoadID = $"{SceneInfo.MapIdx:00}{MuseDashSceneEnemyInfo.CODE_MEDIUM1}_road";
		string medium2AirID = $"{SceneInfo.MapIdx:00}{MuseDashSceneEnemyInfo.CODE_MEDIUM2}_air";
		string medium2RoadID = $"{SceneInfo.MapIdx:00}{MuseDashSceneEnemyInfo.CODE_MEDIUM2}_road";
		string large1AirID = $"{SceneInfo.MapIdx:00}{MuseDashSceneEnemyInfo.CODE_LARGE1}_air";
		string large1RoadID = $"{SceneInfo.MapIdx:00}{MuseDashSceneEnemyInfo.CODE_LARGE1}_road";
		string large2AirID = $"{SceneInfo.MapIdx:00}{MuseDashSceneEnemyInfo.CODE_LARGE2}_air";
		string large2RoadID = $"{SceneInfo.MapIdx:00}{MuseDashSceneEnemyInfo.CODE_LARGE2}_road";
		string hammerAirID = $"{SceneInfo.MapIdx:00}{MuseDashSceneEnemyInfo.CODE_HAMMER}_air";
		string hammerRoadID = $"{SceneInfo.MapIdx:00}{MuseDashSceneEnemyInfo.CODE_HAMMER}_road";
		string hammerAirBID = $"{SceneInfo.MapIdx:00}{MuseDashSceneEnemyInfo.CODE_HAMMER}_air_b";
		string hammerRoadBID = $"{SceneInfo.MapIdx:00}{MuseDashSceneEnemyInfo.CODE_HAMMER}_road_b";
		string raiderAirID = $"{SceneInfo.MapIdx:00}{MuseDashSceneEnemyInfo.CODE_RAIDER}_air";
		string raiderRoadID = $"{SceneInfo.MapIdx:00}{MuseDashSceneEnemyInfo.CODE_RAIDER}_road";
		string raiderAirBID = $"{SceneInfo.MapIdx:00}{MuseDashSceneEnemyInfo.CODE_RAIDER}_air_b";
		string raiderRoadBID = $"{SceneInfo.MapIdx:00}{MuseDashSceneEnemyInfo.CODE_RAIDER}_road_b";
		string ghostAirID = $"{SceneInfo.MapIdx:00}{MuseDashSceneEnemyInfo.CODE_GHOST}_air";
		string ghostRoadID = $"{SceneInfo.MapIdx:00}{MuseDashSceneEnemyInfo.CODE_GHOST}_road";

		// Populate models
		BossModel = LoadModel(assets.FindAssetByName<MonoBehaviour>($"{bossID}_SkeletonData")!);
		AirHeartModel = LoadModel(assets.FindAssetByName<MonoBehaviour>("0002_hp_SkeletonData")!);
		RoadHeartModel = LoadModel(assets.FindAssetByName<MonoBehaviour>("0002_hp_SkeletonData")!);
		AirScoreModel = LoadModel(assets.FindAssetByName<MonoBehaviour>("0003_score_SkeletonData")!);
		RoadScoreModel = LoadModel(assets.FindAssetByName<MonoBehaviour>("0003_score_SkeletonData")!);
		AirGearModel = LoadModel(assets.FindAssetByName<MonoBehaviour>($"{gearAirID}_SkeletonData")!);
		RoadGearModel = LoadModel(assets.FindAssetByName<MonoBehaviour>($"{gearRoadID}_SkeletonData")!);
		MasherModel = LoadModel(assets.FindAssetByName<MonoBehaviour>($"{masherID}_SkeletonData")!);
		AirDoubleModel = LoadModel(assets.FindAssetByName<MonoBehaviour>($"{doubleAirID}_SkeletonData")!);
		RoadDoubleModel = LoadModel(assets.FindAssetByName<MonoBehaviour>($"{doubleRoadID}_SkeletonData")!);
		AirBoss1Model = LoadModel(assets.FindAssetByName<MonoBehaviour>($"{boss1AirID}_SkeletonData")!);
		RoadBoss1Model = LoadModel(assets.FindAssetByName<MonoBehaviour>($"{boss1RoadID}_SkeletonData")!);
		AirBoss2Model = LoadModel(assets.FindAssetByName<MonoBehaviour>($"{boss2AirID}_SkeletonData")!);
		RoadBoss2Model = LoadModel(assets.FindAssetByName<MonoBehaviour>($"{boss2RoadID}_SkeletonData")!);
		AirBoss3Model = LoadModel(assets.FindAssetByName<MonoBehaviour>($"{boss3AirID}_SkeletonData")!);
		RoadBoss3Model = LoadModel(assets.FindAssetByName<MonoBehaviour>($"{boss3RoadID}_SkeletonData")!);
		AirBossGearModel = LoadModel(assets.FindAssetByName<MonoBehaviour>($"{bossGearAirID}_SkeletonData")!);
		RoadBossGearModel = LoadModel(assets.FindAssetByName<MonoBehaviour>($"{bossGearRoadID}_SkeletonData")!);
		AirSmallModel = LoadModel(assets.FindAssetByName<MonoBehaviour>($"{smallAirID}_SkeletonData")!);
		RoadSmallModel = LoadModel(assets.FindAssetByName<MonoBehaviour>($"{smallRoadID}_SkeletonData")!);
		AirMedium1Model = LoadModel(assets.FindAssetByName<MonoBehaviour>($"{medium1AirID}_SkeletonData")!);
		RoadMedium1Model = LoadModel(assets.FindAssetByName<MonoBehaviour>($"{medium1RoadID}_SkeletonData")!);
		AirMedium2Model = LoadModel(assets.FindAssetByName<MonoBehaviour>($"{medium2AirID}_SkeletonData")!);
		RoadMedium2Model = LoadModel(assets.FindAssetByName<MonoBehaviour>($"{medium2RoadID}_SkeletonData")!);
		AirLarge1Model = LoadModel(assets.FindAssetByName<MonoBehaviour>($"{large1AirID}_SkeletonData")!);
		RoadLarge1Model = LoadModel(assets.FindAssetByName<MonoBehaviour>($"{large1RoadID}_SkeletonData")!);
		AirLarge2Model = LoadModel(assets.FindAssetByName<MonoBehaviour>($"{large2AirID}_SkeletonData")!);
		RoadLarge2Model = LoadModel(assets.FindAssetByName<MonoBehaviour>($"{large2RoadID}_SkeletonData")!);
		AirHammerModel = LoadModel(assets.FindAssetByName<MonoBehaviour>($"{hammerAirID}_SkeletonData")!);
		RoadHammerModel = LoadModel(assets.FindAssetByName<MonoBehaviour>($"{hammerRoadID}_SkeletonData")!);
		AirHammerBModel = LoadModel(assets.FindAssetByName<MonoBehaviour>($"{hammerAirBID}_SkeletonData")!);
		RoadHammerBModel = LoadModel(assets.FindAssetByName<MonoBehaviour>($"{hammerRoadBID}_SkeletonData")!);
		AirRaiderModel = LoadModel(assets.FindAssetByName<MonoBehaviour>($"{raiderAirID}_SkeletonData")!);
		RoadRaiderModel = LoadModel(assets.FindAssetByName<MonoBehaviour>($"{raiderRoadID}_SkeletonData")!);
		AirRaiderBModel = LoadModel(assets.FindAssetByName<MonoBehaviour>($"{raiderAirBID}_SkeletonData")!);
		RoadRaiderBModel = LoadModel(assets.FindAssetByName<MonoBehaviour>($"{raiderRoadBID}_SkeletonData")!);
		AirGhostModel = LoadModel(assets.FindAssetByName<MonoBehaviour>($"{ghostAirID}_SkeletonData")!);
		RoadGhostModel = LoadModel(assets.FindAssetByName<MonoBehaviour>($"{ghostRoadID}_SkeletonData")!);
		HpMountModel = LoadModel(assets.FindAssetByName<MonoBehaviour>($"0002_hp_SkeletonData")!);

		// Populate animations

		BossAnims = new(getSpineController(MuseDashSceneEnemyInfo.GetBoss(SceneInfo)));
		PopulateThreeSpeedPathwayAnimations(AirHeartAnims, RoadHeartAnims, static (in req) => MuseDashSceneEnemyInfo.GetHeart(req.path, req.speed));
		PopulateThreeSpeedPathwayAnimations(AirScoreAnims, RoadScoreAnims, static (in req) => MuseDashSceneEnemyInfo.GetScore(req.path, req.speed));
		PopulateThreeSpeedAnimations(MasherAnims, [EntityEnterDirection.RightSide, EntityEnterDirection.TopDown], static (in req) => MuseDashSceneEnemyInfo.GetMasher(req.scene, req.dir, req.speed));
		PopulateThreeSpeedPathwayAnimations(AirGearAnims, RoadGearAnims, static (in req) => MuseDashSceneEnemyInfo.GetGear(req.scene, req.path, req.speed));
		PopulateThreeSpeedPathwayAnimations(AirDoubleAnims, RoadDoubleAnims, static (in req) => MuseDashSceneEnemyInfo.GetDouble(req.scene, req.path, req.speed));
		PopulateThreeSpeedPathwayAnimations(AirBoss1Anims, RoadBoss1Anims, static (in req) => MuseDashSceneEnemyInfo.GetBoss1(req.scene, req.path, req.speed));
		PopulateThreeSpeedPathwayAnimations(AirBoss2Anims, RoadBoss2Anims, static (in req) => MuseDashSceneEnemyInfo.GetBoss2(req.scene, req.path, req.speed));
		PopulateThreeSpeedPathwayAnimations(AirBoss3Anims, RoadBoss3Anims, static (in req) => MuseDashSceneEnemyInfo.GetBoss3(req.scene, req.path, req.speed));
		PopulateThreeSpeedPathwayAnimations(AirBoss3Anims, RoadBoss3Anims, static (in req) => MuseDashSceneEnemyInfo.GetBoss3(req.scene, req.path, req.speed));
		PopulateThreeSpeedPathwayAnimations(AirBossGearA_Anims, RoadBossGearA_Anims, static (in req) => MuseDashSceneEnemyInfo.GetBossGear(req.scene, req.path, req.speed, false));
		PopulateThreeSpeedPathwayAnimations(AirBossGearB_Anims, RoadBossGearB_Anims, static (in req) => MuseDashSceneEnemyInfo.GetBossGear(req.scene, req.path, req.speed, true));
		PopulateThreeSpeedAllDirsAnimations(AirSmallAnims, RoadSmallAnims, static (in req) => MuseDashSceneEnemyInfo.GetSmall(req.scene, req.path, req.dir, req.speed));
		PopulateThreeSpeedAllDirsAnimations(AirMedium1Anims, RoadMedium1Anims, static (in req) => MuseDashSceneEnemyInfo.GetMedium1(req.scene, req.path, req.dir, req.speed));
		PopulateThreeSpeedAllDirsAnimations(AirMedium2Anims, RoadMedium2Anims, static (in req) => MuseDashSceneEnemyInfo.GetMedium2(req.scene, req.path, req.dir, req.speed));
		PopulateThreeSpeedPathwayAnimations(AirLarge1Anims, RoadLarge1Anims, static (in req) => MuseDashSceneEnemyInfo.GetLarge1(req.scene, req.path, req.speed));
		PopulateThreeSpeedPathwayAnimations(AirLarge2Anims, RoadLarge2Anims, static (in req) => MuseDashSceneEnemyInfo.GetLarge2(req.scene, req.path, req.speed));
		PopulateThreeSpeedPathwayAnimations(AirHammerA_Anims, RoadHammerA_Anims, static (in req) => MuseDashSceneEnemyInfo.GetHammer(req.scene, req.path, req.speed, false));
		PopulateThreeSpeedPathwayAnimations(AirHammerB_Anims, RoadHammerB_Anims, static (in req) => MuseDashSceneEnemyInfo.GetHammer(req.scene, req.path, req.speed, true));
		PopulateThreeSpeedPathwayAnimations(AirRaiderA_Anims, RoadRaiderA_Anims, static (in req) => MuseDashSceneEnemyInfo.GetRaider(req.scene, req.path, req.speed, false));
		PopulateThreeSpeedPathwayAnimations(AirRaiderB_Anims, RoadRaiderB_Anims, static (in req) => MuseDashSceneEnemyInfo.GetRaider(req.scene, req.path, req.speed, true));
		PopulateThreeSpeedPathwayAnimations(AirGhostAnims, RoadGhostAnims, static (in req) => MuseDashSceneEnemyInfo.GetGhost(req.scene, req.path, req.speed));
	}

	public struct RequestInfo
	{
		public MuseDashSceneInfo scene;
		public PathwaySide path;
		public EntityEnterDirection dir;
		public int speed;
	}



	MonoBehaviourReader getSpineController(string name) => new(MuseDash1Compatibility.StreamingAssets.FindAssetByName<GameObject>(name)!.GetComponentByName<MonoBehaviour>("SpineActionController")!);

	public delegate string ResolverFn(in RequestInfo info);

	void ProcessThreeSpeedAnimations(MD_Animations3Speed table, in RequestInfo req, MonoBehaviourReader reader) {
		ref MD1_SpineActionControllerData speedToEdit = ref table.GetSpeedForEdit(req.speed, req.dir);
		speedToEdit = new(reader);
	}

	void PopulateThreeSpeedAnimations(MD_Animations3Speed table, ResolverFn resolver) {
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
	void PopulateThreeSpeedAllDirsAnimations(MD_Animations3Speed table, ResolverFn resolver) => PopulateThreeSpeedAnimations(table, [EntityEnterDirection.RightSide, EntityEnterDirection.TopDown, EntityEnterDirection.BottomUp], resolver);
	void PopulateThreeSpeedAllDirsAnimations(MD_Animations3Speed top, MD_Animations3Speed bottom, ResolverFn resolver) => PopulateThreeSpeedAnimations(top, bottom, [EntityEnterDirection.RightSide, EntityEnterDirection.TopDown, EntityEnterDirection.BottomUp], resolver);
	void PopulateThreeSpeedAnimations(MD_Animations3Speed table, ReadOnlySpan<EntityEnterDirection> dirs, ResolverFn resolver) {
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

	void PopulateThreeSpeedAnimations(MD_Animations3Speed top, MD_Animations3Speed bottom, ReadOnlySpan<EntityEnterDirection> dirs, ResolverFn resolver) {
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
	void PopulateThreeSpeedPathwayAnimations(MD_Animations3Speed top, MD_Animations3Speed bottom, ResolverFn resolver) {
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

	public void RenderBackground(MuseDash1Game game) {
		Rlgl.PushMatrix();
		Rlgl.Scalef(MUSEDASH_MULTIPLIER_POSITIONS, MUSEDASH_MULTIPLIER_POSITIONS, 1);
		foreach (var renderer in sortedRenderers) renderer.Render(this);
		Rlgl.PopMatrix();
	}

	public void RenderPathway(MuseDash1Game game, PathwaySide side, float alpha, float size, float rotation) {
		var obj = ((SceneObject)pathwayInfo[(int)side].UserData!);
		var transform = obj.Transform;
		transform.LocalRotationX = 0; transform.LocalRotationY = 0;
		transform.LocalRotationZ = NMath.Remap(rotation, 0, 1, -1, 1);
		transform.LocalRotationW = 1;
		transform.LocalScaleX = size; transform.LocalScaleY = size;
		obj.Color.W = alpha / 255f;
	}

	public void Think(MuseDash1Game game) => RunThinkFuncs(globals.CurTimeDelta);

	public void Refresh(MuseDash1Game game) { }
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

	private MD_Animations3Speed fromVariantSHE(EntityVariant variant, PathwaySide pathway) => variant switch {
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
	public BoneInstance? GetHPMount(DashEnemy enemy) => enemy.Model?.FindBone("hp");
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
	internal void MountToFilesystem() { }
}
