﻿using CloneDash.Game.Entities;
using CloneDash.Game.Input;

namespace CloneDash.Game.Components
{
    /// <summary>
    /// DashGame auto-player
    /// </summary>
    public class AutoPlayer : DashGameComponent
    {
        public AutoPlayer(DashGame game) : base(game) {
            Required = false;
            Enabled = false;
        }

        /// <summary>
        /// Used to store which sustains are currently being held.
        /// </summary>
        public Dictionary<PathwaySide, SustainBeam?> CurrentSustains = new() {
            { PathwaySide.Top, null },
            { PathwaySide.Bottom, null },
        };

        /// <summary>
        /// Entities the autoplayer has passed already.
        /// </summary>
        private static HashSet<MapEntity> Passed { get; set; } = new();
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
        private bool PassedEntity(MapEntity entity) => Passed.Contains(entity);

        /// <summary>
        /// This method automatically scans the visible entities for the next entity to hit at an almost-perfect time, and then hits the entity by simulating input presses
        /// </summary>
        /// <param name="input"></param>
        public void Play(ref InputState input) {
            if (!Enabled)
                return;

            // Mash state functionality
            if (Game.InMashState && CanHitMasher) {
                input.TopClicked = 1; // Attack the masher
                LastMasherHit = DateTime.Now; // Set the masher
                return;
            }

            // Sort the visible entities by closest to furthest
            var ents = Game.GameplayManager.VisibleEntities;
            ents.Sort((x, y) => x.DistanceToHit.CompareTo(y.DistanceToHit));

            // Find the closest interactive entity that hasnt been passed
            var ent = ents.FirstOrDefault(x => x.Interactivity != EntityInteractivity.Noninteractive && !PassedEntity(x) && !x.Dead);


            // Is an entity visible?
            if (ent != default) {
                var pathway = Game.GetPathway(ent);
                switch (ent.Interactivity) {
                    // Same pathway system, either hit or just run into
                    case EntityInteractivity.Hit:
                    case EntityInteractivity.SamePath:
                    case EntityInteractivity.Sustain:
                        if (DashMath.InRange((float)ent.DistanceToHit, -ent.PrePerfectRange, 0.001f)) {
                            if (ent.Pathway == PathwaySide.Top)
                                input.TopClicked = 1;
                            else
                                input.BottomClicked = 1;

                            // Special logic for sustain beams, hold them in memory so they can continue to be held
                            if (ent.Type == EntityType.SustainBeam) {
                                CurrentSustains[ent.Pathway] = (SustainBeam)ent;
                            }

                            // Record the entity as passed
                            Passed.Add(ent);
                        }
                        break;
                    // Entities that need to be avoided
                    case EntityInteractivity.Avoid:
                        if (DashMath.InRange((float)ent.DistanceToHit, -ent.PrePerfectRange, -0.001f)) {
                            if (Game.PlayerController.Pathway == ent.Pathway) {
                                // The entity needs to be avoided, so pick the opposite side the entity is on
                                if (ent.Pathway == PathwaySide.Bottom)
                                    input.TopClicked = 1;
                                else
                                    input.BottomClicked = 1;
                            }
                            // Record the entity as passed
                            Passed.Add(ent);
                        }
                        break;
                    default:
                        Console.WriteLine($"Can't handle {ent.Interactivity}! Write functionality for this entity's interactivity logic!");
                        break;
                }
            }

            // Sustain holding logic
            foreach (var kvp in CurrentSustains) {
                // Is there a sustain in progress on this pathway?
                if (kvp.Value != null) {
                    bool holding = false;

                    if (kvp.Value.StopAcceptingInput == true) { // Is the sustain beam self-reporting as being complete, and if so, note it doesn't need to be held down anymore
                        holding = false;
                        CurrentSustains[kvp.Key] = null;
                    }
                    else // Keep holding the button otherwise
                        holding = true;

                    // Write to input
                    if (kvp.Key == PathwaySide.Top)
                        input.TopHeld = holding;
                    else
                        input.BottomHeld = holding;
                }
            }

            input.TopHeld |= input.TopClicked > 0;
            input.BottomHeld |= input.BottomClicked > 0;
        }

        public const string STRING_AUTO = "AUTO";
        public override void OnDrawScreenSpace(float w, float h) {
            if (Enabled) {
                Graphics.SetDrawColor(255, 255, 255, 255);
                Graphics.DrawText((w / 2), h * 0.07f, STRING_AUTO, "Arial", 30, FontAlignment.Center, FontAlignment.Center);
            }
        }
    }
}
