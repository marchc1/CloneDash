using CloneDash.Common.Game;
using CloneDash.Common.Gamemodes.MuseDash.V1;
using CloneDash.Common.Gamemodes.MuseDash.V1.Data;
using CloneDash.Game.Entities;
using CloneDash.Scenes;

namespace CloneDash.Game;

public class MuseDash1NoteRules(ComboComponent combo, HealthComponent health, MuseDash1HudComponent hud, MashComponent mash, MuseDash1RulesComponent rules, MuseDash1ScenesComponent scenes) : INoteRules<DashEnemy>
{
	public const int FULL_HEALTH_SCORE = 300;

	public bool CanAffectPlayer(DashEnemy note) => !mash.InMashState;

	public void Damage(DashEnemy note) {
		rules.Damage(note.DamageTaken, note);
	}

	public void Kill(DashEnemy note) {
		if (note.DeathAddsToCombo)
			combo.Add();

		if (note is not Health || HealthFull())
			rules.AddFever(CDUtils.DetermineFever(note.FeverGiven, note.KilledPerfect), note, note.JudgedTime);
	}

	public void Punish(DashEnemy note) {
		combo.Break();
	}

	public void Reward(DashEnemy note) {
		if (note is Health) {
			if (HealthFull())
				GiveFullHealthScore(note, note);
			else {
				health.Heal(note.HealthGiven);
				hud.UI?.CreateHealthText(note.HealthGiven, note.Pathway);
			}
		}

		if (note.Blood) {
			bool full = HealthFull();
			health.Heal(MD1_SongChartEntity.DEFAULT_HP);
			if (full)
				GiveFullHealthScore(note, null);
			else
				hud.UI?.CreateHealthText(MD1_SongChartEntity.DEFAULT_HP, note.Pathway);
			scenes.GetActiveScene()?.PlaySound(SceneSound.GotHeart, 0);
		}
	}

	void GiveFullHealthScore(DashEnemy note, DashEnemy? responsible) {
		rules.AddScore(FULL_HEALTH_SCORE, responsible);
		hud.UI?.CreateScoreText(FULL_HEALTH_SCORE, note.Pathway);
	}

	bool HealthFull() => health.Value >= health.Max;
}
