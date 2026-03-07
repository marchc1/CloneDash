using CloneDash.Scenes;

namespace CloneDash.Game.Events;

public class BossMasher(DashGameLevel game, int type) : DashEvent(game)
{
	public override void Activate() {
		Game.Boss.Masher();
	}

	public override void OnBuild() {
		base.OnBuild();

		var boss = Game.Boss;
		var time = Game.Scene.GetBossAnimationTime(BossAnimationType.MultiAttack, boss.Animations);

		Game.LoadEntity(new() {
			Type = EntityType.Masher,
			Pathway = PathwaySide.Both,
			Variant = type == 1 ? EntityVariant.BossMasher : EntityVariant.BossMasherEnd,
			ShowTime = Time - time,
			HitTime = Time,
			Length = Length
		});
	}
}
