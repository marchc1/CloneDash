using AssetStudio;
using CloneDash.Compatibility.MuseDash;
using CloneDash.Compatibility.Unity;
using CloneDash.Game;
using CloneDash.Game.Entities;
using CloneDash.Settings;
using DiscordRPC;
using NAudio.CoreAudioApi;
using Nucleus;
using Nucleus.Audio;
using Nucleus.Common.Graphics;
using Nucleus.Engine;
using Nucleus.ManagedMemory;
using Nucleus.Models.Runtime;
using Nucleus.Types;
using Nucleus.Util;
using Raylib_cs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Color = Nucleus.Common.Types.Color;
using Sound = Nucleus.Audio.Sound;
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
	public string? Mash;
	public int? MashStart;
	public int? MashEnd;
	public string? Hp;
	public string? Score;
	public string? Jump;
	public string? Loud1;
	public string? Loud2;
	public string? Medium1;
	public string? Medium2;
	public string? Quiet;
	public string? Ghost;
	public string? PressIdle;
	public string? PressTop;

	public static readonly MuseDashSceneSounds Default = new() {
		Begin = "sfx_readygo",
		Fever = "char_common_fever",
		Unpause = "sfx_pause321",
		FullCombo = "sfx_full_combo",
		PressIdle = "sfx_press",
		PressTop = "sfx_press_top"
	};

	public static MuseDashSceneSounds operator +(MuseDashSceneSounds a, MuseDashSceneSounds b) {
		if (b.Begin != null) a.Begin = b.Begin;
		if (b.Fever != null) a.Fever = b.Fever;
		if (b.Unpause != null) a.Unpause = b.Unpause;
		if (b.FullCombo != null) a.FullCombo = b.FullCombo;
		if (b.Mash != null) a.Mash = b.Mash;
		if (b.MashStart != null) a.MashStart = b.MashStart;
		if (b.MashEnd != null) a.MashEnd = b.MashEnd;
		if (b.Hp != null) a.Hp = b.Hp;
		if (b.Score != null) a.Score = b.Score;
		if (b.Jump != null) a.Jump = b.Jump;
		if (b.Loud1 != null) a.Loud1 = b.Loud1;
		if (b.Loud2 != null) a.Loud2 = b.Loud2;
		if (b.Medium1 != null) a.Medium1 = b.Medium1;
		if (b.Medium2 != null) a.Medium2 = b.Medium2;
		if (b.Quiet != null) a.Quiet = b.Quiet;
		if (b.Ghost != null) a.Ghost = b.Ghost;
		if (b.PressIdle != null) a.PressIdle = b.PressIdle;
		if (b.PressTop != null) a.PressTop = b.PressTop;
		return a;
	}
}

public class MD_Animations3Speed
{
	public readonly MD_SpineActionController[][] Speeds = [
		[null!, null!, null!],
		[null!, null!, null!],
		[null!, null!, null!]
	];

	public MD_SpineActionController GetSpeed(int speed, EntityEnterDirection dir = EntityEnterDirection.RightSide) {
		Debug.Assert(speed >= 1);
		Debug.Assert(speed <= 3);
		return Speeds[speed - 1][(int)dir] ?? Speeds[speed - 1][0]; // Default to rightside
	}

	public ref MD_SpineActionController GetSpeedForEdit(int speed, EntityEnterDirection dir = EntityEnterDirection.RightSide) {
		Debug.Assert(speed >= 1);
		Debug.Assert(speed <= 3);
		return ref Speeds[speed - 1][(int)dir];
	}
}

public class MD_SpineActionController
{
	public readonly MD_ActionData?[] ActionData;

	public MD_ActionData? Get(ReadOnlySpan<char> name) {
		for (int i = 0, c = ActionData.Length; i < c; i++) {
			var data = ActionData[i];
			if (data == null) continue;
			if (data.Name.Equals(name, StringComparison.InvariantCulture))
				return data;
		}
		return null;
	}

	public MD_SpineActionController(MonoBehaviourReader reader) {
		var animationData = reader.GetAny<List<object>>("actionData")!;
		ActionData = new MD_ActionData?[animationData.Count];

		for (int i = 0; i < animationData.Count; i++) {
			if (animationData[i] is not OrderedDictionary dict) continue;
			MD_ActionData data;
			data = ActionData[i] = new MD_ActionData();

			data.Name = ((string?)dict["name"]) ?? throw new Exception();
			data.IsEndLoop = (((byte?)dict["isEndLoop"]) ?? 0) != 0;
			data.ActionIdx = ((List<object>)dict["actionIdx"]!).Cast<string>().ToArray();
		}
	}
}

public class MD_ActionData
{
	public bool Collapsed;
	public bool IsEndLoop;
	public bool IsRandomSequence;
	public bool IsSelfProtect;
	public string Name = "";
	public int ProtectLevel;
	public int SpineActionKeyIndex;
	public string[] ActionIdx = [];
	public int[] ActionEventIdx = [];
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
													Begin = "sfx_readygo_gc"
												});
	public static readonly MuseDashSceneInfo Gensokyo = new MuseDashSceneInfo(8, "Gensokyo");
	public static readonly MuseDashSceneInfo GameGraveyard = new MuseDashSceneInfo(9, "Game Graveyard");
	public static readonly MuseDashSceneInfo Museland = new MuseDashSceneInfo(10, "Museland", "scene_{0:00}_miku");
	public static readonly MuseDashSceneInfo Mirrorland = new MuseDashSceneInfo(10, "Mirrorland", "scene_{0:00}_rin_len");
	public static readonly MuseDashSceneInfo Warriorland = new MuseDashSceneInfo(11, "Warriorland")
												.MarkUnusable();
	public static readonly MuseDashSceneInfo JadeTemple = new MuseDashSceneInfo(12, "Jade Temple");
}

