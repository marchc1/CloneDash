using Nucleus.Commands;
using Nucleus.Common.Input;
using Nucleus.Common.Types;
using Nucleus.Core;
using Nucleus.Extensions;
using Nucleus.Input;
using Nucleus.Types;
using Nucleus.UI.Elements;

using Raylib_cs;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Nucleus.UI;

/// <summary>
/// The primary selection in row/column terms, Start is the anchor
/// </summary>
public struct TextEditorCaret
{
	public int StartCol = 0;
	public int EndCol = 0;
	public int StartRow = 0;
	public int EndRow = 0;

	public TextEditorCaret() {

	}
	public TextEditorCaret(int row, int col) {
		StartRow = row;
		StartCol = col;
		EndRow = row;
		EndCol = col;
	}
	public TextEditorCaret(int row, int col, int eRow, int eCol) {
		StartRow = row;
		StartCol = col;
		EndRow = eRow;
		EndCol = eCol;
	}

	// Swizzling
	public TextEditorCaret SR_SC => new(StartRow, StartCol);
	public TextEditorCaret SR_EC => new(StartRow, EndCol);
	public TextEditorCaret SR_ER => new(StartRow, EndRow);
	public TextEditorCaret ER_SC => new(EndRow, StartCol);
	public TextEditorCaret ER_EC => new(EndRow, EndCol);
	public TextEditorCaret ER_SR => new(EndRow, StartRow);

	public int Row => StartRow;
	public int Column => StartCol;
}

public class AutocompletePanel : Panel
{
	public TextEditor Editor = null!;
	public int SelectionIndex;
	public ScrollPanel OptionsParent;
	public AutocompletePanel(Element? parent) : base(parent) {
		Size = new(480, 180);
		OptionsParent = new ScrollPanel(this);
		OptionsParent.SetPaintBackgroundEnabled(false);
		OptionsParent.Dock = Dock.Fill;
	}
	public readonly List<string> Options = [];
	public readonly List<Button> Buttons = [];
	public delegate void SelectedFn(ReadOnlySpan<char> selectedText);
	public event SelectedFn? Selected;

	public void AddOption(int fontHeight, ReadOnlySpan<char> text) {
		Options.Add(new(text));
		Button btn = new Button(OptionsParent);
		btn.Dock = Dock.Top;
		btn.SetAutoSize(true);
		btn.TextSize = fontHeight;
		btn.Font = Graphics2D.UI_MONO_BOLD_FONT_NAME;
		btn.Text = text;
		btn.SetTextAlignment(Anchor.CenterLeft);
		btn.SetTextPadding(new(12, 0));
		btn.BorderSize = 0;
		btn.PulsePreservesAlpha = true;
		if (Buttons.Count == 0)
			btn.Pulsing = true;
		btn.OnButtonClick += (_, _) => Selected?.Invoke(btn.Text);
		Buttons.Add(btn);
	}
	public void Reset() {
		OptionsParent.ClearChildren();
		Options.Clear();
		Buttons.Clear();
		SelectionIndex = 0;
	}

	private void Select(int index) {
		if (Buttons.Count == 0)
			return;
		if (SelectionIndex >= 0 && SelectionIndex < Buttons.Count)
			Buttons[SelectionIndex].Pulsing = false;
		SelectionIndex = (index % Buttons.Count + Buttons.Count) % Buttons.Count;
		Buttons[SelectionIndex].Pulsing = true;
	}

	public void MoveSelectUp() => Select(SelectionIndex - 1);
	public void MoveSelectDown() => Select(SelectionIndex + 1);
}

public class TextEditor : Panel, ITextElement, ITextLayout
{
	SchemeableSetting<float> __TextSize = SchemeableSetting<float>.Default(18);
	SchemeableSetting<string> __Font = SchemeableSetting<string>.Default(Graphics2D.UI_FONT_NAME);

	readonly TextEditingCore core = new() { AutoIndent = true };
	readonly TextEditingInput input;

	public TextEditingCore Editing => core;

	internal TextEditorInternals Editor { get; set; }
	internal TextEditorGutter Gutter { get; set; }
	public Scrollbar VScrollbar { get; set; }
	public Scrollbar? HScrollbar { get; set; }

	public TextEditorCaret Caret {
		get {
			var selection = core.Primary;
			return new(selection.Anchor.Line, selection.Anchor.Column, selection.Caret.Line, selection.Caret.Column);
		}
		set => SetSelection(value.StartCol, value.StartRow, value.EndCol, value.EndRow);
	}

