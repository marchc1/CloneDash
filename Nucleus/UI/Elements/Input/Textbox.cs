using Nucleus.Common.Input;
using Nucleus.Common.Types;
using Nucleus.Common.UI;
using Nucleus.Core;
using Nucleus.Extensions;
using Nucleus.Input;
using Nucleus.Types;
using Raylib_cs;

namespace Nucleus.UI;

/// <summary>
/// The primary caret of a <see cref="Textbox"/>, as character offsets into its text
/// </summary>
public class Caret
{
	readonly TextEditingCore core;
	internal Caret(TextEditingCore core) {
		this.core = core;
	}

	private int Offset(TextPosition position) => core.Document.GetOffset(position);
	private TextPosition Pos(int offset) => core.Document.GetPosition(offset);

	public int Position {
		get => Offset(core.Primary.Caret);
		set {
			var selection = core.Primary;
			bool hadSelection = !selection.IsEmpty;
			selection.Caret = Pos(value);
			if (!hadSelection)
				selection.Anchor = selection.Caret;
			core.SetPrimary(selection);
		}
	}

	/// <summary>
	/// Where the selection started, null when nothing is selected
	/// </summary>
	public int? SelectionOrigin {
		get => core.Primary.IsEmpty ? null : Offset(core.Primary.Anchor);
		set {
			var selection = core.Primary;
			selection.Anchor = value.HasValue ? Pos(value.Value) : selection.Caret;
			core.SetPrimary(selection);
		}
	}

	public int SelectionStart => Offset(core.Primary.Start);
	public int SelectionEnd => Offset(core.Primary.End);
	public int SelectionLength => SelectionEnd - SelectionStart;
	public bool HasSelection => !core.Primary.IsEmpty;

	public void MovePosition(string text, int delta) => Position = Math.Clamp(Position + delta, 0, text.Length);
	public void ClearSelection() => core.SetPrimary(new(core.Primary.Caret));
	public void SelectAll() => core.SelectAll();
}

internal struct TextLine
{
	public int Start;
	public int Length;
	public float Width;
	public float Height;
	public float Y;
	public int PrefixStart;

	public readonly int End => Start + Length;
	public override string ToString() => $"Line(start:{Start}, len:{Length}, w:{Width:F1}, h:{Height:F1}, y:{Y:F1})";
}

public class Textbox : Label, ITextLayout
{
	string HelperText = "";

	// These have to exist before Label's constructor sets Text.
	readonly TextEditingCore core = new() { MultiLine = false, CopyLineWhenEmpty = false };
	string cachedText = "";
	bool settingText;

	readonly TextEditingInput input;

	public TextEditingCore Editing => core;
	public readonly Caret Caret;

	public bool MultiLine {
		get => core.MultiLine;
		set {
			if (core.MultiLine == value) return;
			core.MultiLine = value;
			core.CopyLineWhenEmpty = value;
			if (value)
				TextOverflowMode = TextOverflowMode.CharWrap;
			InvalidateLines();
		}
	}

	public bool ReadOnly {
		get => core.ReadOnly;
		set {
			core.ReadOnly = value;
			KeyboardUnfocus();
		}
	}

	public ReadOnlySpan<char> GetHelperText() => HelperText;
	public void SetHelperText(ReadOnlySpan<char> text) => HelperText = new(text);

	public int MaxLength { get => core.MaxLength; set => core.MaxLength = value; }
	public bool IsPassword {
		get => !core.AllowCopy;
		set {
			core.AllowCopy = !value;
			InvalidateLines();
		}
	}
	public int TabSize { get => core.TabSize; set => core.TabSize = value; }
	public override bool WantsTextInput => true;

	public delegate void TextChangedDelegate(Textbox textbox, string oldText, string newText);
	public event TextChangedDelegate? OnUserPressedEnter;
	/// <summary>
	/// Fires when the user changes the text, not when <see cref="Text"/> is set from code.
	/// </summary>
	public event TextChangedDelegate? OnTextChanged;

	readonly List<TextLine> lines = [];
	readonly List<float> prefixWidths = [];
	bool linesInvalid = true;

