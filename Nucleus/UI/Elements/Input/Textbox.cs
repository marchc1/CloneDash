using Nucleus.Common.Input;
using Nucleus.Common.Types;
using Nucleus.Common.UI;
using Nucleus.Core;
using Nucleus.Extensions;
using Nucleus.Input;
using Nucleus.Types;
using Raylib_cs;

using System.Diagnostics;

namespace Nucleus.UI;

public class Caret
{
	public int Position {
		get;
		set;
	} = 0;
	public int? SelectionOrigin { get; set; } = null;
	public int SelectionStart => HasSelection ? Math.Min(Position, SelectionOrigin!.Value) : Position;
	public int SelectionEnd => HasSelection ? Math.Max(Position, SelectionOrigin!.Value) : Position;
	public int SelectionLength => SelectionEnd - SelectionStart;
	public bool HasSelection => SelectionOrigin.HasValue && SelectionOrigin.Value != Position;
	public float? PreferredX { get; set; } = null;
	public void MovePosition(string text, int delta) => Position = Math.Clamp(Position + delta, 0, text.Length);
	public void Clamp(string text) {
		Position = Math.Clamp(Position, 0, text.Length);
		if (SelectionOrigin.HasValue)
			SelectionOrigin = Math.Clamp(SelectionOrigin.Value, 0, text.Length);
	}
	public string GetSelectedText(string text) {
		if (!HasSelection)
			return "";

		return text.Substring(SelectionStart, SelectionLength);
	}
	public string DeleteSelection(string text) {
		if (!HasSelection)
			return text;

		int start = SelectionStart;
		string result = text.Remove(start, SelectionLength);
		Position = start;
		ClearSelection();

		return result;
	}
	public void BeginOrExtendSelection() => SelectionOrigin ??= Position;
	public void ClearSelection() {
		SelectionOrigin = null;
		PreferredX = null;
	}
	public void SelectAll(ReadOnlySpan<char> text) {
		SelectionOrigin = 0;
		Position = text.Length;
	}
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

public class Textbox : Label
{
	string HelperText = "";
	bool __multiLine = false;


	public bool MultiLine {
		get => __multiLine;
		set {
			if (__multiLine == value) return;
			__multiLine = value;
			if (value)
				TextOverflowMode = TextOverflowMode.CharWrap;
			InvalidateLines();
		}
	}

	private bool __readOnly = false;
	public bool ReadOnly {
		get => __readOnly;
		set {
			__readOnly = value;
			KeyboardUnfocus();
		}
	}

	public ReadOnlySpan<char> GetHelperText() => HelperText;
	public void SetHelperText(ReadOnlySpan<char> text) => HelperText = new(text);

	Vector2F startClickPosition;
	public int MaxLength { get; set; } = 0;
	public bool IsPassword { get; set; } = false;
	public int TabSize { get; set; } = 4;
	public readonly Caret Caret = new();
	long lastKeyboardInteraction = Stopwatch.GetTimestamp();
	public override bool WantsTextInput => true;

	public delegate void TextChangedDelegate(Textbox textbox, string oldText, string newText);
	public event TextChangedDelegate? OnUserPressedEnter;
	public event TextChangedDelegate? OnTextChanged;

	readonly List<TextLine> lines = [];
	readonly List<float> prefixWidths = [];
	bool linesInvalid = true;

	float scrollOffsetY = 0;

	readonly record struct UndoEntry(string Text, int CaretPosition);
	readonly List<UndoEntry> undoStack = [];
	readonly List<UndoEntry> redoStack = [];
	const int MaxUndoEntries = 128;
	long lastUndoPush = long.MinValue;

	public Textbox(Element? parent) : base(parent) {
		Text = "";
		KeyboardInputMarshal = new HoldingKeyboardInputMarshal();
		TextSize = 20;
		SetPaintBorderEnabled(true);
	}

	protected override void PerformLayout(float width, float height) {
		base.PerformLayout(width, height);
		InvalidateLines();
	}

	protected override void TextChanged(ReadOnlySpan<char> text) {
		base.TextChanged(text);
		InvalidateLines();
	}

	bool inPerformUndoOrRedo;
	public void SetTextInternal(ReadOnlySpan<char> text, bool resetCaret) {
		if (text.Equals(Text, StringComparison.InvariantCulture))
			return;

		Caret.ClearSelection();
		if (resetCaret)
			Caret.Position = text.Length;
		base.		Text = text;

		InvalidateLines();
	}