	public delegate void EditorPaintDelegate(TextEditor self, float w, float h);
	public event EditorPaintDelegate? PreRenderEditorLines;
	public event AutoCompletePopulateFn? PopulateAutoComplete;
	public event DetermineAutoCompleteStartPositionFn? DetermineAutoCompleteStartPosition;

	public delegate int DetermineAutoCompleteStartPositionFn(TextEditor self, ReadOnlySpan<char> subRowUpToCaret);
	public delegate int AutoCompletePopulateFn(TextEditor self, ReadOnlySpan<char> left, ReadOnlySpan<char> right, Span<char> solutions);

	public bool Autocomplete { get; set; }

	public AutocompletePanel? AutocompletePanel;
	[MemberNotNullWhen(true, nameof(AutocompletePanel))]
	public bool IsAutocompleteActive => IValidatable.IsValid(AutocompletePanel);
	public string? AutocompleteOption => IsAutocompleteActive && AutocompletePanel.SelectionIndex < AutocompletePanel.Options.Count
			? AutocompletePanel.Options[AutocompletePanel.SelectionIndex] : null;

	[MemberNotNull(nameof(AutocompletePanel))]
	public void ShowAutocomplete() {
		if (!IValidatable.IsValid(AutocompletePanel)) {
			AutocompletePanel = new AutocompletePanel(Editor);
			AutocompletePanel.Editor = this;
			AutocompletePanel.Selected += AutocompletePanel_Selected;
		}
	}

	private void AutocompletePanel_Selected(ReadOnlySpan<char> selectedText) {
		// Replaces what was typed since the autocomplete start
		var caret = core.Primary.Caret;
		core.SetSelection(new(new TextPosition(caret.Line, Math.Min(lastAutocompleteStart, caret.Column)), caret));
		core.Insert(new string(selectedText));
		CloseAutocomplete();
		EnsureCaretVisible();
	}

	public ReadOnlySpan<char> Font {
		get => __Font.Get();
		set {
			__Font.SetUserValue(new(value));
			InvalidateLayout();
		}
	}

	public float TextSize {
		get => __TextSize.Get();
		set {
			__TextSize.SetUserValue(value);
			InvalidateLayout();
		}
	}

	public void PopulateAutocomplete() {
		if (!Autocomplete)
			return;

		Span<char> writeBuffer = stackalloc char[1024 * 8];
		var caret = core.Primary.Caret;
		string completionRow = core.Document.GetLine(caret.Line);
		ReadOnlySpan<char> completionWork = completionRow.AsSpan()[..Math.Min(caret.Column, completionRow.Length)];
		int start = DetermineAutoCompleteStartPosition?.Invoke(this, completionWork) ?? -1;
		if (start == -1) {
			CloseAutocomplete();
			return;
		}

		int solutions = PopulateAutoComplete?.Invoke(this, completionWork[..start], completionWork[start..], writeBuffer) ?? 0;
		if (solutions == 0) {
			CloseAutocomplete();
			return;
		}

		ShowAutocomplete();

		AutocompletePanel!.Reset();
		for (int i = 0; i < solutions; i++) {
			int end = writeBuffer.IndexOf('\0');
			if (end == -1)
				end = writeBuffer.Length;
			Span<char> oneString = writeBuffer[..end];
			AutocompletePanel.AddOption((int)FontHeight, oneString);

			if (end + 1 >= writeBuffer.Length)
				break;
			writeBuffer = writeBuffer[(end + 1)..];
		}
		lastAutocompleteStart = start;
	}
	int lastAutocompleteStart;

	public void MoveAutocompletePanel(int row, int column) {
		if (AutocompletePanel == null)
			return;

		AutocompletePanel.
		Position = new(((column - LeftCol) * FontWidth) + (FontWidth / 2), PaddingTop + (((row + 1) - TopRow) * FontHeight));
	}

	public void CloseAutocomplete() {
		if (IValidatable.IsValid(AutocompletePanel))
			AutocompletePanel.Remove();
	}

	/// <summary>
	/// The document's lines.
	/// Changing the list directly skips undo and events
	/// </summary>
	public SafeArray<string> Rows {
		get => core.Document.Lines;
		set => core.ModifyDocument(doc => doc.SetLines(value));
	}

	private bool _showGutter = true, _showDetails = true;

	public bool Multiline { get => core.MultiLine; set => core.MultiLine = value; }
	public bool TriggerExecuteOnEnter { get; set; } = false;
	public bool Readonly { get => core.ReadOnly; set => core.ReadOnly = value; }
	public bool ShowGutter {
		get => _showGutter;
		set {
			_showGutter = value;
			RefreshFromParams();
		}
	}
	public bool ShowDetails {
		get => _showDetails;
		set {
			_showDetails = value;
			RefreshFromParams();
		}
	}
	private void RefreshFromParams() {
		if (Gutter != null)
			Gutter.SetVisible(_showGutter);

		InvalidateLayout();
		InvalidateChildren(true, true);
	}


