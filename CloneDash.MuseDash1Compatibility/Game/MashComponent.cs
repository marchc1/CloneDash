using CloneDash.Common.Game;
using CloneDash.Common.Gamemodes.MuseDash;
using CloneDash.Game.Entities;
using Nucleus.Types;
using System.Diagnostics.CodeAnalysis;

namespace CloneDash.Game;

public class MashComponent(Conductor conductor, GameEventManager events, PathwaysComponent pathways, MuseDash1RulesComponent rules) : IRhythmGameComponent, IRhythmGameThinker
{
	public DashEnemy? MashingEntity;

	const double TIME_BETWEEN_MASH_HITS = (1d / Masher.MASHER_PLAYER_MAX_HITS_PER_SECOND);
	double LastMasherAttemptedHit;
	double LastMasherRealHit;

	[MemberNotNullWhen(true, nameof(MashingEntity))] public bool InMashState { get; private set; }

	public RhythmGameThinkPhase Phase => RhythmGameThinkPhase.Input;

	public bool CheckMashHit() {
		if (!InMashState) return false;
		if (double.IsNaN(LastMasherRealHit)) return false;

		if ((conductor.Time - LastMasherRealHit) < TIME_BETWEEN_MASH_HITS)
			return false;

		if (!double.IsNaN(LastMasherAttemptedHit) && (conductor.Time - LastMasherAttemptedHit) < TIME_BETWEEN_MASH_HITS) {
			LastMasherRealHit = LastMasherAttemptedHit;
			LastMasherAttemptedHit = double.NaN;
			return true;
		}

		return false;
	}

	/// <summary>
	/// Enters the mash state, which causes all attacks to be redirected into this entity.
	/// </summary>
	/// <param name="ent"></param>
	public void EnterMashState(DashEnemy ent) {
		events.Fire(new MashStartGameEvent());
		InMashState = true;
		MashingEntity = ent;
		LastMasherRealHit = conductor.Time;
		LastMasherAttemptedHit = conductor.Time;
	}

	/// <summary>
	/// Exits the mash state.
	/// </summary>
	public void ExitMashState() {
		events.Fire(new MashEndGameEvent());
		InMashState = false;
		MashingEntity = null;
		LastMasherRealHit = double.NaN;
		LastMasherAttemptedHit = double.NaN;
	}

	public void Reset() => ExitMashState();

	public void Think(FrameState frameState) {
		if (rules.IsDead())
			return;

		if (InMashState) {
			if (CheckMashHit())
				MashingEntity.Hit(pathways.GroundPathway, 0);
		}
	}

	public void SubmitMashHit() {
		if (!InMashState)
			return;
		LastMasherAttemptedHit = conductor.Time;
	}
}
