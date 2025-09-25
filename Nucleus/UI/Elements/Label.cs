using Nucleus.Core;
using Nucleus.Extensions;
using Nucleus.Types;
using System.Text.RegularExpressions;

namespace Nucleus.UI
{
	public class Label : Element
    {
		public bool DrawBackground { get; set; } = false;
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
        protected override void ModifyLayout(ref RectangleF renderBounds) {
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
        public override void Paint(float width, float height) {
			if(DrawBackground) {
				Graphics2D.SetDrawColor(BackgroundColor);
				Graphics2D.DrawRectangle(0, 0, width, height);
			}
			var textC = TextColor;
			if (!CanInput()) {
				textC = textC.Adjust(0, 0, -0.5f);
			}

            Vector2F textDrawingPosition = TextAlignment.GetPositionGivenAlignment(RenderBounds.Size, TextPadding);

			// Strawberry Godzilla from Muse Dash
			Regex boldRegex = new("^<b>(.+)<\\/b>$");
			Match boldRegexMatch = boldRegex.Match(Text);
			string displayText = boldRegexMatch.Success ? boldRegexMatch.Groups[1].Value : Text;
			string fontName = boldRegexMatch.Success ? Font + " Mono Bold" : Font;

            Graphics2D.SetDrawColor(textC);
            Graphics2D.DrawText(textDrawingPosition, displayText, fontName, TextSize, TextAlignment);
        }
    }
}
