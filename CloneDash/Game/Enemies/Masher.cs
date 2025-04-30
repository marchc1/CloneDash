using CloneDash.Modding.Descriptors;
using Nucleus;
using Nucleus.Engine;
using Nucleus.Models.Runtime;
using Nucleus.Types;

namespace CloneDash.Game.Entities
{
	public class Masher : CD_BaseEnemy
	{
		public bool StartedHitting { get; private set; } = false;
		public int MaxHits => Math.Clamp((int)Math.Floor(this.Length * DashVars.MASHER_MAX_HITS_PER_SECOND), 1, int.MaxValue);
		private double lastHitTime = 0;

		public Masher() : base(EntityType.Masher) {
			Warns = true;
			Interactivity = EntityInteractivity.Hit;
			DoesDamagePlayer = true;
		}

		private void CheckIfComplete() {
			var level = Level.As<CD_GameLevel>();

			if ((Hits >= MaxHits || level.Conductor.Time > (HitTime + Length)) && !Dead) {
				Complete();
			}
		}

		private void Complete() {
			var level = Level.As<CD_GameLevel>();
			level.SpawnTextEffect($"PERFECT {Hits}/{MaxHits}", level.GetPathway(PathwaySide.Top).Position, TextEffectTransitionOut.SlideUp, Game.Pathway.PATHWAY_DUAL_COLOR);
			Kill();
			ForceDraw = false;
			level.ExitMashState();
			GetStats().Hit(this, 0);
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
				currentAnim = Model.Data.FindAnimation(masherData.Hurt.GetAnimation(Hits));
			}

			level.Sounds.PlaySound(level.Scene.Hitsounds.PunchSound, 0.24f, pitch: 1 + (Hits / 50f));

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

		SceneDescriptor.SceneDescriptor_MasherEnemy masherData;
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

			masherData = scene.Masher;
			if (Variant != EntityVariant.BossMash) {
				Model = masherData.ModelData.Instantiate();

				var approachSpeeds = EnterDirection switch {
					EntityEnterDirection.TopDown => masherData.InAnimations.Down,
					_ => masherData.InAnimations.Normal
				};

				var speedIndex = Speed switch {
					1 => 2,
					2 => 1,
					3 => 0,
					_ => throw new Exception("Invalid speed")
				};

				ApproachAnimation = Model.Data.FindAnimation(string.Format(approachSpeeds.Format, approachSpeeds.Speeds[speedIndex]));
				PerfectHitAnimation = masherData.CompleteAnimations.FindPerfectAnimation(Model);
				GreatHitAnimation = masherData.CompleteAnimations.FindGreatAnimation(Model);
				var showtime = approachSpeeds.Speeds[speedIndex] / 30f;
				ShowTime = HitTime - showtime;
			}
		}
	}
}