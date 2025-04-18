using Nucleus.Engine;
using Nucleus.Core;
using CloneDash.Game.Input;
using System.Numerics;
using Nucleus.Types;
using Raylib_cs;
using CloneDash.Game.Entities;

namespace CloneDash.Game
{
    public partial class CD_GameLevel : Level
    {
        /// <summary>
        /// Currently visible entities this tick
        /// </summary>
        public List<CD_BaseMEntity> VisibleEntities { get; private set; } = [];

        private double LastAttackTime;
        private PathwaySide LastAttackPathway;

        private void HitLogic(PathwaySide pathway) {
            int amountOfTimesHit = pathway == PathwaySide.Top ? InputState.TopClicked : InputState.BottomClicked;
            bool keyHitOnThisSide = amountOfTimesHit > 0;

            if (!keyHitOnThisSide)
                return;

            for (int i = 0; i < amountOfTimesHit; i++) {
                EnterHitState();

                LastAttackTime = Conductor.Time;
                LastAttackPathway = pathway;

                // Hit testing
                PollResult? pollResult = null;
                if (InMashState) {
                    //if (Debug)
                        //Console.WriteLine($"mashing entity = {MashingEntity}");

                    MashingEntity.Hit(pathway);
                }
                else {
                    var poll = Poll(pathway);
                    pollResult = poll;

                    if (poll.Hit) {
                        poll.HitEntity.Hit(pathway);
                        Sounds.PlaySound("punch.wav", true, 0.24f);

                        if (SuppressHitMessages == false) {
                            Color c = poll.HitEntity.HitColor;
                            SpawnTextEffect(poll.Greatness, GetPathway(pathway).Position, TextEffectTransitionOut.SlideUp, c);
                        }
                    }
                }

                // Trigger animation events on the player controller
                var hitSomething = pollResult.HasValue && pollResult.Value.Hit;
                if (pathway == PathwaySide.Top)
                    AttackAir(pollResult ?? default);
                else
                    AttackGround(pollResult ?? default);

                if(hitSomething)
                    Stats.Hits++;

                ExitHitState();

                //if (Debug)
                    //Console.WriteLine($"poll.Hit = {hitSomething}, entity = {((pollResult.HasValue && pollResult.Value.Hit) ? pollResult.Value.HitEntity.ToString() : "NULL")}");
            }
        }
    }
}
