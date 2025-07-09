using CloneDash.Scenes;

namespace CloneDash.Game.Events;

public class BossSingleHit(DashGameLevel game) : DashEvent(game)
{
	public override void Activate() {
		Game.Boss.SingleHit();
	}

	public override void OnBuild() {
		base.OnBuild();

		var boss = Game.Boss;
		var animation = Game.Scene.GetBossAnimation(BossAction == "boss_close_atk_2" ? BossAnimationType.CloseAttackFast : BossAnimationType.CloseAttackSlow, out var speed);

		Game.LoadEntity(new() {
			Type = EntityType.Single,
			Pathway = PathwaySide.Both,
			Variant = BossAction == "boss_close_atk_2" ? EntityVariant.BossHitFast : EntityVariant.BossHitSlow,
			ShowTime = Time - speed,
			HitTime = Time
		});
	}
}
