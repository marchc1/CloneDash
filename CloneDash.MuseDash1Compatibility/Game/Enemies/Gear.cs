using CloneDash.Common.Gamemodes.MuseDash;
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

		protected override void OnHit(PathwaySide side, double distanceToHit) {
			SoftKill();
			GetStats().Pass(this); // I think this is applicable here?
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
			if (visuals.Model == null) return;

			if (Dead) {
				GetGameLevel().SetEnemyKilledPosition(this);
				Position.Y += 1.3f; // TODO: Why..?
				visuals.PerfectHitAnimation?.Apply(visuals.Model, (GetConductor().Time - LastHitTime));
				return;
			}

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
			visuals.SetShowTimeViaLength(showtime, HitTime);

			visuals.ApproachAnimation = visuals.Model?.Data.FindAnimation(animationName);
			visuals.PerfectHitAnimation = visuals.Model?.Data.FindAnimation(scene?.GetEnemyHitAnimation(this, HitAnimationType.Break));
		}
	}
}
