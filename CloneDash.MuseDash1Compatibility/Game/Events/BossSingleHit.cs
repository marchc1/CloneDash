using CloneDash.Common.Gamemodes.MuseDash;
using CloneDash.Common.Gamemodes.MuseDash.V1;
using System.Diagnostics;

namespace CloneDash.Game.Events;

public class BossSingleHit(DashEventComponents components) : DashEvent(components)
{
	public override void Activate() {
		Components.Boss.SingleHit();
	}

	public override void OnBuild() {
		base.OnBuild();

		var boss = Components.Boss;
		var scene = Components.Chart.GetSceneAtTime(Time);
		var anims = boss.Visuals[scene.GetSceneArrayIndex()].Animations;
		var speed = scene.GetBossAnimationTime(BossAction == "boss_close_atk_2" ? BossAnimationType.CloseAttackFast : BossAnimationType.CloseAttackSlow, anims);
		Debug.Assert(speed != 0);

		Components.Chart.LoadEntity(new() {
			Type = MuseDash1EntityType.Single,
			Pathway = PathwaySet.Any,
			Variant = BossAction == "boss_close_atk_2" ? EntityVariant.BossHitFast : EntityVariant.BossHitSlow,
			ShowTime = Time - speed,
			HitTime = Time
		});
	}
}
