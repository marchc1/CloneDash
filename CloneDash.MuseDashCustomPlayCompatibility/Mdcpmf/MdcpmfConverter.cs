using CloneDash.Common.Data;
using CloneDash.Compatibility.MuseDash;
using CloneDash.CustomAlbumsCompatibility.CustomAlbums;
using Nucleus;
using System.Text.Json.Nodes;

namespace CloneDash.MuseDashCustomPlayCompatibility.Mdcpmf;

public static class MdcpmfConverter
{
	public class Result
	{
		public readonly JsonArray Notes = [];
		public readonly List<SceneEvent> SceneEvents = [];
		public readonly List<TempoChange> TempoChanges = [];
		public string Scene = "scene_01";
		public readonly Dictionary<int, List<(double Time, double Speed)>> TimeRemaps = [];

		public readonly Dictionary<string, int> Dropped = [];
		public readonly Dictionary<string, int> Approximated = [];

		internal void Drop(string why) => Dropped[why] = Dropped.GetValueOrDefault(why) + 1;
		internal void Approximate(string why) => Approximated[why] = Approximated.GetValueOrDefault(why) + 1;
	}

	static readonly Dictionary<string, int> sceneNames = new(StringComparer.OrdinalIgnoreCase) {
		["scene_space"] = 1,
		["scene_spacestation"] = 1,
		["scene_city"] = 2,
		["scene_retrocity"] = 2,
		["scene_castle"] = 3,
		["scene_rain"] = 4,
		["scene_rainynight"] = 4,
		["scene_candy"] = 5,
		["scene_candyland"] = 5,
		["scene_oriental"] = 6,
		["scene_groove"] = 7,
		["scene_groovecoaster"] = 7,
		["scene_touhou"] = 8,
		["scene_gensokyo"] = 8,
		["scene_djmax"] = 9,
		["scene_miku"] = 10,
	};

	public static int GetSceneIndex(string mdcpScene, out bool known) {
		known = true;
		if (sceneNames.TryGetValue(mdcpScene, out int idx))
			return idx;
		if (mdcpScene.StartsWith("scene_") && int.TryParse(mdcpScene.AsSpan(6, Math.Min(2, mdcpScene.Length - 6)), out idx) && idx is >= 1 and <= 10)
			return idx;

		known = false;
		return 1;
	}

	static readonly string[] sceneToggleIBMS = ["1O", "1P", "1Q", "1R", "1S", "1T", "1U", "1V", "1W", "1X"];

	static readonly Dictionary<string, string> simpleKinds = new() {
		["large_1"] = "0A",
		["large_2"] = "0B",
		["gear"] = "0H",
		["ghost"] = "21",
		["special_hp"] = "22",
		["special_note"] = "23",
		["bms_touhou_ppoint"] = "20",

		["boss_ra"] = "1A",
		["boss_ra_out"] = "1B",
		["boss_ra_atk_1"] = "11",
		["boss_ra_atk_1_wait"] = "12",
		["boss_ra_far_atk_1"] = "13",
		["boss_ra_far_atk_2"] = "14",
		["boss_ra_far_atk_3"] = "15",
		["boss_ra_gear"] = "18",
		["boss_ra_far_atk_1_action_start"] = "1C",
		["boss_ra_far_atk_1_action_end"] = "1D",
		["boss_ra_far_atk_2_action_start"] = "1E",
		["boss_ra_far_atk_2_action_end"] = "1F",

		["vfx_visibility_hide"] = "1J",
		["vfx_visibility_show"] = "1K",
		["vfx_bossvisibility_hide"] = "1L",
		["vfx_bossvisibility_show"] = "1M",

		["bms_hidebg"] = "25",
		["bms_showbg"] = "26",
		["bms_scrollup"] = "27",
		["bms_scrolldown"] = "28",
		["bms_scrollend"] = "29",
		["bms_glitch"] = "2A",
		["bms_glitchend"] = "2B",
		["bms_rgb"] = "2C",
		["bms_rgbend"] = "2D",
		["bms_darkedge"] = "2E",
		["bms_darkedgeend"] = "2F",
		["bms_nosignal"] = "2G",
		["bms_nosignalend"] = "2H",
		["bms_flare"] = "2I",
		["bms_flareintense"] = "2J",
		["bms_flareend"] = "2K",
		["bms_scenepause"] = "2N",
		["bms_scenepauseend"] = "2O",
	};

	static readonly HashSet<string> silentKinds = [
		"effect_timing",
		"effect_scene_timing",
		"preset_beatbpm",
		"effect_common_midpointshow",
		"effect_af5_colortip",
	];

	static string? directional(string normal, string up, string down, string inType) => inType switch {
		"up" => up,
		"down" => down,
		_ => normal
	};