	public float TopRow { get; set; } = 0;
	public float LeftCol { get; set; } = 0;
	public float MaxVisibleRows { get; set; } = 0;
	public float MaxVisibleCols { get; set; } = 0;
	public float FontWidth { get; set; } = 0;
	public float FontHeight { get; set; } = 0;
	public float Width { get; set; } = 0;
	public float Height { get; set; } = 0;
	public float PaddingTop { get; set; } = 0;
	public float PaddingLeft { get; set; } = 0;
	public float CaretWidth { get; set; } = 2;
	public float GutterLength { get; set; } = 6;
	public float GutterPaddingRight { get; set; } = 2;

	public static ConVar textedit_fontwidthpad = new(nameof(textedit_fontwidthpad), "0", FCvar.Saved, "Additive offset to FontWidth");
	public static ConVar textedit_fontheightpad = new(nameof(textedit_fontheightpad), "1", FCvar.Saved, "Additive offset to FontHeight");

	protected override void PerformLayout(float width, float height) {
		base.PerformLayout(width, height);
		this.Width = width;
		this.Height = height;

		var s = Graphics2D.GetTextSize("W", Font, TextSize);
		FontWidth = s.X + textedit_fontwidthpad.GetInt();
		FontHeight = s.Y + textedit_fontheightpad.GetInt();

		MaxVisibleRows = MathF.Floor(Height / FontHeight);
		MaxVisibleCols = MathF.Floor(Width / FontWidth);
		PaddingTop = FontHeight * 0.3f;
		PaddingLeft = FontWidth * 1f;
		CaretWidth = MathF.Max(2, MathF.Round(FontWidth * 0.15f));
		GutterPaddingRight = FontWidth * 1f;

		if (this.SetScrollS.HasValue) {
			this.SetScroll(SetScrollS.Value);
			this.SetScrollS = null;
		}
	}

	private SyntaxHighlighter __highlighter = SyntaxHighlighter.Blank;
	public SyntaxHighlighter Highlighter {
		get => __highlighter;
		set {
			__highlighter = value;
			__highlighter.Rebuild(Rows);
		}
	}
	internal class TextEditorGutter(TextEditor editor) : Panel(editor)
	{
		// TODO: Move this to the TextEditorGutter class. This is easier for the event transition.
		public override void Paint(float width, float height) {
			editor.Gutter_PaintOverride(this, width, height);
		}
	}
	internal class TextEditorInternals(TextEditor editor) : Panel(editor)
	{
		// TODO: Move these to the TextEditorInternals class. This is easier for the event transition.
		protected override bool MouseClick(FrameState state, ButtonCode button) {
			editor.Editor_MouseClickEvent(this, state, button);
			return true;
		}
		protected override bool MouseRelease(Element self, FrameState state, ButtonCode button) => true;
		protected override bool MouseDrag(Element self, FrameState state, Vector2F delta) {
			editor.Editor_MouseDragEvent(this, state);
			return true;
		}
		protected override bool MouseScroll(Element self, FrameState state, Vector2F delta) => editor.Editor_MouseScrollEvent(state, delta);
		protected override bool KeyPressed(in KeyboardState keyboardState, ButtonCode key) => editor.Editor_OnKeyPressed(in keyboardState, key);
		protected override bool TextInput(in KeyboardState keyboardState, string text) {
			editor.Editor_OnTextInput(in keyboardState, text);
			return true;
		}
		protected override bool OnGainingKeyboardFocus(Element? lastFocus, ref Element? passTo) {
			editor.input.Blink.Reset();
			return base.OnGainingKeyboardFocus(lastFocus, ref passTo);
		}
		public override void Paint(float width, float height) {
			editor.Editor_PaintOverride(this, width, height);
		}
		protected override void OnThink() {
			editor.Editor_Thinking(this);
		}
		public override bool WantsTextInput => true;
	}

	public override bool WantsTextInput => true;
	public TextEditor(Element? parent) : base(parent) {
		input = new(core);
		core.Layout = this;
		core.TextChanged += Core_TextChanged;

		Gutter = new(this);
		Editor = new(this);
		VScrollbar = new Scrollbar(Editor);
		RefreshFromParams();

		Gutter.Dock = Dock.Left;
		Gutter.Size = new(64, 0);
		Editor.Dock = Dock.Fill;

		VScrollbar.Dock = Dock.Right;
		VScrollbar.OnScrolled += Scrollbar_OnScrolled;

		Editor.KeyboardInputMarshal = new HoldingKeyboardInputMarshal();

		SetFont("Consolas", 16);
	}