static class UnityCRC32
{
	static readonly uint[] table = new uint[256];
	static UnityCRC32() {
		for (uint i = 0; i < 256; i++) {
			uint crc = i;
			for (int j = 0; j < 8; j++)
				crc = (crc >> 1) ^ ((crc & 1) != 0 ? 0xEDB88320u : 0);
			table[i] = crc;
		}
	}
	public static uint Hash(string s) {
		uint crc = 0xFFFFFFFF;
		foreach (char c in s)
			crc = (crc >> 8) ^ table[(byte)(crc ^ c)];
		return crc ^ 0xFFFFFFFF;
	}
}

class DecodedClip
{
	public struct CurveData
	{
		public GenericBinding Binding;
		public List<(float time, float value, float inSlope, float outSlope)> Keys;
	}

	public CurveData[] Curves;
	public float StartTime, StopTime;
	public bool LoopTime;

	public static DecodedClip Decode(AnimationClip clip) {
		var result = new DecodedClip();
		result.StartTime = clip.m_MuscleClip?.m_StartTime ?? 0;
		result.StopTime = clip.m_MuscleClip?.m_StopTime ?? 0;
		result.LoopTime = clip.m_MuscleClip?.m_LoopTime ?? false;

		var bindings = clip.m_ClipBindingConstant;
		if (bindings?.genericBindings == null || clip.m_MuscleClip?.m_Clip == null) {
			result.Curves = [];
			return result;
		}

		var muscleClip = clip.m_MuscleClip.m_Clip;

		int totalCurves = 0;
		foreach (var b in bindings.genericBindings) {
			if (b.typeID == ClassIDType.Transform) {
				totalCurves += b.attribute switch {
					1 => 3,
					2 => 4,
					3 => 3,
					4 => 3,
					_ => 1
				};
			}
			else {
				totalCurves++;
			}
		}

		var curveLists = new List<(float time, float value, float inSlope, float outSlope)>[totalCurves];
		for (int i = 0; i < totalCurves; i++)
			curveLists[i] = [];

		if (muscleClip.m_StreamedClip?.data != null && muscleClip.m_StreamedClip.data.Length > 0) {
			var frames = muscleClip.m_StreamedClip.ReadData();
			foreach (var frame in frames) {
				foreach (var key in frame.keyList) {
					if (key.index >= 0 && key.index < totalCurves)
						curveLists[key.index].Add((frame.time, key.value, key.inSlope, key.outSlope));
				}
			}
		}

		if (muscleClip.m_DenseClip != null && muscleClip.m_DenseClip.m_SampleArray?.Length > 0) {
			var dense = muscleClip.m_DenseClip;
			int curveCount = (int)dense.m_CurveCount;
			int streamedCurveCount = (int)(muscleClip.m_StreamedClip?.curveCount ?? 0);

			for (int frameIdx = 0; frameIdx < dense.m_FrameCount; frameIdx++) {
				float t = dense.m_BeginTime + frameIdx / dense.m_SampleRate;
				for (int c = 0; c < curveCount; c++) {
					int sampleIdx = frameIdx * curveCount + c;
					if (sampleIdx >= dense.m_SampleArray.Length) break;
					int curveIdx = streamedCurveCount + c;
					if (curveIdx < totalCurves) {
						float val = dense.m_SampleArray[sampleIdx];
						curveLists[curveIdx].Add((t, val, float.PositiveInfinity, float.PositiveInfinity));
					}
				}
			}
		}

		if (muscleClip.m_ConstantClip?.data != null) {
			int streamedCount = (int)(muscleClip.m_StreamedClip?.curveCount ?? 0);
			int denseCount = (int)(muscleClip.m_DenseClip?.m_CurveCount ?? 0);
			int constantOffset = streamedCount + denseCount;

			for (int c = 0; c < muscleClip.m_ConstantClip.data.Length; c++) {
				int curveIdx = constantOffset + c;
				if (curveIdx < totalCurves) {
					float val = muscleClip.m_ConstantClip.data[c];
					curveLists[curveIdx].Add((result.StartTime, val, 0, 0));
					curveLists[curveIdx].Add((result.StopTime, val, 0, 0));
				}
			}
		}

		var curveDataList = new List<CurveData>();
		int ci = 0;
		foreach (var b in bindings.genericBindings) {
			int numFloats;
			if (b.typeID == ClassIDType.Transform) {
				numFloats = b.attribute switch { 1 => 3, 2 => 4, 3 => 3, 4 => 3, _ => 1 };
			}
			else {
				numFloats = 1;
			}

			for (int f = 0; f < numFloats && ci < totalCurves; f++, ci++) {
				curveDataList.Add(new CurveData {
					Binding = b,
					Keys = curveLists[ci]
				});
			}
		}

		result.Curves = curveDataList.ToArray();
		return result;
	}

