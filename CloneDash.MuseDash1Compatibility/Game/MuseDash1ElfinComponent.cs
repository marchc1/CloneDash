using CloneDash.Common.Game;
using CloneDash.Common.Gamemodes.MuseDash;
using CloneDash.Common.Gamemodes.MuseDash.V1;
using CloneDash.Elfins;
using CloneDash.Game.Entities;
using CloneDash.Game.Logic;
using Nucleus.Types;

namespace CloneDash.Game;

public class MuseDash1ElfinComponent : IRhythmGameComponent, IRhythmGameThinker
{
	public const float AIR_EFFECT_Y = 1.3f;
	public const float BETAGO_FIRST_EMOJI = 3.332f;
	public const float BETAGO_EMOJI = 4.116f;
	public const float BETAGO_LONG_EMOJI = 4.746f;
	public const float DOCTOR_EFFECT_X = -5.4f;
	public const float DOCTOR_EFFECT_Y = 1.7f;
	public const float GROUND_EFFECT_Y = -0.85f;
	public const string HP_EFFECT = "fx_hp_ground";
	public const float LANE_EFFECT_Y = 0.94f;
	public const int RHYTHM_HITS = 6;
	public const float RHYTHM_HEAL = 2;

	const int BETAGO_EMOJIS = 4;
	const string NEON_EGG_R6 = "elfin_r6";

	static readonly string[] NeonEggForms = [NEON_EGG_R6, "elfin_saya", "elfin_lin"];

	readonly ContentSlot<MuseDash1ElfinInstance> slot;
	readonly Conductor conductor;
	readonly GameEventManager events;
	readonly HealthComponent health;
	readonly PathwaysComponent pathways;
	readonly MuseDash1RulesComponent rules;
	readonly ISustainManager sustains;

	int emoji;
	double emojiAt = double.MaxValue;
	string? hatchedForm;
	bool linActive;
	MuseDash1GameplayQuirks loadedQuirks;
	int missToGreatLeft;
	double nextReviveTick;
	int reviveTicksLeft;
	bool reviveUsed;
	bool rhythmBuff;
	int rhythmCounter;

	public MuseDash1ElfinComponent(ContentSlot<MuseDash1ElfinInstance> slot, Conductor conductor, GameEventManager events, HealthComponent health, PathwaysComponent pathways, MuseDash1RulesComponent rules, ISustainManager sustains) {
		this.slot = slot;
		this.conductor = conductor;
		this.events = events;
		this.health = health;
		this.pathways = pathways;
		this.rules = rules;
		this.sustains = sustains;

		events.Listen<FeverStartGameEvent>(Hatch);
		events.Listen<PlayerDeathGameEvent>(KeepPlaying);
		events.Listen<SustainStateChangedGameEvent>(CountSustain);
	}

	public MuseDash1ElfinInstance? Elfin => slot.Value;

	ref MuseDash1GameplayQuirks Quirks => ref rules.Quirks;

	public void Prepare() => Elfin?.Prepare();

	public void Load() {
		Elfin?.Initialize();
		loadedQuirks = Quirks;
		missToGreatLeft = Quirks.MissToGreat;
	}

	public void Think(FrameState frameState) {
		if (Elfin == null) return;

		if (conductor.Time + conductor.PreStartTime >= MuseDash1ElfinInstance.APPEAR_DELAY)
			Elfin.Appear();

		ThinkEmoji();
		ThinkRevive();
	}

	public void CountHit(DashEnemy note, int combo) {
		if (!Quirks.RhythmElfin) return;

		if (rhythmBuff) {
			if (note is SustainBeam or Masher) return;

			rhythmBuff = false;
			rhythmCounter = 0;
			health.Heal(RHYTHM_HEAL);
			Elfin?.PlayAnimation("skill");
			events.Fire(new ElfinSkillGameEvent(HP_EFFECT, Y: IsAir(note) ? LANE_EFFECT_Y : -LANE_EFFECT_Y));
			events.Fire(new ElfinSkillGameEvent("elfin_doctor_fx_skill", DOCTOR_EFFECT_X, DOCTOR_EFFECT_Y));
			return;
		}

		rhythmCounter += combo;
		if (rhythmCounter >= RHYTHM_HITS)
			rhythmBuff = true;
	}

	public void Hurt(DashEnemy note) {
		if (hatchedForm == NEON_EGG_R6)
			events.Fire(new ElfinSkillGameEvent("elfin_r6_fx_skill", Y: IsAir(note) ? LANE_EFFECT_Y : -LANE_EFFECT_Y));
	}

