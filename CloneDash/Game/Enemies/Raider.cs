using CloneDash.Common.Gamemodes.MuseDash;
using CloneDash.Common.Gamemodes.MuseDash.V1;
using Nucleus.Engine;

namespace CloneDash.Game.Entities
{
	public class Raider : DashEnemy
	{
		public Raider() : base(EntityType.Raider) {
			Interactivity = EntityInteractivity.Hit;
		}

		public override void OnReset() {
			base.OnReset();
		}


		protected override void OnHit(PathwaySide side, double distanceToHit) {
			Kill();
			GetStats().Hit(this, distanceToHit);
		}

		protected override void OnMiss() {
			PunishPlayer();
			GetStats().Miss(this);
			if (Level.As<DashGameLevel>().Pathway == this.Pathway) {
				DamagePlayer();
			}
		}

		public override void DetermineAnimationPlayback() {
			if (Dead) {
				GetGameLevel().SetEnemyKilledPosition(this);
				var anim = WasHitPerfect ? PerfectHitAnimation : GreatHitAnimation;
				anim?.Apply(Model, (GetConductor().Time - LastHitTime));
				return;
			}
			GetGameLevel().SetEnemyPosition(this);
			base.DetermineAnimationPlayback();
		}

		public override void Initialize() {
			base.Initialize();
		}

		public override void Build() {
			base.Build();

			var level = Level.As<DashGameLevel>();
			var scene = level.Scene;

			BasicSetup();
			SetMountBoneIfApplicable(scene.GetHPMount(this)!);
		}
	}
}