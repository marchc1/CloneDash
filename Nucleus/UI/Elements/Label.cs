using Nucleus.Core;
using Nucleus.Extensions;
using Nucleus.Types;

using System;
using System.Diagnostics;

namespace Nucleus.UI;

/// <summary>
/// Word wrap mode
/// </summary>
public enum TextOverflowMode
{
	/// <summary> Do nothing. </summary>
	None,
	/// <summary> When a word would overflow the bounds of the right side of the element, start the remainder of the text on a new line, until no text remains.</summary>
	WordWrap,
	/// <summary> When a character would overflow the bounds of the right side of the element, start the remainder of the text on a new line, until no text remains.</summary>
	CharWrap,
	/// <summary> When a word would overflow the bounds of the right side of the element, truncate the end of the string with a '...' </summary>
	WordTruncate,
	/// <summary> When a character would overflow the bounds of the right side of the element, truncate the end of the string with a '...' </summary>
	CharTruncate,
}

public static class TextOverflowModeTools
{
	public static bool IsWrap(this TextOverflowMode textOverflowMode) => textOverflowMode switch {
		TextOverflowMode.WordWrap or TextOverflowMode.CharWrap => true,
		_ => false
	};
	public static bool IsTruncate(this TextOverflowMode textOverflowMode) => textOverflowMode switch {
		TextOverflowMode.WordTruncate or TextOverflowMode.CharTruncate => true,
		_ => false
	};
	public static bool TargetsWord(this TextOverflowMode textOverflowMode) => textOverflowMode switch {
		TextOverflowMode.WordWrap or TextOverflowMode.WordTruncate => true,
		_ => false
	};
	public static bool TargetsCharacter(this TextOverflowMode textOverflowMode) => textOverflowMode switch {
		TextOverflowMode.CharTruncate or TextOverflowMode.CharWrap => true,
		_ => false
	};
}


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
		public bool Truncate;
		public string TruncateText;

		public int Length => (End - Start);

		public override string ToString() {
			return $"'{OriginalText.AsSpan()[Start..End]}' (range: {Start} -> {End}, size: {Width}x{Height})";
		}
	}
	readonly List<TextRange> textRanges = [];
	Vector2F fullTextSize;

	TextOverflowMode textOverflowMode;
	public TextOverflowMode TextOverflowMode {
		get => textOverflowMode;
		set {
			if (textOverflowMode != value)
				InvalidateText();
			textOverflowMode = value;
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

		if (textOverflowMode == TextOverflowMode.None)
			return;

		ReadOnlySpan<char> text = Text;
		TextRange workingRange = new() { OriginalText = Text };
		Vector2F workingArea = RenderBounds.Size - TextPadding - new Vector2F(4, 4);

		if (textOverflowMode.IsTruncate())
			workingArea.W -= Graphics2D.GetTextSize("...", Font, TextSize).X;

		int wordPos = 0;

		bool pushWorkingRange(bool notForced = false) {
			if (workingRange.Length > 0)
				workingRange.End -= textOverflowMode.TargetsWord() ? 1 : 0;

			fullTextSize.W = Math.Max(fullTextSize.W, workingRange.Width);
			fullTextSize.H += workingRange.Height;

			bool truncating = (textOverflowMode.IsTruncate() ||
							 (textOverflowMode.IsWrap() && fullTextSize.H > workingArea.H)) && !notForced;

			workingRange.Truncate = truncating;
			if (truncating) {
				workingRange.TruncateText = $"{Text.AsSpan()[workingRange.Start..workingRange.End]}...";
				workingRange.Width += Graphics2D.GetTextSize("...", Font, TextSize).W;
			}

			textRanges.Add(workingRange);

			workingRange = new TextRange {
				OriginalText = Text,
				Start = wordPos,
				End = wordPos
			};

			return !truncating;
		}

		while (wordPos < text.Length) {
			if (textOverflowMode.TargetsWord()) {
				int spacePos = text[wordPos..].IndexOf(' ');
				bool lastWord = spacePos == -1;
				if (lastWord)
					spacePos = text.Length - wordPos;

				ReadOnlySpan<char> word = text[wordPos..(wordPos + spacePos)];
				Vector2F wordSize = Graphics2D.GetTextSize(word, Font, TextSize);

				if (workingRange.Width > 0 && (workingRange.Width + wordSize.W) > workingArea.W)
					if (!pushWorkingRange())
						break;

				workingRange.Width += wordSize.W;
				if (!lastWord)
					workingRange.Width += Graphics2D.GetTextSize(" ", Font, TextSize).W;

				workingRange.Height = Math.Max(wordSize.H, workingRange.Height);
				workingRange.End += word.Length + 1;

				wordPos += spacePos + 1;
			}
			else {
				char c = text[wordPos];
				Vector2F charSize = Graphics2D.GetTextSize(text.Slice(wordPos, 1), Font, TextSize);

				if (workingRange.Width > 0 && (workingRange.Width + charSize.W) > workingArea.W)
					if (!pushWorkingRange())
						break;

				workingRange.Width += charSize.W;
				workingRange.Height = Math.Max(charSize.H, workingRange.Height);
				workingRange.End++;
				wordPos++;
			}
		}

		if (workingRange.Length > 0)
			pushWorkingRange(true);

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
		Vector2F startDrawingPosition = TextAlignment.GetPositionGivenAlignment(RectangleF.FromPosAndSize(new(0), new(width, height)), TextPadding);
		TextAlignment vertical = TextAlignment.ToTextAlignment().vertical;

		Graphics2D.SetDrawColor(textC);

		if (ranges.Length == 0) {
			Graphics2D.DrawText(startDrawingPosition, Text, Font, TextSize, TextAlignment);
			return;
		}

		if (ranges.Length > 1) {
			if (vertical == Types.TextAlignment.Center)
				startDrawingPosition.Y -= (fullTextSize.H - ranges[0].Height) / 2;
			else if (vertical == Types.TextAlignment.Bottom)
				startDrawingPosition.Y -= fullTextSize.H - ranges[0].Height;
		}

		foreach (var range in ranges) {
			ReadOnlySpan<char> subtext = range.Truncate ? range.TruncateText : Text.AsSpan()[range.Start..range.End];
			Graphics2D.DrawText(startDrawingPosition, subtext, Font, TextSize, TextAlignment);
			if (range.Truncate)
				break;
			startDrawingPosition.Y += range.Height;
		}
	}
}
