namespace CloneDash.Game.Events;

public class BossMasher(CD_GameLevel game) : CD_BaseEvent(game)
{
	public override void Activate() {
		Game.Boss.Masher();
	}

	public override void OnBuild() {
		base.OnBuild();

		var boss = Game.Boss;
		var animation = Game.Scene.Boss.Multi.Attack;

		Game.LoadEntity(new() {
			Type = EntityType.Masher,
			Pathway = PathwaySide.Both,
			Variant = EntityVariant.BossMash,
			ShowTime = Time - (animation.Speed / 30d),
			HitTime = Time
		});
	}
}
