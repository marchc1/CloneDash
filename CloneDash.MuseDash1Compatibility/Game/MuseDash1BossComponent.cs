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
	enum Cue
	{
		Action,
		Hide,
		MashOutcome,
		Show
	}

	readonly Boss boss;
	readonly MuseDash1ChartComponent chart;
	readonly Conductor conductor;
	readonly GameEventManager events;
	readonly RhythmGameLevel level;

	(double Time, DashEnemy? Note, string? Action, Cue Cue)[] cues = [];
	string? currentAction;
	bool isIn;
	int nextCue;
	double playTime;

	public MuseDash1BossComponent(Boss boss, MuseDash1ChartComponent chart, Conductor conductor, GameEventManager events, RhythmGameLevel level) {
		this.boss = boss;
		this.chart = chart;
		this.conductor = conductor;
		this.events = events;
		this.level = level;

		events.Listen<MashEndGameEvent>(EndMash);
		events.Listen<MashHitGameEvent>(HurtMash);
		events.Listen<MashStartGameEvent>(HurtMash);
		events.Listen<NoteHitGameEvent>(Hurt);
		events.Listen<SceneChangedGameEvent>(SyncScenes);
		events.Listen<SeekFinishedGameEvent>(FinishSeek);
	}

	public static bool IsBossAction(string? action) => !string.IsNullOrEmpty(action) && action != "0";

	public void Load() {
		List<(double Time, DashEnemy? Note, string? Action, Cue Cue)> found = [];
		foreach (var enemy in chart.EnemyManager.GetAll())
			if (IsBossAction(enemy.BossAction))
				found.Add((enemy.GetVisualHitTime() - enemy.Dt, enemy, enemy.BossAction!, Cue.Action));
		foreach (var ev in chart.Events) {
			double time = ev.Time + InputSettings.VisualOffset - ev.Dt;
			if (ev is BossVisibilityEvent visibility)
				found.Add((time, null, null, visibility.Visible ? Cue.Show : Cue.Hide));
			else if (IsBossAction(ev.BossAction)) {
				var masher = ev is BossMasher bossMasher ? bossMasher.Masher : null;
				found.Add((time, masher, ev.BossAction!, Cue.Action));
				if (IsBossMasher(masher))
					found.Add((masher!.GetVisualHitTime() + masher.Length, masher, null, Cue.MashOutcome));
			}
		}
		cues = [.. found.OrderBy(cue => cue.Time)];
	}

	public void Reset() {
		currentAction = null;
		isIn = false;
		nextCue = 0;
		playTime = 0;
	}

	public void SeekTo(double time) {
		while (nextCue < cues.Length && cues[nextCue].Time < time)
			Run(cues[nextCue++], true);
	}

	public void Think(FrameState frameState) {
		while (nextCue < cues.Length && cues[nextCue].Time <= conductor.Time)
			Run(cues[nextCue++], false);
	}

	void EndMash(ref MashEndGameEvent ev) {
		if (level.IsSeeking || !IsBossMasher(ev.Note))
			return;

		PlayOutcome(ev.Note!, ev.Success);
		playTime = conductor.Time;
	}

	void FinishSeek(ref SeekFinishedGameEvent ev) {
		if (currentAction != null)
			boss.Advance(ev.Time - playTime);
	}

	void Fire<T>(T ev) where T : struct, IGameEvent {
		if (!level.IsSeeking)
			events.Fire(ev);
	}

	void Hurt(ref NoteHitGameEvent ev) {
		if (level.IsSeeking || ev.Note.Variant is not (EntityVariant.BossHitSlow or EntityVariant.BossHitFast))
			return;

		boss.Play(ActionKeys.BOSS_HURT);
		Track(ActionKeys.BOSS_HURT, true);
		playTime = conductor.Time;
	}

	void HurtMash(ref MashHitGameEvent ev) => HurtMash(ev.Note);

	void HurtMash(ref MashStartGameEvent ev) => HurtMash(ev.Note);

	void HurtMash(DashEnemy note) {
		if (level.IsSeeking || !IsBossMasher(note))
			return;

		if (boss.HasClip(ActionKeys.BOSS_MULTI_HURT)) {
			boss.PlayClip(ActionKeys.BOSS_MULTI_HURT, false);
			Track(ActionKeys.BOSS_MULTI_HURT, false);
		}
		else {
			boss.Play(ActionKeys.BOSS_MULTI_HURT_STATE, true);
			Track(ActionKeys.BOSS_MULTI_HURT_STATE, false);
		}
		playTime = conductor.Time;
	}

	static bool IsBossMasher(DashEnemy? note) => note is Masher && note.Variant is EntityVariant.BossMasher or EntityVariant.BossMasherEnd;

	void Notify(string action) {
		switch (action) {
			case ActionKeys.COMEIN:
				if (isIn)
					return;
				isIn = true;
				Fire(new BossInGameEvent());
				break;
			case ActionKeys.COMEOUT:
			case ActionKeys.BOSS_MULTI_HURT_CANCELL2:
			case ActionKeys.BOSS_MULTI_HURT_END2:
				if (!isIn)
					return;
				isIn = false;
				Fire(new BossOutGameEvent());
				break;
			case ActionKeys.FAR_ATTACK1_START:
			case ActionKeys.FAR_ATTACK2_START:
				Fire(new BossAttackStartGameEvent(action));
				break;
			case ActionKeys.FAR_ATTACK1_END:
			case ActionKeys.FAR_ATTACK2_END:
				Fire(new BossAttackEndGameEvent(action));
				break;
			case ActionKeys.BOSS_ATK_1_TO_2:
			case ActionKeys.BOSS_ATK_2_TO_1:
				Fire(new BossAttackChangeGameEvent(action));
				break;
		}
	}

	void Play(DashEnemy? note, string action) {
		if (!IsBossMasher(note)) {
			boss.Play(action);
			Track(action, true);
			return;
		}

		boss.PlayClip(action, false);
		Track(action, false);
		if (note!.Variant == EntityVariant.BossMasher) {
			boss.QueueClip(ActionKeys.STAND, true);
			Track(ActionKeys.STAND, false);
		}
	}

	void PlayOutcome(DashEnemy note, bool success) {
		bool leaves = note.Variant == EntityVariant.BossMasherEnd;
		string clip = success
			? leaves ? ActionKeys.BOSS_MULTI_HURT_END2 : ActionKeys.BOSS_MULTI_HURT_END1
			: leaves ? ActionKeys.BOSS_MULTI_HURT_CANCELL2 : ActionKeys.BOSS_MULTI_HURT_CANCELL1;
		boss.PlayClip(clip, false);
		Track(clip, true);
		if (!leaves) {
			boss.QueueClip(ActionKeys.STAND, true);
			Track(ActionKeys.STAND, false);
		}
	}

	void Run((double Time, DashEnemy? Note, string? Action, Cue Cue) cue, bool seeking) {
		switch (cue.Cue) {
			case Cue.Action:
				Play(cue.Note, cue.Action!);
				playTime = cue.Time;
				break;
			case Cue.Hide:
				boss.Hide();
				break;
			case Cue.MashOutcome:
				if (!seeking)
					return;
				PlayOutcome(cue.Note!, true);
				playTime = cue.Time;
				break;
			case Cue.Show:
				boss.Show();
				break;
		}
	}

	void SyncScenes(ref SceneChangedGameEvent ev) {
		if (currentAction == null || !boss.Visible || ev.From == ev.To)
			return;

		if (currentAction is ActionKeys.BOSS_MULTI_HURT_END2 or ActionKeys.BOSS_MULTI_ATK_END)
			boss.PlayClip(currentAction, false, currentAction == ActionKeys.BOSS_MULTI_ATK_END ? 2 : 0);
		else
			boss.Play(currentAction, true);
		playTime = Math.Max(playTime, conductor.Time);
	}

	void Track(string action, bool notify) {
		if (currentAction != ActionKeys.BOSS_MULTI_ATK_END || action != ActionKeys.FAR_ATTACK2_START)
			currentAction = action;
		if (notify)
			Notify(action);
	}
}
