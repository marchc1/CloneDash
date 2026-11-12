using CloneDash.Common.Game;
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
		public const int MASH_HIT_SCORE = 20;
		public const double MASH_IDLE_TIME = 0.5;
		public const int MASH_TOTAL_SCORE = 200;
		public const double MUL_HIT_AUTO_THRESHOLD = 25;
		public const double MUL_HIT_HIGH_THRESHOLD = 25;
		public const double MUL_HIT_LOW_THRESHOLD = 5;
		public const double MUL_HIT_MID_THRESHOLD = 7.5;

		public bool StartedHitting { get; private set; } = false;
		public int LowHits => (int)Math.Floor(Length * MUL_HIT_LOW_THRESHOLD);
		public int MaxHits => Math.Clamp((int)Math.Floor(Length * MUL_HIT_HIGH_THRESHOLD), 1, int.MaxValue);
		public int MidHits => (int)Math.Floor(Length * MUL_HIT_MID_THRESHOLD);
		private double lastHitTime = 0;
		private bool perfect = true;

		readonly FeverComponent fever;
		readonly HitsoundComponent hitsounds;

		public Masher(DashEnemyComponents components, FeverComponent fever, HitsoundComponent hitsounds) : base(MuseDash1EntityType.Masher, components) {
			this.fever = fever;
			this.hitsounds = hitsounds;
			Warns = true;
			Interactivity = EntityInteractivity.Hit;
			DoesDamagePlayer = true;
		}

		private void CheckIfComplete() {
			if (Dead)
				return;

			double time = GetConductor().Time;
			bool ended = time > GetJudgementHitTime() + Math.Max(Length, PreGreatRange);
			if (!StartedHitting) {
				if (ended)
					End(false);
				return;
			}

			if (Hits >= MaxHits) {
				Complete(true);
				return;
			}

			bool mashing = time - lastHitTime <= MASH_IDLE_TIME;
			if (Hits < LowHits) {
				if (ended && Hits == LowHits - 1) {
					Hits++;
					Complete(false, time: GetJudgementHitTime() + Length);
				}
				else if (ended || !mashing)
					Fail();
			}
			else if (ended) {
				bool total = Hits >= MidHits - 1;
				Hits++;
				Complete(total, time: GetJudgementHitTime() + Length);
			}
			else if (!mashing) {
				GetStats().BreakCombo();
				Components.Combo.Break();
				Complete(false, false);
			}
		}

		protected override void OnKill() {
			if (perfect)
				base.OnKill();
		}

		protected override void OnReward() {
			if (perfect) {
				Components.Rules.AddScore(MASH_TOTAL_SCORE, this);
				GetStats().Hit(this, 0);
			}
			else
				GetStats().Great(this);
		}

		private void Complete(bool perfect, bool showResult = true, double? time = null) {
			JudgedTime = time ?? GetConductor().Time;
			this.perfect = perfect;
			WasHitPerfect = perfect;
			if (showResult) {
				if (perfect)
					Components.Hud.UI?.CreatePerfectHitText(0, PathwaySet.Any, fever.InFever, 0);
				else
					Components.Hud.UI?.CreateGreatHitText(0, PathwaySet.Any, fever.InFever, 0);
			}
			End(true);
			Kill();
		}

		private void End(bool success) {
			ForceDraw = false;
			if (Components.Mash.MashingEntity == this)
				Components.Mash.ExitMashState(success);
			SoftKill();
		}

		private void Fail() {
			End(false);
			DamagePlayer();
			GetStats().Miss(this);
		}

		public override void Think(FrameState frameState) {
			CheckIfComplete();
		}
		protected override void OnHit(PathwayId pathway, double distanceToHit) {
			base.OnHit(pathway, distanceToHit);
			if (!Dead)
				Components.Rules.AddScore(MASH_HIT_SCORE, this);

			if (!Level.As<RhythmGameLevel>().IsSeeking)
				hitsounds.Play(SceneSound.HitMasher, Hits);

			if (MaxHits == 1) {
				Hits = 1;
				Complete(true);
				return;
			}

			if (Dead)
				return;

			if (StartedHitting == false) {
				Components.Mash.EnterMashState(this);
				StartedHitting = true;

				ForceDraw = true;
			}

			lastHitTime = GetConductor().Time;
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
			perfect = true;
			currentAnim = null;
		}

		Nucleus.Models.Runtime.Animation? currentAnim;
		public override void DetermineAnimationPlayback(DashEnemyVisuals visuals) {
			if (visuals.Model == null) return;

			Components.Scenes.SetEnemyKilledPosition(this);

			if (Dead) {
				var anim = WasHitPerfect ? visuals.PerfectHitAnimation : visuals.GreatHitAnimation;
				anim?.Apply(visuals.Model, (GetConductor().Time - LastHitTime));
				return;
			}

			if (StartedHitting) {
				Position = Components.Scenes.GetCenterPosition();
				currentAnim?.Apply(visuals.Model, (GetConductor().Time - lastHitTime));
				return;
			}
			Components.Scenes.SetEnemyPosition(this);


			base.DetermineAnimationPlayback(visuals);
		}
		public override void OnBuildVisuals(DashEnemyVisuals visuals) {
			base.OnBuildVisuals(visuals);
			var scene = visuals.Scene;

			if (!Variant.IsBoss()) {
				visuals.Model = scene.GetEnemyModel(this)?.Instantiate();
				double showtime = 1;
				visuals.ApproachAnimation = visuals.Model?.Data.FindAnimation(scene.GetEnemyApproachAnimation(this, out showtime));
				SetupHitAnimations(visuals);
				visuals.SetShowTimeViaLength(showtime, HitTime);
			}
		}
	}
}