	private int chars;

	private void Core_TextChanged(TextEditingCore _) {
		Highlighter.Rebuild(Rows);
		chars = 0;
		foreach (var row in Rows)
			chars += row.Length;
		OnTextEdited?.Invoke(this);
	}

	private void Editor_OnTextInput(in KeyboardState state, string text) {
		input.TextInput(in state, text);
		PopulateAutocomplete();
		EnsureCaretVisible();
		OnTextInput?.Invoke(this, in state, text);
	}

	private void Scrollbar_OnScrolled(float value) {
		int maxRows = (int)MaxVisibleRows;

		float v = Math.Max(MathF.Round(value), 0);
		TopRow = Math.Clamp(v, 0, Math.Max(Rows.Count - maxRows, 0));
	}

	long lastAutoScroll;

	private void Editor_Thinking(Element self) {
		input.EndFrame();

		// The second part is a hack, but it allows mouse selecting autocomplete items.
		if (UI.GetKeyboardFocusedElement() != self && (!IValidatable.IsValid(UI.GetHoveredElement()) || !UI.GetHoveredElement()!.IsIndirectChildOf(self)))
			CloseAutocomplete();
		if (self.IsHovered())
			EngineCore.SetMouseCursor(MouseCursor.MOUSE_CURSOR_IBEAM);

		// Dragging past the edges scrolls
		if (self.IsDepressed(ButtonCode.MouseLeft) && Stopwatch.GetElapsedTime(lastAutoScroll).TotalSeconds > 0.05) {
			var local = self.GetMousePos();
			var bounds = self.GetRenderBounds();
			bool scrolled = false;
			if (local.Y < 0 && TopRow > 0) {
				TopRow--;
				scrolled = true;
			}
			else if (local.Y > bounds.H && TopRow < MaxTopRow) {
				TopRow++;
				scrolled = true;
			}
			if (local.X < 0 && LeftCol > 0) {
				LeftCol--;
				scrolled = true;
			}
			else if (local.X > bounds.W) {
				LeftCol++;
				scrolled = true;
			}
			if (scrolled) {
				lastAutoScroll = Stopwatch.GetTimestamp();
				Editor_MouseDragEvent(self, null);
			}
		}

		MoveAutocompletePanel(core.Primary.Caret.Line, lastAutocompleteStart);
	}

	float MaxTopRow => MathF.Max(0, Rows.Count - MaxVisibleRows);

	static readonly Color BACKGROUND_FOCUSED = new(20, 32, 25, 127);
	static readonly Color BACKGROUND_UNFOCUSED = new(20, 25, 32, 127);
	static readonly Color FOREGROUND_FOCUSED = new(85, 110, 95, 255);
	static readonly Color FOREGROUND_UNFOCUSED = new(85, 95, 110, 255);

	private float ColumnX(int column) => PaddingLeft + (column - LeftCol) * FontWidth;
	private float RowY(int row) => PaddingTop + (row - TopRow) * FontHeight;

	public void RenderRowPiece(int character, int row, string text, Color color) {
		Graphics2D.SetDrawColor(color);
		Graphics2D.DrawText(ColumnX(character), PaddingTop + (row * FontHeight) + 2, text, Font, TextSize);
	}

	private bool LineHasCaret(int line) {
		foreach (var selection in core.Selections) {
			if (selection.Caret.Line == line)
				return true;
		}
		return false;
	}