	public static float Eval(List<(float time, float value, float inSlope, float outSlope)> keys, float t) {
		if (keys.Count == 0) return 0;
		if (keys.Count == 1) return keys[0].value;
		if (t <= keys[0].time) return keys[0].value;
		if (t >= keys[^1].time) return keys[^1].value;

		int i = 0;
		for (; i < keys.Count - 2; i++)
			if (t < keys[i + 1].time) break;

		var k0 = keys[i];
		var k1 = keys[i + 1];
		float dt = k1.time - k0.time;
		if (dt <= 0) return k0.value;

		if (float.IsPositiveInfinity(k0.outSlope) || float.IsPositiveInfinity(k1.inSlope)) {
			float u = (t - k0.time) / dt;
			return k0.value + (k1.value - k0.value) * u;
		}

		float uu = (t - k0.time) / dt;
		float uu2 = uu * uu;
		float uu3 = uu2 * uu;
		float a = 2 * uu3 - 3 * uu2 + 1;
		float b = uu3 - 2 * uu2 + uu;
		float c = uu3 - uu2;
		float d = -2 * uu3 + 3 * uu2;
		return a * k0.value + b * (k0.outSlope * dt) + c * (k1.inSlope * dt) + d * k1.value;
	}
}

public abstract class SceneComponent
{
	public SceneObject Object { get; internal set; } = null!;
	public SceneTransform Transform => Object.Transform;
	public virtual void Awake() { }
}

public class SceneTransform : SceneComponent
{
	public float LocalX, LocalY, LocalZ;
	public float LocalScaleX = 1, LocalScaleY = 1, LocalScaleZ = 1;
	public float LocalRotationX, LocalRotationY, LocalRotationZ, LocalRotationW = 1;

	public SceneTransform? Parent { get; private set; }
	readonly List<SceneTransform> children = [];
	public IReadOnlyList<SceneTransform> Children => children;

	public void SetParent(SceneTransform? parent) {
		Parent?.children.Remove(this);
		Parent = parent;
		Parent?.children.Add(this);
	}

	public void GetWorldPosition(out float wx, out float wy, out float wz) {
		wx = LocalX; wy = LocalY; wz = LocalZ;
		var p = Parent;
		while (p != null) {
			wx *= p.LocalScaleX; wy *= p.LocalScaleY; wz *= p.LocalScaleZ;
			RotateVector(p.LocalRotationX, p.LocalRotationY, p.LocalRotationZ, p.LocalRotationW,
				wx, wy, wz, out wx, out wy, out wz);
			wx += p.LocalX; wy += p.LocalY; wz += p.LocalZ;
			p = p.Parent;
		}
	}

	public void GetWorldScale(out float sx, out float sy) {
		sx = LocalScaleX; sy = LocalScaleY;
		var p = Parent;
		while (p != null) { sx *= p.LocalScaleX; sy *= p.LocalScaleY; p = p.Parent; }
	}

	public float GetWorldRotationZ() {
		float totalRad = LocalRotationZRadians();
		var p = Parent;
		while (p != null) { totalRad += p.LocalRotationZRadians(); p = p.Parent; }
		return totalRad * (180f / MathF.PI);
	}

	float LocalRotationZRadians() =>
		MathF.Atan2(2f * (LocalRotationW * LocalRotationZ + LocalRotationX * LocalRotationY),
					1f - 2f * (LocalRotationY * LocalRotationY + LocalRotationZ * LocalRotationZ));

	static void RotateVector(float qx, float qy, float qz, float qw,
		float vx, float vy, float vz, out float ox, out float oy, out float oz) {
		float dot = qx * vx + qy * vy + qz * vz;
		float qsq = qx * qx + qy * qy + qz * qz;
		float cx = qy * vz - qz * vy, cy = qz * vx - qx * vz, cz = qx * vy - qy * vx;
		ox = 2f * dot * qx + (qw * qw - qsq) * vx + 2f * qw * cx;
		oy = 2f * dot * qy + (qw * qw - qsq) * vy + 2f * qw * cy;
		oz = 2f * dot * qz + (qw * qw - qsq) * vz + 2f * qw * cz;
	}

	public void ReadFrom(Transform unityTransform) {
		LocalX = unityTransform.m_LocalPosition.X;
		LocalY = unityTransform.m_LocalPosition.Y;
		LocalZ = unityTransform.m_LocalPosition.Z;
		LocalScaleX = unityTransform.m_LocalScale.X;
		LocalScaleY = unityTransform.m_LocalScale.Y;
		LocalScaleZ = unityTransform.m_LocalScale.Z;
		LocalRotationX = unityTransform.m_LocalRotation.X;
		LocalRotationY = unityTransform.m_LocalRotation.Y;
		LocalRotationZ = unityTransform.m_LocalRotation.Z;
		LocalRotationW = unityTransform.m_LocalRotation.W;
	}

	public void ComputeGlobalTransform(out Vector3 position, out Quaternion rotation) {
		position = new(LocalX, LocalY, LocalZ);
		rotation = new(LocalRotationX, LocalRotationY, LocalRotationZ, LocalRotationW);
		var parent = Parent;
		while (parent != null) {
			position = new Vector3(position.X * parent.LocalScaleX, position.Y * parent.LocalScaleY, position.Z * parent.LocalScaleZ);
			position = RotateVector(new(parent.LocalRotationX, parent.LocalRotationY, parent.LocalRotationZ, parent.LocalRotationW), position);
			position += new Vector3(parent.LocalX, parent.LocalY, parent.LocalZ);
			rotation = MultiplyQuaternion(new(parent.LocalRotationX, parent.LocalRotationY, parent.LocalRotationZ, parent.LocalRotationW), rotation);
			parent = parent.Parent;
		}
	}

