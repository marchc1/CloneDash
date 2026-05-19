using Nucleus.Common.Input;
using Nucleus.Common.Types;
using Nucleus.Core;
using Nucleus.Extensions;
using Nucleus.Input;
using Nucleus.Types;
using Raylib_cs;

namespace Nucleus.UI;

public class Caret
{
	public int Position { get; set; } = 0;
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
	public void SelectAll(string text) {
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

	public readonly int End => Start + Length;
	public override string ToString() => $"Line(start:{Start}, len:{Length}, w:{Width:F1}, h:{Height:F1}, y:{Y:F1})";
}

public class Textbox : Label
{
	public string HelperText { get; set; } = "";

	private bool __multiLine = false;
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
	public int MaxLength { get; set; } = 0;
	public bool IsPassword { get; set; } = false;
	public int TabSize { get; set; } = 4;
	public readonly Caret Caret = new();
	public DateTime LastKeyboardInteraction { get; private set; } = DateTime.Now;

	public delegate void TextChangedDelegate(Textbox textbox, string oldText, string newText);
	public event TextChangedDelegate? OnUserPressedEnter;
	public event TextChangedDelegate? OnTextChanged;

	readonly List<TextLine> lines = [];
	bool linesInvalid = true;

	float scrollOffsetY = 0;

	readonly record struct UndoEntry(string Text, int CaretPosition);
	readonly List<UndoEntry> undoStack = [];
	readonly List<UndoEntry> redoStack = [];
	const int MaxUndoEntries = 128;
	DateTime lastUndoPush = DateTime.MinValue;

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

	protected override void TextChanged(string oldText, string newText) {
		base.TextChanged(oldText, newText);
		InvalidateLines();
	}

	public void SetText(string text) {
		Text = text;
		Caret.Position = text.Length;
		Caret.ClearSelection();
		InvalidateLines();
	}

	public void SelectAll() {
		Caret.SelectAll(Text);
	}

	void PushUndo(bool force = false) {
		if (!force && (DateTime.Now - lastUndoPush).TotalMilliseconds < 400 && undoStack.Count > 0) {
			undoStack[^1] = new UndoEntry(Text, Caret.Position);
			return;
		}

		if (undoStack.Count >= MaxUndoEntries)
			undoStack.RemoveAt(0);

		undoStack.Add(new UndoEntry(Text, Caret.Position));
		redoStack.Clear();
		lastUndoPush = DateTime.Now;
	}

	void PerformUndo() {
		if (undoStack.Count == 0) return;

		var entry = undoStack[^1];
		undoStack.RemoveAt(undoStack.Count - 1);
		redoStack.Add(new UndoEntry(Text, Caret.Position));

		var old = Text;
		Text = entry.Text;
		Caret.Position = entry.CaretPosition;
		Caret.ClearSelection();
		FireTextChanged(old);
	}