	float scrollOffsetY = 0;
	float scrollOffsetX = 0;

	public Textbox(Element? parent) : base(parent) {
		Caret = new(core);
		input = new(core);
		core.Layout = this;
		core.TextChanged += Core_TextChanged;

		Text = "";
		KeyboardInputMarshal = new HoldingKeyboardInputMarshal();
		TextSize = 20;
		TextOverflowMode = TextOverflowMode.None;
		SetPaintBorderEnabled(true);
	}

	public override bool IsDisabled() => !IsMouseInputEnabled() || !IsKeyboardInputEnabled();

	public override ReadOnlySpan<char> Text {
		get => cachedText;
		set {
			if (value.Equals(cachedText, StringComparison.Ordinal))
				return;
			settingText = true;
			core.SetText(new string(value));
			settingText = false;
			// Text set from code isn't an undo step.
			core.ResetUndo();
			SyncText(false);
		}
	}

	private void Core_TextChanged(TextEditingCore _) {
		if (!settingText)
			SyncText(true);
	}

	private void SyncText(bool fromUser) {
		string old = cachedText;
		string now = core.GetText();
		// Label compares against Text (cachedText), has to happen before cachedText changes
		base.Text = now;
		cachedText = now;
		InvalidateLines();
		if (fromUser && old != now)
			OnTextChanged?.Invoke(this, old, now);
	}

	protected override void PerformLayout(float width, float height) {
		base.PerformLayout(width, height);
		InvalidateLines();
	}

	protected override void TextChanged(ReadOnlySpan<char> text) {
		base.TextChanged(text);
		InvalidateLines();
	}

	public void SetTextInternal(ReadOnlySpan<char> text, bool resetCaret) {
		int caret = Caret.Position;
		Text = text;
		if (!resetCaret)
			core.SetCaret(core.Document.GetPosition(caret));
	}

	public void SelectAll() => core.SelectAll();

	void InvalidateLines() {
		linesInvalid = true;
	}

	string DisplayText => IsPassword ? new string('•', cachedText.Length) : cachedText;

	RectangleF TextArea() {
		var c = GetContentRect();
		var p = GetTextPadding();
		return RectangleF.XYWH(p.X, p.Y, MathF.Max(0, c.W - p.X * 2), MathF.Max(0, c.H - p.Y * 2));
	}

	#region Layout

	void AddLine(ReadOnlySpan<char> text, int start, int length, float y, float height) {
		int prefixStart = prefixWidths.Count;
		float x = 0;
		prefixWidths.Add(0);
		for (int i = 0; i < length; i++) {
			x += Graphics2D.GetTextSize(text.Slice(start + i, 1), Font, GetRenderTextSize()).X;
			prefixWidths.Add(x);
		}
		lines.Add(new TextLine { Start = start, Length = length, Width = x, Height = height, Y = y, PrefixStart = prefixStart });
	}

	float PrefixWidth(in TextLine line, int col) => prefixWidths[line.PrefixStart + Math.Clamp(col, 0, line.Length)];

	int ColumnAtX(in TextLine line, float x) {
		for (int i = 0; i < line.Length; i++) {
			float left = PrefixWidth(line, i), right = PrefixWidth(line, i + 1);
			if (x < (left + right) * 0.5f)
				return i;
		}
		return line.Length;
	}