	private static Vector3 RotateVector(Quaternion q, Vector3 v) {
		float ux = q.X, uy = q.Y, uz = q.Z, s = q.W;
		float dotUV = ux * v.X + uy * v.Y + uz * v.Z;
		float dotUU = ux * ux + uy * uy + uz * uz;
		float cx = uy * v.Z - uz * v.Y, cy = uz * v.X - ux * v.Z, cz = ux * v.Y - uy * v.X;
		return new Vector3(
			2f * dotUV * ux + (s * s - dotUU) * v.X + 2f * s * cx,
			2f * dotUV * uy + (s * s - dotUU) * v.Y + 2f * s * cy,
			2f * dotUV * uz + (s * s - dotUU) * v.Z + 2f * s * cz);
	}

	private static Quaternion MultiplyQuaternion(Quaternion a, Quaternion b) {
		return new Quaternion(
			a.W * b.X + a.X * b.W + a.Y * b.Z - a.Z * b.Y,
			a.W * b.Y - a.X * b.Z + a.Y * b.W + a.Z * b.X,
			a.W * b.Z + a.X * b.Y - a.Y * b.X + a.Z * b.W,
			a.W * b.W - a.X * b.X - a.Y * b.Y - a.Z * b.Z);
	}
}

public abstract class SceneRenderer : SceneComponent
{
	public int SortingLayerID;
	public int SortingOrder;
	public abstract void Render(MuseDashScene scene);
}

public class SceneSpriteRenderer : SceneRenderer
{
	public SpriteRenderer UnitySpriteRenderer { get; }
	ITexture? texture;
	float texRectX, texRectY, texRectW, texRectH;
	int atlasW, atlasH;
	float unitW, unitH;
	float pivotX, pivotY;
	bool flipX, flipY;
	System.Numerics.Vector4 color;
	private SpriteMaskInteraction MaskInteraction;

	public SceneSpriteRenderer(SpriteRenderer sr) { UnitySpriteRenderer = sr; }

	public override void Awake() {
		if (UnitySpriteRenderer.m_MaskInteraction != SpriteMaskInteraction.None) {
			// TODO: masks
			return;
		}

		var sprite = UnitySpriteRenderer.GetSprite();
		if (sprite == null) return;
		var tex2d = sprite.m_RD.GetTexture();
		if (tex2d == null) return;

		texture = ((MuseDashScene)Object.Scene).LoadTexture(tex2d);
		atlasW = (int)texture.Width; atlasH = (int)texture.Height;
		texRectX = sprite.m_RD.textureRect.x; texRectY = sprite.m_RD.textureRect.y;
		texRectW = sprite.m_RD.textureRect.width; texRectH = sprite.m_RD.textureRect.height;

		float ppu = sprite.m_PixelsToUnits;
		if (ppu <= 0) ppu = 100f;
		unitW = sprite.m_Rect.width / ppu; unitH = sprite.m_Rect.height / ppu;
		pivotX = sprite.m_Pivot.X; pivotY = sprite.m_Pivot.Y;
		flipX = UnitySpriteRenderer.m_FlipX; flipY = UnitySpriteRenderer.m_FlipY;

		var c = UnitySpriteRenderer.m_Color;
		color = new(c.R, c.G, c.B, c.A);
		SortingOrder = UnitySpriteRenderer.m_SortingOrder;
		SortingLayerID = (int)UnitySpriteRenderer.m_SortingLayerID;

		MaskInteraction = UnitySpriteRenderer.m_MaskInteraction;
	}

	public void SetSprite(Sprite sprite, MuseDashScene scene) {
		var tex2d = sprite.m_RD.GetTexture();
		if (tex2d == null) return;
		texture = scene.LoadTexture(tex2d);
		atlasW = (int)texture.Width; atlasH = (int)texture.Height;
		texRectX = sprite.m_RD.textureRect.x; texRectY = sprite.m_RD.textureRect.y;
		texRectW = sprite.m_RD.textureRect.width; texRectH = sprite.m_RD.textureRect.height;
		float ppu = sprite.m_PixelsToUnits;
		if (ppu <= 0) ppu = 100f;
		unitW = sprite.m_Rect.width / ppu; unitH = sprite.m_Rect.height / ppu;
		pivotX = sprite.m_Pivot.X; pivotY = sprite.m_Pivot.Y;
	}

