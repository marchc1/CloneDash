using CloneDash.Common;
using CloneDash.Common.Game;
using CloneDash.Common.Gamemodes.MuseDash;
using CloneDash.Common.Gamemodes.MuseDash.V1;
using CloneDash.Game.Entities;
using CloneDash.Game.Input;
using CloneDash.Game.Logic;
using CloneDash.Settings;
using Nucleus;
using Nucleus.Types;

using Color = Nucleus.Common.Types.Color;

namespace CloneDash.Game;

public class MuseDash1JudgeComponent(MuseDash1CharacterComponent character, MuseDash1ChartComponent chart, Conductor conductor, GameEventManager events, FeverComponent fever, HitsoundComponent hitsounds, InputComponent input, RhythmGameLevel level, MashComponent mash, PathwaysComponent pathways, MuseDash1RulesComponent rules, ISustainManager sustains) : IRhythmGameThinker
{
	public PollResult LastPollResult = PollResult.Empty;

	readonly MuseDash1CharacterComponent Character = character;
	readonly MuseDash1ChartComponent Chart = chart;
	readonly Conductor Conductor = conductor;
	readonly FeverComponent Fever = fever;
	readonly HitsoundComponent Hitsounds = hitsounds;
	readonly InputComponent Input = input;
	readonly MashComponent Mash = mash;
	readonly PathwaysComponent Pathways = pathways;
	readonly MuseDash1RulesComponent Rules = rules;
	readonly ISustainManager Sustains = sustains;

	PathwayId LastAttackPathway;
	double LastAttackTime;

	public bool InHit { get; private set; } = false;

	public RhythmGameThinkPhase Phase => RhythmGameThinkPhase.Judge;

	public bool SuppressHitMessages { get; set; }
	public bool CanHit(PathwayId pathway) {
		if (Sustains.IsSustaining(pathway))
			return false;

		if (Pathways.Layout.IsAirSide(pathway) && Character.InAir)
			return false;

		return true;
	}

	public void EnterHitState() {
		InHit = true;
		SuppressHitMessages = false;
	}

	public void ExitHitState() {
		InHit = false;
	}

	/// <summary>
	/// Polling function which figures out the closest, potentially-hit entity and returns the result.
	/// </summary>
	/// <param name="pathway"></param>
	/// <returns>A <see cref="PollResult"/>, if it hit something, Hit is true, and vice versa.</returns>
	public PollResult Poll(in PollParams parms) {
		var visibleEnemies = Chart.EnemyManager.GetLastVisible();
		foreach (DashEnemy entity in visibleEnemies) {
			// If the entity has no interactivity, ignore it in the poll
			if (!entity.Interactive)
				continue;

			// If the entity says its dead, ignore it
			if (entity.Dead)
				continue;

			switch (entity.Interactivity) {
				case EntityInteractivity.Hit:
				case EntityInteractivity.Sustain:
					bool isValidHit = entity.Interactivity != EntityInteractivity.Sustain ? !entity.Dead : !(entity as SustainBeam)!.HeldState;
					if (isValidHit && entity.Pathway.Contains(parms.Pathway)) {
						double distance = entity.GetJudgementTimeUntilHit();
						double pregreat = -entity.PreGreatRange, postgreat = entity.PostGreatRange;
						double preperfect = -entity.PrePerfectRange, postperfect = entity.PostPerfectRange;
						if (NMath.InRange(distance, pregreat, postgreat)) { // hit occured
							LastPollResult = PollResult.Create(entity, distance);
							return LastPollResult;
						}
					}
					break;
			}
		}

		LastPollResult = PollResult.Empty;
		return LastPollResult;
	}

	public void Think(FrameState frameState) {
		var visibleEnemies = Chart.EnemyManager.GetLastVisible();

		// Start input processing.
		// Bottom is executed first, so if two pathway attacks happen on the same frame, it can exit the jump state
		// before jumping again, allowing the attack to work as expected
		for (int pathway = Pathways.Layout.Count - 1; pathway >= 0; pathway--)
			HitLogic(Pathways.Layout.PathwayAt(pathway));

		// Skipped while dead: notes still scroll by during the fail fade-in, but nothing gets collected/missed/hit
		// (that played pickup sounds and healed a dead player).
		if (Rules.IsDead())
			return;

		// This loop is mostly for per-tick polls that need to occur, ie. when entities have been fully missed.
		// It is ran after input processing.
		foreach (var entity in visibleEnemies) {
			var timeToHit = entity.GetJudgementTimeUntilHit();
			switch (entity.Interactivity) {
				case EntityInteractivity.Hit:
				case EntityInteractivity.Sustain:
					if (!entity.Dead) {
						// Is it too late for the player to hit this entity anyway?
						if (timeToHit < -entity.PreGreatRange
							&& !(entity is SustainBeam se && se.HeldState == true)
							&& !(entity is Masher me && me.Hits > 0)
						) {
							entity.Miss();
							events.Fire(new NoteMissedGameEvent(entity));
						}
					}
					break;
				case EntityInteractivity.SamePath:
					if (NMath.InRange(timeToHit, -entity.PreGreatRange, 0)) {
						PathwaySet characterPathways = Character.CharacterPathways;
						var entityPathway = Pathways.PrimaryPathway(entity.Pathway);
						if ((characterPathways.Count >= 2 || Character.IsCharacterOn(entity.Pathway) || Character.IsCharacterNear(entityPathway)) && entity.Hits == 0) {
							entity.Hit(entityPathway, 0);
							if (!level.IsSeeking)
								events.Fire(new BonusCollectedGameEvent(entity, entityPathway));
							Hitsounds.Play(entity.Type switch {
								MuseDash1EntityType.Heart => SceneSound.GotHeart,
								MuseDash1EntityType.Score => SceneSound.GotScore,
								_ => SceneSound.GotScore
							}, 1);
						}
					}
					break;
				case EntityInteractivity.Avoid:
					// If the player is sustaining on the pathway this entity is on, then ignore
					if (Sustains.IsSustaining(Pathways.Layout.Clamp(entity.Pathway)))
						break;

					// Checks if the player has completely failed to avoid the entity, and if so, damages the player.

					bool broken = false;
					bool windowEnded = timeToHit < -entity.PreGreatRange;
					if (Character.IsCharacterOn(entity.Pathway) && timeToHit < -entity.PrePerfectRange && !windowEnded && !entity.DidRewardPlayer) {
						//entity.Hit(Game.PlayerController.Pathway);
						if (Rules.BreaksAvoids()) {
							if (entity.Hits == 0)
								entity.Hit(Pathways.PrimaryPathway(entity.Pathway), 0);
							broken = true;
						}
						else
							entity.DamagePlayer();
					}

					// If the player is now avoiding the entity, then reward the player for missing it, and make it so they cant be damaged by it)
					if (!broken && windowEnded && !entity.DidDamagePlayer) {
						entity.Pass();
					}

					break;
			}
			//entity.WhenVisible();
		}

		level.AddDebugString("HoldingTopPathwaySustain", Sustains.GetSustainsActiveCount(Pathways.JumpPathway));
		level.AddDebugString("HoldingBottomPathwaySustain", Sustains.GetSustainsActiveCount(Pathways.GroundPathway));
	}

