using CloneDash.Settings;
using Nucleus;

namespace CloneDash.Game
{
	public class DashBonusEntity(EntityType type) : DashEnemy(type)
	{
		public Nucleus.Models.Runtime.Animation? OutAnimation;

		public override void DetermineAnimationPlayback() {
			if (Dead) {
				Position = new(Game.Pathway.GetPathwayLeft(), Game.Pathway.GetPathwayY(Pathway));
				OutAnimation?.Apply(Model, (GetConductor().Time - LastHitTime));
				return;
			}

			Position = new((float)XPosFromTimeOffset((float)-InputSettings.VisualOffset), 450);
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
