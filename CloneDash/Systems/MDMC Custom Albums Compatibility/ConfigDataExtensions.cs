using CloneDash.Systems.Muse_Dash_Compatibility;
using System.Text.Json.Nodes;
using static CloneDash.MuseDashCompatibility;

namespace CustomAlbums.Utilities
{
	public static class ConfigDataExtensions
	{
		public static bool IsAprilFools(this NoteConfigData config) {
			return config.prefab_name.EndsWith("_fool");
		}

		public static NoteType GetNoteType(this NoteConfigData config) {
			return (NoteType)config.type;
		}

		public static bool IsAnyScene(this NoteConfigData config) {
			return config.scene == "0";
		}

		public static bool IsAnyPathway(this NoteConfigData config) {
			return config.pathway == 0 && config.score == 0 && config.fever == 0 && config.damage == 0;
		}

		public static bool IsAnySpeed(this NoteConfigData config) {
			return config.GetNoteType() == NoteType.Boss
				   || config.GetNoteType() == NoteType.None
				   || config.ibms_id == "16"
				   || config.ibms_id == "17";
		}

		public static bool IsPhase2BossGear(this NoteConfigData config) {
			return config.GetNoteType() == NoteType.Block && config.boss_action.EndsWith("_atk_2");
		}

		public static MusicConfigData ToMusicConfigData(this JsonNode node) {
			var config = new MusicConfigData();
			config.id = node["id"]?.GetValue<int>() ?? -1;

			config.time = node["time"].GetValue<decimal>();
			config.note_uid = node["note_uid"]?.GetValue<string>() ?? string.Empty;
			config.length = node["length"].GetValue<decimal>();
			config.pathway = node["pathway"]?.GetValue<int>() ?? 0;
			config.blood = node["blood"]?.GetValue<bool>() ?? false;

			return config;
		}
	}
}
