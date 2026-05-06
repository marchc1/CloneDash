using CloneDash.Common.Gamemodes.MuseDash;
using CloneDash.Common.Gamemodes.MuseDash.V1;
using System.Diagnostics;

namespace CloneDash.Game.Events;

public class BossMasher(MuseDash1Game game, int type) : DashEvent(game)
{
	public override void Activate() {
		Game.Boss.Masher();
	}

	public override void OnBuild() {
		base.OnBuild();

		var boss = Game.Boss;
		var scene = Game.GetSceneAtTime(Time);
		var anims = boss.Visuals[scene.GetSceneArrayIndex()].Animations;
		var time = scene.GetBossAnimationTime(BossAnimationType.MultiAttack, anims);

		Debug.Assert(time != 0);
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
