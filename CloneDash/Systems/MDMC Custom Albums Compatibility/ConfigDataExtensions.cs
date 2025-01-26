using System.Text.Json.Nodes;
using static CloneDash.MuseDashCompatibility;

namespace CustomAlbums.Utilities
{
	public static class ConfigDataExtensions
	{
		public static bool IsAprilFools(this NoteConfigData config) {
			return config.prefab_name.EndsWith("_fool");
		}
		//public static IBMSCode GetNoteType(this NoteConfigData config) {
			//return (NoteType)config.type;
		//}

		public static bool IsAnyScene(this NoteConfigData config) {
			return config.scene == "0";
		}

		public static bool IsAnyPathway(this NoteConfigData config) {
			return config.pathway == 0 && config.score == 0 && config.fever == 0 && config.damage == 0;
		}

		public static bool IsAnySpeed(this NoteConfigData config) {
			return false;
			//return config.GetNoteType() == NoteType.Boss
				   //|| config.GetNoteType() == NoteType.None
				   //|| config.ibms_id == "16"
				   //|| config.ibms_id == "17";
		}

		public static bool IsPhase2BossGear(this NoteConfigData config) {
			return false;
			//return config.GetNoteType() == NoteType.Block && config.boss_action.EndsWith("_atk_2");
		}
	}
}
