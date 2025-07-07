using Nucleus.Types;
namespace CloneDash.Game.Entities
{
	public class Gear : DashEnemy
	{
		public Gear() : base(EntityType.Gear) {
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

		public override void DetermineAnimationPlayback() {
			Position = new(0, 450);
			base.DetermineAnimationPlayback();
		}

		public override void PostThink(FrameState frameState) {
			base.Think(frameState);
		}

		public override void Build() {
			base.Build();

			var level = Level.As<DashGameLevel>();
			var scene = level.Scene;

			Model = scene.GetEnemyModel(this).Instantiate();

			var animationName = scene.GetEnemyApproachAnimation(this, out var showtime);
			SetShowTimeViaLength(showtime);

			ApproachAnimation = Model.Data.FindAnimation(animationName);

			Scale = new(level.GlobalScale);
		}
	}
}
