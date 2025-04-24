using CloneDash.Systems;
using Nucleus.Core;
using Nucleus.Engine;
using Nucleus.Types;
using Raylib_cs;

namespace CloneDash.Game.Entities
{
	public class Health : CD_BaseEnemy
	{
		public Health() : base(EntityType.Heart) {
			Interactivity = EntityInteractivity.SamePath;
			DeathAddsToCombo = false;
			DoesDamagePlayer = true;
		}

		protected override void OnHit(PathwaySide side) {
			Kill();
			RewardPlayer(true);
		}

		protected override void OnReward() {
			var lvl = GetGameLevel();
			lvl.Heal(this.ScoreGiven);
			lvl.SpawnTextEffect($"+{this.ScoreGiven} HP", lvl.GetPathway(this).Position, TextEffectTransitionOut.SlideUpThenToLeft, new Color(235, 235, 235, 255));
		}
	}
}