	private void Editor_PaintOverride(Element self, float width, float height) {
		int i = 0;
		int row = (int)TopRow;
		PreRenderEditorLines?.Invoke(this, width, height);
		SetBgColor(self.IsKeyboardFocused() ? BACKGROUND_FOCUSED : BACKGROUND_UNFOCUSED);
		SetFgColor(self.IsKeyboardFocused() ? FOREGROUND_FOCUSED : FOREGROUND_UNFOCUSED);
		bool focused = this.Editor.IsKeyboardFocused();

		while (i < MaxVisibleRows) {
			if (row >= Rows.Count)
				break;

			var y = PaddingTop + ((i) * FontHeight);

			Color rowColor;
			if (focused && LineHasCaret(row))
				rowColor = new Color(42, 45, 50, 155);
			else if (i % 2 == 1)
				rowColor = new Color(2, 5, 10, 155);
			else
				rowColor = new Color(13, 16, 22, 155);
			Graphics2D.SetDrawColor(rowColor);
			Graphics2D.DrawRectangle(0, y, width, FontHeight);

			row += 1;
			i += 1;
		}

		// Selections go under the text
		if (focused || core.HasSelection) {
			Graphics2D.SetDrawColor(97, 137, 200, focused ? 140 : 70);
			foreach (var selection in core.Selections) {
				if (selection.IsEmpty)
					continue;
				int first = Math.Max(selection.Start.Line, (int)TopRow);
				int last = Math.Min(selection.End.Line, (int)(TopRow + MaxVisibleRows));
				for (int line = first; line <= last; line++) {
					int startColumn = line == selection.Start.Line ? selection.Start.Column : 0;
					int endColumn = line == selection.End.Line ? selection.End.Column : Rows[line]?.Length ?? 0;
					float rectWidth = (endColumn - startColumn) * FontWidth;
					// a bit extra for the line break
					if (line != selection.End.Line)
						rectWidth += FontWidth * 0.5f;
					Graphics2D.DrawRectangle(ColumnX(startColumn), RowY(line), rectWidth, FontHeight);
				}
			}
		}

		i = 0;
		row = (int)TopRow;

		while (i < MaxVisibleRows) {
			if (row >= Rows.Count)
				break;

			var y = PaddingTop + ((i) * FontHeight);

			var xOffset = 0;

			int whitespace = 0;
			var decs = Highlighter.Rows[row];
			if (decs != null) {
				for (int ip = 0; ip < decs.Count; ip++) {
					ref RowDecorator dec = ref decs[ip];
					RenderRowPiece(xOffset, i, dec.Text, dec.Color);
					string txt = dec.Text;
					int txtlen = txt.Length;
					xOffset += txtlen;

					for (int ti = 0; ti < txtlen; ti++) {
						if (!char.IsWhiteSpace(txt[ti]))
							break;
						whitespace++;
					}
				}
			}

			var whitespaceCounter = MathF.Ceiling((whitespace - 1f) / 4);

			Graphics2D.SetDrawColor(130, 135, 140, 60);
			for (int wi = 0; wi < whitespaceCounter; wi++) {
				var xV = ColumnX(wi * 4) + (FontWidth * 0.5f);
				Graphics2D.DrawLine(new(xV, y), new(xV, y + FontHeight), 1);
			}

			row += 1;
			i += 1;
		}

		if (focused && input.Blink.Visible) {
			Graphics2D.SetDrawColor(255, 255, 255, 255);
			foreach (var selection in core.Selections) {
				var caret = selection.Caret;
				if (caret.Line < TopRow || caret.Line >= TopRow + MaxVisibleRows)
					continue;
				Graphics2D.DrawRectangle(MathF.Round(ColumnX(caret.Column)), RowY(caret.Line), CaretWidth, FontHeight);
			}
		}

		if (_showDetails) {
			Graphics2D.SetDrawColor(Highlighter.Color);
			var selection = core.Primary;
			string linePart;
			if (selection.Anchor.Line != selection.Caret.Line)
				linePart = $"selected: {selection.Start.Line} - {selection.End.Line}";
			else
				linePart = $"line {selection.Caret.Line} / {Rows.Count}";
			if (core.HasMultipleCarets)
				linePart += $" - {core.Selections.Count} carets";
			Graphics2D.DrawText(width - 4, height - 2, $"{StringExtensions.FormatNumberByThousands(chars)} chars - {linePart} - {Highlighter.Name}", Font, 14, Anchor.BottomRight);
		}
	}

	private TextPosition MouseToPosition(Element self, out float layoutX) {
		Vector2F local = self.GetMousePos();
		var pos = CalculatePos(local.X, local.Y);
		layoutX = local.X - PaddingLeft + LeftCol * FontWidth;
		return new(pos.EndRow, pos.EndCol);
	}

	private void Editor_MouseDragEvent(Element self, FrameState? state) {
		var pos = MouseToPosition(self, out float x);
		input.PointerDrag(pos, x, state?.Keyboard.AltDown ?? false);
	}

	/// <summary>
	/// Element-local point to a row/column, clamped onto the text
	/// </summary>
	public TextEditorCaret CalculatePos(float x, float y) {
		x = x - PaddingLeft;
		y = y - PaddingTop;

		int row = (int)MathF.Floor(y / FontHeight + TopRow);
		row = Math.Clamp(row, 0, Rows.Count - 1);
		int column = (int)MathF.Round(x / FontWidth + LeftCol);
		column = Math.Clamp(column, 0, core.Document.GetLine(row).Length);
		return new(row, column);
	}
	public TextEditorCaret CalculatePos(Vector2F point) => CalculatePos(point.X, point.Y);

	public bool DoesLineOverflowTop(int line) {
		if (Rows.Count <= (int)MaxVisibleRows)
			return false;
		return line < TopRow;
	}

