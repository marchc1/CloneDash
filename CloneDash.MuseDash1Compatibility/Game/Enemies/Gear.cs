using CloneDash.Common.Gamemodes.MuseDash.V1;
using Nucleus.Types;
using Raylib_cs;
namespace CloneDash.Game.Entities
{
	public class Gear : DashEnemy
	{
		public Gear() : base(MuseDash1EntityType.Gear) {
			Interactivity = EntityInteractivity.Avoid;
			DoesDamagePlayer = true;
		}

		public override void Initialize() {
			base.Initialize();
		}

		protected override void OnPass() {
			RewardPlayer();
			GetStats().Pass(this);
		}

		protected override void OnPunishment() {
			base.OnPunishment();
			GetStats().Miss(this);
		}

		public override void DetermineAnimationPlayback(DashEnemyVisuals visuals) {
			GetGameLevel().SetEnemyPosition(this);
			base.DetermineAnimationPlayback(visuals);
		}

		public override void PostThink(FrameState frameState) {
			base.Think(frameState);
		}
		public override void OnBuildVisuals(DashEnemyVisuals visuals) {
			base.OnBuildVisuals(visuals);

			var scene = visuals.Scene;
			visuals.Model = scene.GetEnemyModel(this)?.Instantiate();

			var animationName = scene.GetEnemyApproachAnimation(this, out var showtime);
			SetShowTimeViaLength(showtime);

			visuals.ApproachAnimation = visuals.Model?.Data.FindAnimation(animationName);
		}
	}
}
