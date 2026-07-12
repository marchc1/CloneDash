using Nucleus.Common.Types;
using Nucleus.Common.UI;
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

public interface ITextElement
{
	ReadOnlySpan<char> Text {
		get => "";
		set { }
	}

	ReadOnlySpan<char> Font { get; set; }
	float TextSize { get; set; }
}

public class Label : Element, ITextElement
{
	struct TextRange
	{
		//public string OriginalText;
		public int Start;
		public int End;
		public float Width;
		public float Height;
		public bool Truncate;
		public string TruncateText;

		public int Length => (End - Start);

		//public override string ToString() {
		//	return $"'{OriginalText.AsSpan()[Start..End]}' (range: {Start} -> {End}, size: {Width}x{Height})";
		//}
	}

	/// <summary>
	/// direct access to this should be avoided...
	/// but a lot of things use strings right now in element code...
	/// </summary>
	protected string text = "";
	Vector2F textPadding;
	SchemeableSetting<float> __TextSize = SchemeableSetting<float>.Default(18);
	SchemeableSetting<string> __Font = SchemeableSetting<string>.Default(Graphics2D.UI_FONT_NAME);
	private bool __autosize = false;
	private Anchor textAlignment = Anchor.Center;
	readonly List<TextRange> textRanges = [];
	Vector2F fullTextSize;
	TextOverflowMode textOverflowMode;
	bool textInvalid = true;
	SchemeableSetting<Color> textColor = SchemeableSetting<Color>.Default(DefaultTextColor);

	public Label(Element? parent, ReadOnlySpan<char> text = "Label", ReadOnlySpan<char> name = default) : base(parent, name) {
		Text = text;
		SetPaintBackgroundEnabled(false);
		SetPaintBorderEnabled(false);
	}

	protected override void PostRenderBoundsFlush(ref RectangleF bounds) {
		if (__autosize) {
			var size = GetContentSize();
			bounds.Width = size.W;
			bounds.Height = size.H;
		}
	}

	public Vector2F GetContentSize() {
		if (Font.IsEmpty || TextSize <= 1)
			return Vector2F.Zero;

		ValidateText();

		ReadOnlySpan<char> font = Font;
		float curTextSize = TextSize;
		Span<TextRange> ranges = textRanges.AsSpan();

		Vector2F size;
		if (ranges.Length <= 0) {
			size = Graphics2D.GetTextSize(Text, font, curTextSize);
		}
		else {
			size = default;
			foreach (var range in ranges) {
				ReadOnlySpan<char> subtext = range.Truncate ? range.TruncateText : Text[range.Start..range.End];
				var rangeSize = Graphics2D.GetTextSize(subtext, font, curTextSize);
				size = new(Math.Max(size.X, rangeSize.X), size.Y + rangeSize.Y);
				if (range.Truncate)
					break;
			}
		}

		Vector2F finalSize = size + (GetTextPadding());
		// We have to expand ourselves by dock margins!
		// This is weird, but its the only way to make docking happy
		RectangleF margin = DockMargin;
		finalSize.X += margin.Left + margin.Right;
		finalSize.Y += margin.Top + margin.Bottom;

		return finalSize;
	}


	public Color GetTextColor() => textColor.Get();
	public void SetTextColor(Color value) => textColor.SetUserValue(value);

	public Anchor GetTextAlignment() => textAlignment;
	public void SetTextAlignment(Anchor value) {
		textAlignment = value;
		InvalidateLayout();
	}

	public bool GetAutoSize() => __autosize;
	public void SetAutoSize(bool value) {
		if (__autosize == value) return;
		__autosize = value;
		InvalidateLayout();
	}

	public Vector2F GetTextPadding() => textPadding;
	public void SetTextPadding(Vector2F value) {
		textPadding = value;
		InvalidateText();
	}

	public virtual ReadOnlySpan<char> Text {
		get => text;
		set {
			if (Text.Equals(value, StringComparison.InvariantCulture))
				return;

			this.text = new(value);
			TextChanged(value);
		}
	}

	protected virtual void TextChanged(ReadOnlySpan<char> text) {
		if (__autosize)
			InvalidateLayout();
		InvalidateText();
	}

	public ReadOnlySpan<char> Font {
		get => __Font.Get();
		set {
			__Font.SetUserValue(new(value));
			InvalidateLayout();
			InvalidateText();
		}
	}

	public float TextSize {
		get => __TextSize.Get();
		set {
			__TextSize.SetUserValue(value);
			InvalidateLayout();
			InvalidateText();
		}
	}

	public float GetRenderTextSize() => __TextSize.Get() * GetReferenceSize(DynamicTextSizeReference);

	public TextOverflowMode TextOverflowMode {
		get => textOverflowMode;
		set {
			if (textOverflowMode != value)
				InvalidateText();
			textOverflowMode = value;
		}
	}

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
		ReadOnlySpan<char> font = Font;
		float textSize = GetRenderTextSize();
		TextRange workingRange = new() { };
		Vector2F workingArea = __autosize ? new Vector2F(EngineCore.GetWindowWidth(), EngineCore.GetWindowHeight()) : Size - GetTextPadding() - new Vector2F(4, 4);

		if (textOverflowMode.IsTruncate())
			workingArea.W -= Graphics2D.GetTextSize("...", font, textSize).X;

		float lineHeight = Graphics2D.GetTextSize(" ", font, textSize).H;

		int wordPos = 0;

