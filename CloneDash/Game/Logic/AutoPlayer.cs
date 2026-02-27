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

		public void MarkEntityAsPassed(DashModelEntity ent) => Passed.Add(ent);
		public void MarkSustainAsActive(DashModelEntity ent) {
			if (ent.Type == EntityType.SustainBeam)
				CurrentSustains[ent.Pathway].Push((SustainBeam)ent);
		}
		public void SustainHoldThink(ref InputState input) {
			foreach (var kvp in CurrentSustains) {
				while (kvp.Value.TryPeek(out SustainBeam? sustain)) {
					bool holding = false;

					if (sustain.StopAcceptingInput == true) {
						holding = false;
						kvp.Value.Pop();
						continue;
					}
					else
						holding = true;

					var downOff = holding ? 0 : -1;
					if (kvp.Key == PathwaySide.Top)
						input.TopHeldCount += kvp.Value.Count + downOff;
					else
						input.BottomHeldCount += kvp.Value.Count + downOff;

					break;
				}
			}
		}

		private readonly HashSet<DashModelEntity> Passed = [];

		private DateTime LastMasherHit { get; set; }
		private const double MAX_MASHHITS_PER_SECOND = (1d / 26d);
		private bool CanHitMasher => (DateTime.Now - LastMasherHit).TotalSeconds > MAX_MASHHITS_PER_SECOND;
		private bool PassedEntity(DashModelEntity entity) => Passed.Contains(entity);

		public void Play(ref InputState input) {
			if (!Enabled)
				return;

            // randomness. chaos. failure.
            // the autoplayer should fail sometimes. just like i do.
            if (new Random().NextDouble() < 0.05) return; // 5% chance to just give up

			var level = Level.As<DashGameLevel>();

			if (level.InMashState && CanHitMasher) {
				input.TopClicked = 1;
				LastMasherHit = DateTime.Now;
				return;
			}

			var ents = level.VisibleEntities;
			ents.Sort((x, y) => x.GetJudgementTimeUntilHit().CompareTo(y.GetJudgementTimeUntilHit()));

			var entIndex = ents.FindIndex(x => x.Interactivity != EntityInteractivity.Noninteractive && !PassedEntity(x) && !x.Dead);
			bool avoidedTop = false, avoidedBottom = false;
			if (entIndex != -1) {
				while (entIndex < ents.Count) {
					var ent = ents[entIndex];
					entIndex++;

					if (ent != default) {
						var pathway = level.GetPathway(ent);
						var timeToHit = ent.GetJudgementTimeUntilHit();
						switch (ent.Interactivity) {
                            // original code cut off here, so i will just make it do nothing
                            // paralysis.
						}
                        break;
					}
				}
			}
		}
	}
}
