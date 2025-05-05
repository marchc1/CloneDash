using CloneDash.Settings;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CloneDash.Modding.Descriptors.SceneDescriptor;

namespace CloneDash.Game
{
	public class CD_BonusEntity(EntityType type) : CD_BaseEnemy(type)
	{
		public Nucleus.Models.Runtime.Animation AirAnimation;
		public Nucleus.Models.Runtime.Animation GroundAnimation;
		public Nucleus.Models.Runtime.Animation OutAnimation;

		public override void DetermineAnimationPlayback() {
			if (Dead) {
				Position = new(Game.Pathway.GetPathwayLeft(), Game.Pathway.GetPathwayY(Pathway));
				OutAnimation?.Apply(Model, (GetConductor().Time - LastHitTime));
				return;
			}

			Position = new((float)XPosFromTimeOffset((float)-InputSettings.VisualOffset), 450);
			(Pathway == PathwaySide.Top ? AirAnimation : GroundAnimation).Apply(Model, AnimationTime);
		}

		public void BuildFromScene(SceneDescriptor_Bonus mountable) {
			Model = mountable.ModelData.Instantiate();

			AirAnimation = mountable.ModelData.FindAnimation(mountable.AirAnimation) ?? throw new Exception();
			GroundAnimation = mountable.ModelData.FindAnimation(mountable.GroundAnimation) ?? throw new Exception();
			OutAnimation = mountable.ModelData.FindAnimation(mountable.OutAnimation) ?? throw new Exception();
		}

		public override void Render() {
			base.Render();
		}
	}
}
