using CloneDash.Common.Game;
using CloneDash.Common.Gamemodes.MuseDash;
using CloneDash.Common.Gamemodes.MuseDash.V1;
using CloneDash.Common.Systems;
using CloneDash.Game.Entities;
using CloneDash.Game.Input;
using CloneDash.Game.Logic;
using CloneDash.Scenes;
using Nucleus;
using Nucleus.Engine;
using Nucleus.Types;

namespace CloneDash.Game;

public class MuseDash1CharacterComponent : IRhythmGameComponent, IRhythmGameThinker
{
	public IMuseDash1CharacterInstance Character { get; }

	readonly Conductor Conductor;
	readonly GameEventManager Events;
	readonly RhythmGameLevel level;
	readonly MashComponent Mash;
	readonly PathwaysComponent Pathways;
	readonly MuseDash1GameplayQuirksComponent quirks;
	readonly MuseDash1ScenesComponent Scenes;
	readonly ISustainManager Sustains;

	double LastHologramJumpTIme = -2000000000000d;
	PathwayId LastJumpPathway;
	double LastJumpTime = -2000000000000d;
	SecondOrderSystem? YOffsetSOS;

	public MuseDash1CharacterComponent(ContentSlot<IMuseDash1CharacterInstance> slot, Conductor conductor, GameEventManager events, RhythmGameLevel level, MashComponent mash, PathwaysComponent pathways, MuseDash1GameplayQuirksComponent quirks, MuseDash1ScenesComponent scenes, ISustainManager sustains) {
		Character = slot.Value!;
		Conductor = conductor;
		Events = events;
		this.level = level;
		Mash = mash;
		Pathways = pathways;
		this.quirks = quirks;
		Scenes = scenes;
		Sustains = sustains;

		events.Listen<PlayerDamagedGameEvent>(PlayHurtAnimation);
		events.Listen<PlayerDeathGameEvent>(TriggerDeath);
		events.Listen<SustainStateChangedGameEvent>(RelaySustainCallback);
	}

	/// <summary>
	/// Can the player jump right now?
	/// </summary>
	public bool CanJump => !InAir;

	/// <summary>
	/// Gets the current pathway the player is on. Returns the pathway jumped to if jumping, else bottom.
	/// </summary>
	public PathwaySet CharacterPathways {
		get {
			var state = Sustains.GetSustainState();
			if (state.IsEmpty) return (InAir ? LastJumpPathway : Pathways.GroundPathway).AsSet;
			return state;
		}
	}

	public double Hologram_AirTime => (Conductor.Time - LastHologramJumpTIme);

	public double Hologram_TimeToAnimationEnds => Character.GetSecondary().GetAnimationDuration() - (Conductor.Time - LastHologramJumpTIme);

	/// <summary>
	/// Is the player in the air right now?
	/// </summary>
	public bool InAir => (Conductor.Time - LastJumpTime) < Character.GetJumpDuration();

	public float PlayerScale => 1 / 200f;

	public double TimeToAnimationEnds => Character.GetPrimary().GetAnimationDuration() - (Conductor.Time - LastJumpTime);

	// todo: confirm this is the right behavior
	static bool PathwayTransitionAnimation => true;

	ref MuseDash1GameplayQuirks Quirks => ref quirks.Value;

	public bool AttackAir(PathwayId pathway, in PollResult result) {
		if (Mash.InMashState) {
			Events.Fire(new CharacterAttackGameEvent(pathway, true, result));
			PlayCharacterAnimation(CharacterAnimationType.JumpHit);

			return true;
		}

		if (result.Hit) {
			Events.Fire(new CharacterAttackGameEvent(pathway, true, result));

			var isDHE = result.Hit && result.HitEntity is DoubleHitEnemy;

			if (isDHE)
				PlayCharacterAnimation(CharacterAnimationType.BigHit);
			else if (result.HitEntity is not SustainBeam)
				if (!InAir && !Sustains.IsSustaining() && PathwayTransitionAnimation)
					PlayCharacterAnimation(CharacterAnimationType.UpHit);
				else
					PlayCharacterAnimation(result.IsPerfect ? CharacterAnimationType.JumpHit : CharacterAnimationType.JumpHitGreat);

			LastJumpPathway = pathway;
			if (Sustains.IsSustaining())
				LastHologramJumpTIme = Conductor.Time;
			else
				LastJumpTime = Conductor.Time;

			return true;
		}
		else if (CanJump) {
			Events.Fire(new CharacterAttackGameEvent(pathway, true, result));
			if (!Sustains.IsSustaining())
				PlayCharacterAnimation(CharacterAnimationType.Jump);

			LastJumpPathway = pathway;
			if (Sustains.IsSustaining())
				LastHologramJumpTIme = Conductor.Time;
			else
				LastJumpTime = Conductor.Time;

			return true;
		}

		return false;
	}

