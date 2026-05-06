using CloneDash.Common.Gamemodes.MuseDash.V1;
using CloneDash.Settings;
using Nucleus;

namespace CloneDash.Game
{
	public class DashBonusEntity(EntityType type) : DashEnemy(type)
	{
		public override void DetermineAnimationPlayback(DashEnemyVisuals visuals) {
			if (Dead) {
				GetGameLevel().SetEnemyKilledPosition(this);
				visuals.OutAnimation?.Apply(visuals.Model, (GetConductor().Time - LastHitTime));
				return;
			}

			GetGameLevel().SetEnemyPosition(this);
			Position = new(Position.X + (float)XPosFromTimeOffset((float)-InputSettings.VisualOffset), Position.Y);
			visuals.ApproachAnimation?.Apply(visuals.Model, Math.Max(0, AnimationTime));
		}

		public override bool VisTest(float gamewidth, float gameheight, float xPosition) {
			return NMath.InRange(GetVisualTimeUntilHit(), -1, 3);
		}

		public override void Render() {
			base.Render();
		}
	}
}
