using Nucleus;
using Nucleus.Engine;
using Nucleus.Types;
using static Nucleus.NMath;

namespace CloneDash.Game.Entities
{
	public class Raider : CD_BaseEnemy
	{
		private EnemyPostHitPhysicsController postHitPhysics;
		ParabolaFrom3Points parabola;
		public Raider() : base(EntityType.Raider) {
			postHitPhysics = new(this);
			Interactivity = EntityInteractivity.Hit;
			parabola = Parabola.From3Points(
				new(1, 1),
				new(0.5f, -.3f),
				new(0, 0)
				);
		}

		public override void OnReset() {
			base.OnReset();
			postHitPhysics = new(this);
		}


		protected override void OnHit(PathwaySide side) {
			Kill();
			postHitPhysics.Hit(NMath.Random.Vec2(new(180, 290), new(-100, -180)), NMath.Random.Single(12.5f, 22.5f));
		}

		protected override void OnMiss() {
			PunishPlayer();
			if (Level.As<CD_GameLevel>().Pathway == this.Pathway) {
				DamagePlayer();
			}
		}

		public override void ChangePosition(ref Vector2F pos) {
			var scrw = Raylib_cs.Raylib.GetScreenWidth();
			if (Dead)
				postHitPhysics.PassthroughPosition(ref pos);
			else
				pos.Y = (float)NMath.Remap(parabola.CalculateY((float)this.DistanceToHit), 0, 1, pos.Y, EngineCore.GetScreenBounds().H);
		}
		public override void Initialize() {
			base.Initialize();
		}

		public override void Build() {
			
		}
	}
}