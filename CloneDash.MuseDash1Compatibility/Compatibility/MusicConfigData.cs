using CloneDash.Common.Gamemodes.MuseDash.V1.Data;
using System.Text.Json.Serialization;

namespace CloneDash.Compatibility.MuseDash;

public class MusicConfigData
{
	public int id;
	public decimal time;
	public string note_uid;
	public decimal length;
	public bool blood;
	public int pathway;
	public string? pathway_name;
	public int layer;
	[JsonIgnore] public MD1_CurvePath? curve;

	public MusicConfigData Copy() {
		return new MusicConfigData() {
			id = id,
			time = time,
			note_uid = note_uid,
			length = length,
			blood = blood,
			pathway = pathway,
			pathway_name = pathway_name,
			layer = layer,
			curve = curve
		};
	}
}