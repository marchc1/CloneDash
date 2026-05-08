using CloneDash.Common.Gamemodes.MuseDash;
using CloneDash.Common.Gamemodes.MuseDash.V1;
using Nucleus.Common.Types;
using Nucleus.Engine;

using Raylib_cs;

namespace CloneDash.Game.Entities
{
	public class Score : DashBonusEntity
	{
		public Score() : base(EntityType.Score) {
			Interactivity = EntityInteractivity.SamePath;
			DeathAddsToCombo = false;
			DoesDamagePlayer = true;
		}

		protected override void OnHit(PathwaySide side, double distanceToHit) {
			RewardPlayer();
			Kill();
		}

		protected override void OnReward() {
			var lvl = GetGameLevel();

			base.OnReward();
			lvl.GetSceneUI()?.CreateScoreText(ScoreGiven);
		}
		public override void OnBuildVisuals(DashEnemyVisuals visuals) {
			base.OnBuildVisuals(visuals);
			
			visuals.Model = visuals.Scene.GetEnemyModel(this)?.Instantiate();
			visuals.ApproachAnimation = visuals.Model?.Data.FindAnimation(visuals.Scene.GetEnemyApproachAnimation(this, out _));
			visuals.OutAnimation = visuals.Model?.Data.FindAnimation(visuals.Scene.GetEnemyHitAnimation(this, HitAnimationType.Perfect));
		}
	}
}