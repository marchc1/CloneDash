using Nucleus.Engine;
using Nucleus.Types;

namespace CloneDash.Game.Entities
{
	public class Masher : CD_BaseEnemy
	{
		public const int MASHER_MAX_HITS_PER_SECOND = 25;

		public bool StartedHitting { get; private set; } = false;
		public int MaxHits => Math.Clamp((int)Math.Floor(this.Length * MASHER_MAX_HITS_PER_SECOND), 1, int.MaxValue);
		private double lastHitTime = 0;

		public Masher() : base(EntityType.Masher) {
			Warns = true;
			Interactivity = EntityInteractivity.Hit;
			DoesDamagePlayer = true;
		}

		private void CheckIfComplete() {
			var level = Level.As<CD_GameLevel>();

			if ((Hits >= MaxHits || level.Conductor.Time > (GetJudgementHitTime() + Length)) && !Dead) {
				Complete();
			}
		}

		protected override void OnReward() {
			base.OnReward();
			GetStats().Hit(this, 0);
		}

		private void Complete() {
			var level = Level.As<CD_GameLevel>();
			level.SpawnTextEffect($"PERFECT {Hits}/{MaxHits}", level.GetPathway(PathwaySide.Top).Position, TextEffectTransitionOut.SlideUp, Game.Pathway.PATHWAY_DUAL_COLOR);
			Kill();
			ForceDraw = false;
			level.ExitMashState();
		}

		public override void Think(FrameState frameState) {
			CheckIfComplete();
		}
		protected override void OnHit(PathwaySide side, double distanceToHit) {
			var level = Level.As<CD_GameLevel>();

			if (MaxHits == 1) {
				Hits = 1;
				Complete();
				return;
			}

			if (Dead)
				return;

			if (StartedHitting == false) {
				level.EnterMashState(this);
				StartedHitting = true;

				ForceDraw = true;
			}

			lastHitTime = level.Conductor.Time;
			if (Model != null) {
				currentAnim = Model.Data.FindAnimation(level.Scene.GetMasherHitAnimation());
			}

			level.Scene.PlayHitSound(this, Hits);

			CheckIfComplete();
		}

		protected override void OnMiss() {
			if (StartedHitting) return;
			if (Hits > 0) return;

			DamagePlayer();
			GetStats().Miss(this);
		}

		public override void Initialize() {
			base.Initialize();
		}


		public override void OnReset() {
			base.OnReset();
			StartedHitting = false;
		}

		Nucleus.Models.Runtime.Animation? currentAnim;
		public override void DetermineAnimationPlayback() {
			if (Model == null) return;

			Position = new(Game.Pathway.GetPathwayLeft(), Game.Pathway.GetPathwayY(PathwaySide.Both));
			if (Dead) {
				var anim = WasHitPerfect ? PerfectHitAnimation : GreatHitAnimation;
				anim?.Apply(Model, (GetConductor().Time - LastHitTime));
				return;
			}

			if (StartedHitting) {
				currentAnim?.Apply(Model, (GetConductor().Time - lastHitTime));
				return;
			}

			Position = new(0, 450);

			base.DetermineAnimationPlayback();
		}

		public override void Build() {
			base.Build();

			var level = Level.As<CD_GameLevel>();
			var scene = level.Scene;

			if (Variant != EntityVariant.BossMash) {
				Model = scene.GetEnemyModel(this).Instantiate();
				ApproachAnimation = Model.Data.FindAnimation(scene.GetEnemyApproachAnimation(this, out var showtime));
				SetShowTimeViaLength(showtime);
			}
		}
	}
}