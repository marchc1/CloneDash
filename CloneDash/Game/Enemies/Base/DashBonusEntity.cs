using CloneDash.Settings;
using Nucleus;

namespace CloneDash.Game
{
	public class DashBonusEntity(EntityType type) : DashEnemy(type)
	{
		public Nucleus.Models.Runtime.Animation? OutAnimation;

		public override void DetermineAnimationPlayback() {
			if (Dead) {
				GetGameLevel().SetEnemyKilledPosition(this);
				OutAnimation?.Apply(Model, (GetConductor().Time - LastHitTime));
				return;
			}

			GetGameLevel().SetEnemyPosition(this);
			// Position = new(Position.X + (float)XPosFromTimeOffset((float)-InputSettings.VisualOffset), Position.Y);
			ApproachAnimation?.Apply(Model, Math.Max(0, AnimationTime));
		}

		public override bool VisTest(float gamewidth, float gameheight, float xPosition) {
			return NMath.InRange(GetVisualTimeUntilHit(), -1, 3);
		}

		public override void Render() {
			base.Render();
		}
	}
}