	public void SelectAll() {
		Caret.SelectAll(text);
	}

	/// <summary>
	/// Pushes a state onto the undo list
	/// </summary>
	/// <param name="force"></param>
	void PushUndo(bool force) {
		if (inPerformUndoOrRedo)
			return;

		if (!force && lastUndoPush != long.MinValue && Stopwatch.GetElapsedTime(lastUndoPush).TotalMilliseconds < 400 && undoStack.Count > 0)
			return;

		if (undoStack.Count >= MaxUndoEntries)
			undoStack.RemoveAt(0);

		undoStack.Add(new UndoEntry(text, Caret.Position));
		redoStack.Clear();
		lastUndoPush = Stopwatch.GetTimestamp();
	}

	void PerformUndo() {
		if (undoStack.Count == 0) return;
		inPerformUndoOrRedo = true;
		UndoEntry entry = undoStack[^1];
		undoStack.RemoveAt(undoStack.Count - 1);
		redoStack.Add(new UndoEntry(text, Caret.Position));

		var old = text;
		Text = entry.Text;
		Caret.Position = entry.CaretPosition;
		Caret.ClearSelection();
		FireTextChanged(old);

		inPerformUndoOrRedo = false;
	}

	void PerformRedo() {
		if (redoStack.Count == 0) return;
		inPerformUndoOrRedo = true;
		var entry = redoStack[^1];
		redoStack.RemoveAt(redoStack.Count - 1);
		undoStack.Add(new UndoEntry(text, Caret.Position));

		var old = text;
		Text = entry.Text;
		Caret.Position = entry.CaretPosition;
		Caret.ClearSelection();
		FireTextChanged(old);

		inPerformUndoOrRedo = false;
	}
	void InvalidateLines() {
		linesInvalid = true;
	}
	protected override bool MouseClick(FrameState state, ButtonCode button) {
		base.MouseClick(state, button);
		startClickPosition = GetMousePos();
		Caret.ClearSelection();
		int charIdx = HitTestPosition(startClickPosition);
		Caret.Position = charIdx;
		return true;
	}
	string DisplayText => IsPassword ? new string('•', text.Length) : text;

	RectangleF TextArea() {
		var c = GetContentRect();
		var p = GetTextPadding();
		return RectangleF.XYWH(p.X, p.Y, MathF.Max(0, c.W - p.X * 2), MathF.Max(0, c.H - p.Y * 2));
	}

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

		string text = DisplayText ?? "";
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
		charIndex = Math.Clamp(charIndex, 0, text.Length);

