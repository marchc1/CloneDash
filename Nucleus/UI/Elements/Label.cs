using Nucleus.Core;
using Nucleus.Extensions;
using Nucleus.Types;

using System;
using System.Diagnostics;

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

		Vector2F textPadding;
		public Vector2F TextPadding {
			get => textPadding;
			set {
				textPadding = value;
				InvalidateText();
			}
		}

		public override void TextChanged(string oldText, string newText) {
			base.TextChanged(oldText, newText);
			if (__autosize)
				InvalidateLayout();
			InvalidateText();
		}

		struct TextRange
		{
			public string OriginalText;
			public int Start;
			public int End;
			public float Width;
			public float Height;

			public int Length => (End - Start);

			public override string ToString() {
				return $"'{OriginalText.AsSpan()[Start..End]}' (range: {Start} -> {End}, size: {Width}x{Height})";
			}
		}
		readonly List<TextRange> textRanges = [];
		Vector2F fullTextSize;
		float textHeightOffset;

		bool wordWrap;
		public bool WordWrap {
			get => wordWrap;
			set {
				if (wordWrap != value)
					InvalidateText();
				wordWrap = value;
			}
		}

		bool textInvalid = true;
		private void InvalidateText() {
			textInvalid = true;
		}
		private void ValidateText() {
			if (!textInvalid)
				return;

			textRanges.Clear();
			fullTextSize = default;

			if(wordWrap == false) {
				return;
			}

			int y = 0;

			int startPos = 0;
			int endPos = 0;


			ReadOnlySpan<char> text = Text;
			TextRange workingRange = new() { OriginalText = Text };
			Vector2F workingArea = RenderBounds.Size - TextPadding - new Vector2F(4, 4);

			int wordPos = 0;
			int spacePos;

			void pushWorkingRange() {
				if (textRanges.Count >= 1)
					textHeightOffset += textRanges[textRanges.Count - 1].Height;
				textRanges.Add(workingRange);

				fullTextSize.W = Math.Max(fullTextSize.W, workingRange.Width);
				fullTextSize.H += workingRange.Height;

				workingRange = default;
				workingRange.Start = wordPos;
				workingRange.End = wordPos;
				workingRange.OriginalText = Text;
			}

			while (wordPos < text.Length) {
				spacePos = text[wordPos..].IndexOf(' ');
				bool lastWord = spacePos == -1;
				if (lastWord)
					spacePos = text.Length - wordPos;

				ReadOnlySpan<char> word = text[wordPos..(wordPos + spacePos)];
				Vector2F wordSize = Graphics2D.GetTextSize(word, Font, TextSize);

				if (workingRange.Width > 0 && (workingRange.Width + wordSize.W) > workingArea.W)
					pushWorkingRange();

				workingRange.Width += wordSize.W;

				if (!lastWord)
					workingRange.Width += Graphics2D.GetTextSize(" ", Font, TextSize).W;

				workingRange.Height = Math.Max(wordSize.H, workingRange.Height);
				workingRange.End += word.Length + (lastWord ? 0 : 1);

				wordPos += spacePos + 1;
			}


			if (workingRange.Length > 0)
				pushWorkingRange();

			textInvalid = false;
		}

		protected override void Initialize() {
			base.Initialize();
		}
		protected override void PerformLayout(float width, float height) {
			base.PerformLayout(width, height);
			InvalidateText();
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

			Span<TextRange> ranges = textRanges.AsSpan();
			Vector2F startDrawingPosition = TextAlignment.GetPositionGivenAlignment(RenderBounds.Size, TextPadding);
			TextAlignment vertical = TextAlignment.ToTextAlignment().vertical;

			Graphics2D.SetDrawColor(textC);

			if (ranges.Length == 0) {
				Graphics2D.DrawText(startDrawingPosition, Text, Font, TextSize, TextAlignment);
				return;
			}

			if (ranges.Length > 1) {
				if (vertical == Types.TextAlignment.Center)
					startDrawingPosition.Y -= textHeightOffset / 2;
				else if (vertical == Types.TextAlignment.Bottom)
					startDrawingPosition.Y -= textHeightOffset;
			}

			
			foreach (var range in ranges) {
				Graphics2D.DrawText(startDrawingPosition, Text.AsSpan()[range.Start..range.End], Font, TextSize, TextAlignment);
				startDrawingPosition.Y += range.Height;
			}
		}
	}
}
