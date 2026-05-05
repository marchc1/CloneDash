using CloneDash.Common.Gamemodes.MuseDash.V1;

namespace CloneDash.Data
{
	public class ChartEvent
	{
		public float Time;
		public double Length;
		public EventType Type;
		public string? BossAction;

		public int? Damage;
		public int? Score;
		public int? Fever;
	}
}