	void PerformRedo() {
		if (redoStack.Count == 0) return;

		var entry = redoStack[^1];
		redoStack.RemoveAt(redoStack.Count - 1);
		undoStack.Add(new UndoEntry(Text, Caret.Position));

		var old = Text;
		Text = entry.Text;
		Caret.Position = entry.CaretPosition;
		Caret.ClearSelection();
		FireTextChanged(old);
	}
	void InvalidateLines() {
		linesInvalid = true;
	}
	string DisplayText => IsPassword ? new string('•', Text.Length) : Text;
	void ValidateLines() {
		if (!linesInvalid) return;
		linesInvalid = false;
		lines.Clear();

		string text = DisplayText ?? "";
		if (text.Length == 0) {
			float lineH = Graphics2D.GetTextSize("X", Font, TextSize).Y;
			lines.Add(new TextLine { Start = 0, Length = 0, Width = 0, Height = lineH, Y = 0 });
			return;
		}

		if (!MultiLine) {
			var sz = Graphics2D.GetTextSize(text, Font, TextSize);
			lines.Add(new TextLine { Start = 0, Length = text.Length, Width = sz.X, Height = sz.Y, Y = 0 });
			return;
		}

		float availableW = RenderBounds.Width - TextPadding.X * 2 - 4;
		if (availableW <= 0) availableW = 1;

		float yAccum = 0;

		int cursor = 0;
		while (cursor <= text.Length) {
			int nlPos = text.IndexOf('\n', cursor);
			bool isLastSegment = nlPos == -1;
			int segEnd = isLastSegment ? text.Length : nlPos;
			int segLen = segEnd - cursor;

			if (segLen == 0) {
				float lineH = Graphics2D.GetTextSize("X", Font, TextSize).Y;
				lines.Add(new TextLine { Start = cursor, Length = 0, Width = 0, Height = lineH, Y = yAccum });
				yAccum += lineH;
			}
			else {
				int pos = cursor;
				while (pos < segEnd) {
					float lineW = 0;
					float lineH = 0;
					int lineStart = pos;

					while (pos < segEnd) {
						var chSz = Graphics2D.GetTextSize(text.AsSpan().Slice(pos, 1), Font, TextSize);
						if (lineW > 0 && lineW + chSz.X > availableW)
							break;
						lineW += chSz.X;
						lineH = Math.Max(lineH, chSz.Y);
						pos++;
					}

					if (lineH == 0)
						lineH = Graphics2D.GetTextSize("X", Font, TextSize).Y;

					lines.Add(new TextLine {
						Start = lineStart,
						Length = pos - lineStart,
						Width = lineW,
						Height = lineH,
						Y = yAccum
					});
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
		charIndex = Math.Clamp(charIndex, 0, Text.Length);

		for (int i = 0; i < lines.Count; i++) {
			var line = lines[i];
			if (charIndex >= line.Start && charIndex <= line.Start + line.Length) {
				if (charIndex == line.Start + line.Length && i + 1 < lines.Count && charIndex < Text.Length)
					if (charIndex < Text.Length && Text[charIndex] == '\n')
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

		int clampedCol = Math.Min(col, line.Length);
		string text = DisplayText;
		return Graphics2D.GetTextSize(text.AsSpan().Slice(line.Start, clampedCol), Font, TextSize).X;
	}

	int HitTestPosition(Vector2F localPos) {
		ValidateLines();

		float textAreaX = TextPadding.X + 2;
		float textAreaY = TextPadding.Y + 2;
		float relX = localPos.X - textAreaX;
		float relY = localPos.Y - textAreaY + scrollOffsetY;

		string text = DisplayText;

		int targetLine = lines.Count - 1;
		for (int i = 0; i < lines.Count; i++) {
			if (relY < lines[i].Y + lines[i].Height) {
				targetLine = i;
				break;
			}
		}

		var line = lines[targetLine];

		float lineStartX = GetLineDrawX(line, RenderBounds.Width);
		relX -= lineStartX - textAreaX;

		float accumX = 0;
		for (int i = 0; i < line.Length; i++) {
			var chSz = Graphics2D.GetTextSize(text.AsSpan().Slice(line.Start + i, 1), Font, TextSize);
			if (relX < accumX + chSz.X * 0.5f)
				return line.Start + i;
			accumX += chSz.X;
		}

		return line.Start + line.Length;
	}

	float GetLineDrawX(TextLine line, float width) {
		float padX = TextPadding.X + 2;
		float availW = width - padX * 2;

		var halign = TextAlignment.ToTextAlignment().Horizontal;
		return halign switch {
			Types.TextAlignment.Center => padX + (availW - line.Width) / 2f,
			Types.TextAlignment.Right => padX + availW - line.Width,
			_ => padX
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
		float visibleH = RenderBounds.Height - TextPadding.Y * 2 - 4;

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

	protected override bool OnLosingKeyboardFocus(Element? lostTo) {
		base.OnLosingKeyboardFocus(lostTo);
		Caret.ClearSelection();
		return true;
	}

	private DateTime lastClickTime = DateTime.MinValue;
	private int clickCount = 0;

	protected override bool MouseRelease(Element self, FrameState state, ButtonCode button) {
		if (ReadOnly && !MultiLine) return true;
		if (button != ButtonCode.MouseLeft) return true;

		KeyboardFocus();

		var now = DateTime.Now;
		if ((now - lastClickTime).TotalMilliseconds < 400)
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
			var (start, end) = GetWordAtPosition(Text, charIdx);
			Caret.SelectionOrigin = start;
			Caret.Position = end;
		}
		else {
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
		float visibleH = RenderBounds.Height - TextPadding.Y * 2 - 4;
		scrollOffsetY = Math.Clamp(scrollOffsetY, 0, Math.Max(0, totalH - visibleH));

		return true;
	}

	void FireTextChanged(string oldText) {
		if (Text != oldText)
			OnTextChanged?.Invoke(this, oldText, Text);
	}

	void InsertText(string insert) {
		var old = Text;
		PushUndo(force: true);

		if (Caret.HasSelection)
			Text = Caret.DeleteSelection(Text);

		if (MaxLength > 0 && Text.Length + insert.Length > MaxLength)
			insert = insert[..(MaxLength - Text.Length)];

		if (insert.Length == 0) {
			FireTextChanged(old);
			return;
		}

		Text = Text.Insert(Caret.Position, insert);
		Caret.Position += insert.Length;
		Caret.ClearSelection();
		FireTextChanged(old);
	}

	protected override bool TextInput(in KeyboardState keyboardState, string text) {
		var oldText = Text;

		PushUndo();
		if (Caret.HasSelection)
			Text = Caret.DeleteSelection(Text);

		if (MaxLength > 0 && Text.Length >= MaxLength) {
			FireTextChanged(oldText);
			return true;
		}

		// todo: MaxLength handling here...
		Text = Text.Insert(Caret.Position, text);
		Caret.MovePosition(Text, text.Length);
		Caret.ClearSelection();
		FireTextChanged(oldText);
		EnsureCaretVisible();
		return true;
	}

	protected override bool KeyPressed(in KeyboardState state, ButtonCode key) {
		var action = key.GetAction();
		if (action.Type == CharacterType.NoAction)
			return true;

		LastKeyboardInteraction = DateTime.Now;
		var oldText = Text;

		bool ctrl = state.ControlDown;
		bool shift = state.ShiftDown;

		if (ctrl) {
			switch (key) {
				case ButtonCode.KeyA:
					SelectAll();
					return true;

				case ButtonCode.KeyC:
					if (Caret.HasSelection)
						Clipboard.Text = Caret.GetSelectedText(Text);
					return true;

				case ButtonCode.KeyX:
					if (!ReadOnly && Caret.HasSelection) {
						PushUndo(force: true);
						Clipboard.Text = Caret.GetSelectedText(Text);
						Text = Caret.DeleteSelection(Text);
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
						Caret.Position = FindWordBoundaryLeft(Text, Caret.Position);
					else
						Caret.MovePosition(Text, -1);

					Caret.PreferredX = null;
					break;

				case "RIGHT":
					if (!selecting && Caret.HasSelection) {
						Caret.Position = Caret.SelectionEnd;
						Caret.ClearSelection();
					}
					else if (ctrl)
						Caret.Position = FindWordBoundaryRight(Text, Caret.Position);
					else
						Caret.MovePosition(Text, 1);

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
				Caret.Position = Text.Length;
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
					PushUndo(force: true);
					Text = Caret.DeleteSelection(Text);
					FireTextChanged(oldText);
				}
				else if (Caret.Position > 0) {
					PushUndo();
					int deleteCount = ctrl ? Caret.Position - FindWordBoundaryLeft(Text, Caret.Position) : 1;
					int deleteStart = Caret.Position - deleteCount;
					Text = Text.Remove(deleteStart, deleteCount);
					Caret.Position = deleteStart;
					FireTextChanged(oldText);
				}
				EnsureCaretVisible();
				break;

			case CharacterType.DeleteForwards:
				if (Caret.HasSelection) {
					PushUndo(force: true);
					Text = Caret.DeleteSelection(Text);
					FireTextChanged(oldText);
				}
				else if (Caret.Position < Text.Length) {
					PushUndo();
					int deleteCount = ctrl ? FindWordBoundaryRight(Text, Caret.Position) - Caret.Position : 1;
					Text = Text.Remove(Caret.Position, deleteCount);
					FireTextChanged(oldText);
				}
				EnsureCaretVisible();
				break;

			case CharacterType.Enter:
				if (MultiLine) {
					PushUndo(force: true);
					InsertText("\n");
					EnsureCaretVisible();
				}
				else {
					KeyboardUnfocus();
					OnUserPressedEnter?.Invoke(this, "", Text);
				}
				break;

			case CharacterType.Tab:
				if (MultiLine) {
					PushUndo(force: true);
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
			Caret.Position = Text.Length;
			return;
		}

		var line = lines[targetLine];
		string text = DisplayText;
		float accumX = 0;
		int bestPos = line.Start;

		for (int i = 0; i < line.Length; i++) {
			var chSz = Graphics2D.GetTextSize(text.AsSpan().Slice(line.Start + i, 1), Font, TextSize);
			if (accumX + chSz.X * 0.5f > preferredX) {
				bestPos = line.Start + i;
				break;
			}
			accumX += chSz.X;
			bestPos = line.Start + i + 1;
		}

		Caret.Position = Math.Min(bestPos, line.Start + line.Length);
	}

	public override void Paint(float width, float height) {
		ValidateLines();

		SetBgColor(IsKeyboardFocused() ? new(20, 32, 25, 127) : new(20, 25, 32, 127));
		SetFgColor(IsKeyboardFocused() ? new(85, 110, 95, 255) : new(85, 95, 110, 255));

		Color back;
		if (!ReadOnly) {
			back = MixColorBasedOnMouseState(this, GetBgColor(), new(0, 1.1f, 2.3f, 1f), new(0, 1.2f, 0.6f, 1f));
		}
		else {
			back = GetBgColor();
		}

		Graphics2D.SetDrawColor(back);
		Graphics2D.DrawRectangle(0, 0, width, height);

		string text = DisplayText ?? "";
		bool showPlaceholder = text.Length == 0;

		var colorStore = TextColor;
		if (showPlaceholder) {
			TextNocall = HelperText;
			TextColor = TextColor.Adjust(0, -0.1, -0.4);
		}

		if (Caret.HasSelection && !showPlaceholder)
			DrawSelection(width, height);

		if (showPlaceholder || lines.Count == 0)
			base.Paint(width, height);
		else
			DrawTextLines(width, height);

		if (showPlaceholder) {
			TextNocall = "";
			TextColor = colorStore;
		}

		if (IsKeyboardFocused() && (DateTime.Now - LastKeyboardInteraction).TotalSeconds % 0.666 < 0.333)
			DrawCaret(width, height);
	}
	public override void PaintBorder(float width, float height) {
		Color fore = MixColorBasedOnMouseState(this, GetFgColor(), new(0, 1.1f, 1.3f, 1f), new(0, 1.2f, 0.6f, 1f));
		Graphics2D.SetDrawColor(fore);
		Graphics2D.DrawRectangleOutline(0, 0, width, height, BorderSize);
	}

	void DrawTextLines(float width, float height) {
		string text = DisplayText;
		var textC = TextColor;
		if (!IsMouseInputEnabled())
			textC = textC.Adjust(0, 0, -0.5f);

		Graphics2D.SetDrawColor(textC);

		float padY = TextPadding.Y + 2;

		foreach (var line in lines) {
			float drawY = padY + line.Y - scrollOffsetY;

			if (drawY + line.Height < 0) continue;
			if (drawY > height) break;

			if (line.Length == 0) continue;

			float drawX = GetLineDrawX(line, width);
			ReadOnlySpan<char> lineText = text.AsSpan().Slice(line.Start, line.Length);
			Graphics2D.DrawText(drawX, drawY, lineText, Font, TextSize, Anchor.TopLeft);
		}
	}

	void DrawSelection(float width, float height) {
		int selStart = Caret.SelectionStart;
		int selEnd = Caret.SelectionEnd;
		string text = DisplayText;

		float padY = TextPadding.Y + 2;

		Graphics2D.SetDrawColor(170, 200, 255, 80);

		foreach (var line in lines) {
			int lineEnd = line.Start + line.Length;
			if (selEnd <= line.Start || selStart >= lineEnd)
				continue;

			int overlapStart = Math.Max(selStart, line.Start);
			int overlapEnd = Math.Min(selEnd, lineEnd);

			float startX = GetLineDrawX(line, width);
			if (overlapStart > line.Start)
				startX += Graphics2D.GetTextSize(text.AsSpan().Slice(line.Start, overlapStart - line.Start), Font, TextSize).X;

			float selW = Graphics2D.GetTextSize(text.AsSpan().Slice(overlapStart, overlapEnd - overlapStart), Font, TextSize).X;
			float drawY = padY + line.Y - scrollOffsetY;

			float pad = 2;
			Graphics2D.DrawRectangle(startX - pad, drawY - pad, selW + pad * 2, line.Height + pad * 2);
		}
	}

	void DrawCaret(float width, float height) {
		ValidateLines();
		string text = DisplayText;

		var (lineIdx, col) = CharIndexToLineCol(Caret.Position);
		if (lineIdx < 0 || lineIdx >= lines.Count) return;
		var line = lines[lineIdx];

		float drawX = GetLineDrawX(line, width) + GetCaretXInLine(lineIdx, col);
		float padY = TextPadding.Y + 2;
		float drawY = padY + line.Y - scrollOffsetY;

		Graphics2D.SetDrawColor(240, 248, 255);
		Graphics2D.DrawLine(drawX, drawY, drawX, drawY + line.Height);
	}
}