using CloneDash.Scenes;

namespace CloneDash.Game.Events;

public class BossMasher(DashGameLevel game) : DashEvent(game)
{
	public override void Activate() {
		Game.Boss.Masher();
	}

	public override void OnBuild() {
		base.OnBuild();

		var boss = Game.Boss;
		var animation = Game.Scene.GetBossAnimation(BossAnimationType.MultiAttack, out var time);

		Game.LoadEntity(new() {
			Type = EntityType.Masher,
			Pathway = PathwaySide.Both,
			Variant = EntityVariant.BossMash,
			ShowTime = Time - time,
			HitTime = Time
		});
	}
}
