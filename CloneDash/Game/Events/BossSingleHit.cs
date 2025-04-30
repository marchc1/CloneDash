namespace CloneDash.Game.Events;

public class BossSingleHit(CD_GameLevel game) : CD_BaseEvent(game)
{
	public override void Activate() {
		Game.Boss.SingleHit();
	}

	public override void OnBuild() {
		base.OnBuild();

		var boss = Game.Boss;
		var animation = BossAction == "boss_close_atk_2" ? Game.Scene.Boss.Close.AttackFast : Game.Scene.Boss.Close.AttackSlow;

		Game.LoadEntity(new() {
			Type = EntityType.Single,
			Pathway = PathwaySide.Both,
			Variant = BossAction == "boss_close_atk_2" ? EntityVariant.BossHitFast : EntityVariant.BossHitSlow,
			ShowTime = Time - (animation.Speed / 30d),
			HitTime = Time
		});
	}
}
