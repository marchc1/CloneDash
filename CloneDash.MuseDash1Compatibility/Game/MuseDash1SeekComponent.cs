using CloneDash.Common.Game;
using CloneDash.Common.Gamemodes.MuseDash;
using CloneDash.Common.Gamemodes.MuseDash.V1;
using CloneDash.Game.Entities;
using CloneDash.Game.Input;
using CloneDash.Game.Logic;

namespace CloneDash.Game;

public class MuseDash1SeekComponent(AutoPlayer autoPlayer, MuseDash1CharacterComponent character, MuseDash1ChartComponent chart, Conductor conductor, FeverComponent fever, MuseDash1FeverFXComponent feverFX, RhythmGameLevel level, MashComponent mash, PathwaysComponent pathways, ISustainManager sustains) : IRhythmGameComponent
{
	readonly AutoPlayer AutoPlayer = autoPlayer;
	readonly MuseDash1CharacterComponent Character = character;
	readonly MuseDash1ChartComponent Chart = chart;
	readonly Conductor Conductor = conductor;
	readonly FeverComponent Fever = fever;
	readonly MuseDash1FeverFXComponent FeverFX = feverFX;
	readonly MashComponent Mash = mash;
	readonly PathwaysComponent Pathways = pathways;
	readonly ISustainManager Sustains = sustains;

	public void Reset() { }

	public void SeekTo(double time) {
		if (time > 0) {
			foreach (var ev in Chart.Events) {
				switch (ev.TriggerType) {
					case EventTriggerType.AtTime:
						Conductor.ForceTimeTo(ev.Time);
						if (ev.Time < time)
							Chart.ActivateEvent(ev);

						Conductor.ForceTimeTo(ev.Time + ev.Length);
						if (ev.Time + ev.Length < time)
							Chart.DeactivateEvent(ev);

						break;
					case EventTriggerType.AtTimeMinusLength:
						Conductor.ForceTimeTo(ev.Time - ev.Length);
						if (ev.Time - ev.Length < time)
							Chart.ActivateEvent(ev);

						Conductor.ForceTimeTo(ev.Time);
						if (ev.Time < time)
							Chart.DeactivateEvent(ev);
						break;
				}
			}

			foreach (var entity in level.Entities) {
				if (entity is DashEnemy mEnt && mEnt.GetJudgementHitTime() < time) {
					Conductor.ForceTimeTo(mEnt.GetJudgementHitTime()); // Hack...

					// Evaluate if fever must end
					if (Fever.ShouldExit && Fever.InFever)
						Fever.Exit();

					switch (mEnt) {
						case Masher masher: // Mashers need a bit more trickery
							Mash.EnterMashState(masher);
							for (int i = 0; i < masher.MaxHits; i++)
								masher.Hit(Pathways.PrimaryPathway(mEnt.Pathway), 0);
							Mash.ExitMashState(true);
							masher.RewardPlayer();
							AutoPlayer.MarkEntityAsPassed(mEnt);
							break;
						case SustainBeam sustain:
							sustain.Hit(Pathways.PrimaryPathway(sustain.Pathway), 0);
							sustain.RewardPlayer();

							AutoPlayer.MarkSustainAsActive(sustain);
							// Force time to the smaller value: either the end of the sustain, or the seeking time.
							// This fixes issues when placing an autoplayer in the middle of a sustain.
							var endOfSustainIsh = sustain.GetJudgementHitTime() + sustain.Length + 0.01;
							Conductor.ForceTimeTo(Math.Min(endOfSustainIsh, time));

							// hack, but will force sustain to cancel if its ready
							InputState s = default;
							AutoPlayer.SustainHoldThink(ref s);

							AutoPlayer.MarkEntityAsPassed(mEnt);
							if (time > endOfSustainIsh)
								AutoPlayer.MarkSustainAsInactive(sustain);
							break;
						default:
							switch (mEnt.Interactivity) {
								case EntityInteractivity.Hit:
									mEnt.Hit(Pathways.PrimaryPathway(mEnt.Pathway), 0);
									mEnt.RewardPlayer();
									break;
								case EntityInteractivity.SamePath:
									mEnt.Hit(Pathways.PrimaryPathway(mEnt.Pathway), 0);
									mEnt.RewardPlayer();
									break;
								case EntityInteractivity.Avoid:
									mEnt.Pass();
									mEnt.RewardPlayer();
									break;
							}
							AutoPlayer.MarkEntityAsPassed(mEnt);
							break;
					}
					Conductor.RemoveForcedTime();
				}
			}

			// HACK - but it solves fever FX not playing
			if (Fever.InFever) FeverFX.Activate();

			// ALSO A HACK - but it solves some animation issues when mid-sustain.
			if (Sustains.IsSustaining())
				Character.PlayCharacterAnimation(CharacterAnimationType.Press);
		}
	}
}
