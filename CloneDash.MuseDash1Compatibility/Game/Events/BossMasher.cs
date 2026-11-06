using CloneDash.Common.Gamemodes.MuseDash;
using CloneDash.Common.Gamemodes.MuseDash.V1;
using System.Diagnostics;

namespace CloneDash.Game.Events;

public class BossMasher(DashEventComponents components, int type) : DashEvent(components)
{
	public override void Activate() {
		Components.Boss.Masher();
	}

	public override void OnBuild() {
		base.OnBuild();

		var boss = Components.Boss;
		var scene = Components.Chart.GetSceneAtTime(Time);
		var anims = boss.Visuals[scene.GetSceneArrayIndex()].Animations;
		var time = scene.GetBossAnimationTime(BossAnimationType.MultiAttack, anims);

		Debug.Assert(time != 0);
		Components.Chart.LoadEntity(new() {
			Type = MuseDash1EntityType.Masher,
			Pathway = PathwaySet.Any,
			Variant = type == 1 ? EntityVariant.BossMasher : EntityVariant.BossMasherEnd,
			ShowTime = Time - time,
			HitTime = Time,
			Length = Length
		});
	}
}
