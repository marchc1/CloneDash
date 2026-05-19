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
	ReadOnlySpan<char> GetText() => "";
	ReadOnlySpan<char> GetFont();
	float GetTextSize();

	void SetText(ReadOnlySpan<char> text) { }
	void SetFont(ReadOnlySpan<char> font);
	void SetTextSize(float textSize);
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
	SchemeableSetting<float> TextSize = SchemeableSetting<float>.Default(18);
	SchemeableSetting<string> Font = SchemeableSetting<string>.Default(Graphics2D.UI_FONT_NAME);
	private bool __autosize = false;
	private Anchor textAlignment = Anchor.Center;
	readonly List<TextRange> textRanges = [];
	Vector2F fullTextSize;
	TextOverflowMode textOverflowMode;
	bool textInvalid = true;
	SchemeableSetting<Color> textColor = SchemeableSetting<Color>.Default(DefaultTextColor);

	public Label(Element? parent, ReadOnlySpan<char> text = "Label", ReadOnlySpan<char> name = default) : base(parent, name) {
		SetText(text);
		SetPaintBackgroundEnabled(false);
		SetPaintBorderEnabled(false);
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

	public virtual ReadOnlySpan<char> GetText() => text;
	public virtual void SetText(ReadOnlySpan<char> text) {
		if (GetText().Equals(text, StringComparison.InvariantCulture))
			return;

		this.text = new(text);
		TextChanged(text);
	}

	protected virtual void TextChanged(ReadOnlySpan<char> text) {
		if (__autosize)
			InvalidateLayout();
		InvalidateText();
	}

	public ReadOnlySpan<char> GetFont() => Font.Get();
	public void SetFont(ReadOnlySpan<char> font) {
		Font.SetUserValue(new(font));
		InvalidateLayout();
		InvalidateText();
	}

	public float GetTextSize() => TextSize.Get();
	public void SetTextSize(float textSize) {
		TextSize.SetUserValue(textSize);
		InvalidateLayout();
		InvalidateText();
	}

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

		ReadOnlySpan<char> text = GetText();
		ReadOnlySpan<char> font = GetFont();
		float textSize = GetTextSize();
		//TextRange workingRange = new() { OriginalText = GetText() };
		TextRange workingRange = new() { };
		Vector2F workingArea = RenderBounds.Size - GetTextPadding() - new Vector2F(4, 4);

		if (textOverflowMode.IsTruncate())
			workingArea.W -= Graphics2D.GetTextSize("...", font, textSize).X;

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
				workingRange.TruncateText = $"{GetText()[workingRange.Start..workingRange.End]}...";
				workingRange.Width += Graphics2D.GetTextSize("...", GetFont(), textSize).W;
			}

			textRanges.Add(workingRange);

			workingRange = new TextRange {
				// OriginalText = GetText(),
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
				Vector2F wordSize = Graphics2D.GetTextSize(word, font, textSize);

				if (workingRange.Width > 0 && (workingRange.Width + wordSize.W) > workingArea.W)
					if (!pushWorkingRange())
						break;

				workingRange.Width += wordSize.W;
				if (!lastWord)
					workingRange.Width += Graphics2D.GetTextSize(" ", font, textSize).W;

				workingRange.Height = Math.Max(wordSize.H, workingRange.Height);
				workingRange.End += word.Length + 1;

				wordPos += spacePos + 1;
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
		if (!GetAutoSize())
			return;

		Element? parent = GetParent();
		if (parent == null)
			return;

		Vector2F textSize;
		ValidateText();

		var parentBounds = parent!.RenderBounds;
		Span<TextRange> ranges = textRanges.AsSpan();
		Vector2F startDrawingPosition = GetTextAlignment().GetPositionGivenAlignment(RectangleF.FromPosAndSize(new(0), new(parentBounds.W, parentBounds.H)), GetTextPadding());
		TextAlignment vertical = GetTextAlignment().ToTextAlignment().Vertical;

		ReadOnlySpan<char> font = GetFont();
		float curTextSize = GetTextSize();

		if (ranges.Length <= 0) {
			textSize = Graphics2D.GetTextSize(GetText(), font, curTextSize);
		}
		else {
			textSize = new(0, 0);

			if (vertical == Types.TextAlignment.Center)
				startDrawingPosition.Y -= (fullTextSize.H - ranges[0].Height) / 2;
			else if (vertical == Types.TextAlignment.Bottom)
				startDrawingPosition.Y -= fullTextSize.H - ranges[0].Height;

			foreach (var range in ranges) {
				ReadOnlySpan<char> subtext = range.Truncate ? range.TruncateText : GetText()[range.Start..range.End];
				var rangeSize = Graphics2D.GetTextSize(subtext, font, curTextSize);
				textSize = new(Math.Max(textSize.X, rangeSize.X), textSize.Y + rangeSize.Y);
				if (range.Truncate)
					break;
				startDrawingPosition.Y += range.Height;
			}
		}
		Size = new(textSize.X + GetTextPadding().X, textSize.Y + GetTextPadding().Y);

		if (!DockMargin.IsZero)
			Size = Size + new Vector2F((DockMargin.X + DockMargin.W) * 2, (DockMargin.Y + DockMargin.H) * 2);
		if (!parent.DockPadding.IsZero)
			Size = Size + new Vector2F((parent.DockPadding.X + parent.DockPadding.W) * 2, (parent.DockPadding.Y + parent.DockPadding.H) * 2);
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
		Vector2F startDrawingPosition = GetTextAlignment().GetPositionGivenAlignment(RectangleF.FromPosAndSize(new(0), new(width, height)), GetTextPadding());
		TextAlignment vertical = GetTextAlignment().ToTextAlignment().Vertical;

		Graphics2D.SetDrawColor(textC);

		ReadOnlySpan<char> text = GetText();
		ReadOnlySpan<char> font = GetFont();
		float textSize = GetTextSize();

		if (ranges.Length == 0) {
			Graphics2D.DrawText(startDrawingPosition, text, font, textSize, GetTextAlignment());
			return;
		}

		if (ranges.Length > 1) {
			if (vertical == Types.TextAlignment.Center)
				startDrawingPosition.Y -= (fullTextSize.H - ranges[0].Height) / 2;
			else if (vertical == Types.TextAlignment.Bottom)
				startDrawingPosition.Y -= fullTextSize.H - ranges[0].Height;
		}

		foreach (var range in ranges) {
			ReadOnlySpan<char> subtext = range.Truncate ? range.TruncateText : GetText()[range.Start..range.End];
			Graphics2D.DrawText(startDrawingPosition, subtext, font, textSize, GetTextAlignment());
			if (range.Truncate)
				break;
			startDrawingPosition.Y += range.Height;
		}
	}

	public override void ApplySchemeSettings(IScheme scheme) {
		base.ApplySchemeSettings(scheme);

		SetTextColor(scheme.GetColor("Nucleus.Text"));
		var fontStyle = scheme.GetFontStyle("Nucleus.Default");
		Font.SetSchemeValue(fontStyle.Name);
		TextSize.SetSchemeValue(fontStyle.Tall);
	}
}