	public override void Render(MuseDashScene scene) {
		if (texture == null || texRectW <= 0 || texRectH <= 0) return;
		if (!MuseDashScene.IsActiveInHierarchy(Object)) return;

		Transform.GetWorldPosition(out float wx, out float wy, out _);
		Transform.GetWorldScale(out float sx, out float sy);

		float w = unitW * MathF.Abs(sx), h = unitH * MathF.Abs(sy);
		float offX = -pivotX * w, offY = -(1f - pivotY) * h;

		float flippedY = atlasH - texRectY - texRectH;
		float u0 = texRectX / atlasW, v0 = flippedY / atlasH;
		float u1 = (texRectX + texRectW) / atlasW, v1 = (flippedY + texRectH) / atlasH;

		if (texture.HasPublicFlags(PublicTextureFlags.RequiresFlippedV)) {
			v0 = 1 - v0;
			v1 = 1 - v1;
		}

		if (flipX ^ (sx < 0)) (u0, u1) = (u1, u0);
		if (flipY ^ (sy < 0)) (v0, v1) = (v1, v0);

		float screenX = wx + offX, screenY = -wy + offY;
		float rotDeg = Transform.GetWorldRotationZ();
		uint texId = texture.GetTextureHandle();
		var fcolor = this.color * this.Object.GetColor();

		if (MathF.Abs(rotDeg) > 0.01f) {
			float cx = wx, cy = -wy;
			float rad = -rotDeg * (MathF.PI / 180f);
			float cos = MathF.Cos(rad), sin = MathF.Sin(rad);
			float x0 = offX, y0 = offY, x1 = offX, y1 = offY + h;
			float x2 = offX + w, y2 = offY + h, x3 = offX + w, y3 = offY;

			Rlgl.SetTexture(texId); Rlgl.Begin(DrawMode.QUADS);
			Rlgl.Color4f(fcolor.X, fcolor.Y, fcolor.Z, fcolor.W);
			Rlgl.TexCoord2f(u0, v0); Rlgl.Vertex2f(cx + x0 * cos - y0 * sin, cy + x0 * sin + y0 * cos);
			Rlgl.TexCoord2f(u0, v1); Rlgl.Vertex2f(cx + x1 * cos - y1 * sin, cy + x1 * sin + y1 * cos);
			Rlgl.TexCoord2f(u1, v1); Rlgl.Vertex2f(cx + x2 * cos - y2 * sin, cy + x2 * sin + y2 * cos);
			Rlgl.TexCoord2f(u1, v0); Rlgl.Vertex2f(cx + x3 * cos - y3 * sin, cy + x3 * sin + y3 * cos);
			Rlgl.End(); Rlgl.SetTexture(0);
		}
		else {
			Rlgl.SetTexture(texId); Rlgl.Begin(DrawMode.QUADS);
			Rlgl.Color4f(fcolor.X, fcolor.Y, fcolor.Z, fcolor.W);
			Rlgl.TexCoord2f(u0, v0); Rlgl.Vertex2f(screenX, screenY);
			Rlgl.TexCoord2f(u0, v1); Rlgl.Vertex2f(screenX, screenY + h);
			Rlgl.TexCoord2f(u1, v1); Rlgl.Vertex2f(screenX + w, screenY + h);
			Rlgl.TexCoord2f(u1, v0); Rlgl.Vertex2f(screenX + w, screenY);
			Rlgl.End(); Rlgl.SetTexture(0);
		}
	}
}

class RuntimeClip
{
	public record struct TargetCurve(
		SceneTransform Target,
		int PosX, int PosY, int PosZ,
		int RotX, int RotY, int RotZ, int RotW,
		int ScX, int ScY, int ScZ
	);

	public DecodedClip Decoded;
	public List<TargetCurve> TransformTargets = [];
	public float Duration;
	public bool Loop;
}

public class SceneAnimator : SceneComponent
{
	public Animator? UnityAnimator { get; set; }

	readonly List<RuntimeClip> runtimeClips = [];
	float time;
	public int ResolvedClipCount => runtimeClips.Count;

	public override void Awake() {
		if (UnityAnimator == null) return;

		var controller = UnityAnimator.GetController();
		if (controller == null) return;

		var scene = (MuseDashScene)Object.Scene;
		List<AnimationClip> animClips = [];

		switch (controller) {
			case AnimatorController ac:
				foreach (var pptr in ac.m_AnimationClips)
					if (pptr.TryGet(out var clip)) animClips.Add(clip);
				break;
			case AnimatorOverrideController aoc:
				if (aoc.m_Controller.TryGet(out var baseCtrl) && baseCtrl is AnimatorController baseAc)
					foreach (var pptr in baseAc.m_AnimationClips)
						if (pptr.TryGet(out var clip)) animClips.Add(clip);
				if (aoc.m_Clips != null)
					foreach (var ov in aoc.m_Clips)
						if (ov.m_OverrideClip.TryGet(out var overrideClip)) animClips.Add(overrideClip);
				break;
		}

		if (animClips.Count == 0) return;

		var hashToTransform = new Dictionary<uint, SceneTransform>();
		BuildPathHashes(Object.Transform, "", hashToTransform);

		foreach (var clip in animClips) {
			var decoded = DecodedClip.Decode(clip);
			if (decoded.Curves.Length == 0) continue;

			var runtime = new RuntimeClip {
				Decoded = decoded,
				Duration = decoded.StopTime - decoded.StartTime,
				Loop = decoded.LoopTime || clip.m_WrapMode == 2 || clip.m_WrapMode == 0
			};

			var grouped = new Dictionary<uint, (int posX, int posY, int posZ, int rotX, int rotY, int rotZ, int rotW, int scX, int scY, int scZ)>();

			for (int i = 0; i < decoded.Curves.Length; i++) {
				ref var curve = ref decoded.Curves[i];
				if (curve.Binding.typeID != ClassIDType.Transform) continue;
				if (curve.Keys.Count == 0) continue;

				uint pathHash = curve.Binding.path;
				if (!grouped.TryGetValue(pathHash, out var g))
					g = (-1, -1, -1, -1, -1, -1, -1, -1, -1, -1);

				uint attr = curve.Binding.attribute;
				int componentIdx = GetComponentIndex(decoded.Curves, i, curve.Binding);

				switch (attr) {
					case 1: // position
						if (componentIdx == 0) g.posX = i;
						else if (componentIdx == 1) g.posY = i;
						else if (componentIdx == 2) g.posZ = i;
						break;
					case 2: // rotation
						if (componentIdx == 0) g.rotX = i;
						else if (componentIdx == 1) g.rotY = i;
						else if (componentIdx == 2) g.rotZ = i;
						else if (componentIdx == 3) g.rotW = i;
						break;
					case 3: // scale
						if (componentIdx == 0) g.scX = i;
						else if (componentIdx == 1) g.scY = i;
						else if (componentIdx == 2) g.scZ = i;
						break;
					case 4: // euler (treat as rotation via euler angles — simplified) TODO
						break;
				}

				grouped[pathHash] = g;
			}

			foreach (var (pathHash, g) in grouped) {
				if (!hashToTransform.TryGetValue(pathHash, out var target)) continue;
				runtime.TransformTargets.Add(new(target, g.posX, g.posY, g.posZ,
					g.rotX, g.rotY, g.rotZ, g.rotW, g.scX, g.scY, g.scZ));
			}

			if (runtime.TransformTargets.Count > 0)
				runtimeClips.Add(runtime);
		}

	}

