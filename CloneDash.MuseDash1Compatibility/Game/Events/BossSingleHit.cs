using CloneDash.Common.Gamemodes.MuseDash;
using CloneDash.Common.Gamemodes.MuseDash.V1;
using System.Diagnostics;

namespace CloneDash.Game.Events;

public class BossSingleHit(MuseDash1Game game) : DashEvent(game)
{
	public override void Activate() {
		Game.Boss.SingleHit();
	}

	public override void OnBuild() {
		base.OnBuild();

		var boss = Game.Boss;
		var scene = Game.GetSceneAtTime(Time);
		var anims = boss.Visuals[scene.GetSceneArrayIndex()].Animations;
		var speed = scene.GetBossAnimationTime(BossAction == "boss_close_atk_2" ? BossAnimationType.CloseAttackFast : BossAnimationType.CloseAttackSlow, anims);
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