	public bool TrySaveMiss(DashEnemy note) {
		bool saveable = note is not (SustainBeam or Masher) && note.Type != MuseDash1EntityType.Gear;

		if (rhythmBuff && saveable)
			return true;

		if (missToGreatLeft > 0 && saveable) {
			missToGreatLeft--;
			bool air = IsAir(note);
			if (note is DoubleHitEnemy { Partner: DashEnemy { Dead: false } partner }) {
				Elfin?.PlayAnimation("skill_double");
				events.Fire(new ElfinSkillGameEvent("elfin_carrot_robot_fx_skill", Y: IsAir(partner) ? AIR_EFFECT_Y : GROUND_EFFECT_Y));
			}
			else
				Elfin?.PlayAnimation(air ? "skill_air" : "skill_ground");
			events.Fire(new ElfinSkillGameEvent("elfin_carrot_robot_fx_skill", Y: air ? AIR_EFFECT_Y : GROUND_EFFECT_Y));
			return true;
		}

		if (Quirks.HoldMissToGreat) {
			if (sustains.ActiveSustains() == 0) {
				if (linActive) {
					linActive = false;
					Elfin?.PlayAnimation("skill_end");
				}
				return false;
			}

			if (!saveable)
				return false;

			if (!linActive) {
				linActive = true;
				Elfin?.PlayAnimation("skill_start", "skill_loop");
			}
			return true;
		}

		return false;
	}

	public bool TryUpgradeGreat(DashEnemy note) => rhythmBuff && note.Type is MuseDash1EntityType.Single or MuseDash1EntityType.Double or MuseDash1EntityType.Ghost;

	public void Reset() {
		Elfin?.Hide();
		Quirks = loadedQuirks;
		emoji = 0;
		emojiAt = double.MaxValue;
		hatchedForm = null;
		linActive = false;
		missToGreatLeft = loadedQuirks.MissToGreat;
		nextReviveTick = 0;
		reviveTicksLeft = 0;
		reviveUsed = false;
		rhythmBuff = false;
		rhythmCounter = 0;
	}

	void Hatch(ref FeverStartGameEvent ev) {
		if (!Quirks.NeonEgg || hatchedForm != null) return;

		var form = hatchedForm = NeonEggForms[Random.Shared.Next(NeonEggForms.Length)];
		MuseDash1GameplayQuirks.ApplyElfinQuirks($"elfin/musedash1/{form}", ref Quirks);
		Elfin?.SwapModel(form);
		events.Fire(new ElfinSkillGameEvent("elfin_egg_fx_skill"));
	}

	bool IsAir(DashEnemy note) => pathways.Layout.IsAirSide(pathways.PrimaryPathway(note.Pathway));

	void CountSustain(ref SustainStateChangedGameEvent ev) {
		if (ev.Completed || ev.Sustain.HeldState == true)
			CountHit(ev.Sustain, 1);
	}

	void KeepPlaying(ref PlayerDeathGameEvent ev) {
		if (!Quirks.PlayableAfterDeath || Elfin?.HasAnimation("normal_die") != true) return;

		Elfin.PlayAnimation("normal_die", "die_standby");
		emoji = 1;
		emojiAt = conductor.Time + BETAGO_FIRST_EMOJI;
	}

	void ThinkEmoji() {
		if (conductor.Time < emojiAt) return;

		emojiAt = conductor.Time + (emoji == 3 ? BETAGO_LONG_EMOJI : BETAGO_EMOJI);
		Elfin?.PlayOverlay($"die_random_0{emoji}");
		emoji = emoji % BETAGO_EMOJIS + 1;
	}

	void ThinkRevive() {
		if (health.Dead) return;

		double time = conductor.Time;
		if (Quirks.ReviveDeadline > 0 && !reviveUsed && health.Value < Quirks.ReviveDeadline) {
			reviveUsed = true;
			reviveTicksLeft = (int)Math.Round(Quirks.ReviveDuration / Quirks.ReviveTick);
			nextReviveTick = Math.Ceiling(time / Quirks.ReviveTick) * Quirks.ReviveTick;
			health.SetIFrameTime(Quirks.ReviveIFrame);
			events.Fire(new ElfinSkillGameEvent("elfin_fan_robot_fx_skill"));
			events.Fire(new ElfinSkillGameEvent("elfin_fan_robot_fx_role_skill"));
		}

		while (reviveTicksLeft > 0 && time >= nextReviveTick) {
			health.Heal(Quirks.ReviveHeal);
			reviveTicksLeft--;
			nextReviveTick += Quirks.ReviveTick;
		}
	}
}