	static int GetComponentIndex(DecodedClip.CurveData[] curves, int curveIdx, GenericBinding binding) {
		int count = 0;
		for (int i = curveIdx - 1; i >= 0; i--) {
			if (curves[i].Binding == binding) count++;
			else break;
		}
		return count;
	}

	void BuildPathHashes(SceneTransform transform, string path, Dictionary<uint, SceneTransform> map) {
		uint hash = UnityCRC32.Hash(path);
		map.TryAdd(hash, transform);

		foreach (var child in transform.Children) {
			string childPath = string.IsNullOrEmpty(path) ? child.Object.Name : path + "/" + child.Object.Name;
			BuildPathHashes(child, childPath, map);
		}
	}

	public void Evaluate(float deltaTime) {
		if (runtimeClips.Count == 0) return;
		time += deltaTime;

		foreach (var clip in runtimeClips) {
			float t;
			if (clip.Loop && clip.Duration > 0) {
				t = clip.Decoded.StartTime + ((time - clip.Decoded.StartTime) % clip.Duration);
				if (t < clip.Decoded.StartTime) t += clip.Duration;
			}
			else {
				t = Math.Clamp(time, clip.Decoded.StartTime, clip.Decoded.StopTime);
			}

			foreach (var target in clip.TransformTargets) {
				var tr = target.Target;
				if (target.PosX >= 0) tr.LocalX = DecodedClip.Eval(clip.Decoded.Curves[target.PosX].Keys, t);
				if (target.PosY >= 0) tr.LocalY = DecodedClip.Eval(clip.Decoded.Curves[target.PosY].Keys, t);
				if (target.PosZ >= 0) tr.LocalZ = DecodedClip.Eval(clip.Decoded.Curves[target.PosZ].Keys, t);
				if (target.RotX >= 0) tr.LocalRotationX = DecodedClip.Eval(clip.Decoded.Curves[target.RotX].Keys, t);
				if (target.RotY >= 0) tr.LocalRotationY = DecodedClip.Eval(clip.Decoded.Curves[target.RotY].Keys, t);
				if (target.RotZ >= 0) tr.LocalRotationZ = DecodedClip.Eval(clip.Decoded.Curves[target.RotZ].Keys, t);
				if (target.RotW >= 0) tr.LocalRotationW = DecodedClip.Eval(clip.Decoded.Curves[target.RotW].Keys, t);
				if (target.ScX >= 0) tr.LocalScaleX = DecodedClip.Eval(clip.Decoded.Curves[target.ScX].Keys, t);
				if (target.ScY >= 0) tr.LocalScaleY = DecodedClip.Eval(clip.Decoded.Curves[target.ScY].Keys, t);
				if (target.ScZ >= 0) tr.LocalScaleZ = DecodedClip.Eval(clip.Decoded.Curves[target.ScZ].Keys, t);
			}
		}
	}
}
public class SceneObject
{
	public string Name = "";
	public bool Active = true;
	public SceneTransform Transform { get; } = new();
	public MuseDashScene Scene { get; internal set; } = null!;
	readonly List<SceneComponent> components = [];
	public IReadOnlyList<SceneComponent> Components => components;
	public System.Numerics.Vector4 Color = new(1, 1, 1, 1);

	public SceneObject() { Transform.Object = this; components.Add(Transform); }

	public T AddComponent<T>(T component) where T : SceneComponent {
		component.Object = this; components.Add(component); return component;
	}
	public T? GetComponent<T>() where T : SceneComponent {
		foreach (var c in components) if (c is T t) return t; return null;
	}
	public IEnumerable<T> GetComponents<T>() where T : SceneComponent {
		foreach (var c in components) if (c is T t) yield return t;
	}
	public void Awake() { foreach (var c in components) c.Awake(); }

	public string Dump(StringBuilder? sb = null, int depth = 0) {
		sb ??= new();
		string indent = new(' ', depth * 2);
		string compInfo = components.Count > 1 ? $" [{string.Join(", ", components.Skip(1).Select(c => c.GetType().Name))}]" : "";
		sb.AppendLine($"{indent}{Name} ({Transform.LocalX:F2}, {Transform.LocalY:F2}){compInfo}");
		foreach (var child in Transform.Children) child.Object.Dump(sb, depth + 1);
		return sb.ToString();
	}

	public System.Numerics.Vector4 GetColor() {
		var c = new System.Numerics.Vector4(1, 1, 1, 1);
		var p = this;
		while (p != null) { c *= p.Color; p = p.Transform.Parent?.Object; }
		return c;
	}
}