	public static string? GetIBMS(MdcpmfNote note) => note.BaseKind switch {
		"small_1" => directional("01", "02", "03", note.InType),
		"mid_1" => directional("04", "05", "06", note.InType),
		"mid_2" => directional("07", "08", "09", note.InType),
		"assault" => note.InType == "down" ? "0I" : "0C",
		"hammer" => note.InType == "up" ? "0J" : "0D",
		"double" or "double_ra" => "0E",
		"long" or "long_short" => "0F",
		"punch" or "punch_ra" => "0G",
		"boss_ra_punch" => "16",
		"boss_ra_punch_out" => "17",
		_ => simpleKinds.GetValueOrDefault(note.BaseKind)
	};

	static Bms? noteLookup;
	static Dictionary<string, NoteConfigData> GetNoteLookup() {
		if (noteLookup == null) {
			noteLookup = new Bms();
			noteLookup.InitNoteData();
		}
		return noteLookup.NoteData;
	}

	static NoteConfigData? FindConfig(string ibms, int pathway, int speed, string scene) {
		var lookup = GetNoteLookup();
		if (lookup.TryGetValue(Bms.GetNoteDataKey(ibms, pathway, speed, scene), out var config))
			return config;
		if (lookup.TryGetValue(Bms.GetNoteDataKey(ibms, pathway, speed, "0"), out config))
			return config;
		return null;
	}

	enum BossPhase { Idle, Far1, Far2 }

	static BossPhase? RequiredPhase(string baseKind) => baseKind switch {
		"boss_ra_far_atk_1" => BossPhase.Far1,
		"boss_ra_far_atk_2" or "boss_ra_far_atk_3" => BossPhase.Far2,
		"boss_ra_atk_1" or "boss_ra_atk_1_wait" or "boss_ra_punch" or "boss_ra_punch_out" or "boss_ra_out" => BossPhase.Idle,
		_ => null
	};

	static string? TransitionKind(BossPhase from, BossPhase to) => (from, to) switch {
		(BossPhase.Idle, BossPhase.Far1) => "boss_ra_far_atk_1_action_start",
		(BossPhase.Idle, BossPhase.Far2) => "boss_ra_far_atk_2_action_start",
		(BossPhase.Far1, BossPhase.Idle) => "boss_ra_far_atk_1_action_end",
		(BossPhase.Far2, BossPhase.Idle) => "boss_ra_far_atk_2_action_end",
		(BossPhase.Far1, BossPhase.Far2) => "boss_ra_atk_1_to_2",
		(BossPhase.Far2, BossPhase.Far1) => "boss_ra_atk_2_to_1",
		_ => null
	};

	const double BOSS_TRANSITION_LEAD = 1.0;

	static List<MdcpmfNote> InsertBossTransitions(List<MdcpmfNote> notes, Result result) {
		List<MdcpmfNote> ret = new(notes.Count);
		BossPhase phase = BossPhase.Idle;
		double lastBossTime = double.NegativeInfinity;

		foreach (var note in notes) {
			switch (note.BaseKind) {
				case "boss_ra": phase = BossPhase.Idle; break;
				case "boss_ra_far_atk_1_action_start": phase = BossPhase.Far1; break;
				case "boss_ra_far_atk_2_action_start": phase = BossPhase.Far2; break;
				case "boss_ra_far_atk_1_action_end":
				case "boss_ra_far_atk_2_action_end": phase = BossPhase.Idle; break;
				case "boss_ra_atk_1_to_2": phase = BossPhase.Far2; break;
				case "boss_ra_atk_2_to_1": phase = BossPhase.Far1; break;
			}

			var required = RequiredPhase(note.BaseKind);
			if (required != null && required != phase) {
				var kind = TransitionKind(phase, required.Value);
				if (kind != null) {
					double t = Math.Max(lastBossTime + 0.001, note.Time - BOSS_TRANSITION_LEAD);
					t = Math.Min(t, note.Time);
					ret.Add(new MdcpmfNote { Kind = kind, BaseKind = kind, Time = t, InType = "normal", Speed = note.Speed, Layer = note.Layer });
					result.Approximate("inserted boss phase change");
				}
				phase = required.Value;
			}

			if (note.BaseKind.StartsWith("boss_ra"))
				lastBossTime = note.Time;
			ret.Add(note);
		}

		return ret;
	}

	static void AddNote(Result result, ref int objectId, decimal time, NoteConfigData config, decimal length, int pathway, int layer) {
		result.Notes.Add(new JsonObject {
			["id"] = objectId++,
			["time"] = time,
			["note_uid"] = config.uid,
			["length"] = length,
			["pathway"] = pathway,
			["layer"] = layer,
			["blood"] = false
		});
	}