	public bool DoesLineOverflowBottom(int line) {
		int maxRows = (int)MaxVisibleRows;
		if (Rows.Count <= maxRows)
			return false;
		return line > (TopRow + (maxRows - 1));
	}

	public void ScrollToLine(int line, float ratio) {
		int currentRows = Rows.Count;
		int maxRows = (int)MaxVisibleRows;

		// cannot scroll anyway
		if (currentRows <= maxRows) {
			TopRow = 0;
			return;
		}

		TopRow = Math.Clamp(line - (int)((maxRows - 1) * ratio), 0, MaxTopRow);
	}
	public void MoveTopToLine(int line) => ScrollToLine(line, 0);
	public void MoveCenterToLine(int line) => ScrollToLine(line, 0.5f);
	public void MoveBottomToLine(int line) => ScrollToLine(line, 1);

	public enum LineOverflow
	{
		Above = -1,
		Within = 0,
		Below = 1
	}
	public enum SideOverflow
	{
		Left = -1,
		Within = 0,
		Right = 1
	}
	public LineOverflow GetLineOverflow(int targLine) {
		int topRow = (int)TopRow;
		int bottomRow = (int)(TopRow + MaxVisibleRows);

		if (NMath.InRange(targLine, topRow, bottomRow - 1))
			return LineOverflow.Within;

		return targLine < topRow ? LineOverflow.Above : LineOverflow.Below;
	}

	float VisibleColumns {
		get {
			float width = Editor.GetRenderBounds().W - PaddingLeft;
			if (VScrollbar.IsVisible())
				width -= VScrollbar.GetRenderBounds().W;
			return MathF.Max(1, MathF.Floor(width / MathF.Max(1, FontWidth)) - 1);
		}
	}

	public void EnsureCaretVisible() {
		var caret = core.Primary.Caret;
		if (MaxVisibleRows > 0) {
			if (caret.Line < TopRow)
				TopRow = caret.Line;
			else if (caret.Line >= TopRow + MaxVisibleRows)
				TopRow = caret.Line - MaxVisibleRows + 1;
			TopRow = Math.Clamp(TopRow, 0, MaxTopRow);
		}

		float columns = VisibleColumns;
		if (caret.Column < LeftCol)
			LeftCol = caret.Column;
		else if (caret.Column > LeftCol + columns)
			LeftCol = caret.Column - columns;
		LeftCol = MathF.Max(0, LeftCol);
	}

	public delegate bool TextEditorKeyInterceptFn(TextEditor self, in KeyboardState state, ButtonCode key);

	/// <summary>
	/// Gets keys before the editor does, true eats the key.
	/// </summary>
	public TextEditorKeyInterceptFn? InterceptKey;

	private bool Editor_OnKeyPressed(in KeyboardState state, ButtonCode key) {
		if (InterceptKey != null && InterceptKey(this, in state, key))
			return true;

		bool handled = false;
		bool enter = key == ButtonCode.KeyEnter || key == ButtonCode.KeyPadEnter;

		if (IsAutocompleteActive && (key is ButtonCode.KeyUp or ButtonCode.KeyDown or ButtonCode.KeyTab or ButtonCode.KeyEscape || enter)) {
			if (key == ButtonCode.KeyUp)
				AutocompletePanel.MoveSelectUp();
			else if (key == ButtonCode.KeyDown)
				AutocompletePanel.MoveSelectDown();
			else if (key == ButtonCode.KeyEscape)
				CloseAutocomplete();
			else if (AutocompleteOption is string option)
				AutocompletePanel_Selected(option);
			handled = true;
		}
		else if (enter && !Multiline) {
			if (TriggerExecuteOnEnter) {
				OnExecute?.Invoke(this);
				SetText("");
			}
			else
				// We release focus instead.
				KeyboardUnfocus();
			handled = true;
		}
		else if (key == ButtonCode.KeyTab && OnTab != null && !state.ShiftDown && !state.ControlDown) {
			OnTab.Invoke(this);
			handled = true;
		}
		else if (key == ButtonCode.KeyS && state.ControlDown && !state.AltDown) {
			OnSave?.Invoke(this);
			handled = true;
		}
		else if (key == ButtonCode.KeyP && state.AltDown && !state.ControlDown) {
			OnExecute?.Invoke(this);
			handled = true;
		}
		else {
			handled = input.KeyPressed(in state, key);
			if (handled && key is ButtonCode.KeyBackspace or ButtonCode.KeyDelete)
				PopulateAutocomplete();
		}

		if (handled)
			EnsureCaretVisible();

		OnKeyPressed?.Invoke(this, in state, key);

		// Let these go up to the parent if we didn't use them
		if (!handled && key is ButtonCode.KeyTab or ButtonCode.KeyEscape)
			return false;
		return true;
	}

