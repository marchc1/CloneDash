using CloneDash.Common.Game;
using CloneDash.Common.Gamemodes.MuseDash.V1;
using CloneDash.Common.Gamemodes.MuseDash.V1.Data;
using CloneDash.Game.Entities;
using CloneDash.Scenes;

namespace CloneDash.Game;

public class MuseDash1NoteRules(ComboComponent combo, MuseDash1ElfinComponent elfin, HealthComponent health, MuseDash1HudComponent hud, MashComponent mash, MuseDash1RulesComponent rules, MuseDash1ScenesComponent scenes) : INoteRules<DashEnemy>
{
	public const int FULL_HEALTH_SCORE = 300;

	public bool CanAffectPlayer(DashEnemy note) => !mash.InMashState;

	public void Damage(DashEnemy note) {
		float before = health.Value;
		rules.Damage(note.DamageTaken, note);
		if (health.Value < before)
			elfin.Hurt(note);
	}

	public void Kill(DashEnemy note) {
		if (note.DeathAddsToCombo) {
			combo.Add();
			elfin.CountHit(note, note is DoubleHitEnemy { Partner: not null } ? 2 : 1);
		}

		if (note is not Health || HealthFull())
			rules.AddFever(CDUtils.DetermineFever(note.FeverGiven, note.KilledPerfect), note, note.JudgedTime);

		if (rules.Quirks.PerfectHpRevive > 0 && note.KilledPerfect && note is not DashBonusEntity)
			health.Heal(rules.Quirks.PerfectHpRevive);
	}

	public void Punish(DashEnemy note) {
		combo.Break();
	}

	public void Reward(DashEnemy note) {
		if (note is Score && rules.Quirks.MusicNoteAddHp > 0)
			health.Heal(rules.Quirks.MusicNoteAddHp);

		if (note is Health) {
			if (rules.Quirks.BloodGivesIFrame)
				health.SetIFrameTime();
			if (HealthFull())
				GiveFullHealthScore(note, note);
			else {
				health.Heal(note.HealthGiven);
				hud.UI?.CreateHealthText(note.HealthGiven, note.Pathway);
			}
		}

		if (note.Blood) {
			if (rules.Quirks.BloodGivesIFrame)
				health.SetIFrameTime();
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