	public static Result Convert(MdcpmfChart chart) {
		Result result = new();

		int sceneIndex = GetSceneIndex(chart.Scene, out bool knownScene);
		if (!knownScene)
			result.Approximate($"unknown scene '{chart.Scene}', using scene_01");
		result.Scene = $"scene_{sceneIndex:00}";

		// Tempo. Beat is in whole notes, same as the Conductor
		double beat = 0, lastTime = 0, lastBPM = chart.BPM;
		foreach (var block in chart.BpmBlocks) {
			if (lastBPM > 0)
				beat += (block.Time - lastTime) * lastBPM / 240.0;
			lastTime = block.Time;
			lastBPM = block.BPM;
			result.TempoChanges.Add(new TempoChange(block.Time, (int)Math.Round(beat), block.BPM));
		}

		var notes = InsertBossTransitions(chart.Notes, result);

		int objectId = 1;
		foreach (var note in notes) {
			Interlude.Spin(submessage: "Reading CustomPlay chart...");

			if (note.Kind == "boss_ra_atk_1_to_2" || note.Kind == "boss_ra_atk_2_to_1") {
				var swap = FindConfig(note.Kind == "boss_ra_atk_1_to_2" ? "1G" : "1H", 0, Math.Clamp(note.Speed + 1, 1, 3), result.Scene);
				if (swap != null) AddNote(result, ref objectId, (decimal)note.Time, swap, 0, 0, note.Layer);
				continue;
			}

			if (note.Kind == "vfx_changescene") {
				int idx = GetSceneIndex(note.InType, out bool known);
				if (!known)
					result.Approximate($"unknown scene '{note.InType}', using scene_01");
				var sceneConfig = FindConfig(sceneToggleIBMS[idx - 1], 0, 1, "0");
				if (sceneConfig != null)
					AddNote(result, ref objectId, (decimal)note.Time, sceneConfig, 0, 0, note.Layer);
				else
					result.SceneEvents.Add(new SceneEvent { time = (decimal)note.Time, uid = $"SceneEvent/{idx:00}" });
				continue;
			}

			if (note.Lane == MdcpmfLane.Middle) {
				// todo: middle lane needs gamemode support
				result.Drop($"TODO: {note.BaseKind} middle lane");
				continue;
			}

			var ibms = GetIBMS(note);
			if (ibms == null) {
				if (!silentKinds.Contains(note.BaseKind) && !note.BaseKind.StartsWith("effect_"))
					result.Drop(note.Kind);
				continue;
			}

			int speed = Math.Clamp(note.Speed + 1, 1, 3);
			decimal time = (decimal)note.Time;
			decimal length = 0;

			switch (note.BaseKind) {
				case "long_short":
					length = 0.001m;
					break;
				case "long" or "punch" or "punch_ra" or "boss_ra_punch" or "boss_ra_punch_out":
					if (note.HasOutTime)
						length = (decimal)(note.OutTime - note.Time);
					else if (note.BaseKind == "long")
						length = 0.001m;
					break;
			}

			if (note.IsCurve)
				result.Approximate("TODO: curve sustains");
			if (note.IsReverseTrack)
				result.Approximate("TODO: reverse track");
			if (note.Modifiers != null)
				result.Approximate("TODO: note modifiers");

			if (note.BaseKind is "double" or "double_ra") {
				var bottom = FindConfig(ibms, 0, speed, result.Scene);
				var top = FindConfig(ibms, 1, speed, result.Scene);
				if (bottom == null || top == null) {
					result.Drop($"{note.Kind} (no note config)");
					continue;
				}
				AddNote(result, ref objectId, time, bottom, 0, 0, note.Layer);
				AddNote(result, ref objectId, time, top, 0, 1, note.Layer);
				continue;
			}

			int pathway = note.Lane == MdcpmfLane.Air ? 1 : 0;
			var config = FindConfig(ibms, pathway, speed, result.Scene);
			if (config == null) {
				result.Drop($"{note.Kind} (no note config for {ibms}, pathway {pathway}, speed {speed})");
				continue;
			}

			AddNote(result, ref objectId, time, config, length, pathway, note.Layer);
		}

		foreach (var timing in chart.Timings) {
			if (!result.TimeRemaps.TryGetValue(timing.Layer, out var keys))
				result.TimeRemaps[timing.Layer] = keys = [];
			keys.Add((timing.Time, timing.Value));
		}

		return result;
	}

	public static void LogReport(Result result, string chartName) {
		foreach (var kvp in result.Dropped)
			Logs.Warn($"CustomPlay: '{chartName}': dropped {kvp.Value}x {kvp.Key}");
		foreach (var kvp in result.Approximated)
			Logs.Info($"CustomPlay: '{chartName}': {kvp.Value}x {kvp.Key}");
	}
}