		bool pushWorkingRange(bool notForced = false) {
			if (workingRange.Length > 0)
				workingRange.End -= textOverflowMode.TargetsWord() ? 1 : 0;

			if (workingRange.Height <= 0)
				workingRange.Height = lineHeight;

			fullTextSize.W = Math.Max(fullTextSize.W, workingRange.Width);
			fullTextSize.H += workingRange.Height;

			bool truncating = (textOverflowMode.IsTruncate() ||
							 (textOverflowMode.IsWrap() && fullTextSize.H > workingArea.H)) && !notForced;

			workingRange.Truncate = truncating;
			if (truncating) {
				workingRange.TruncateText = $"{Text[workingRange.Start..workingRange.End]}...";
				workingRange.Width += Graphics2D.GetTextSize("...", Font, textSize).W;
			}

			textRanges.Add(workingRange);

			workingRange = new TextRange {
				Start = wordPos,
				End = wordPos
			};

			return !truncating;
		}

		while (wordPos < text.Length) {
			if (text[wordPos] == '\n') {
				wordPos++;
				workingRange.End = wordPos;
				if (!pushWorkingRange(true))
					break;
				continue;
			}
			if (text[wordPos] == '\r') {
				wordPos++;
				workingRange.End = wordPos;
				continue;
			}

			if (textOverflowMode.TargetsWord()) {
				int remaining = text.Length - wordPos;
				ReadOnlySpan<char> slice = text[wordPos..];
				int spacePos = -1;
				for (int i = 0; i < slice.Length; i++) {
					if (slice[i] == ' ' || slice[i] == '\n' || slice[i] == '\r') {
						spacePos = i;
						break;
					}
				}
				bool lastWord = spacePos == -1;
				if (lastWord)
					spacePos = remaining;

				bool brokeOnNewline = !lastWord && (slice[spacePos] == '\n' || slice[spacePos] == '\r');

				ReadOnlySpan<char> word = text[wordPos..(wordPos + spacePos)];
				Vector2F wordSize = Graphics2D.GetTextSize(word, font, textSize);

				if (workingRange.Width > 0 && (workingRange.Width + wordSize.W) > workingArea.W)
					if (!pushWorkingRange())
						break;

				workingRange.Width += wordSize.W;
				if (!lastWord && !brokeOnNewline)
					workingRange.Width += Graphics2D.GetTextSize(" ", font, textSize).W;

				workingRange.Height = Math.Max(wordSize.H, workingRange.Height);
				workingRange.End += word.Length + (brokeOnNewline ? 0 : 1);

				wordPos += spacePos + (brokeOnNewline ? 0 : 1);
			}
			else {
				char c = text[wordPos];
				Vector2F charSize = Graphics2D.GetTextSize(text.Slice(wordPos, 1), font, textSize);

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

	protected override void PerformLayout(float width, float height) {

	}

	public override void PaintBackground(float width, float height) {
		Graphics2D.SetDrawColor(GetBgColor());
		Graphics2D.DrawRectangle(0, 0, width, height);
	}
	public override void Paint(float width, float height) {
		ValidateText();

		var textC = GetTextColor();
		if (!IsMouseInputEnabled()) {
			textC = textC.Adjust(0, 0, -0.5f);
		}

		Span<TextRange> ranges = textRanges.AsSpan();
		Vector2F startDrawingPosition = GetTextAlignment().GetPositionGivenAlignment(RectangleF.FromPosAndSize(new(0), new(width, height)), __autosize ? GetTextPadding() / 2 : GetTextPadding());
		TextAlignment vertical = GetTextAlignment().ToTextAlignment().Vertical;
		TextAlignment horizontal = GetTextAlignment().ToTextAlignment().Horizontal;

		Graphics2D.SetDrawColor(textC);

		ReadOnlySpan<char> text = Text;
		ReadOnlySpan<char> font = Font;
		float textSize = GetRenderTextSize();

		if (ranges.Length == 0) {
			Vector2F drawPos = startDrawingPosition;
			Graphics2D.DrawText(drawPos, text, font, textSize, GetTextAlignment());
			return;
		}

		if (ranges.Length > 1) {
			if (vertical == Types.TextAlignment.Center)
				startDrawingPosition.Y -= (fullTextSize.H - ranges[0].Height) / 2;
			else if (vertical == Types.TextAlignment.Bottom)
				startDrawingPosition.Y -= fullTextSize.H - ranges[0].Height;
		}

		foreach (var range in ranges) {
			ReadOnlySpan<char> subtext = range.Truncate ? range.TruncateText : Text[range.Start..range.End];
			Vector2F drawPos = startDrawingPosition;
			switch (horizontal) {
				case TextAlignment.Center: drawPos.X = width / 2; break;
				case TextAlignment.Right: drawPos.X = width; break;
			}
			Graphics2D.DrawText(drawPos, subtext, font, textSize, GetTextAlignment());
			if (range.Truncate)
				break;
			startDrawingPosition.Y += range.Height;
		}
	}

	public override void ApplySchemeSettings(IScheme scheme) {
		base.ApplySchemeSettings(scheme);

		textColor.SetSchemeValue(scheme.GetColor("Nucleus.Text"));
		var fontStyle = scheme.GetFontStyle("Nucleus.Default");
		__Font.SetSchemeValue(fontStyle.Name);
		__TextSize.SetSchemeValue(fontStyle.Tall);
	}
}