	void ValidateLines() {
		if (!linesInvalid) return;
		linesInvalid = false;
		lines.Clear();
		prefixWidths.Clear();

		string text = DisplayText;
		float textSize = GetRenderTextSize();
		float emptyLineH = Graphics2D.GetTextSize("X", Font, textSize).Y;
		if (text.Length == 0) {
			AddLine(text, 0, 0, 0, emptyLineH);
			return;
		}

		if (!MultiLine) {
			AddLine(text, 0, text.Length, 0, Graphics2D.GetTextSize(text, Font, textSize).Y);
			return;
		}

		float availableW = TextArea().W;
		if (availableW <= 0) availableW = 1;

		float yAccum = 0;

		int cursor = 0;
		while (cursor <= text.Length) {
			int nlPos = text.IndexOf('\n', cursor);
			bool isLastSegment = nlPos == -1;
			int segEnd = isLastSegment ? text.Length : nlPos;
			int segLen = segEnd - cursor;

			if (segLen == 0) {
				AddLine(text, cursor, 0, yAccum, emptyLineH);
				yAccum += emptyLineH;
			}
			else {
				int pos = cursor;
				while (pos < segEnd) {
					float lineW = 0;
					float lineH = 0;
					int lineStart = pos;

					while (pos < segEnd) {
						var chSz = Graphics2D.GetTextSize(text.AsSpan().Slice(pos, 1), Font, textSize);
						if (lineW > 0 && lineW + chSz.X > availableW)
							break;
						lineW += chSz.X;
						lineH = Math.Max(lineH, chSz.Y);
						pos++;
					}

					if (lineH == 0)
						lineH = emptyLineH;

					AddLine(text, lineStart, pos - lineStart, yAccum, lineH);
					yAccum += lineH;
				}
			}

			if (isLastSegment)
				break;

			cursor = nlPos + 1;
		}
	}

	(int lineIdx, int col) CharIndexToLineCol(int charIndex) {
		ValidateLines();
		string text = cachedText;
		charIndex = Math.Clamp(charIndex, 0, text.Length);

		for (int i = 0; i < lines.Count; i++) {
			var line = lines[i];
			if (charIndex >= line.Start && charIndex <= line.Start + line.Length) {
				// Wrapped line ends where the next starts, prefer the next one
				if (charIndex == line.End && i + 1 < lines.Count && lines[i + 1].Start == charIndex)
					continue;
				return (i, charIndex - line.Start);
			}
		}
		var last = lines[^1];
		return (lines.Count - 1, Math.Max(0, charIndex - last.Start));
	}

	float GetCaretXInLine(int lineIndex, int col) {
		ValidateLines();
		if (lineIndex < 0 || lineIndex >= lines.Count) return 0;
		var line = lines[lineIndex];
		if (col <= 0) return 0;

		return PrefixWidth(line, col);
	}

	private int VisualLineAtY(float y) {
		for (int i = 0; i < lines.Count; i++)
			if (y < lines[i].Y + lines[i].Height)
				return i;
		return lines.Count - 1;
	}

	private int HitTestPosition(Vector2F mouseLocalToElement, out float lineX) {
		ValidateLines();

		// Into content space (where Paint draws), then into the text area exactly as DrawTextLines does
		Vector2F local = mouseLocalToElement - GetContentRect().Pos;
		var line = lines[VisualLineAtY(local.Y - TextTop() + scrollOffsetY)];
		lineX = local.X - GetLineDrawX(line);
		return line.Start + ColumnAtX(line, lineX);
	}

	private TextPosition PositionFromMouse(out float lineX) => core.Document.GetPosition(HitTestPosition(GetMousePos(), out lineX));

	float GetLineDrawX(TextLine line) {
		RectangleF area = TextArea();

		// doesn't fit, scroll instead of aligning
		if (!MultiLine && line.Width > area.W)
			return area.X - MathF.Min(scrollOffsetX, line.Width - area.W + 1);

		var halign = GetTextAlignment().ToTextAlignment().Horizontal;
		return halign switch {
			Types.TextAlignment.Center => area.X + (area.W - line.Width) / 2f,
			Types.TextAlignment.Right => area.X + area.W - line.Width,
			_ => area.X
		};
	}

	/// <summary>
	/// Y of the first line. Multi-line text that doesn't fit always starts at the top
	/// </summary>
	private float TextTop() {
		RectangleF area = TextArea();
		if (lines.Count == 0)
			return area.Y;

		float textHeight = lines[lines.Count - 1].Y + lines[lines.Count - 1].Height;
		if (MultiLine && textHeight > area.H)
			return area.Y;
		return GetTextAlignment().ToTextAlignment().Vertical switch {
			Types.TextAlignment.Center => MathF.Round(area.Y + (area.H - textHeight) / 2f),
			Types.TextAlignment.Bottom => area.Y + area.H - textHeight,
			_ => area.Y
		};
	}

