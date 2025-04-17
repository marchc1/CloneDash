using CloneDash.Animation;
using Nucleus;
using Nucleus.Core;
using Nucleus.Engine;
using Nucleus.Types;
using Raylib_cs;

namespace CloneDash.Game
{
    public class Pathway : LogicalEntity
    {
		/// <summary>
		/// Top pathway will be placed at Y 0 + (H * PATHWAY_TOP_PERCENTAGE)
		/// </summary>
		public static float PATHWAY_TOP_PERCENTAGE => .346f;
		/// <summary>
		/// Top pathway will be placed at Y H - (H * PATHWAY_BOTTOM_PERCENTAGE)
		/// </summary>
		public static float PATHWAY_BOTTOM_PERCENTAGE => .3722f;
		/// <summary>
		/// Both pathway will be placed at X 0 + (H * PATHWAY_LEFT_PERCENTAGE)
		/// </summary>
		public static float PATHWAY_LEFT_PERCENTAGE => .3722f;
        public bool IsClicked => ValueDependantOnPathway(Side, Level.As<CD_GameLevel>().InputState.TopClicked > 0, Level.As<CD_GameLevel>().InputState.BottomClicked > 0);
        public bool IsPressed => ValueDependantOnPathway(Side, Level.As<CD_GameLevel>().InputState.TopHeld, Level.As<CD_GameLevel>().InputState.BottomHeld);

		public static float GetPathwayY(PathwaySide side) =>
			side == PathwaySide.Both 
				? (GetPathwayY(PathwaySide.Top) + GetPathwayY(PathwaySide.Bottom)) / 2
				: side == PathwaySide.Top
					? EngineCore.GetWindowHeight() * PATHWAY_TOP_PERCENTAGE
					: EngineCore.GetWindowHeight() - (EngineCore.GetWindowHeight() * PATHWAY_BOTTOM_PERCENTAGE);

		/// <summary>
		/// The half of the screen the pathway resides on.
		/// </summary>
		public PathwaySide Side { get; set; } = PathwaySide.None;

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

        public Pathway(PathwaySide side) : base() {
            Side = side;

        }

        public static bool ComparePathwayType(PathwaySide a, PathwaySide b) {
            if (a == b)
                return true;
            else if (a == PathwaySide.Both || b == PathwaySide.Both)
                return true;

            return false;
        }

        public static T ValueDependantOnPathway<T>(PathwaySide input, T topResult, T bottomResult) {
            return input == PathwaySide.Top ? topResult : bottomResult;
        }

        public static Color GetColor(PathwaySide side, int alpha = -1) {
            var c = ValueDependantOnPathway(side, DashVars.TopPathwayColor, DashVars.BottomPathwayColor);

            return new(c.R, c.G, c.B, alpha == -1 ? c.A : alpha);
        }
        public Color Color => GetColor(Side);

        public SecondOrderSystem Animator { get; private set; } = new(8.4f, 0.5f, 1f, 1);
        public Vector2F Position { get; private set; }
        public override void Think(FrameState frameState) {
            Position = new Vector2F(frameState.WindowHeight * PATHWAY_LEFT_PERCENTAGE, GetPathwayY(Side));
        }
        public override void PostRender(FrameState frameState) {

        }

		public void Render() {
			var beatInfluence = 1 - Level.As<CD_GameLevel>().Conductor.NoteDivisorRealtime(4);
			var realInfluence = Animator.Update((IsClicked || IsPressed) ? 2 : beatInfluence);
			var size = Raymath.Remap(realInfluence, 0, 1, 36, 42);
			var curtimeOffset = (float)Level.Curtime * 120;

			float divisors = 3;
			float ring_offset = 360 / divisors / 2;

			var alpha = (int)Raymath.Remap(realInfluence, 0, 1, 79, 130);

			Graphics2D.SetDrawColor(ValueDependantOnPathway(Side, DashVars.TopPathwayColor, DashVars.BottomPathwayColor), alpha);
			Graphics2D.DrawRing(Position, (32 / 2) - 4, (32 / 2));

			var ringPartSize = 360f / divisors;
			for (float i = 0; i < 360f; i += ringPartSize) {
				Graphics2D.DrawRing(Position, size, size / 1.15f, curtimeOffset + i, curtimeOffset + i + (ringPartSize - ring_offset));
			}
		}
    }
}
