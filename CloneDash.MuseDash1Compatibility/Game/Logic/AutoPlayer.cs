using CloneDash.Common.Gamemodes.MuseDash;
using CloneDash.Common.Gamemodes.MuseDash.V1;
using CloneDash.Game.Entities;
using CloneDash.Game.Input;
using CloneDash.Settings;

using Nucleus;
using Nucleus.Core;
using Nucleus.Engine;
using Nucleus.Entities;
using Nucleus.Types;
using System.Diagnostics;

namespace CloneDash.Game.Logic
{
	public class AutoPlayer : LogicalEntity
	{
		/// <summary>
		/// Used to store which sustains are currently being held.
		/// </summary>
		public readonly Stack<SustainBeam>[] CurrentSustains = StackBasedSustainManager.CreatePathwayStacks();

		Stack<SustainBeam> StackOf(DashEnemy ent) => CurrentSustains[Level.As<MuseDash1Game>().PrimaryPathway(ent.Pathway).Index];

		/// <summary>
		/// Mark an entity as passed. Only used externally during seeking, for the sake of not confusing the autoplayer
		/// </summary>
		/// <param name="ent"></param>
		public void MarkEntityAsPassed(DashEnemy ent) => Passed.Add(ent);
		public void MarkSustainAsActive(DashEnemy ent) {
			if (ent.Type == MuseDash1EntityType.SustainBeam)
				StackOf(ent).Push((SustainBeam)ent);
		}

		/// <summary>
		/// Mark an entity as inactive, and also completes the sustain beam early. Only used externally during seeking, for the sake of not confusing the autoplayer
		/// </summary>
		/// <param name="ent"></param>
		public void MarkSustainAsInactive(DashEnemy ent) {
			if (ent.Type == MuseDash1EntityType.SustainBeam) {
				bool popped = StackOf(ent).TryPop(out var beam);
				Debug.Assert(popped && beam == (SustainBeam)ent);
				beam?.Complete();
			}
		}

		public void SustainHoldThink(ref InputState input) {
			var layout = Level.As<MuseDash1Game>().Layout;
			for (int pathway = 0; pathway < layout.Count; pathway++) {
				var stack = CurrentSustains[pathway];
				// Is there a sustain in progress on this pathway?
				while (stack.TryPeek(out SustainBeam? sustain)) {
					bool holding = false;

					if (sustain.StopAcceptingInput == true) { // Is the sustain beam self-reporting as being complete, and if so, note it doesn't need to be held down anymore
						holding = false;
						stack.Pop();
						//Logs.Debug("Sustain stopped because it is complete");
						continue;
					}
					else // Keep holding the button otherwise
						holding = true;

					// Write to input
					var downOff = holding ? 0 : -1;
					input.Hold(layout.Definitions[pathway].Action, stack.Count + downOff);

					break;
				}
			}
		}

		/// <summary>
		/// Entities the autoplayer has passed already.
		/// </summary>
		private readonly HashSet<DashEnemy> Passed = [];

		/// <summary>
		/// Last time the autoplayer hit a masher. Used to limit masher hits.
		/// </summary>
		private double LastMasherHit { get; set; }
		/// <summary>
		/// How many times the auto-player will hit a masher per second.
		/// </summary>
		private const double TIME_BETWEEN_MASH_HITS = (1d / Masher.MASHER_AUTOPLAYER_MAX_HITS_PER_SECOND);
		/// <summary>
		/// Ran-per-tick function to check if the delta-time since the last masher hit is greater than MAX_MASHHITS_PER_SECOND
		/// </summary>
		private bool CanHitMasher => (Level.As<MuseDash1Game>().Conductor.Time - LastMasherHit) > TIME_BETWEEN_MASH_HITS;
		/// <summary>
		/// Internal function to check the hashmap for passed-by entities.
		/// </summary>
		/// <param name="entity"></param>
		/// <returns></returns>
		private bool PassedEntity(DashEnemy entity) => Passed.Contains(entity);

