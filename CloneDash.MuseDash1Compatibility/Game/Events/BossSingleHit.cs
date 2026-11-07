using CloneDash.Common.Gamemodes.MuseDash;
using CloneDash.Common.Gamemodes.MuseDash.V1;

namespace CloneDash.Game.Events;

public class BossSingleHit(DashEventComponents components) : DashEvent(components)
{
	public override void OnBuild() {
		base.OnBuild();

		Components.Chart.LoadEntity(new() {
			Type = MuseDash1EntityType.Single,
			Pathway = PathwaySet.Any,
			Variant = BossAction == "boss_close_atk_2" ? EntityVariant.BossHitFast : EntityVariant.BossHitSlow,
			ShowTime = Time - Dt,
			HitTime = Time,
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