		for (int i = 0; i < lines.Count; i++) {
			var line = lines[i];
			if (charIndex >= line.Start && charIndex <= line.Start + line.Length) {
				if (charIndex == line.Start + line.Length && i + 1 < lines.Count && charIndex < text.Length)
					if (charIndex < text.Length && text[charIndex] == '\n')
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

	int HitTestPosition(Vector2F mouseLocalToElement) {
		ValidateLines();

		// Into content space (where Paint draws), then into the text area exactly as DrawTextLines does
		Vector2F local = mouseLocalToElement - GetContentRect().Pos;
		RectangleF area = TextArea();
		float relY = local.Y - area.Y + scrollOffsetY;

		int targetLine = lines.Count - 1;
		for (int i = 0; i < lines.Count; i++) {
			if (relY < lines[i].Y + lines[i].Height) {
				targetLine = i;
				break;
			}
		}

		var line = lines[targetLine];
		return line.Start + ColumnAtX(line, local.X - GetLineDrawX(line));
	}

	float GetLineDrawX(TextLine line) {
		RectangleF area = TextArea();

		var halign = GetTextAlignment().ToTextAlignment().Horizontal;
		return halign switch {
			Types.TextAlignment.Center => area.X + (area.W - line.Width) / 2f,
			Types.TextAlignment.Right => area.X + area.W - line.Width,
			_ => area.X
		};
	}

	static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c == '_';

	int FindWordBoundaryLeft(string text, int pos) {
		if (pos <= 0)
			return 0;

		pos--;
		while (pos > 0 && !IsWordChar(text[pos]))
			pos--;
		while (pos > 0 && IsWordChar(text[pos - 1]))
			pos--;

		return pos;
	}

	int FindWordBoundaryRight(string text, int pos) {
		if (pos >= text.Length)
			return text.Length;

		while (pos < text.Length && IsWordChar(text[pos]))
			pos++;
		while (pos < text.Length && !IsWordChar(text[pos]))
			pos++;

		return pos;
	}

	(int start, int end) GetWordAtPosition(string text, int pos) {
		if (text.Length == 0) return (0, 0);
		pos = Math.Clamp(pos, 0, text.Length - 1);

		int start = pos, end = pos;

		if (IsWordChar(text[pos])) {
			while (start > 0 && IsWordChar(text[start - 1])) start--;
			while (end < text.Length - 1 && IsWordChar(text[end + 1])) end++;
			return (start, end + 1);
		}

		while (start > 0 && !IsWordChar(text[start - 1]) && text[start - 1] != '\n') start--;
		while (end < text.Length - 1 && !IsWordChar(text[end + 1]) && text[end + 1] != '\n') end++;
		return (start, end + 1);
	}

	void EnsureCaretVisible() {
		if (!MultiLine) return;

		ValidateLines();
		var (lineIdx, col) = CharIndexToLineCol(Caret.Position);
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

	protected override void OnThink() {
		if (IsHovered())
			EngineCore.SetMouseCursor(MouseCursor.MOUSE_CURSOR_IBEAM);
	}

	protected override bool OnGainingKeyboardFocus(Element? lastFocus, ref Element? passTo) {
		base.OnGainingKeyboardFocus(lastFocus, ref passTo);
		// Reset the undo/redo stacks.
		undoStack.Clear();
		redoStack.Clear();
		return true;
	}

	protected override bool OnLosingKeyboardFocus(Element? lostTo) {
		base.OnLosingKeyboardFocus(lostTo);
		Caret.ClearSelection();
		// Reset the undo/redo stacks.
		undoStack.Clear();
		redoStack.Clear();
		return true;
	}

	private long lastClickTime = long.MinValue;
	private int clickCount = 0;

	protected override bool MouseRelease(Element self, FrameState state, ButtonCode button) {
		if (!IsHovered()) return true;
		if (ReadOnly && !MultiLine) return true;
		if (button != ButtonCode.MouseLeft) return true;

		KeyboardFocus();

		long now = Stopwatch.GetTimestamp();
		if (lastClickTime != long.MinValue && Stopwatch.GetElapsedTime(lastClickTime, now).TotalMilliseconds < 400)
			clickCount++;
		else
			clickCount = 1;
		lastClickTime = now;

		var localPos = self.GetMousePos();

		if (clickCount == 3) {
			ValidateLines();
			int charIdx = HitTestPosition(localPos);
			var (lineIdx, _) = CharIndexToLineCol(charIdx);
			if (lineIdx >= 0 && lineIdx < lines.Count) {
				var line = lines[lineIdx];
				Caret.SelectionOrigin = line.Start;
				Caret.Position = line.Start + line.Length;
			}
		}
		else if (clickCount == 2) {
			int charIdx = HitTestPosition(localPos);
			var (start, end) = GetWordAtPosition(text, charIdx);
			Caret.SelectionOrigin = start;
			Caret.Position = end;
		}
		else if ((GetMousePos() - startClickPosition).Length <= 3) {
			int charIdx = HitTestPosition(localPos);
			Caret.Position = charIdx;
			Caret.ClearSelection();
		}

		return true;
	}

	protected override bool MouseDrag(Element self, FrameState state, Vector2F delta) {
		base.MouseDrag(self, state, delta);

		if (!Caret.HasSelection && !Caret.SelectionOrigin.HasValue)
			Caret.SelectionOrigin = Caret.Position;
		else
			Caret.SelectionOrigin ??= Caret.Position;

		var localPos = self.GetMousePos();
		Caret.Position = HitTestPosition(localPos);
		EnsureCaretVisible();

		return true;
	}

	protected override bool MouseScroll(Element self, FrameState state, Vector2F delta) {
		if (!MultiLine) {
			base.MouseScroll(self, state, delta);
			return true;
		}

		scrollOffsetY -= delta.Y * 20;
		ValidateLines();

		float totalH = lines.Count > 0 ? lines[^1].Y + lines[^1].Height : 0;
		float visibleH = TextArea().H;
		scrollOffsetY = Math.Clamp(scrollOffsetY, 0, Math.Max(0, totalH - visibleH));

		return true;
	}

	void FireTextChanged(string oldText) {
		if (text != oldText)
			OnTextChanged?.Invoke(this, oldText, text);
	}

	void InsertText(string insert) {
		var old = text;

		bool pushedUndo = false;
		if (Caret.HasSelection) {
			pushedUndo = true;
			PushUndo(true);
			SetTextInternal(Caret.DeleteSelection(text), true);
		}
		if (MaxLength > 0 && text.Length + insert.Length > MaxLength)
			insert = insert[..(MaxLength - text.Length)];

		if (insert.Length == 0) {
			FireTextChanged(old);
			return;
		}

		if (!pushedUndo)
			PushUndo(true);

		SetTextInternal(text.Insert(Math.Clamp(Caret.Position, 0, text.Length), insert), false);
		Caret.Position += insert.Length;
		Caret.ClearSelection();
		FireTextChanged(old);
	}

	protected override bool TextInput(in KeyboardState keyboardState, string inputText) {
		var oldText = this.text;

		if (Caret.HasSelection) {
			PushUndo(true);
			Text = Caret.DeleteSelection(this.text);
		}

		if (MaxLength > 0 && this.text.Length >= MaxLength) {
			FireTextChanged(oldText);
			return true;
		}

		// todo: MaxLength handling here...
		int oldPosition = Caret.Position;
		PushUndo(false);
		Text = this.text.Insert(Caret.Position, inputText);
		Caret.MovePosition(this.text, inputText.Length);
		Caret.ClearSelection();
		FireTextChanged(oldText);
		EnsureCaretVisible();
		Caret.Position = oldPosition + inputText.Length;
		return true;
	}

	protected override bool KeyPressed(in KeyboardState state, ButtonCode key) {
		var action = key.GetAction();
		if (action.Type == CharacterType.NoAction)
			return true;

		lastKeyboardInteraction = Stopwatch.GetTimestamp();
		var oldText = text;

		bool ctrl = state.ControlDown;
		bool shift = state.ShiftDown;

		if (ctrl) {
			switch (key) {
				case ButtonCode.KeyA:
					SelectAll();
					return true;

				case ButtonCode.KeyC:
					if (Caret.HasSelection)
						Clipboard.Text = Caret.GetSelectedText(text);
					return true;

				case ButtonCode.KeyX:
					if (!ReadOnly && Caret.HasSelection) {
						Clipboard.Text = Caret.GetSelectedText(text);
						PushUndo(true);
						Text = Caret.DeleteSelection(text);
						FireTextChanged(oldText);
					}
					return true;

				case ButtonCode.KeyV:
					if (!ReadOnly) {
						string clip = Clipboard.Text ?? "";
						if (!MultiLine)
							clip = clip.Replace("\n", "").Replace("\r", "");
						InsertText(clip);
					}
					return true;

				case ButtonCode.KeyZ:
					if (!ReadOnly) {
						if (shift)
							PerformRedo();
						else
							PerformUndo();
					}
					return true;

				case ButtonCode.KeyY:
					if (!ReadOnly)
						PerformRedo();
					return true;
			}
		}

		if (action.Type == CharacterType.Arrow) {
			bool selecting = shift;

			if (selecting)
				Caret.BeginOrExtendSelection();

			switch (action.Extra) {
				case "LEFT":
					if (!selecting && Caret.HasSelection) {
						Caret.Position = Caret.SelectionStart;
						Caret.ClearSelection();
					}
					else if (ctrl)
						Caret.Position = FindWordBoundaryLeft(text, Caret.Position);
					else
						Caret.MovePosition(text, -1);

					Caret.PreferredX = null;
					break;

				case "RIGHT":
					if (!selecting && Caret.HasSelection) {
						Caret.Position = Caret.SelectionEnd;
						Caret.ClearSelection();
					}
					else if (ctrl)
						Caret.Position = FindWordBoundaryRight(text, Caret.Position);
					else
						Caret.MovePosition(text, 1);

					Caret.PreferredX = null;
					break;

				case "UP":
					if (MultiLine)
						MoveCaretVertically(-1);
					else if (!selecting && Caret.HasSelection) {
						Caret.Position = Caret.SelectionStart;
						Caret.ClearSelection();
					}
					break;

				case "DOWN":
					if (MultiLine)
						MoveCaretVertically(1);
					else if (!selecting && Caret.HasSelection) {
						Caret.Position = Caret.SelectionEnd;
						Caret.ClearSelection();
					}
					break;
			}

			if (!selecting)
				Caret.ClearSelection();

			EnsureCaretVisible();
			return true;
		}

		if (key == ButtonCode.KeyHome) {
			if (shift) Caret.BeginOrExtendSelection();

			if (ctrl)
				Caret.Position = 0;
			else {
				var (lineIdx, _) = CharIndexToLineCol(Caret.Position);
				Caret.Position = lines[lineIdx].Start;
			}

			if (!shift) Caret.ClearSelection();
			Caret.PreferredX = null;
			EnsureCaretVisible();
			return true;
		}

		if (key == ButtonCode.KeyEnd) {
			if (shift) Caret.BeginOrExtendSelection();

			if (ctrl)
				Caret.Position = text.Length;
			else {
				var (lineIdx, _) = CharIndexToLineCol(Caret.Position);
				var line = lines[lineIdx];
				Caret.Position = line.Start + line.Length;
			}

			if (!shift) Caret.ClearSelection();
			Caret.PreferredX = null;
			EnsureCaretVisible();
			return true;
		}

		if (ReadOnly) return true;

		if (action.Type == CharacterType.VisibleCharacter) {
			// Handled by text input now
			return true;
		}

		switch (action.Type) {
			case CharacterType.DeleteBackwards:
				if (Caret.HasSelection) {
					PushUndo(false);
					SetTextInternal(Caret.DeleteSelection(text), false);
					FireTextChanged(oldText);
				}
				else if (Caret.Position > 0) {
					int deleteCount = ctrl ? Caret.Position - FindWordBoundaryLeft(text, Caret.Position) : 1;
					int deleteStart = Caret.Position - deleteCount;
					PushUndo(false);
					Text = text.Remove(deleteStart, deleteCount);
					Caret.Position = deleteStart;
					FireTextChanged(oldText);
				}
				EnsureCaretVisible();
				break;

			case CharacterType.DeleteForwards:
				if (Caret.HasSelection) {
					PushUndo(false);
					SetTextInternal(Caret.DeleteSelection(text), false);
					FireTextChanged(oldText);
				}
				else if (Caret.Position < text.Length) {
					int deleteCount = ctrl ? FindWordBoundaryRight(text, Caret.Position) - Caret.Position : 1;
					int deleteStart = Caret.Position;
					PushUndo(false);
					Text = text.Remove(Caret.Position, deleteCount);
					Caret.Position = deleteStart;
					FireTextChanged(oldText);
				}
				EnsureCaretVisible();
				break;

			case CharacterType.Enter:
				if (MultiLine) {
					InsertText("\n");
					EnsureCaretVisible();
				}
				else {
					KeyboardUnfocus();
					OnUserPressedEnter?.Invoke(this, "", text);
				}
				break;

			case CharacterType.Tab:
				if (MultiLine) {
					InsertText(new string(' ', TabSize));
					EnsureCaretVisible();
				}
				break;
		}

		return true;
	}

	void MoveCaretVertically(int direction) {
		ValidateLines();

		var (lineIdx, col) = CharIndexToLineCol(Caret.Position);

		Caret.PreferredX ??= GetCaretXInLine(lineIdx, col);
		float preferredX = Caret.PreferredX.Value;

		int targetLine = lineIdx + direction;
		if (targetLine < 0) {
			Caret.Position = 0;
			return;
		}
		if (targetLine >= lines.Count) {
			Caret.Position = this.text.Length;
			return;
		}

		var line = lines[targetLine];
		Caret.Position = line.Start + ColumnAtX(line, preferredX);
	}

	SchemeableSetting<Color> bgFocusedColor = SchemeableSetting<Color>.Default(new(20, 32, 25, 127));
	SchemeableSetting<Color> fgFocusedColor = SchemeableSetting<Color>.Default(new(85, 110, 95, 255));
	SchemeableSetting<Color> selectionColor = SchemeableSetting<Color>.Default(new(170, 200, 255, 80));
	SchemeableSetting<Color> caretColor = SchemeableSetting<Color>.Default(new(240, 248, 255, 255));

	public override void ApplySchemeSettings(IScheme scheme) {
		base.ApplySchemeSettings(scheme);

		SetBgSchemeColor(scheme.GetColor("Nucleus.Textbox.Background", scheme.GetColor("Nucleus.Background")));
		SetFgSchemeColor(scheme.GetColor("Nucleus.Textbox.Border", scheme.GetColor("Nucleus.Border")));
		bgFocusedColor.SetSchemeValue(scheme.GetColor("Nucleus.Textbox.BackgroundFocused", new(20, 32, 25, 127)));
		fgFocusedColor.SetSchemeValue(scheme.GetColor("Nucleus.Textbox.BorderFocused", new(85, 110, 95, 255)));
		selectionColor.SetSchemeValue(scheme.GetColor("Nucleus.Textbox.Selection", new(170, 200, 255, 80)));
		caretColor.SetSchemeValue(scheme.GetColor("Nucleus.Textbox.Caret", new(240, 248, 255, 255)));
	}

	Color GetStateBgColor() => IsKeyboardFocused() && !HasUserBgColor ? bgFocusedColor.Get() : GetBgColor();
	Color GetStateFgColor() => IsKeyboardFocused() && !HasUserFgColor ? fgFocusedColor.Get() : GetFgColor();

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

		bool showPlaceholder = (DisplayText ?? "").Length == 0;

		if (Caret.HasSelection && !showPlaceholder)
			DrawSelection(width, height);

		if (showPlaceholder) {
			if (HelperText.Length > 0 && lines.Count > 0) {
				Graphics2D.SetDrawColor(GetTextColor().Adjust(0, -0.1, -0.4));
				var line = lines[0];
				Graphics2D.DrawText(GetLineDrawX(line), TextArea().Y, HelperText, Font, GetRenderTextSize(), Anchor.TopLeft);
			}
		}
		else
			DrawTextLines(width, height);

		if (IsKeyboardFocused() && Stopwatch.GetElapsedTime(lastKeyboardInteraction).TotalSeconds % 0.666 < 0.333)
			DrawCaret(width, height);
	}
	public override void PaintBorder(float width, float height) {
		Color fore = MixColorBasedOnMouseState(this, GetStateFgColor(), new(0, 1.1f, 1.3f, 1f), new(0, 1.2f, 0.6f, 1f));
		Graphics2D.SetDrawColor(fore);
		Graphics2D.DrawRectangleOutline(0, 0, width, height, BorderSize);
	}

	void DrawTextLines(float width, float height) {
		string text = DisplayText;
		var textC = GetTextColor();
		if (!IsMouseInputEnabled())
			textC = textC.Adjust(0, 0, -0.5f);

		Graphics2D.SetDrawColor(textC);

		float padY = TextArea().Y;

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

	void DrawSelection(float width, float height) {
		int selStart = Caret.SelectionStart;
		int selEnd = Caret.SelectionEnd;

		float padY = TextArea().Y;

		Graphics2D.SetDrawColor(selectionColor.Get());

		foreach (var line in lines) {
			int lineEnd = line.Start + line.Length;
			if (selEnd <= line.Start || selStart >= lineEnd)
				continue;

			int overlapStart = Math.Max(selStart, line.Start);
			int overlapEnd = Math.Min(selEnd, lineEnd);

			float startX = GetLineDrawX(line) + PrefixWidth(line, overlapStart - line.Start);
			float selW = PrefixWidth(line, overlapEnd - line.Start) - PrefixWidth(line, overlapStart - line.Start);
			float drawY = padY + line.Y - scrollOffsetY;

			float pad = 2;
			Graphics2D.DrawRectangle(startX - pad, drawY - pad, selW + pad * 2, line.Height + pad * 2);
		}
	}

	void DrawCaret(float width, float height) {
		ValidateLines();

		var (lineIdx, col) = CharIndexToLineCol(Caret.Position);
		if (lineIdx < 0 || lineIdx >= lines.Count) return;
		var line = lines[lineIdx];

		float drawX = MathF.Round(GetLineDrawX(line) + GetCaretXInLine(lineIdx, col));
		float drawY = TextArea().Y + line.Y - scrollOffsetY;

		Graphics2D.SetDrawColor(caretColor.Get());
		Graphics2D.DrawLine(drawX, drawY, drawX, drawY + line.Height);
	}
}