	private void EnsureCaretVisible() {
		ValidateLines();
		var (lineIdx, col) = CharIndexToLineCol(Caret.Position);

		if (!MultiLine) {
			var only = lines[0];
			float areaW = TextArea().W;
			if (only.Width <= areaW) {
				scrollOffsetX = 0;
				return;
			}

			float caretX = GetCaretXInLine(lineIdx, col);
			if (caretX < scrollOffsetX)
				scrollOffsetX = caretX;
			else if (caretX > scrollOffsetX + areaW - 1)
				scrollOffsetX = caretX - areaW + 1;

			scrollOffsetX = Math.Clamp(scrollOffsetX, 0, only.Width - areaW + 1);
			return;
		}

		if (lineIdx < 0 || lineIdx >= lines.Count) return;

		var line = lines[lineIdx];
		float caretY = line.Y;
		float visibleH = TextArea().H;

		if (caretY < scrollOffsetY)
			scrollOffsetY = caretY;
		else if (caretY + line.Height > scrollOffsetY + visibleH)
			scrollOffsetY = caretY + line.Height - visibleH;

		scrollOffsetY = Math.Max(0, scrollOffsetY);
	}

	#endregion

	#region ITextLayout

	private int VisualLineOf(TextPosition position, out int col) {
		var location = CharIndexToLineCol(core.Document.GetOffset(position));
		col = location.col;
		return location.lineIdx;
	}

	TextPosition ITextLayout.MoveVertically(TextPosition from, int count, ref float? preferredX) {
		ValidateLines();
		int lineIndex = VisualLineOf(from, out int col);
		preferredX ??= GetCaretXInLine(lineIndex, col);

		int target = lineIndex + count;
		if (target < 0) return core.Document.Start;
		if (target >= lines.Count) return core.Document.End;

		var line = lines[target];
		return core.Document.GetPosition(line.Start + ColumnAtX(line, preferredX.Value));
	}

	int ITextLayout.PageLines {
		get {
			ValidateLines();
			float lineH = lines.Count > 0 ? lines[0].Height : 1;
			return Math.Max(1, (int)(TextArea().H / MathF.Max(1, lineH)));
		}
	}

	float ITextLayout.GetX(TextPosition position) {
		int lineIndex = VisualLineOf(position, out int col);
		return GetCaretXInLine(lineIndex, col);
	}

	TextPosition ITextLayout.PositionAt(int line, float x) {
		ValidateLines();
		int offset = core.Document.GetOffset(new(line, 0));
		int lineIndex = CharIndexToLineCol(offset).lineIdx;
		return core.Document.GetPosition(lines[lineIndex].Start + ColumnAtX(lines[lineIndex], x));
	}

	#endregion

	#region Input

	protected override void OnThink() {
		if (IsHovered())
			EngineCore.SetMouseCursor(MouseCursor.MOUSE_CURSOR_IBEAM);
		input.EndFrame();
	}

	protected override bool OnGainingKeyboardFocus(Element? lastFocus, ref Element? passTo) {
		base.OnGainingKeyboardFocus(lastFocus, ref passTo);
		core.ResetUndo();
		input.Blink.Reset();
		return true;
	}

	protected override bool OnLosingKeyboardFocus(Element? lostTo) {
		base.OnLosingKeyboardFocus(lostTo);
		core.SetCaret(core.Primary.Caret);
		core.ResetUndo();
		scrollOffsetX = 0;
		return true;
	}

	protected override bool MouseClick(FrameState state, ButtonCode button) {
		base.MouseClick(state, button);
		if (button != ButtonCode.MouseLeft)
			return true;

		if (!IsKeyboardFocused())
			KeyboardFocus();

		var pos = PositionFromMouse(out _);
		input.PointerDown(pos, state.Keyboard.ShiftDown, state.Keyboard.ControlDown, state.Keyboard.AltDown);
		EnsureCaretVisible();
		return true;
	}

