using Nucleus.Core;
using Nucleus.Extensions;
using Nucleus.Types;

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

		public override void TextChanged(string oldText, string newText) {
			base.TextChanged(oldText, newText);
			if (__autosize)
				InvalidateLayout();
			InvalidateText();
		}

		struct TextRange
		{
			public int Start;
			public int End;
			public float Width;
			public float Height;
		}
		readonly List<TextRange> textRanges = [];

		bool textInvalid = true;
		private void InvalidateText() {
			textInvalid = true;
		}
		private void ValidateText() {
			if (!textInvalid)
				return;

			textRanges.Clear();
			int x = 0;

			int startPos = 0;
			int endPos = 0;

			ReadOnlySpan<char> text = Text;
			TextRange workingRange = default;
			Vector2F workingArea = RenderBounds.Size - TextPadding;
			int wordPos = 0;
			int spacePos;
			while (wordPos < text.Length && (spacePos = text[wordPos..].IndexOf(' ')) != -1) {
				// Get one word
				ReadOnlySpan<char> word = text[wordPos..spacePos];
				Vector2F wordSize = Graphics2D.GetTextSize(word, Font, TextSize);
			}

			textInvalid = false;
		}

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
			ValidateText();

			if (DrawBackground) {
				Graphics2D.SetDrawColor(BackgroundColor);
				Graphics2D.DrawRectangle(0, 0, width, height);
			}
			var textC = TextColor;
			if (!CanInput()) {
				textC = textC.Adjust(0, 0, -0.5f);
			}

			Vector2F textDrawingPosition = TextAlignment.GetPositionGivenAlignment(RenderBounds.Size, TextPadding);
			Graphics2D.SetDrawColor(textC);
			Graphics2D.DrawText(textDrawingPosition, Text, Font, TextSize, TextAlignment);
		}
	}
}
