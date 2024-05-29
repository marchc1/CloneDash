using Nucleus.Core;
using Nucleus.Types;

namespace Nucleus.UI
{
    public class Label : Element
    {
        public Anchor TextAlignment { get; set; } = Anchor.Center;
        private bool __autosize = false;
        public bool AutoSize {
            get { return __autosize; }
            set {
                __autosize = value;
                InvalidateLayout();
            }
        }
        public Vector2F TextPadding { get; set; } = new(0);

        protected override void Initialize() {
            base.Initialize();
        }
        protected override void ModifyRenderBounds(ref RectangleF renderBounds) {
            if (!AutoSize)
                return;

            Vector2F textSize = Graphics2D.GetTextSize(Text, Font, TextSize);

            renderBounds.W = textSize.X + TextPadding.X;
            renderBounds.H = textSize.Y + TextPadding.Y;

            if (!DockMargin.IsZero) {
                renderBounds.W += (DockMargin.X + DockMargin.W) * 2;
                renderBounds.H += (DockMargin.Y + DockMargin.H) * 2;
            }
            if (!Parent.DockPadding.IsZero) {
                renderBounds.W += (Parent.DockPadding.X + Parent.DockPadding.W) * 2;
                renderBounds.H += (Parent.DockPadding.Y + Parent.DockPadding.H) * 2;
            }
        }
        protected override void Paint(float width, float height) {
            Vector2F textDrawingPosition = Anchor.CalculatePosition(new Vector2F(0, 0), new Vector2F(width, height), TextAlignment);
            var textDrawingAlignment = TextAlignment.ToTextAlignment();

            Graphics2D.SetDrawColor(TextColor);
            Graphics2D.DrawText(textDrawingPosition, Text, Font, TextSize, textDrawingAlignment.horizontal, textDrawingAlignment.vertical);
        }
    }
}
