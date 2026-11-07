using CloneDash.Common.Game;
using CloneDash.Common.Gamemodes.MuseDash.V1;
using CloneDash.Compatibility.MuseDash;
using CloneDash.Game.Entities;
using CloneDash.Game.Events;
using CloneDash.Scenes;
using CloneDash.Settings;
using Nucleus.Types;

namespace CloneDash.Game;

public class MuseDash1BossComponent : IRhythmGameComponent, IRhythmGameThinker
{
	readonly Boss boss;
	readonly MuseDash1ChartComponent chart;
	readonly Conductor conductor;
	readonly RhythmGameLevel level;

	(double Time, DashEnemy? Note, string Action)[] actions = [];
	int nextAction;

	public MuseDash1BossComponent(Boss boss, MuseDash1ChartComponent chart, Conductor conductor, GameEventManager events, RhythmGameLevel level) {
		this.boss = boss;
		this.chart = chart;
		this.conductor = conductor;
		this.level = level;

		events.Listen<MashEndGameEvent>(EndMash);
		events.Listen<MashHitGameEvent>(HurtMash);
		events.Listen<MashStartGameEvent>(HurtMash);
		events.Listen<NoteHitGameEvent>(Hurt);
		events.Listen<SceneChangedGameEvent>(SyncScenes);
	}

	public static bool IsBossAction(string? action) => !string.IsNullOrEmpty(action) && action != "0";

	public void Load() {
		List<(double Time, DashEnemy? Note, string Action)> found = [];
		foreach (var enemy in chart.EnemyManager.GetAll())
			if (IsBossAction(enemy.BossAction))
				found.Add((enemy.GetVisualHitTime() - enemy.Dt, enemy, enemy.BossAction!));
		foreach (var ev in chart.Events)
			if (IsBossAction(ev.BossAction))
				found.Add((ev.Time + InputSettings.VisualOffset - ev.Dt, ev is BossMasher masher ? masher.Masher : null, ev.BossAction!));
		actions = [.. found.OrderBy(action => action.Time)];
	}

	public void Reset() => nextAction = 0;

	public void SeekTo(double time) {
		int last = -1;
		while (nextAction < actions.Length && actions[nextAction].Time < time)
			last = nextAction++;
		if (last < 0)
			return;

		var (actionTime, note, action) = actions[last];
		Play(note, action);
		boss.Advance(time - actionTime);
	}

	public void Think(FrameState frameState) {
		while (nextAction < actions.Length && actions[nextAction].Time <= conductor.Time) {
			var (_, note, action) = actions[nextAction++];
			Play(note, action);
		}
	}

	void EndMash(ref MashEndGameEvent ev) {
		if (level.IsSeeking || !IsBossMasher(ev.Note))
			return;

		bool leaves = ev.Note!.Variant == EntityVariant.BossMasherEnd;
		if (ev.Success)
			boss.PlayClip(leaves ? ActionKeys.BOSS_MULTI_HURT_END2 : ActionKeys.BOSS_MULTI_HURT_END1, false);
		else
			boss.PlayClip(leaves ? ActionKeys.BOSS_MULTI_HURT_CANCELL2 : ActionKeys.BOSS_MULTI_HURT_CANCELL1, false);
		if (!leaves)
			boss.QueueClip(ActionKeys.STAND, true);
	}

	void Hurt(ref NoteHitGameEvent ev) {
		if (ev.Note.Variant is EntityVariant.BossHitSlow or EntityVariant.BossHitFast)
			boss.Play(ActionKeys.BOSS_HURT);
	}

	void HurtMash(ref MashHitGameEvent ev) => HurtMash(ev.Note);

	void HurtMash(ref MashStartGameEvent ev) => HurtMash(ev.Note);

	void HurtMash(DashEnemy note) {
		if (level.IsSeeking || !IsBossMasher(note))
			return;

		if (boss.HasClip(ActionKeys.BOSS_MULTI_HURT))
			boss.PlayClip(ActionKeys.BOSS_MULTI_HURT, false);
		else
			boss.Play(ActionKeys.BOSS_MULTI_HURT_STATE);
	}

	static bool IsBossMasher(DashEnemy? note) => note is Masher && note.Variant is EntityVariant.BossMasher or EntityVariant.BossMasherEnd;

	void Play(DashEnemy? note, string action) {
		if (!IsBossMasher(note)) {
			boss.Play(action);
			return;
		}

		boss.PlayClip(action, false);
		if (note!.Variant == EntityVariant.BossMasher)
			boss.QueueClip(ActionKeys.STAND, true);
	}

	void SyncScenes(ref SceneChangedGameEvent ev) => boss.SyncScenes(ev.From, ev.To);
}
