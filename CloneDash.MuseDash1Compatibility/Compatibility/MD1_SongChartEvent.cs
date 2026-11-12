using CloneDash.Common.Gamemodes.MuseDash.V1;

namespace CloneDash.Common.Gamemodes.MuseDash.V1.Data;

public class MD1_SongChartEvent
{
	public double Time;
	public double Length;
	public double Dt;
	public EventType Type;
	public string? BossAction;

	public double LeftGreatRange = 0.08;
	public double LeftPerfectRange = 0.05;
	public double RightGreatRange = 0.08;
	public double RightPerfectRange = 0.05;
	public int Speed;

	public int? Damage;
	public int? Score;
	public int? Fever;
}
