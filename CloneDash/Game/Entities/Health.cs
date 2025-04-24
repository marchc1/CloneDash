using CloneDash.Systems;
using Nucleus.Core;
using Nucleus.Engine;
using Nucleus.Types;
using Raylib_cs;

namespace CloneDash.Game.Entities
{
	public class Health : CD_BonusEntity
	{
		public Health() : base(EntityType.Heart) {
			Interactivity = EntityInteractivity.SamePath;
			DeathAddsToCombo = false;
			DoesDamagePlayer = true;
		}

		protected override void OnHit(PathwaySide side) {
			Kill();
			RewardPlayer(true);
		}

		public override void Build() {
			base.Build();
			var level = Level.As<CD_GameLevel>();
			var scene = level.Scene;
			BuildFromScene(scene.Heart);
		}
	}
}