	public delegate void OnVoid(TextEditor self);
	public event OnVoid? OnTextEdited;
	public event OnVoid? OnSave;
	public event OnVoid? OnExecute;
	public event OnVoid? OnTab;

	protected override void OnThink() {
		base.OnThink();
		if (MaxVisibleRows > 0) {
			VScrollbar.Scroll = TopRow;
			VScrollbar.Update(new(0, Rows.Count), new(0, MaxVisibleRows));
			VScrollbar.ScrollDelta = 3;
		}
	}

	float MaxLeftCol {
		get {
			int longest = 0;
			foreach (var row in Rows)
				longest = Math.Max(longest, row.Length);
			return MathF.Max(0, longest - VisibleColumns);
		}
	}

	private bool Editor_MouseScrollEvent(FrameState state, Vector2F delta) {
		// Shift scrolls sideways
		if (state.Keyboard.ShiftDown && delta.X == 0)
			delta = new(delta.Y, 0);

		bool used = false;
		if (delta.Y != 0 && MaxTopRow > 0) {
			TopRow = Math.Clamp(TopRow - delta.Y * 4, 0, MaxTopRow);
			used = true;
		}
		if (delta.X != 0) {
			float maxLeft = MathF.Max(MaxLeftCol, LeftCol);
			if (maxLeft > 0) {
				LeftCol = Math.Clamp(LeftCol - delta.X * 4, 0, maxLeft);
				used = true;
			}
		}
		// Nothing to scroll, let the parent have it
		return used;
	}

	private void Editor_MouseClickEvent(Element self, FrameState state, ButtonCode button) {
		CloseAutocomplete();
		if (button != ButtonCode.MouseLeft)
			return;

		self.KeyboardFocus();
		var pos = MouseToPosition(self, out _);
		input.PointerDown(pos, state.Keyboard.ShiftDown, state.Keyboard.ControlDown, state.Keyboard.AltDown);
		EnsureCaretVisible();
	}

	public void UndoBackwards() {
		core.Undo();
		EnsureCaretVisible();
	}
	public void UndoForwards() {
		core.Redo();
		EnsureCaretVisible();
	}

	public void ResetUndoStack() => core.ResetUndo();

	public void SetText(string? text) {
		core.SetText((text ?? "").Replace("\t", new string(' ', core.TabSize)));
		core.SetCaret(core.Document.Start);
		TopRow = 0;
		LeftCol = 0;
	}
	public void RemoveLine(int index = 0) {
		core.ModifyDocument(document => {
			if (index >= 0 && index < document.LineCount)
				document.Lines.RemoveAt(index);
		});
	}
	public void AppendLine(string text) {
		core.ModifyDocument(document => {
			if (document.LineCount > 0 && document.GetLine(document.LineCount - 1).Length <= 0)
				document.Lines[document.LineCount - 1] = text;
			else
				document.Lines.Add(text);
		});
	}
	public string GetText() => core.GetText();

	/// <summary>
	/// Sets the primary selection with (col, row) as the anchor, dropping other carets.
	/// </summary>
	public void SetCaret(int col, int row, int? endCol = null, int? endRow = null) {
		var anchor = new TextPosition(Math.Max(0, row), Math.Max(0, col));
		var caret = new TextPosition(Math.Max(0, endRow ?? row), Math.Max(0, endCol ?? col));
		core.SetSelection(new(anchor, caret));
	}
	private float? SetScrollS = null;

	public delegate void TextEditorKeyPressedFn(TextEditor self, in KeyboardState state, ButtonCode button);
	public delegate void TextEditorTextInputFn(TextEditor self, in KeyboardState state, string inText);

	public event TextEditorKeyPressedFn? OnKeyPressed;
	public event TextEditorTextInputFn? OnTextInput;

	public void SetScroll(float percent) {
		if (MaxVisibleRows <= 0) {
			this.SetScrollS = percent;
			return;
		}
		TopRow = MaxTopRow * Math.Clamp(percent, 0, 1);
	}

	public enum SkipMode
	{
		KeyboardSkip,
		MouseClickSkip,
	}

	/// <summary>
	/// Column of the previous word boundary, like Ctrl+Left
	/// </summary>
	public static int? GetSkipGroupLeft(string row, int column, SkipMode mode) {
		var core = new TextEditingCore();
		core.Document.SetLines([row]);
		return core.WordLeft(new(0, Math.Clamp(column, 0, row.Length))).Column;
	}
	/// <summary>
	/// Column of the next word boundary, like Ctrl+Right
	/// </summary>
	public static int? GetSkipGroupRight(string row, int column, SkipMode mode) {
		var core = new TextEditingCore();
		core.Document.SetLines([row]);
		return core.WordRight(new(0, Math.Clamp(column, 0, row.Length))).Column;
	}