	public void AttackGround(PathwayId pathway, PollResult result) {
		if (Mash.InMashState) {
			Events.Fire(new CharacterAttackGameEvent(pathway, false, result));
			PlayCharacterAnimation(CharacterAnimationType.AttackPerfect);
			return;
		}

		Events.Fire(new CharacterAttackGameEvent(pathway, false, result));
		if (result.Hit) {
			if (result.HitEntity is DoubleHitEnemy)
				PlayCharacterAnimation(CharacterAnimationType.BigHit);
			else if (result.HitEntity is not SustainBeam)
				if (InAir && PathwayTransitionAnimation)
					PlayCharacterAnimation(CharacterAnimationType.DownHit);
				else
					PlayCharacterAnimation(result.IsPerfect ? CharacterAnimationType.AttackPerfect : CharacterAnimationType.AttackGreat);
		}
		else if (!Sustains.IsSustaining()) {
			if (InAir)
				PlayCharacterAnimation(CharacterAnimationType.DownHit);
			else
				PlayCharacterAnimation(CharacterAnimationType.AttackMiss);
		}

		LastJumpTime = -2000000000000d;
		LastHologramJumpTIme = -2000000000000d;
	}

	public float GetPlayerY(bool secondary) {
		var height = EngineCore.GetWindowHeight();

		var bot = Scenes.GetPathwayPosition(Pathways.GroundPathway);
		var character = secondary ? Character.GetSecondary() : Character.GetPrimary();
		if (!character.IsInAir())
			return bot.Y + -1f;

		var top = Scenes.GetPathwayPosition(LastJumpPathway);
		float ratio = (float)NMath.Remap(character.GetTimeToAnimationEnd(), character.GetAnimationDuration(), 0, 0, 1, clampOutput: true);
		return (float)NMath.Remap(NMath.Ease.InCirc(NMath.Ease.InExpo(ratio)), 0, 1, top.Y, bot.Y) + -1f; // TODO: re-evaluate
	}

	public bool IsCharacterNear(PathwayId pathway) {
		if (ResolveCharacterY(Sustains.GetSustainState()) is not float y)
			return false;

		var tolerance = MathF.Abs(Scenes.GetPathwayPosition(Pathways.JumpPathway).Y - Scenes.GetPathwayPosition(Pathways.GroundPathway).Y) * 0.25f;
		return MathF.Abs(y - Scenes.GetPathwayPosition(pathway).Y) <= tolerance;
	}

	public bool IsCharacterOn(PathwaySet pathways) => CharacterPathways == Pathways.Layout.Clamp(pathways);

	public void Load() {
		Interlude.Spin(submessage: "Initializing your character...");
		using (StaticSequentialProfiler.StartStackFrame("Initialize Character")) {
			Interlude.Spin();
			Character.Initialize();
			Interlude.Spin();

			PlayCharacterAnimation(CharacterAnimationType.In);
		}
	}

	/// <summary>
	/// This is a callback for <see cref="ISustainManager"/> implementations. It expects wasSustaining, inSustaining, and sustainCount to be relative to the current pathway.
	/// </summary>
	/// <param name="sustain"></param>
	/// <param name="pathway"></param>
	/// <param name="wasSustainingBefore"></param>
	/// <param name="isSustainingNow"></param>
	/// <param name="sustainCount"></param>
	public void OnSustainCallback(SustainBeam sustain, PathwayId pathway, bool wasSustainingBefore, bool isSustainingNow, int sustainCount) {
		bool nowInsustain = Sustains.ActiveSustains() > 0;

		// todo
		if (isSustainingNow) {
			// TODO: Evaluate this...
			// if (InAir || Sustains.IsSustaining(PathwaySide.Top) && pathway == PathwaySide.Bottom)
			// 	PlayCharacterAnimation(CharacterAnimationType.DownPress);
			// else if (!InAir || Sustains.IsSustaining(PathwaySide.Bottom))
			// 	PlayCharacterAnimation(CharacterAnimationType.UpPress);
			// else

			PlayCharacterAnimation(CharacterAnimationType.Press);
		}
		else if (!nowInsustain) {
			if (Pathways.Layout.IsAirSide(pathway))
				PlayCharacterAnimation(CharacterAnimationType.UpPressEnd);
			else
				PlayCharacterAnimation(CharacterAnimationType.Run);
		}

		if (Scenes.HasActiveScene(out var scene))
			scene.OnPressStateChange(nowInsustain, wasSustainingBefore);
	}

