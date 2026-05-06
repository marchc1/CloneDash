using CloneDash.Common.Gamemodes.MuseDash.V1;

namespace CloneDash.Common.Gamemodes.MuseDash.V1.Data;

public class MD1_SongChartEvent
{
	public float Time;
	public double Length;
	public EventType Type;
	public string? BossAction;

	public int? Damage;
	public int? Score;
	public int? Fever;
}