	public TextEditorCaret GetCaretBottom() {
		var end = core.Primary.End;
		return new(end.Line, end.Column);
	}
	public TextEditorCaret GetCaretBottomRight() => GetCaretBottom();
	public TextEditorCaret GetCaretTop() {
		var start = core.Primary.Start;
		return new(start.Line, start.Column);
	}
	public TextEditorCaret GetCaretTopLeft() => GetCaretTop();

	public void SetSelection(int startCol, int startRow, int endCol, int endRow) => SetCaret(startCol, startRow, endCol, endRow);
	public bool HasSelection() => core.HasSelection;

	public string GetSelection() {
		var selection = core.Primary;
		return selection.IsEmpty ? "" : core.Document.GetText(selection.Start, selection.End);
	}

	public bool InsertText(string txt) {
		if (Readonly || txt == null)
			return false;
		bool done = core.Insert(txt);
		EnsureCaretVisible();
		return done;
	}
	public bool DeleteSelection() => core.DeleteSelection();

	protected override bool OnGainingKeyboardFocus(Element? lastFocus, ref Element? passTo) {
		passTo = Editor;
		return base.OnGainingKeyboardFocus(lastFocus, ref passTo);
	}

	private void Gutter_PaintOverride(Element self, float width, float height) {
		Graphics2D.SetDrawColor(40, 50, 55, 155);
		Graphics2D.DrawRectangle(0, 0, width, height);
		bool focused = this.Editor.IsKeyboardFocused();

		int i = 0, row = (int)TopRow;
		while (i < MaxVisibleRows) {
			if (Rows[row] == null)
				break;

			float y = PaddingTop + i * FontHeight;

			Color rowColor;
			if (focused && LineHasCaret(row))
				rowColor = new Color(90, 100, 110, 155);
			else if (i % 2 == 1)
				rowColor = new Color(4, 7, 10, 125);
			else
				rowColor = new Color(24, 30, 40, 125);
			Graphics2D.SetDrawColor(rowColor);
			Graphics2D.DrawRectangle(0, y, width, FontHeight);

			row += 1;
			i += 1;
		}

		i = 0;
		row = (int)TopRow;

		while (i < MaxVisibleRows) {
			if (Rows[row] == null)
				break;

			var str = (row + 1).ToString();

			float y = PaddingTop + i * FontHeight;
			float x = ((width / FontWidth) - str.Length) * FontWidth - GutterPaddingRight;

			Graphics2D.SetDrawColor(220, 220, 220, 255);
			Graphics2D.DrawText(x, y + 2, str, Font, TextSize);

			row += 1;
			i += 1;
		}
	}

	public void SetFont(ReadOnlySpan<char> fontName, float fontSize) {
		__Font.SetUserValue(new(fontName.SliceNullTerminatedString()));
		__TextSize.SetUserValue(fontSize);

		var s = Graphics2D.GetTextSize("W", fontName, fontSize);
		FontWidth = s.X;
		FontHeight = s.Y;

		MaxVisibleRows = MathF.Floor(Height / FontHeight - 1);
		PaddingTop = FontHeight * 0.3f;
		PaddingLeft = FontWidth * 1f;
		GutterPaddingRight = FontWidth * 1f;
	}

	public override void Paint(float width, float height) {
		Graphics2D.SetDrawColor(10, 15, 20, 190);
		Graphics2D.DrawRectangle(0, 0, width, height);
	}

	#region ITextLayout

	TextPosition ITextLayout.MoveVertically(TextPosition from, int lines, ref float? preferredX) {
		preferredX ??= from.Column * FontWidth;
		int target = from.Line + lines;
		if (target < 0) return core.Document.Start;
		if (target >= Rows.Count) return core.Document.End;
		int column = (int)MathF.Round(preferredX.Value / MathF.Max(1, FontWidth));
		return new(target, Math.Clamp(column, 0, core.Document.GetLine(target).Length));
	}

	int ITextLayout.PageLines => Math.Max(1, (int)MaxVisibleRows - 1);

	float ITextLayout.GetX(TextPosition position) => position.Column * FontWidth;

	TextPosition ITextLayout.PositionAt(int line, float x) {
		line = Math.Clamp(line, 0, Rows.Count - 1);
		int column = (int)MathF.Round(x / MathF.Max(1, FontWidth));
		return new(line, Math.Clamp(column, 0, core.Document.GetLine(line).Length));

	}

	#endregion
}
