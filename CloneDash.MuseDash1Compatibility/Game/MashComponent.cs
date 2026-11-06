using CloneDash.Common.Game;
using CloneDash.Common.Gamemodes.MuseDash;
using CloneDash.Game.Entities;
using Nucleus.Types;
using System.Diagnostics.CodeAnalysis;

namespace CloneDash.Game;

public class MashComponent(GameEventManager events, PathwaysComponent pathways, MuseDash1RulesComponent rules) : IRhythmGameComponent
{
	public DashEnemy? MashingEntity;

	[MemberNotNullWhen(true, nameof(MashingEntity))] public bool InMashState { get; private set; }

	/// <summary>
	/// Enters the mash state, which causes all attacks to be redirected into this entity.
	/// </summary>
	/// <param name="ent"></param>
	public void EnterMashState(DashEnemy ent) {
		events.Fire(new MashStartGameEvent());
		InMashState = true;
		MashingEntity = ent;
	}

	/// <summary>
	/// Exits the mash state.
	/// </summary>
	public void ExitMashState() {
		events.Fire(new MashEndGameEvent());
		InMashState = false;
		MashingEntity = null;
	}

	public void Reset() => ExitMashState();

	public void SubmitMashHit() {
		if (!InMashState || rules.IsDead())
			return;
		MashingEntity.Hit(pathways.GroundPathway, 0);
	}
}
