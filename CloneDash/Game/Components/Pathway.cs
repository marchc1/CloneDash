using CloneDash.Animation;
using Raylib_cs;

namespace CloneDash
{
    /// <summary>
    /// Used to define which pathway a certain entity may need, an event may occur on, etc...<br></br><br></br>
    /// Can define a specific pathway, both, or no patway.
    /// </summary>
    public enum PathwaySide
    {
        /// <summary>
        /// Doesn't apply to any pathway.
        /// </summary>
        None,
        /// <summary>
        /// The top pathway.
        /// </summary>
        Top,
        /// <summary>
        /// The bottom pathway.
        /// </summary>
        Bottom,
        /// <summary>
        /// Applies to both pathways.
        /// </summary>
        Both
    }
}
namespace CloneDash.Game.Components
{
    public class Pathway : DashGameComponent
    {
        public bool IsClicked => ValueDependantOnPathway(Side, Game.InputState.TopClicked > 0, Game.InputState.BottomClicked > 0);
        public bool IsPressed => ValueDependantOnPathway(Side, Game.InputState.TopHeld, Game.InputState.BottomHeld);

        /// <summary>
        /// The half of the screen the pathway resides on.
        /// </summary>
        public PathwaySide Side { get; private set; } = PathwaySide.None;

        private bool checkSide(PathwaySide side) {
            if (Side == PathwaySide.None || Side == PathwaySide.Both)
                throw new NotImplementedException("A pathway must have a very specific side attached to it.");

            return Side == side;
        }

        /// <summary>
        /// Is this the top pathway?
        /// </summary>
        public bool IsTopPathway => checkSide(PathwaySide.Top);
        /// <summary>
        /// Is this the bottom pathway?
        /// </summary>
        public bool IsBottomPathway => checkSide(PathwaySide.Bottom);

        /// <summary>
        /// The size of the pathway hit marker is changed by both every quarter note and when an input event occurs; this animation smoother is used for input events.
        /// </summary>
        public SecondOrderSystem InputAnimator { get; private set; } = new(0.4f, 0.5f, 1f, 1);

        public Pathway(DashGame game, PathwaySide side) : base(game) {
            Side = side;
            OnTick();
        }

        public static bool ComparePathwayType(PathwaySide a, PathwaySide b) {
            if (a == b)
                return true;
            else if (a == PathwaySide.Both || b == PathwaySide.Both)
                return true;

            return false;
        }

        public static T ValueDependantOnPathway<T>(PathwaySide input, T topResult, T bottomResult)  {
            return input == PathwaySide.Top ? topResult : bottomResult;
        }

        public static Color GetColor(PathwaySide side) => ValueDependantOnPathway(side, DashVars.TopPathwayColor, DashVars.BottomPathwayColor);
        public Color Color => GetColor(Side);

        public SecondOrderSystem Animator { get; private set; } = new(8.4f, 0.5f, 1f, 1);
        public Vector2F Position { get; private set; }
        public override void OnTick() {
            Position = new Vector2F(Game.ScreenManager.ScrWidth * DashVars.PATHWAY_XDISTANCE, (Game.ScreenManager.ScrHeight / 2) * ValueDependantOnPathway(Side, -DashVars.PATHWAY_YDISTANCE, DashVars.PATHWAY_YDISTANCE)); 
        }
        public override void OnDrawGameSpace() {
            var beatInfluence = 1 - Game.Conductor.NoteDivisorRealtime(4);
            var realInfluence = Animator.Update((IsClicked || IsPressed) ? 2 : beatInfluence);
            var size = Raymath.Remap(realInfluence, 0, 1, 25, 32);
            var curtimeOffset = (float)DashVars.Curtime * 120;

            float divisors = 3;
            float ring_offset = 60;

            var alpha = (int)Raymath.Remap(realInfluence, 0, 1, 79, 130);

            Graphics.SetDrawColor(ValueDependantOnPathway(Side, DashVars.TopPathwayColor, DashVars.BottomPathwayColor), alpha);
            Graphics.DrawRing(Position, (25 / 2) - 3, (25 / 2));

            var ringPartSize = 360f / divisors;
            for (float i = 0; i < 360f; i += ringPartSize) {
                Graphics.DrawRing(Position, size, size / 1.15f, curtimeOffset + i, curtimeOffset + i + (ringPartSize - ring_offset));
            }
        }
    }
}
