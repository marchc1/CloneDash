using CloneDash.Common.Gamemodes.MuseDash;
using CloneDash.Common.Gamemodes.MuseDash.V1;

using Nucleus.Engine;

namespace CloneDash.Game.Entities
{
	public class Health : DashBonusEntity
	{
		public Health() : base(MuseDash1EntityType.Heart) {
			Interactivity = EntityInteractivity.SamePath;
			DeathAddsToCombo = false;
			DoesDamagePlayer = true;
		}

		protected override void OnHit(PathwaySide side, double distanceToHit) {
			// Hack to trick Kill into not calling RewardPlayer without heal.
			// Set it back right after
			DoesRewardPlayer = false;
			Kill();
			DoesRewardPlayer = true;
			RewardPlayer(true);
		}

		public override void OnBuildVisuals(DashEnemyVisuals visuals) {
			base.OnBuildVisuals(visuals);
			var level = Level.As<MuseDash1Game>();
			var scene = visuals.Scene;
			visuals.Model = scene.GetEnemyModel(this)?.Instantiate();
			visuals.ApproachAnimation = visuals.Model?.Data.FindAnimation(scene.GetEnemyApproachAnimation(this, out _));
			visuals.OutAnimation = visuals.Model?.Data.FindAnimation(scene.GetEnemyHitAnimation(this, HitAnimationType.Perfect));
			XPosSetup(visuals);
		}
	}
}