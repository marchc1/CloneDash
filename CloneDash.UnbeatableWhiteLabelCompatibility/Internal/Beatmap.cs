using System;
using System.Collections.Generic;
using System.Text;

namespace CloneDash.Unbeatable.Internal;

public class Beatmap
{
	public float countdownEnd {
		get {
			return this.notes[0].time;
		}
	}

	public int GetSignificantNoteCount() {
		int num = 0;
		NoteInfo noteInfo = null;
		foreach (NoteInfo noteInfo2 in this.notes) {
			if (noteInfo2.type != NoteType.Freestyle || noteInfo == null || noteInfo.type != NoteType.Freestyle || noteInfo2.side != noteInfo.side) {
				num++;
			}
		}
		return num;
	}

	public static bool TrySplitPath(string path, out string song, out string difficulty) {
		string[] array = path.Split('/', StringSplitOptions.None);
		if (array.Length == 2) {
			song = array[0];
			difficulty = array[1];
			return true;
		}
		song = "";
		difficulty = "";
		return false;
	}
	
	public GeneralInfo general = new GeneralInfo();
	public MetadataInfo metadata = new MetadataInfo();
	public List<EventInfo> events = new List<EventInfo>();
	public List<TimingPointInfo> timingPoints = new List<TimingPointInfo>();
	public List<HitObjectInfo> hitObjects = new List<HitObjectInfo>();
	public List<FlipInfo> flips = new List<FlipInfo>();
	public List<NoteInfo> notes = new List<NoteInfo>();
	public List<CommandInfo> commands = new List<CommandInfo>();
}
