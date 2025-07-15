using CloneDash.Game.Entities;
using CloneDash.Game.Input;

using Nucleus;
using Nucleus.Core;
using Nucleus.Engine;
using Nucleus.Entities;
using Nucleus.Types;

namespace CloneDash.Game.Logic
{
	public class AutoPlayer : LogicalEntity
	{
		/// <summary>
		/// Used to store which sustains are currently being held.
		/// </summary>
		public Dictionary<PathwaySide, Stack<SustainBeam>> CurrentSustains = new() {
			{ PathwaySide.Top, new() },
			{ PathwaySide.Bottom, new() },
		};

		/// <summary>
		/// Entities the autoplayer has passed already.
		/// </summary>
		private static HashSet<DashModelEntity> Passed { get; set; } = new();
		/// <summary>
		/// Last time the autoplayer hit a masher. Used to limit masher hits.
		/// </summary>
		private static DateTime LastMasherHit { get; set; }
		/// <summary>
		/// How many times the auto-player will hit a masher per second.
		/// </summary>
		private const double MAX_MASHHITS_PER_SECOND = (1d / 26d);
		/// <summary>
		/// Ran-per-tick function to check if the delta-time since the last masher hit is greater than MAX_MASHHITS_PER_SECOND
		/// </summary>
		private bool CanHitMasher => (DateTime.Now - LastMasherHit).TotalSeconds > MAX_MASHHITS_PER_SECOND;
		/// <summary>
		/// Internal function to check the hashmap for passed-by entities.
		/// </summary>
		/// <param name="entity"></param>
		/// <returns></returns>
		private bool PassedEntity(DashModelEntity entity) => Passed.Contains(entity);

		/// <summary>
		/// This method automatically scans the visible entities for the next entity to hit at an almost-perfect time, and then hits the entity by simulating input presses
		/// </summary>
		/// <param name="input"></param>
		public void Play(ref InputState input) {
			if (!Enabled)
				return;

			var level = Level.As<DashGameLevel>();

			// Mash state functionality
			if (level.InMashState && CanHitMasher) {
				input.TopClicked = 1; // Attack the masher
				LastMasherHit = DateTime.Now; // Set the masher
				return;
			}

			// Sort the visible entities by closest to furthest
			var ents = level.VisibleEntities;
			ents.Sort((x, y) => x.GetJudgementTimeUntilHit().CompareTo(y.GetJudgementTimeUntilHit()));

			// Find the closest interactive entity that hasnt been passed
			var entIndex = ents.FindIndex(x => x.Interactivity != EntityInteractivity.Noninteractive && !PassedEntity(x) && !x.Dead);
			bool avoidedTop = false, avoidedBottom = false;
			if (entIndex != -1) {
				while (entIndex < ents.Count) {
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
								if (NMath.InRange((float)timeToHit, -ent.PrePerfectRange, 0.001f)) {
									if (ent.Pathway == PathwaySide.Top && !avoidedTop)
										input.TopClicked += 1;
									else if(!avoidedBottom) {
										if (ent.Interactivity == EntityInteractivity.SamePath) {
											if (level.InAir || level.Sustains.IsSustaining())
												input.BottomClicked += 1;
										}
										else
											input.BottomClicked += 1;
									}

									// Special logic for sustain beams, hold them in memory so they can continue to be held
									if (ent.Type == EntityType.SustainBeam) {
										CurrentSustains[ent.Pathway].Push((SustainBeam)ent);
									}

									// Record the entity as passed
									Passed.Add(ent);
									//Logs.Debug($"Hit [{level.Entities.IndexOf(ent)}] {ent}");
								}
								break;
							// Entities that need to be avoided
							case EntityInteractivity.Avoid:
								if (NMath.InRange((float)timeToHit, -ent.PrePerfectRange, -0.001f)) {
									if (level.Pathway == ent.Pathway) {
										// The entity needs to be avoided, so pick the opposite side the entity is on
										if (ent.Pathway == PathwaySide.Bottom && input.TopClicked <= 0) {
											input.TopClicked += 1;
											avoidedTop = true;
										}
										else if (input.BottomClicked <= 0) {
											input.BottomClicked += 1;
											avoidedBottom = true;
										}
									}
									// Record the entity as passed
									Passed.Add(ent);
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
			foreach (var kvp in CurrentSustains) {
				// Is there a sustain in progress on this pathway?
				if (kvp.Value.TryPeek(out SustainBeam? sustain)) {
					bool holding = false;

					if (sustain.StopAcceptingInput == true) { // Is the sustain beam self-reporting as being complete, and if so, note it doesn't need to be held down anymore
						holding = false;
						kvp.Value.Pop();
						//Logs.Debug("Sustain stopped because it is complete");
					}
					else // Keep holding the button otherwise
						holding = true;

					// Write to input
					var downOff = holding ? 0 : -1;
					if (kvp.Key == PathwaySide.Top)
						input.TopHeldCount += kvp.Value.Count + downOff;
					else
						input.BottomHeldCount += kvp.Value.Count + downOff;
				}
			}
		}

		public const string STRING_AUTO = "AUTO";

		public override void Render2D(FrameState frameState) {
			if (Enabled) {
				Graphics2D.SetDrawColor(255, 255, 255, 255);
				Graphics2D.DrawText((frameState.WindowWidth / 2), frameState.WindowHeight * 0.07f, STRING_AUTO, "Noto Sans", 30, Anchor.Center);
			}
		}

		public void Reset() {
			Passed.Clear();
		}
	}
}
