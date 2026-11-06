using CloneDash.Common.Game;
using CloneDash.Common.Gamemodes.MuseDash.V1;
using CloneDash.Common.Gamemodes.MuseDash.V1.Data;
using CloneDash.Game.Entities;
using CloneDash.Scenes;

namespace CloneDash.Game;

public class MuseDash1NoteRules(ComboComponent combo, HealthComponent health, MuseDash1HudComponent hud, MashComponent mash, MuseDash1RulesComponent rules, MuseDash1ScenesComponent scenes) : INoteRules<DashEnemy>
{
	public bool CanAffectPlayer(DashEnemy note) => !mash.InMashState;

	public void Damage(DashEnemy note) {
		rules.Damage(note.DamageTaken, note);
	}

	public void Kill(DashEnemy note) {
		if (note.DeathAddsToCombo)
			combo.Add();

		rules.AddFever((int)note.FeverGiven, note);
	}

	public void Punish(DashEnemy note) {
		combo.Break();
	}

	public void Reward(DashEnemy note) {
		if (note is Health) {
			health.Heal(note.HealthGiven);
			hud.UI?.CreateHealthText(note.HealthGiven, note.Pathway);
		}

		if (note.Blood) {
			health.Heal(MD1_SongChartEntity.DEFAULT_HP);
			hud.UI?.CreateHealthText(MD1_SongChartEntity.DEFAULT_HP, note.Pathway);
			scenes.GetActiveScene()?.PlaySound(SceneSound.GotHeart, 0);
		}
	}
}
