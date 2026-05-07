using CloneDash.Common.Gamemodes.MuseDash;
using CloneDash.Common.Gamemodes.MuseDash.V1;
using Nucleus;
using Nucleus.Engine;
using Nucleus.Types;
using Raylib_cs;

namespace CloneDash.Game.Entities
{
	public class Masher : DashEnemy
	{
		public const int MASHER_HITS_PER_SECOND_OF_LENGTH = 25;
		public const int MASHER_PLAYER_MAX_HITS_PER_SECOND = 26;
		public const int MASHER_AUTOPLAYER_MAX_HITS_PER_SECOND = 10;

		public bool StartedHitting { get; private set; } = false;
		public int MaxHits => Math.Clamp((int)Math.Floor(this.Length * MASHER_HITS_PER_SECOND_OF_LENGTH), 1, int.MaxValue);
		private double lastHitTime = 0;

		public Masher() : base(EntityType.Masher) {
			Warns = true;
			Interactivity = EntityInteractivity.Hit;
			DoesDamagePlayer = true;
		}

		private void CheckIfComplete() {
			var level = Level.As<MuseDash1Game>();

			if ((Hits >= MaxHits || level.Conductor.Time > (GetJudgementHitTime() + Length)) && !Dead) {
				Complete();
			}
		}

		protected override void OnReward() {
			base.OnReward();
			GetStats().Hit(this, 0);
		}

		private void Complete() {
			var level = Level.As<MuseDash1Game>();
			level.SpawnTextEffect($"PERFECT {Hits}/{MaxHits}", level.GetPathway(PathwaySide.Top).Position, TextEffectTransitionOut.SlideUp, PathwayExts.PATHWAY_DUAL_COLOR);
			Kill();
			ForceDraw = false;
			level.ExitMashState();
			if (Variant.IsBoss())
				SendSignal(GetGameLevel().Boss, EntitySignalType.MashOver);
		}

		public override void Think(FrameState frameState) {
			CheckIfComplete();
		}
		protected override void OnHit(PathwaySide side, double distanceToHit) {
			base.OnHit(side, distanceToHit);
			var level = Level.As<MuseDash1Game>();

			if (!level.IsSeeking)
				level.PlaySceneSound(SceneSound.HitMasher, Hits);

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
			var currentVisuals = GetActiveVisuals();
			if (currentVisuals.Model != null) 
				currentAnim = currentVisuals.Model.Data.FindAnimation(currentVisuals.Scene.GetMasherHitAnimation(Speed, EnterDirection));

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
			lastHitTime = 0;
			currentAnim = null;
		}

		Nucleus.Models.Runtime.Animation? currentAnim;
		public override void DetermineAnimationPlayback(DashEnemyVisuals visuals) {
			if (visuals.Model == null) return;

			GetGameLevel().SetEnemyKilledPosition(this);

			if (Dead) {
				var anim = WasHitPerfect ? visuals.PerfectHitAnimation : visuals.GreatHitAnimation;
				anim?.Apply(visuals.Model, (GetConductor().Time - LastHitTime));
				return;
			}

			if (StartedHitting) {
				Position = GetGameLevel().GetPathwayPosition(PathwaySide.Both);
				currentAnim?.Apply(visuals.Model, (GetConductor().Time - lastHitTime));
				return;
			}
			GetGameLevel().SetEnemyPosition(this);


			base.DetermineAnimationPlayback(visuals);
		}
		public override void OnBuildVisuals(DashEnemyVisuals visuals) {
			base.OnBuildVisuals(visuals);
			var level = Level.As<MuseDash1Game>();
			var scene = visuals.Scene;

			if (!Variant.IsBoss()) {
				visuals.Model = scene.GetEnemyModel(this)?.Instantiate();
				double showtime = 1;
				visuals.ApproachAnimation = visuals.Model?.Data.FindAnimation(scene.GetEnemyApproachAnimation(this, out showtime));
				SetupHitAnimations(visuals);
				SetShowTimeViaLength(showtime);
			}
		}
	}
}