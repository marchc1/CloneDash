using Nucleus.Types;

namespace Nucleus.UI
{
	/// <summary>
	/// FlexPanel's resizing mode. Todo: better explanations
	/// </summary>
	public enum FlexChildrenResizingMode
    {
        /// <summary>
        /// Does not perform a resizing operation on the children of the FlexPanel.
        /// </summary>
        DoNotResize,
        /// <summary>
        /// Fits the opposite direction (opposite to FlexDirection) to match the FlexPanel's size on that axis.
        /// </summary>
        FitToOppositeDirection,
        /// <summary>
        /// Stretches the opposite direction (opposite to FlexDirection)  to match the FlexPanel's size on the opposite axis.
        /// </summary>
        StretchToOppositeDirection,
        /// <summary>
        /// Fits all elements into the FlexPanel's size on the Direction axis / Children.Count.
        /// </summary>
        StretchToFit
    }
    public class FlexPanel : Panel
    {
        public Directional180 Direction { get; set; } = Directional180.Horizontal;
        public FlexChildrenResizingMode ChildrenResizingMode { get; set; } = FlexChildrenResizingMode.DoNotResize;
        protected override void Initialize() {
            base.Initialize();
        }
        public override T Add<T>(T? toAdd = null) where T : class {
            InvalidateChildren(self: true, recursive: true);
            return base.Add(toAdd);
        }
        public override void Paint(float width, float height) {

        }
        protected override void PostLayoutChildren() {
            var childrenCount = Children.Count;
            var ourBounds = RenderBounds;

            for (int i = 0; i < childrenCount; i++) {
                Element child = Children[i];

                var chT = 1.0f / (childrenCount + 1.0f);
                var chF0 = (1.0f / childrenCount) * (i + 0.0f);
                var chF1 = chT * (i + 1.0f);

                switch (ChildrenResizingMode) {
                    case FlexChildrenResizingMode.DoNotResize:
                    case FlexChildrenResizingMode.FitToOppositeDirection:
                    case FlexChildrenResizingMode.StretchToOppositeDirection:
                        if (Direction == Directional180.Horizontal)
                            child.Position = new(chF1 * ourBounds.Width, ourBounds.Height / 2);
                        else
                            child.Position = new(ourBounds.Width / 2, chF1 * ourBounds.Height);

                        child.Dock = Dock.None;
                        child.Origin = Anchor.Center;
                        break;
                    case FlexChildrenResizingMode.StretchToFit:
                        if (Direction == Directional180.Horizontal)
                            child.Position = new(chF0 * ourBounds.Width, 0);
                        else
                            child.Position = new(0, chF0 * ourBounds.Height);

                        child.Dock = Dock.None;
                        child.Origin = Anchor.TopLeft;
                        break;
                }

                switch (ChildrenResizingMode) {
                    case FlexChildrenResizingMode.DoNotResize:
                        break;
                    case FlexChildrenResizingMode.FitToOppositeDirection:
                        if (Direction == Directional180.Horizontal)
                            child.Size = new(child.RenderBounds.Width, ourBounds.Height);
                        else
                            child.Size = new(ourBounds.Width, child.RenderBounds.Height);
                        break;
                    case FlexChildrenResizingMode.StretchToOppositeDirection:
                        if (Direction == Directional180.Horizontal)
                            child.Size = new(ourBounds.Height, ourBounds.Height);
                        else
                            child.Size = new(ourBounds.Width, ourBounds.Width);
                        break;
                    case FlexChildrenResizingMode.StretchToFit:
                        if (Direction == Directional180.Horizontal)
                            child.Size = new(ourBounds.Width / childrenCount, ourBounds.Height);
                        else
                            child.Size = new(ourBounds.Width, ourBounds.Height / childrenCount);

                        break;
                }

                if (!DockPadding.IsZero) {
                    child.Position += new Vector2F(DockPadding.X, DockPadding.Y);
                    child.Size -= new Vector2F(DockPadding.X, DockPadding.Y);
                    child.Size -= new Vector2F(DockPadding.W, DockPadding.H);
                }
            }
        }
    }
}