public class MuseDashScene : ISceneDescriptor
{
	public const float MUSEDASH_MULTIPLIER_POSITIONS = 1;
	readonly PathwayInformation[] pathwayInfo = new PathwayInformation[4];
	readonly List<SceneObject> allObjects = [];
	readonly List<SceneRenderer> sortedRenderers = [];
	readonly List<SceneAnimator> animators = [];
	SceneObject? root;
	public readonly MuseDashSceneInfo SceneInfo;
	readonly Dictionary<long, ITexture> textureCache = [];
	readonly Dictionary<long, ModelData> loadedModels = [];
	readonly Dictionary<long, SceneObject> pathIdToObject = [];

	internal ModelData LoadModel(MonoBehaviour? skeletonAnimation) {
		if (loadedModels.TryGetValue(skeletonAnimation!.m_PathID, out var mdl)) return mdl;

		loadedModels[skeletonAnimation.m_PathID] = mdl = MuseDashModelConverter.MD_GetModelData(EngineCore.Level, skeletonAnimation!);
		return mdl;
	}

	internal ITexture LoadTexture(AssetStudio.Texture2D? texture2D) {
		if (textureCache.TryGetValue(texture2D!.m_PathID, out var tex)) return tex;
		textureCache[texture2D.m_PathID] = tex = MuseDashCompatibility.ConvertTexture(EngineCore.Level, texture2D!);
		return tex;
	}

	public MuseDashScene(MuseDashSceneInfo info) {
		SceneInfo = info;
	}

