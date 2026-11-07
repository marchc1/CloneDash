using CloneDash.Common.Gamemodes.MuseDash;
using CloneDash.Common.Gamemodes.MuseDash.V1;

namespace CloneDash.Game.Events;

public class BossMasher(DashEventComponents components, int type) : DashEvent(components)
{
	public DashEnemy? Masher { get; private set; }

	public override void OnBuild() {
		base.OnBuild();

		Masher = Components.Chart.LoadEntity(new() {
			Type = MuseDash1EntityType.Masher,
			Pathway = PathwaySet.Any,
			Variant = type == 1 ? EntityVariant.BossMasher : EntityVariant.BossMasherEnd,
			ShowTime = Time - Dt,
			HitTime = Time,
			Length = Length,
			Speed = Speed,
			LeftGreatRange = LeftGreatRange,
			LeftPerfectRange = LeftPerfectRange,
			RightGreatRange = RightGreatRange,
			RightPerfectRange = RightPerfectRange,
			Fever = Fever ?? 0,
			Damage = Damage ?? 0,
			Score = Score ?? 0
		});
	}
}