	private void HitLogic(PathwayId pathway) {
		int amountOfTimesHit = Input.State.ClickCount(Pathways.Layout[pathway].Action);
		bool keyHitOnThisSide = amountOfTimesHit > 0;

		if (!keyHitOnThisSide)
			return;

		PollParams pollParams = new();
		pollParams.AmountOfTimesHit = amountOfTimesHit;
		pollParams.HitsRemaining = amountOfTimesHit;
		pollParams.Pathway = pathway;

		while (pollParams.HitsRemaining > 0) {
			EnterHitState();

			LastAttackTime = Conductor.Time;
			LastAttackPathway = pathway;

			// Hit testing
			PollResult pollResult = default;
			if (Mash.InMashState) {
				//if (Debug)
				//Console.WriteLine($"mashing entity = {MashingEntity}");
				Mash.SubmitMashHit();
			}
			else {
				pollResult = Poll(in pollParams);

				if (pollResult.Hit) {
					pollResult.HitEntity.WasHitPerfect = pollResult.IsPerfect;
					pollResult.HitEntity.Hit(pathway, pollResult.DistanceToHit);

					if (SuppressHitMessages == false && !level.IsSeeking) {
						Color c = pollResult.HitEntity.HitColor;

						bool showearlylate = GameSettings.gp_earlylate.GetBool();
						EarlyLate earlylate = EarlyLate.Perfect;
						if (showearlylate) {
							// TODO: tolerances for early/late..
						}

						// Mashers don't create perfects, they'll start the mash hit UI
						if (pollResult.HitEntity.Type != MuseDash1EntityType.Masher) {
							events.Fire(new NoteHitGameEvent(pollResult.HitEntity, pathway, pollResult.IsPerfect, pollResult.Precision, Fever.InFever, earlylate));
						}

						Hitsounds.Play(pollResult.HitEntity.Type switch {
							MuseDash1EntityType.Single => pollResult.HitEntity.Variant switch {
								EntityVariant.Small => SceneSound.HitSmall,
								EntityVariant.Medium1 => SceneSound.HitMedium1,
								EntityVariant.Medium2 => SceneSound.HitMedium2,
								EntityVariant.Large1 => SceneSound.HitLarge1,
								EntityVariant.Large2 => SceneSound.HitLarge2,
								EntityVariant.Boss1 => SceneSound.HitBoss1,
								EntityVariant.Boss2 => SceneSound.HitBoss2,
								EntityVariant.Boss3 => SceneSound.HitBoss3,
								EntityVariant.BossHitFast => SceneSound.HitBossFast,
								EntityVariant.BossHitSlow => SceneSound.HitBossSlow,
								_ => SceneSound.HitMedium1
							},
							MuseDash1EntityType.Hammer => SceneSound.HitHammer,
							MuseDash1EntityType.Double => SceneSound.HitGemini,
							MuseDash1EntityType.SustainBeam => SceneSound.StartedHold,
							MuseDash1EntityType.Raider => SceneSound.HitRaider,
							MuseDash1EntityType.Ghost => SceneSound.HitGhost,
							// I think HP/score get handled on their own
							_ => SceneSound.HitMedium1
						}, 1);
					}
				}
			}

			// Trigger animation events on the player controller
			var hitSomething = pollResult.Hit;
			if (Pathways.Layout.IsAirSide(pathway))
				Character.AttackAir(pathway, pollResult);
			else
				Character.AttackGround(pathway, pollResult);

			ExitHitState();

			pollParams.HitsRemaining--;
		}
		//if (Debug)
		//Console.WriteLine($"poll.Hit = {hitSomething}, entity = {((pollResult.HasValue && pollResult.Value.Hit) ? pollResult.Value.HitEntity.ToString() : "NULL")}");
	}
}