	protected override bool MouseRelease(Element self, FrameState state, ButtonCode button) {
		if (!IsHovered()) return true;
		if (button != ButtonCode.MouseLeft) return true;

		// Subclasses that skip MouseClick (NumSlider) still focus here.
		if (!IsKeyboardFocused()) {
			KeyboardFocus();
			core.SetCaret(PositionFromMouse(out _));
			EnsureCaretVisible();
		}
		return true;
	}

	protected override bool MouseDrag(Element self, FrameState state, Vector2F delta) {
		base.MouseDrag(self, state, delta);
		var pos = PositionFromMouse(out float lineX);
		input.PointerDrag(pos, lineX, state.Keyboard.AltDown);
		EnsureCaretVisible();
		return true;
	}

	protected override bool MouseScroll(Element self, FrameState state, Vector2F delta) {
		// Shift scrolls sideways
		if (state.Keyboard.ShiftDown && delta.X == 0)
			delta = new(delta.Y, 0);

		ValidateLines();

		// Nothing to scroll, let the parent have it
		if (!MultiLine) {
			if (delta.X == 0 || lines.Count == 0)
				return false;
			float maxX = lines[0].Width - TextArea().W + 1;
			if (maxX <= 0)
				return false;
			scrollOffsetX = Math.Clamp(scrollOffsetX - delta.X * 20, 0, maxX);
			return true;
		}
		if (delta.Y == 0)
			return false;

		float totalH = lines.Count > 0 ? lines[^1].Y + lines[^1].Height : 0;
		float maxScroll = Math.Max(0, totalH - TextArea().H);
		if (maxScroll <= 0)
			return false;

		scrollOffsetY = Math.Clamp(scrollOffsetY - delta.Y * 20, 0, maxScroll);
		return true;
	}

	protected override bool TextInput(in KeyboardState keyboardState, string inputText) {
		input.TextInput(in keyboardState, inputText);
		EnsureCaretVisible();
		return true;
	}

	protected override bool KeyPressed(in KeyboardState state, ButtonCode key) {
		if ((key == ButtonCode.KeyEnter || key == ButtonCode.KeyPadEnter) && !MultiLine) {
			KeyboardUnfocus();
			OnUserPressedEnter?.Invoke(this, "", cachedText);
			return true;
		}

		bool handled = input.KeyPressed(in state, key);
		if (handled)
			EnsureCaretVisible();

		// Let these go up to the parent if we didn't use them
		if (!handled && key is ButtonCode.KeyTab or ButtonCode.KeyEscape)
			return false;
		return true;
	}

	#endregion

	#region Painting

	SchemeableSetting<Color> bgFocusedColor = SchemeableSetting<Color>.Default(new(20, 32, 25, 127));
	SchemeableSetting<Color> selectionColor = SchemeableSetting<Color>.Default(new(170, 200, 255, 80));
	SchemeableSetting<Color> caretColor = SchemeableSetting<Color>.Default(new(240, 248, 255, 255));

	public override void ApplySchemeSettings(IScheme scheme) {
		base.ApplySchemeSettings(scheme);

		SetBgSchemeColor(scheme.GetColor("Nucleus.Textbox.Background", scheme.GetColor("Nucleus.Background")));
		SetFgSchemeColor(scheme.GetColor("Nucleus.Textbox.Border", scheme.GetColor("Nucleus.Border")));
		bgFocusedColor.SetSchemeValue(scheme.GetColor("Nucleus.Textbox.BackgroundFocused", new(20, 32, 25, 127)));
		selectionColor.SetSchemeValue(scheme.GetColor("Nucleus.Textbox.Selection", new(170, 200, 255, 80)));
		caretColor.SetSchemeValue(scheme.GetColor("Nucleus.Textbox.Caret", new(240, 248, 255, 255)));
	}

	Color GetStateBgColor() => IsKeyboardFocused() && !HasUserBgColor ? bgFocusedColor.Get() : GetBgColor();
	private Color GetStateFgColor() => IsKeyboardFocused() && !HasUserFgColor ? GetFocusColor() : GetFgColor();

