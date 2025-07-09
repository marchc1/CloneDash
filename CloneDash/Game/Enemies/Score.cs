using CloneDash.Scenes;

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
			lvl.SpawnTextEffect($"+{ScoreGiven}", lvl.GetPathway(this).Position, TextEffectTransitionOut.SlideUpThenToLeft, new Color(190, 190, 235, 255));
		}

		public override void Build() {
			base.Build();
			var level = Level.As<DashGameLevel>();
			var scene = level.Scene;

			Model = scene.GetEnemyModel(this).Instantiate();
			ApproachAnimation = Model.Data.FindAnimation(scene.GetEnemyApproachAnimation(this, out _));
			OutAnimation = Model.Data.FindAnimation(scene.GetEnemyHitAnimation(this, HitAnimationType.Perfect));
		}
	}
}