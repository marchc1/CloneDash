using CloneDash.Settings;

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
			ApproachAnimation?.Apply(Model, AnimationTime);
		}

		public override void Render() {
			base.Render();
		}
	}
}