	public override void Paint(float width, float height) {
		ValidateLines();

		Color back;
		if (!ReadOnly) {
			back = MixColorBasedOnMouseState(this, GetStateBgColor(), new(0, 1.1f, 2.3f, 1f), new(0, 1.2f, 0.6f, 1f));
		}
		else {
			back = GetStateBgColor();
		}

		Graphics2D.SetDrawColor(back);
		Graphics2D.DrawRectangle(0, 0, width, height);

		bool showPlaceholder = cachedText.Length == 0;

		if (!showPlaceholder && (IsKeyboardFocused() || core.HasSelection))
			foreach (var selection in core.Selections)
				if (!selection.IsEmpty)
					DrawSelection(selection);

		if (showPlaceholder) {
			if (HelperText.Length > 0 && lines.Count > 0) {
				Graphics2D.SetDrawColor(GetTextColor().Adjust(0, -0.1, -0.4));
				var line = lines[0];
				Graphics2D.DrawText(GetLineDrawX(line), TextTop(), HelperText, Font, GetRenderTextSize(), Anchor.TopLeft);
			}
		}
		else
			DrawTextLines(width, height);

		if (IsKeyboardFocused() && input.Blink.Visible)
			foreach (var selection in core.Selections)
				DrawCaret(selection.Caret);
	}
	public override void PaintBorder(float width, float height) {
		Color fore = MixColorBasedOnMouseState(this, GetStateFgColor(), new(0, 1.1f, 1.3f, 1f), new(0, 1.2f, 0.6f, 1f));
		Graphics2D.SetDrawColor(fore);
		Graphics2D.DrawRectangleOutline(0, 0, width, height, BorderSize);
	}

	void DrawTextLines(float width, float height) {
		string text = DisplayText;
		var textC = GetTextColor();
		if (IsDisabled())
			textC = textC.Adjust(0, 0, -0.5f);

		Graphics2D.SetDrawColor(textC);

		float padY = TextTop();

		foreach (var line in lines) {
			float drawY = padY + line.Y - scrollOffsetY;

			if (drawY + line.Height < 0) continue;
			if (drawY > height) break;

			if (line.Length == 0) continue;

			float drawX = GetLineDrawX(line);
			ReadOnlySpan<char> lineText = text.AsSpan().Slice(line.Start, line.Length);
			Graphics2D.DrawText(drawX, drawY, lineText, Font, GetRenderTextSize(), Anchor.TopLeft);
		}
	}

	private void DrawSelection(TextSelection selection) {
		int selStart = core.Document.GetOffset(selection.Start);
		int selEnd = core.Document.GetOffset(selection.End);

		float padY = TextTop();

		Graphics2D.SetDrawColor(selectionColor.Get());

		foreach (var line in lines) {
			int lineEnd = line.Start + line.Length;
			if (selEnd <= line.Start || selStart >= lineEnd) {
				// sliver for empty lines in a selection
				if (!(line.Length == 0 && selStart <= line.Start && selEnd > line.Start))
					continue;
			}

			int overlapStart = Math.Max(selStart, line.Start);
			int overlapEnd = Math.Min(selEnd, lineEnd);

			float startX = GetLineDrawX(line) + PrefixWidth(line, overlapStart - line.Start);
			float selW = MathF.Max(PrefixWidth(line, overlapEnd - line.Start) - PrefixWidth(line, overlapStart - line.Start), line.Length == 0 ? 4 : 0);
			float drawY = padY + line.Y - scrollOffsetY;

			Graphics2D.DrawRectangle(startX, drawY, selW, line.Height);
		}
	}

	private void DrawCaret(TextPosition position) {
		ValidateLines();

		var (lineIdx, col) = CharIndexToLineCol(core.Document.GetOffset(position));
		if (lineIdx < 0 || lineIdx >= lines.Count) return;
		var line = lines[lineIdx];

		float drawX = MathF.Round(GetLineDrawX(line) + GetCaretXInLine(lineIdx, col));
		float drawY = TextTop() + line.Y - scrollOffsetY;

		Graphics2D.SetDrawColor(caretColor.Get());
		Graphics2D.DrawRectangle(drawX, drawY, 2, line.Height);
	}

	#endregion
}
