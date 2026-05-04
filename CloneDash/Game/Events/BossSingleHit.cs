using CloneDash.Scenes;
using System.Diagnostics;

namespace CloneDash.Game.Events;

public class BossSingleHit(DashGameLevel game) : DashEvent(game)
{
	public override void Activate() {
		Game.Boss.SingleHit();
	}

	public override void OnBuild() {
		base.OnBuild();

		var boss = Game.Boss;
		var speed = Game.Scene.GetBossAnimationTime(BossAction == "boss_close_atk_2" ? BossAnimationType.CloseAttackFast : BossAnimationType.CloseAttackSlow, boss.Animations);
		Debug.Assert(speed != 0);

		Game.LoadEntity(new() {
			Type = EntityType.Single,
			Pathway = PathwaySide.Both,
			Variant = BossAction == "boss_close_atk_2" ? EntityVariant.BossHitFast : EntityVariant.BossHitSlow,
			ShowTime = Time - speed,
			HitTime = Time
		});
	}
}