		/// <summary>
		/// This method automatically scans the visible entities for the next entity to hit at an almost-perfect time, and then hits the entity by simulating input presses
		/// </summary>
		/// <param name="input"></param>
		public void Play(ref InputState input) {
			if (!Enabled)
				return;

			var level = Level.As<MuseDash1Game>();

			// Mash state functionality
			if (level.InMashState && CanHitMasher) {
				input.Clicked[(int)InputAction.AirAttack] = 1; // Attack the masher
				LastMasherHit = Level.As<MuseDash1Game>().Conductor.Time; // Set the masher
				return;
			}

			// Sort the visible entities by closest to furthest
			var ents = level.EnemyManager.GetLastVisible();

			// Find the closest interactive entity that hasnt been passed
			var entIndex = GetClosestInteractiveIdx(ents);
			bool avoidedTop = false, avoidedBottom = false;
			if (entIndex != -1) {
				while (entIndex < ents.Length) {
					var ent = ents[entIndex];
					entIndex++;

					// Is an entity visible?
					if (ent != default) {
						var pathway = level.GetPathway(ent);
						var timeToHit = ent.GetJudgementTimeUntilHit();
						switch (ent.Interactivity) {
							// Same pathway system, either hit or just run into
							case EntityInteractivity.SamePath:
							case EntityInteractivity.Hit:
							case EntityInteractivity.Sustain:
								if (NMath.InRange((float)timeToHit, -ent.PrePerfectRange, 0.001f) && AvoidHittingAgain(ent)) {
									if (ent.Pathway == level.JumpPathway.AsSet && !avoidedTop)
										input.Click(InputAction.AirAttack);
									else if (!avoidedBottom) {
										if (ent.Interactivity == EntityInteractivity.SamePath) {
											// If currently in the air, always try to click a same path ... or if sustaining
											if (level.InAir || level.Sustains.IsSustaining())
												input.Click(InputAction.GroundAttack);
										}
										else
											input.Click(InputAction.GroundAttack);
									}

									// Special logic for sustain beams, hold them in memory so they can continue to be held
									if (ent.Type == MuseDash1EntityType.SustainBeam)
										StackOf(ent).Push((SustainBeam)ent);

									// Record the entity as passed
									MarkEntityAsPassed(ent);
									//Logs.Debug($"Hit [{level.Entities.IndexOf(ent)}] {ent}");
								}
								break;
							// Entities that need to be avoided
							case EntityInteractivity.Avoid:
								if (NMath.InRange((float)timeToHit, -ent.PrePerfectRange, -0.001f)) {
									if (level.IsCharacterOn(ent.Pathway)) {
										// The entity needs to be avoided, so pick the opposite side the entity is on
										if (ent.Pathway == level.GroundPathway.AsSet && input.ClickCount(InputAction.AirAttack) <= 0) {
											input.Click(InputAction.AirAttack);
											avoidedTop = true;
										}
										else if (input.ClickCount(InputAction.GroundAttack) <= 0) {
											input.Click(InputAction.GroundAttack);
											avoidedBottom = true;
										}
									}
									// Record the entity as passed
									MarkEntityAsPassed(ent);
									//Logs.Debug($"Avoided [{level.Entities.IndexOf(ent)}] {ent}");
								}
								break;
							default:
								Console.WriteLine($"Can't handle {ent.Interactivity}! Write functionality for this entity's interactivity logic!");
								break;
						}
					}
				}
			}

			// Sustain holding logic
			SustainHoldThink(ref input);
		}

		private bool AvoidHittingAgain(DashEnemy ent) {
			switch (ent.Type) {
				default:
					return !ent.Dead;
				case MuseDash1EntityType.Masher:
					return true;
			}
		}

		private int GetClosestInteractiveIdx(Span<DashEnemy> ents) {
			for (int i = 0; i < ents.Length; i++) {
				var x = ents[i];
				if (x.Interactivity != EntityInteractivity.Noninteractive && !PassedEntity(x) && !x.Dead)
					return i;
			}
			return -1;
		}

		public const string STRING_AUTO = "AUTO";

		// public override void Render2D(FrameState frameState) {
		// 	if (Enabled) {
		// 		Graphics2D.SetDrawColor(255, 255, 255, 255);
		// 		Graphics2D.DrawText((frameState.WindowWidth / 2), 24, STRING_AUTO, Graphics2D.UI_FONT_NAME, 30, Anchor.Center);
		// 	}
		// }

		public void Reset() {
			Passed.Clear();
			foreach (var stack in CurrentSustains)
				stack.Clear();
			LastMasherHit = default;
		}
	}
}
