using Nucleus;
using Nucleus.Engine;
using Nucleus.Types;

namespace CloneDash.Game.Entities
{
	public class Masher : CD_BaseEnemy
	{
		public bool StartedHitting { get; private set; } = false;
		public int MaxHits => Math.Clamp((int)Math.Floor(this.Length * DashVars.MASHER_MAX_HITS_PER_SECOND), 1, int.MaxValue);


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
		}

		public override void Think(FrameState frameState) {
			CheckIfComplete();
		}
		protected override void OnHit(PathwaySide side) {
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

			level.Sounds.PlaySound(level.Scene.PunchSound, 0.24f, pitch: 1 + (Hits / 50f));

			CheckIfComplete();
		}

		protected override void OnMiss() {
			if (StartedHitting) return;
			if (Hits > 0) return;

			DamagePlayer();
		}

		public override void Initialize() {
			base.Initialize();
		}


		public override void OnReset() {
			base.OnReset();
			StartedHitting = false;
		}

		public override void ChangePosition(ref Vector2F pos) {
			var level = Level.As<CD_GameLevel>();
			if (level.InMashState) {
				pos.X = level.XPos + (EngineCore.GetWindowHeight() * .1f);
			}
		}

		public override void Build() {

		}
	}
}