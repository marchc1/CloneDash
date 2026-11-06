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
		public const double MASH_IDLE_TIME = 0.5;
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
			bool ended = time > GetJudgementHitTime() + Length;
			if (!StartedHitting) {
				if (ended)
					End();
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
					Complete(false);
				}
				else if (ended || !mashing)
					Fail();
			}
			else if (ended) {
				bool total = Hits >= MidHits - 1;
				Hits++;
				Complete(total);
			}
			else if (!mashing)
				Complete(false);
		}

		protected override void OnReward() {
			base.OnReward();
			if (perfect)
				GetStats().Hit(this, 0);
			else
				GetStats().Great(this);
		}

		private void Complete(bool perfect) {
			this.perfect = perfect;
			WasHitPerfect = perfect;
			if (perfect)
				Components.Hud.UI?.CreatePerfectHitText(0, PathwaySet.Any, fever.InFever, 0);
			else
				Components.Hud.UI?.CreateGreatHitText(0, PathwaySet.Any, fever.InFever, 0);
			Kill();
			End();
		}

		private void End() {
			ForceDraw = false;
			Components.Mash.ExitMashState();
			if (Variant.IsBoss())
				SendSignal(Components.Chart.Boss, EntitySignalType.MashOver);
			SoftKill();
		}

		private void Fail() {
			End();
			DamagePlayer();
			GetStats().Miss(this);
		}

		public override void Think(FrameState frameState) {
			CheckIfComplete();
		}
		protected override void OnHit(PathwayId pathway, double distanceToHit) {
			base.OnHit(pathway, distanceToHit);

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