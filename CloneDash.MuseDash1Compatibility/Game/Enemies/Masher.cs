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
		public const int MASHER_HITS_PER_SECOND_OF_LENGTH = 25;
		public const int MASHER_PLAYER_MAX_HITS_PER_SECOND = 26;
		public const int MASHER_AUTOPLAYER_MAX_HITS_PER_SECOND = 10;

		public bool StartedHitting { get; private set; } = false;
		public int MaxHits => Math.Clamp((int)Math.Floor(this.Length * MASHER_HITS_PER_SECOND_OF_LENGTH), 1, int.MaxValue);
		private double lastHitTime = 0;

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
			if ((Hits >= MaxHits || GetConductor().Time > (GetJudgementHitTime() + Length)) && !Dead) {
				Complete();
			}
		}

		protected override void OnReward() {
			base.OnReward();
			GetStats().Hit(this, 0);
		}

		private void Complete() {
			Components.Hud.UI?.CreatePerfectHitText(0, PathwaySet.Any, fever.InFever, 0);
			Kill();
			ForceDraw = false;
			Components.Mash.ExitMashState();
			if (Variant.IsBoss())
				SendSignal(Components.Chart.Boss, EntitySignalType.MashOver);
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
				Complete();
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