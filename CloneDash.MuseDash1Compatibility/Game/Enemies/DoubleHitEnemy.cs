using CloneDash.Common.Gamemodes.MuseDash;
using CloneDash.Common.Gamemodes.MuseDash.V1;
using CloneDash.Game.Statistics;

namespace CloneDash.Game.Entities
{
	public class DoubleHitEnemy : DashEnemy
	{
		bool completesPair;

		public DoubleHitEnemy(DashEnemyComponents components) : base(MuseDash1EntityType.Double, components) {
			Interactivity = EntityInteractivity.Hit;
			DoesDamagePlayer = true;
		}

		public DashEnemy? Partner => SameTimeNotes.Find(note => note is DoubleHitEnemy);

		public override bool KilledPerfect => WasHitPerfect && (Partner is not DashEnemy partner || GetStats().GetStatisticsForEnemy(partner).State == EnemyStatisticsState.Perfect);

		public override void OnReset() {
			base.OnReset();
			completesPair = false;
		}
		public override void Initialize() {
			base.Initialize();
		}

		protected override void OnHit(PathwayId pathway, double distanceToHit) {
			var partner = Partner;
			completesPair = partner == null || IsHit(GetStats().GetStatisticsForEnemy(partner).State);
			Kill();
			GetStats().Hit(this, distanceToHit);
		}

		protected override void OnKill() {
			if (!completesPair)
				return;
			if (Partner != null)
				Components.Combo.Add();
			base.OnKill();
		}

		protected override void OnReward() {
			if (!completesPair)
				return;
			Components.Rules.AddScore(CDUtils.DetermineScoreMultiplied(ScoreGiven, Components.Rules.InFeverAt(JudgedTime), Components.Combo.Value, KilledPerfect), this);
		}

		static bool IsHit(EnemyStatisticsState state) => state is EnemyStatisticsState.Perfect or EnemyStatisticsState.Great;

		protected override void OnMiss() {
			DamagePlayer();
			GetStats().Miss(this);
		}

		public override void DetermineAnimationPlayback(DashEnemyVisuals visuals) {
			if (Dead) {
				Components.Scenes.SetEnemyKilledPosition(this);
				var anim = WasHitPerfect ? visuals.PerfectHitAnimation : visuals.GreatHitAnimation;
				anim?.Apply(visuals.Model, (GetConductor().Time - LastHitTime));
				return;
			}
			Components.Scenes.SetEnemyPosition(this);
			base.DetermineAnimationPlayback(visuals);
		}
		public override void OnBuildVisuals(DashEnemyVisuals visuals) {
			base.OnBuildVisuals(visuals);
			BasicSetup(visuals);
		}
	}
}