	public static MuseDashScene? GetScene(ReadOnlySpan<char> name) {
		var sceneInfo = MuseDashSceneInfo.GetSceneInfo(name);
		if (sceneInfo == null)
			return null;

		var sceneGameObject = MuseDashCompatibility.StreamingAssets.FindAssetByName<GameObject>(sceneInfo.MapName)!;
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

	SceneObject ImportGameObject(GameObject unityGO, SceneTransform? parent) {
		if (pathIdToObject.TryGetValue(unityGO.m_PathID, out var existing)) return existing;

		var obj = new SceneObject { Name = unityGO.m_Name ?? "", Active = unityGO.m_IsActive, Scene = this };
		allObjects.Add(obj);
		pathIdToObject[unityGO.m_PathID] = obj;

		var unityTransform = unityGO.GetFirstComponent<Transform>();
		if (unityTransform != null) obj.Transform.ReadFrom(unityTransform);
		if (parent != null) obj.Transform.SetParent(parent);

		foreach (var comp in unityGO.Components) {
			switch (comp) {
				case SpriteRenderer sr: obj.AddComponent(new SceneSpriteRenderer(sr)); break;
				case Animator animator: obj.AddComponent(new SceneAnimator { UnityAnimator = animator }); break;
			}
		}

		if (unityTransform != null)
			foreach (var childTransform in unityTransform.GetChildren()) {
				var childGO = childTransform.GetGameObject();
				if (childGO != null) ImportGameObject(childGO, obj.Transform);
			}
		return obj;
	}

	void BuildRenderOrder() {
		sortedRenderers.Clear();
		foreach (var obj in allObjects) {
			if (!IsActiveInHierarchy(obj)) continue;
			foreach (var renderer in obj.GetComponents<SceneRenderer>()) sortedRenderers.Add(renderer);
		}
		sortedRenderers.Sort((a, b) => {
			int cmp = a.SortingLayerID.CompareTo(b.SortingLayerID);
			if (cmp != 0) return cmp;
			cmp = a.SortingOrder.CompareTo(b.SortingOrder);
			if (cmp != 0) return cmp;
			a.Transform.GetWorldPosition(out _, out _, out float az);
			b.Transform.GetWorldPosition(out _, out _, out float bz);
			return bz.CompareTo(az);
		});
	}

	internal static bool IsActiveInHierarchy(SceneObject obj) {
		var current = obj;
		while (current != null) {
			if (!current.Active) return false;
			current = current.Transform.Parent?.Object;
		}
		return true;
	}

	Sound? BeginSound;
	Sound? FeverSound;
	Sound? UnpauseSound;
	Sound? FullComboSound;

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
	MusicTrack? PressIdle;

	MD_SpineActionController BossAnims = null!;

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

	public void Initialize(DashGameLevel game) {
		BeginSound = MuseDashCompatibility.LoadSoundFromName(game, SceneInfo.Sounds.Begin ?? throw new NullReferenceException());
		FeverSound = MuseDashCompatibility.LoadSoundFromName(game, SceneInfo.Sounds.Fever ?? throw new NullReferenceException());
		UnpauseSound = MuseDashCompatibility.LoadSoundFromName(game, SceneInfo.Sounds.Unpause ?? throw new NullReferenceException());
		FullComboSound = MuseDashCompatibility.LoadSoundFromName(game, SceneInfo.Sounds.FullCombo ?? throw new NullReferenceException());

		BeginSound.BindVolumeToConVar(AudioSettings.snd_voicevolume);
		FeverSound.BindVolumeToConVar(AudioSettings.snd_voicevolume);
		UnpauseSound.BindVolumeToConVar(AudioSettings.snd_voicevolume);
		FullComboSound.BindVolumeToConVar(AudioSettings.snd_voicevolume);

		PressIdle = MuseDashCompatibility.LoadMusicFromName(game, SceneInfo.Sounds.PressIdle ?? throw new NullReferenceException());

		PressIdle.BindVolumeToConVar(AudioSettings.snd_hitvolume);

		var assets = MuseDashCompatibility.StreamingAssets;

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



	MonoBehaviourReader getSpineController(string name) => new(MuseDashCompatibility.StreamingAssets.FindAssetByName<GameObject>(name)!.GetFirstComponent<MonoBehaviour>()!);
	MonoBehaviourReader getSpineControllerPrefab(string prefab, string name) => new(MuseDashCompatibility.StreamingAssets.LoadAsset<GameObject>(prefab + "/" + name)!.GetRequiredResult().GetFirstComponent<MonoBehaviour>()!);

	public delegate string ResolverFn(in RequestInfo info);

	void ProcessThreeSpeedAnimations(MD_Animations3Speed table, in RequestInfo req, MonoBehaviourReader reader) {
		ref MD_SpineActionController speedToEdit = ref table.GetSpeedForEdit(req.speed, req.dir);
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

	public void RenderBackground(DashGameLevel game) {
		Rlgl.PushMatrix();
		Rlgl.Scalef(MUSEDASH_MULTIPLIER_POSITIONS, MUSEDASH_MULTIPLIER_POSITIONS, 1);
		foreach (var renderer in sortedRenderers) renderer.Render(this);
		Rlgl.PopMatrix();
	}

	public void RenderPathway(DashGameLevel game, PathwaySide side, float alpha, float size, float rotation) {
		var obj = ((SceneObject)pathwayInfo[(int)side].UserData!);
		var transform = obj.Transform;
		transform.LocalRotationX = 0; transform.LocalRotationY = 0;
		transform.LocalRotationZ = NMath.Remap(rotation, 0, 1, -1, 1);
		transform.LocalRotationW = 1;
		transform.LocalScaleX = size; transform.LocalScaleY = size;
		obj.Color.W = alpha / 255f;
	}

	public void Think(DashGameLevel game) {
		float dt = (float)globals.CurTimeDelta;
		foreach (var anim in animators) anim.Evaluate(dt);
	}

	public void Refresh(DashGameLevel game) { }
	public void PlaySound(SceneSound sound, int hits) {
		switch (sound) {
			case SceneSound.Begin: BeginSound?.Play(); break;
			case SceneSound.Fever: FeverSound?.Play(); break;
			case SceneSound.Unpause: UnpauseSound?.Play(); break;
			case SceneSound.FullCombo: FullComboSound?.Play(); break;
		}
	}

	public double GetBossAnimationTime(BossAnimationType type, AnimationHandler anim) {
		MD_ActionData? actions = GetBossAnimation(type);
		if (actions == null)
			return 0;

		return anim.GetModelData()?.FindAnimation(actions?.ActionIdx?.FirstOrDefault())?.Duration ?? 0;
	}

	public double PlayBossAnimation(int channel, BossAnimationType type, AnimationHandler anim) {
		MD_ActionData? actions = GetBossAnimation(type);
		if (actions == null)
			return 0;
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
			case BossAnimationType.Standby0: actions = BossAnims.Get("standby"); break;
			case BossAnimationType.In: actions = BossAnims.Get("in"); break;
			case BossAnimationType.Out: actions = BossAnims.Get("out"); break;
			case BossAnimationType.CloseAttackSlow: actions = BossAnims.Get("boss_close_atk_1"); break;
			case BossAnimationType.CloseAttackFast: actions = BossAnims.Get("boss_close_atk_2"); break;
			case BossAnimationType.Hurt: actions = BossAnims.Get("boss_hurt"); break;
			case BossAnimationType.From0To1: actions = BossAnims.Get("boss_far_atk_1_start"); break;
			case BossAnimationType.AttackAir1: actions = BossAnims.Get("boss_far_atk_1_L"); break;
			case BossAnimationType.AttackGround1: actions = BossAnims.Get("boss_far_atk_1_R"); break;
			case BossAnimationType.From1To0: actions = BossAnims.Get("boss_far_atk_1_end"); break;
			case BossAnimationType.From0To2: actions = BossAnims.Get("boss_far_atk_2_start"); break;
			case BossAnimationType.AttackAir2: actions = BossAnims.Get("boss_far_atk_2"); break;
			case BossAnimationType.AttackGround2: actions = BossAnims.Get("boss_far_atk_2"); break;
			case BossAnimationType.From2To0: actions = BossAnims.Get("boss_far_atk_2_end"); break;
			case BossAnimationType.From1To2: actions = BossAnims.Get("atk_1_to_2"); break;
			case BossAnimationType.From2To1: actions = BossAnims.Get("atk_2_to_1"); break;
		}
		return actions;
	}

	public string? GetEnemyApproachAnimation(DashEnemy enemy, out double time) {
		time = 0;

		MD_SpineActionController? anim = null;

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
		string request = type == HitAnimationType.Great ? "note_out_g" : "note_out_p";
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
					EntityVariant.Medium2 => enemy.Pathway == PathwaySide.Top ? AirMedium1Model : RoadMedium2Model,
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
		var s = MasherAnims.GetSpeed(speed, dir).Get("note_multihit_hurt")?.ActionIdx;
		return s?[Random.Shared.Next(0, s.Length)] ?? "";
	}
	public ref readonly PathwayInformation GetPathwayInformation(PathwaySide pathway) => ref pathwayInfo[(int)pathway];
	public Color GetPathwayColor(PathwaySide side) => GetPathwayInformation(side).Color;
	public Vector2F GetPathwayPosition(PathwaySide side) => GetPathwayInformation(side).Position;
	public MusicTrack? GetPressIdleSound() => PressIdle;
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
