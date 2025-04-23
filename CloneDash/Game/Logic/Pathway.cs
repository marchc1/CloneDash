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

		public static readonly Color PATHWAY_TOP_COLOR = new Color(178, 255, 252, 120);
		public static readonly Color PATHWAY_BOTTOM_COLOR = new Color(248, 178, 255, 120);
		public static readonly Color PATHWAY_DUAL_COLOR = new Color(220, 160, 140, 255);

		/// <summary>
		/// Top pathway will be placed at Y coordinate (winH * PATHWAY_TOP_PERCENTAGE)
		/// </summary>
		public static float PATHWAY_TOP_PERCENTAGE => -.25f;
		/// <summary>
		/// Bottom pathway will be placed at Y coordinate(winH * PATHWAY_BOTTOM_PERCENTAGE)
		/// </summary>
		public static float PATHWAY_BOTTOM_PERCENTAGE => .25f;
		/// <summary>
		/// Both pathways will be placed at X coordinate (winH * PATHWAY_LEFT_PERCENTAGE)
		/// </summary>
		public static float PATHWAY_LEFT_PERCENTAGE => -0.98f;

		public static float GetPathwayLeft() => EngineCore.GetWindowHeight() * PATHWAY_LEFT_PERCENTAGE;
		public static float GetPathwayTop() => EngineCore.GetWindowHeight() * PATHWAY_TOP_PERCENTAGE;
		public static float GetPathwayBottom() => EngineCore.GetWindowHeight() * PATHWAY_BOTTOM_PERCENTAGE;
		public static float GetPathwayMiddle() {
			var height = EngineCore.GetWindowHeight();
			return ((height * PATHWAY_TOP_PERCENTAGE) + (height * PATHWAY_BOTTOM_PERCENTAGE)) / 2;
		}

        public bool IsClicked => ValueDependantOnPathway(Side, Level.As<CD_GameLevel>().InputState.TopClicked > 0, Level.As<CD_GameLevel>().InputState.BottomClicked > 0);
        public bool IsPressed => ValueDependantOnPathway(Side, Level.As<CD_GameLevel>().InputState.TopHeld, Level.As<CD_GameLevel>().InputState.BottomHeld);

		public static float GetPathwayY(PathwaySide side) => side switch {
			PathwaySide.Both => GetPathwayMiddle(),
			PathwaySide.Top => GetPathwayTop(),
			PathwaySide.Bottom => GetPathwayBottom(),
			_ => GetPathwayBottom()
		};

		/// <summary>
		/// The half of the screen the pathway resides on.
		/// </summary>
		public PathwaySide Side { get; set; } = PathwaySide.None;

        private bool checkSide(PathwaySide side) {
            if (Side == PathwaySide.None || Side == PathwaySide.Both)
                throw new NotImplementedException("A pathway must be attached to either the top or bottom side of the screen.");

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
            var c = ValueDependantOnPathway(side, Game.Pathway.PATHWAY_TOP_COLOR, Game.Pathway.PATHWAY_BOTTOM_COLOR);

            return new(c.R, c.G, c.B, alpha == -1 ? c.A : alpha);
        }
        public Color Color => GetColor(Side);

        public SecondOrderSystem Animator { get; private set; } = new(8.4f, 0.5f, 1f, 1);
        public Vector2F Position { get; private set; }
        public override void Think(FrameState frameState) {
            Position = new Vector2F(GetPathwayLeft(), GetPathwayY(Side));
        }
        public override void PostRender(FrameState frameState) {

        }

		public void Render() {
			var beatInfluence = 1 - Level.As<CD_GameLevel>().Conductor.NoteDivisorRealtime(4);
			var realInfluence = Animator.Update((IsClicked || IsPressed) ? 2 : beatInfluence);
			var size = Raymath.Remap(realInfluence, 0, 1, 36, 42) * 2;
			var curtimeOffset = (float)Level.Curtime * 120;

			float divisors = 3;
			float ring_offset = 360 / divisors / 2;

			var alpha = (int)Raymath.Remap(realInfluence, 0, 1, 79, 130);

			Graphics2D.SetDrawColor(ValueDependantOnPathway(Side, Game.Pathway.PATHWAY_TOP_COLOR, Game.Pathway.PATHWAY_BOTTOM_COLOR), alpha);
			Graphics2D.DrawRing(Position, ((32 / 2) - 4) * 2, ((32 / 2)) * 2);

			var ringPartSize = 360f / divisors;
			for (float i = 0; i < 360f; i += ringPartSize) {
				Graphics2D.DrawRing(Position, size, size / 1.15f, curtimeOffset + i, curtimeOffset + i + (ringPartSize - ring_offset));
			}
		}
    }
}