	public void PlayCharacterAnimation(CharacterAnimationType type) {
		switch (type) {
			case CharacterAnimationType.Press:
			case CharacterAnimationType.DownPress:
			case CharacterAnimationType.UpPressStart:
			case CharacterAnimationType.UpPress:
			case CharacterAnimationType.UpPressEnd:
			case CharacterAnimationType.BigPress:
			case CharacterAnimationType.PressGroundToBig:
			case CharacterAnimationType.PressAirToBig:
			case CharacterAnimationType.PressBigToGround:
			case CharacterAnimationType.PressBigToAir:
			case CharacterAnimationType.PressHitToGround:
			case CharacterAnimationType.PressHitToAir:
			case CharacterAnimationType.UpPressHurt:
			case CharacterAnimationType.Run:
			case CharacterAnimationType.Jump:
				Character.GetPrimary().PlayAnimation(type);
				break;
			default:
				if (Sustains.IsSustaining()) {
					if (!level.IsSeeking)
						Character.GetSecondary().PlayAnimation(type);
				}
				else {
					Character.GetPrimary().PlayAnimation(type);
				}
				break;
		}
	}

	public void Reset() {
		if (Sustains.IsSustaining() && Scenes.HasActiveScene(out var scene))
			scene.OnPressStateChange(false, true);

		Character.Reset();
		LastJumpPathway = Pathways.JumpPathway;
		LastJumpTime = -2000000000000d;
		LastHologramJumpTIme = -2000000000000d;
	}

	public float? ResolveCharacterY(PathwaySet held) {
		if (Mash.InMashState)
			return Scenes.GetCenterPosition().Y;

		float sum = 0;
		int count = 0;
		for (int i = 0; i < Pathways.Layout.Count; i++) {
			var pathway = Pathways.Layout.PathwayAt(i);
			if (!held.Contains(pathway))
				continue;

			sum += Sustains.GetActiveSustain(pathway)?.GetHeldY() ?? Scenes.GetPathwayPosition(pathway).Y;
			count++;
		}

		return count == 0 ? null : sum / count;
	}

	public void Think(FrameState frameState) {
		float? yoff = null;

		yoff = ResolveCharacterY(Sustains.GetSustainState());

		if (yoff.HasValue) {
			if (YOffsetSOS == null)
				YOffsetSOS = new(15, 1, 1, yoff.Value);

			yoff = yoff.Value + -1f;
		}
		else
			YOffsetSOS = null;

		var playerY = yoff ?? GetPlayerY(false);

		float conductorInTime = Conductor.PreStartTime <= 0 ? 1 : (float)NMath.Remap(Conductor.Time, -Conductor.PreStartTime, -Conductor.PreStartTime / 1.5f, 0, 1, clampInput: true);
		conductorInTime = 1 - NMath.Ease.OutQuad(conductorInTime);

		// This sucks... todo, figure out how to get this proper
		var leftPlayer = Scenes.GetCenterPosition().X;

		Character.GetPrimary().SetPos(new Vector2F(
			(leftPlayer - 1) - (conductorInTime * 1),
			-(YOffsetSOS?.Update(playerY) ?? playerY)
		));
		Character.GetPrimary().SetScale(new(PlayerScale));

		Character.GetSecondary().SetPos(new Vector2F(
			(leftPlayer - 1),
			-GetPlayerY(true)
		));
		Character.GetSecondary().SetScale(new(PlayerScale));

		Character.Think();
	}

	void PlayHurtAnimation(ref PlayerDamagedGameEvent ev) {
		if (!ev.Hurt)
			return;

		if (InAir)
			PlayCharacterAnimation(CharacterAnimationType.JumpHurt);
		else
			PlayCharacterAnimation(CharacterAnimationType.Hurt);
	}

	void RelaySustainCallback(ref SustainStateChangedGameEvent ev) {
		OnSustainCallback(ev.Sustain, ev.Pathway, ev.WasSustainingBefore, ev.IsSustainingNow, ev.SustainCount);
	}

	void TriggerDeath(ref PlayerDeathGameEvent ev) {
		if (Quirks.PlayableAfterDeath)
			return;

		Character.GetPrimary().PlayAnimation(CharacterAnimationType.Die);
	}
}
