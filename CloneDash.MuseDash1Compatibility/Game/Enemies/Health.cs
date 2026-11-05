using CloneDash.Common.Gamemodes.MuseDash;
using CloneDash.Common.Gamemodes.MuseDash.V1;

using Nucleus.Engine;

namespace CloneDash.Game.Entities
{
	public class Health : DashBonusEntity
	{
		public Health(DashEnemyComponents components) : base(MuseDash1EntityType.Heart, components) {
			Interactivity = EntityInteractivity.SamePath;
			DeathAddsToCombo = false;
			DoesDamagePlayer = true;
		}

		protected override void OnHit(PathwayId pathway, double distanceToHit) {
			Kill();
		}

		public override void OnBuildVisuals(DashEnemyVisuals visuals) {
			base.OnBuildVisuals(visuals);
			var scene = visuals.Scene;
			visuals.Model = scene.GetEnemyModel(this)?.Instantiate();
			visuals.ApproachAnimation = visuals.Model?.Data.FindAnimation(scene.GetEnemyApproachAnimation(this, out _));
			visuals.OutAnimation = visuals.Model?.Data.FindAnimation(scene.GetEnemyHitAnimation(this, HitAnimationType.Perfect));
			XPosSetup(visuals);
		}
	}
}