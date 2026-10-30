using System.Diagnostics;

namespace Nucleus.UI;

/// <summary>
/// Where things are on screen
/// </summary>
public interface ITextLayout
{
	/// <summary>
	/// Moves up/down by visual lines (wrapped lines count), keeping <paramref name="preferredX"/> where it can.
	/// </summary>
	TextPosition MoveVertically(TextPosition from, int lines, ref float? preferredX);
	int PageLines { get; }
	float GetX(TextPosition position);
	TextPosition PositionAt(int line, float x);
}

public enum TextMovement
{
	Left,
	Right,
	WordLeft,
	WordRight,
	Up,
	Down,
	PageUp,
	PageDown,
	Home,
	End,
	DocumentStart,
	DocumentEnd
}

public enum TextEditKind
{
	Other,
	Typing,
	Deleting
}

/// <summary>
/// Text editing logic shared by <see cref="Textbox"/> and <see cref="TextEditor"/>
/// </summary>
public class TextEditingCore
{
	public TextDocument Document { get; } = new();
	public ITextLayout? Layout { get; set; }

	public bool MultiLine { get; set; } = true;
	public bool ReadOnly { get; set; }
	/// <summary>
	/// 0 means no limit
	/// </summary>
	public int MaxLength { get; set; }
	public int TabSize { get; set; } = 4;
	/// <summary>
	/// New lines copy the indentation of the line they came from
	/// </summary>
	public bool AutoIndent { get; set; }
	/// <summary>
	/// Copy/cut with nothing selected takes the whole line, multi-line only
	/// </summary>
	public bool CopyLineWhenEmpty { get; set; } = true;
	public bool AllowCopy { get; set; } = true;
	public bool UndoEnabled { get; set; } = true;
	public int MaxUndo { get; set; } = 200;
	/// <summary>
	/// Typing/deleting within this many seconds of the last one merges into the same undo step
	/// </summary>
	public static double UndoCoalesceTime = 1.5;

	public event Action<TextEditingCore>? TextChanged;
	public event Action<TextEditingCore>? SelectionChanged;

	readonly List<TextSelection> selections = [new(default(TextPosition))];
	int primary;

	public IReadOnlyList<TextSelection> Selections => selections;
	public int PrimaryIndex => primary;
	public TextSelection Primary => selections[primary];
	public bool HasMultipleCarets => selections.Count > 1;
	public bool HasSelection {
		get {
			foreach (var selection in selections) {
				if (!selection.IsEmpty)
					return true;
			}
			return false;
		}
	}

	public string GetText() => Document.GetText();
	private string GetLine(int line) => Document.GetLine(line);
	int LastLine => Document.LineCount - 1;

	#region Selections

	public void SetSelection(TextSelection selection) {
		selections.Clear();
		selections.Add(selection);
		primary = 0;
		boxActive = false;
		SelectionMoved();
	}

	public void SetCaret(TextPosition position) => SetSelection(new(position));

	/// <summary>
	/// Replaces the primary selection without touching the other carets.
	/// </summary>
	public void SetPrimary(TextSelection selection) {
		selections[primary] = selection;
		SelectionMoved();
	}

	public void SetSelections(IEnumerable<TextSelection> list, int primaryIndex) {
		selections.Clear();
		selections.AddRange(list);
		if (selections.Count == 0)
			selections.Add(new(Document.End));
		primary = Math.Clamp(primaryIndex, 0, selections.Count - 1);
		boxActive = false;
		SelectionMoved();
	}

	public void SelectAll() => SetSelection(new(Document.Start, Document.End));
	public void SelectWordAt(TextPosition position) {
		var range = WordRangeAt(position);
		SetSelection(new(range.Start, range.End));
	}
	public void SelectLineAt(TextPosition position) {
		var range = LineRangeAt(position);
		SetSelection(new(range.Start, range.End));
	}

	private void SelectionMoved() {
		Normalize();
		BreakUndoGroup();
		SelectionChanged?.Invoke(this);
	}

	/// <summary>
	/// Clamps, sorts and merges overlapping selections
	/// </summary>
	private void Normalize() {
		for (int i = 0; i < selections.Count; i++) {
			var selection = selections[i];
			selection.Anchor = Document.Clamp(selection.Anchor);
			selection.Caret = Document.Clamp(selection.Caret);
			selections[i] = selection;
		}

		if (selections.Count == 1) {
			primary = 0;
			return;
		}

		var primarySel = selections[primary];
		var tagged = new List<(TextSelection Sel, bool Primary)>(selections.Count);
		for (int i = 0; i < selections.Count; i++)
			tagged.Add((selections[i], i == primary));
		tagged.Sort((a, b) => a.Sel.Start.CompareTo(b.Sel.Start));

		selections.Clear();
		primary = 0;
		var current = tagged[0];
		for (int i = 1; i < tagged.Count; i++) {
			var next = tagged[i];
			bool overlaps = next.Sel.Start < current.Sel.End || next.Sel.Start == current.Sel.Start
				|| ((current.Sel.IsEmpty || next.Sel.IsEmpty) && next.Sel.Start == current.Sel.End);

			if (overlaps) {
				var start = TextPosition.Min(current.Sel.Start, next.Sel.Start);
				var end = TextPosition.Max(current.Sel.End, next.Sel.End);
				bool reversed = current.Sel.Caret < current.Sel.Anchor;
				var merged = reversed ? new TextSelection(end, start) : new TextSelection(start, end);
				current = (merged, current.Primary || next.Primary);
			}
			else {
				if (current.Primary)
					primary = selections.Count;
				selections.Add(current.Sel);
				current = next;
			}
		}
		if (current.Primary)
			primary = selections.Count;
		selections.Add(current.Sel);
	}

	#endregion

	#region Editing engine

	public struct Edit(TextPosition start, TextPosition end, string text)
	{
		public TextPosition Start = start;
		public TextPosition End = end;
		public string Text = text;
		public bool SelectInserted;
		public bool Keep;
	}

	/// <summary>
	/// Runs <paramref name="make"/> for every selection and applies all the edits as one change.
	/// </summary>
	/// <param name="make">null leaves that selection alone</param>
	public bool ApplyEdits(Func<TextSelection, int, Edit?> make, TextEditKind kind) {
		if (ReadOnly)
			return false;

		Normalize();
		var linesBefore = Document.Lines.ToArray();
		var selBefore = selections.ToArray();
		int primaryBefore = primary;
		var starts = Document.GetLineStarts();

		int count = selections.Count;
		var edits = new (int S, int E, string? Text, bool Select, bool Keep, int Anchor, int Caret)[count];
		int prevEnd = 0;
		bool changed = false;
		for (int i = 0; i < count; i++) {
			var sel = selections[i];
			int anchor = ToOffset(starts, sel.Anchor);
			int caret = ToOffset(starts, sel.Caret);
			var edit = make(sel, i);
			if (edit is not Edit change) {
				edits[i] = (caret, caret, null, false, true, anchor, caret);
				continue;
			}

			int start = ToOffset(starts, Document.Clamp(TextPosition.Min(change.Start, change.End)));
			int end = ToOffset(starts, Document.Clamp(TextPosition.Max(change.Start, change.End)));
			// Edits from different carets can't overlap
			start = Math.Max(start, prevEnd);
			end = Math.Max(end, start);
			prevEnd = end;
			string text = TextDocument.Normalize(change.Text ?? "");
			if (end > start || text.Length > 0)
				changed = true;
			edits[i] = (start, end, text, change.SelectInserted, change.Keep, anchor, caret);
		}

		if (!changed)
			return false;

		// back to front, earlier offsets don't shift
		for (int i = count - 1; i >= 0; i--) {
			var edit = edits[i];
			if (edit.Text == null || (edit.E == edit.S && edit.Text.Length == 0))
				continue;
			Document.Replace(Document.GetPosition(starts, edit.S), Document.GetPosition(starts, edit.E), edit.Text);
		}

		var newStarts = Document.GetLineStarts();
		int delta = 0;
		for (int i = 0; i < count; i++) {
			var edit = edits[i];
			int removed = edit.E - edit.S;
			int added = edit.Text?.Length ?? 0;
			int newS = edit.S + delta;

			TextSelection sel;
			if (edit.Text == null || edit.Keep)
				sel = new(FromOffset(newStarts, Remap(edit.Anchor, delta, edit, removed, added, newS)), FromOffset(newStarts, Remap(edit.Caret, delta, edit, removed, added, newS)));
			else if (edit.Select)
				sel = new(FromOffset(newStarts, newS), FromOffset(newStarts, newS + added));
			else
				sel = new(FromOffset(newStarts, newS + added));

			selections[i] = sel;
			delta += added - removed;
		}

		boxActive = false;
		Normalize();
		RecordUndo(linesBefore, selBefore, primaryBefore, kind);
		TextChanged?.Invoke(this);
		SelectionChanged?.Invoke(this);
		return true;
	}

	private static int Remap(int offset, int delta, (int S, int E, string? Text, bool Select, bool Keep, int Anchor, int Caret) edit, int removed, int added, int newS) {
		if (offset < edit.S) return offset + delta;
		if (offset >= edit.E) return offset + delta + added - removed;
		return newS + added;
	}

	private static int ToOffset(int[] starts, TextPosition position) => starts[Math.Clamp(position.Line, 0, starts.Length - 1)] + position.Column;
	private TextPosition FromOffset(int[] starts, int offset) => Document.GetPosition(starts, offset);

	/// <summary>
	/// Swaps the whole document out in one undo step.
	/// </summary>
	private bool ReplaceAll(IEnumerable<string> newLines, IEnumerable<TextSelection> newSelections, int newPrimary, TextEditKind kind = TextEditKind.Other) {
		if (ReadOnly)
			return false;

		var linesBefore = Document.Lines.ToArray();
		var selBefore = selections.ToArray();
		int primaryBefore = primary;

		Document.SetLines(newLines);
		selections.Clear();
		selections.AddRange(newSelections);
		primary = Math.Clamp(newPrimary, 0, selections.Count - 1);
		boxActive = false;
		Normalize();

		RecordUndo(linesBefore, selBefore, primaryBefore, kind);
		BreakUndoGroup();
		TextChanged?.Invoke(this);
		SelectionChanged?.Invoke(this);
		return true;
	}

	/// <summary>
	/// Replaces everything as one undo step, with the caret at the end.
	/// </summary>
	public void SetText(string? text) {
		var linesBefore = Document.Lines.ToArray();
		var selBefore = selections.ToArray();
		int primaryBefore = primary;

		text = TextDocument.Normalize(text ?? "");
		if (!MultiLine)
			text = text.Replace("\n", "");
		Document.SetText(text);

		selections.Clear();
		selections.Add(new(Document.End));
		primary = 0;
		boxActive = false;

		RecordUndo(linesBefore, selBefore, primaryBefore, TextEditKind.Other);
		BreakUndoGroup();
		TextChanged?.Invoke(this);
		SelectionChanged?.Invoke(this);
	}

	#endregion

	#region Undo

	class UndoEntry
	{
		public required string[] LinesBefore;
		public required string[] LinesAfter;
		public required TextSelection[] SelBefore;
		public required TextSelection[] SelAfter;
		public int PrimaryBefore;
		public int PrimaryAfter;
		public TextEditKind Kind;
		public long Stamp;
	}

	readonly List<UndoEntry> undo = [];
	readonly List<UndoEntry> redo = [];
	bool undoGroupOpen;

	public int UndoCount => undo.Count;
	public int RedoCount => redo.Count;

	private void RecordUndo(string[] linesBefore, TextSelection[] selBefore, int primaryBefore, TextEditKind kind) {
		if (!UndoEnabled || ReadOnly)
			return;

		redo.Clear();
		long now = Stopwatch.GetTimestamp();
		var linesAfter = Document.Lines.ToArray();
		var selAfter = selections.ToArray();

		if (undoGroupOpen && kind != TextEditKind.Other && undo.Count > 0) {
			var last = undo[undo.Count - 1];
			if (last.Kind == kind && Stopwatch.GetElapsedTime(last.Stamp).TotalSeconds < UndoCoalesceTime) {
				last.LinesAfter = linesAfter;
				last.SelAfter = selAfter;
				last.PrimaryAfter = primary;
				last.Stamp = now;
				return;
			}
		}

		undo.Add(new() {
			LinesBefore = linesBefore,
			LinesAfter = linesAfter,
			SelBefore = selBefore,
			SelAfter = selAfter,
			PrimaryBefore = primaryBefore,
			PrimaryAfter = primary,
			Kind = kind,
			Stamp = now
		});
		if (undo.Count > MaxUndo)
			undo.RemoveAt(0);
		undoGroupOpen = kind != TextEditKind.Other;
	}

	public void BreakUndoGroup() => undoGroupOpen = false;

	public void ResetUndo() {
		undo.Clear();
		redo.Clear();
		undoGroupOpen = false;
	}

	public bool Undo() {
		if (ReadOnly || undo.Count == 0)
			return false;
		var entry = undo[undo.Count - 1];
		undo.RemoveAt(undo.Count - 1);
		redo.Add(entry);
		Restore(entry.LinesBefore, entry.SelBefore, entry.PrimaryBefore);
		return true;
	}

	public bool Redo() {
		if (ReadOnly || redo.Count == 0)
			return false;
		var entry = redo[redo.Count - 1];
		redo.RemoveAt(redo.Count - 1);
		undo.Add(entry);
		Restore(entry.LinesAfter, entry.SelAfter, entry.PrimaryAfter);
		return true;
	}

	private void Restore(string[] lines, TextSelection[] savedSelections, int savedPrimary) {
		Document.SetLines(lines);
		selections.Clear();
		selections.AddRange(savedSelections);
		primary = Math.Clamp(savedPrimary, 0, selections.Count - 1);
		boxActive = false;
		undoGroupOpen = false;
		Normalize();
		TextChanged?.Invoke(this);
		SelectionChanged?.Invoke(this);
	}

	#endregion

	#region Commands

	private string PrepareInsert(string text) {
		text = TextDocument.Normalize(text).Replace("\t", new string(' ', TabSize));
		if (!MultiLine)
			text = text.Replace("\n", "");
		return text;
	}

	private int SelectedLength(TextSelection selection) => selection.IsEmpty ? 0 : Document.GetText(selection.Start, selection.End).Length;


	public bool Type(string text) {
		if (string.IsNullOrEmpty(text))
			return false;
		text = PrepareInsert(text);

		if (MaxLength > 0) {
			int selected = 0;
			foreach (var s in selections)
				selected += SelectedLength(s);
			int room = Math.Max(0, MaxLength - (Document.Length - selected)) / selections.Count;
			if (text.Length > room)
				text = text[..room];
		}

		if (text.Contains('\n'))
			BreakUndoGroup();
		return ApplyEdits((s, _) => new Edit(s.Start, s.End, text), TextEditKind.Typing);
	}

	/// <summary>
	/// Inserts at every caret, ignoring MaxLength and typing merges.
	/// </summary>
	public bool Insert(string text) {
		if (string.IsNullOrEmpty(text))
			return false;
		text = PrepareInsert(text);
		BreakUndoGroup();
		bool done = ApplyEdits((s, _) => new Edit(s.Start, s.End, text), TextEditKind.Other);
		BreakUndoGroup();
		return done;
	}

	/// <summary>
	/// Changes the document directly, even when <see cref="ReadOnly"/>. Not undoable.
	/// </summary>
	public void ModifyDocument(Action<TextDocument> change) {
		change(Document);
		if (Document.LineCount == 0)
			Document.SetLines([""]);
		Normalize();
		TextChanged?.Invoke(this);
	}

	public bool DeleteSelection()
		=> ApplyEdits((s, _) => s.IsEmpty ? null : new Edit(s.Start, s.End, ""), TextEditKind.Other);

	public bool Backspace(bool word) {
		return ApplyEdits((selection, _) => {
			if (!selection.IsEmpty)
				return new Edit(selection.Start, selection.End, "");

			var caret = selection.Caret;
			TextPosition start;
			if (word)
				start = WordLeft(caret);
			else if (caret.Column == 0) {
				if (caret.Line == 0)
					return null;
				start = new(caret.Line - 1, GetLine(caret.Line - 1).Length);
			}
			else {
				string line = GetLine(caret.Line);
				// In the indentation, go back to the previous tab stop
				if (TabSize > 1 && line.AsSpan(0, caret.Column).IndexOfAnyExcept(' ') == -1)
					start = new(caret.Line, (caret.Column - 1) / TabSize * TabSize);
				else
					start = new(caret.Line, caret.Column - 1);
			}
			return new Edit(start, caret, "");
		}, TextEditKind.Deleting);
	}

	public bool Delete(bool word) {
		return ApplyEdits((selection, _) => {
			if (!selection.IsEmpty)
				return new Edit(selection.Start, selection.End, "");

			var caret = selection.Caret;
			int length = GetLine(caret.Line).Length;
			TextPosition end;
			if (word)
				end = WordRight(caret);
			else if (caret.Column < length)
				end = new(caret.Line, caret.Column + 1);
			else if (caret.Line < LastLine)
				end = new(caret.Line + 1, 0);
			else
				return null;
			return new Edit(caret, end, "");
		}, TextEditKind.Deleting);
	}

	public bool NewLine() {
		if (!MultiLine)
			return false;
		BreakUndoGroup();
		bool done = ApplyEdits((selection, _) => {
			string indent = "";
			if (AutoIndent) {
				string line = GetLine(selection.Start.Line);
				int indentWidth = line.AsSpan().IndexOfAnyExcept(' ');
				if (indentWidth == -1)
					indentWidth = line.Length;
				indent = new string(' ', Math.Min(indentWidth, selection.Start.Column));
			}
			return new Edit(selection.Start, selection.End, "\n" + indent);
		}, TextEditKind.Other);
		BreakUndoGroup();
		return done;
	}

	/// <summary>
	/// Tab inserts spaces to the next tab stop or indents a multi-line selection, shift always outdents
	/// </summary>
	public bool Tab(bool shift) {
		bool spansLines = false;
		foreach (var selection in selections) {
			if (selection.Start.Line != selection.End.Line)
				spansLines = true;
		}

		if (shift || spansLines)
			return ChangeIndent(shift ? -1 : 1);

		return ApplyEdits((s, _) => new Edit(s.Start, s.End, new string(' ', TabSize - (s.Start.Column % TabSize))),
			TextEditKind.Typing);
	}

	/// <summary>
	/// Lines touched by any selection. A selection ending at column 0 doesn't count that line.
	/// </summary>
	private SortedSet<int> TouchedLines() {
		var lines = new SortedSet<int>();
		foreach (var selection in selections) {
			int last = selection.End.Line;
			if (selection.End.Line > selection.Start.Line && selection.End.Column == 0)
				last--;
			for (int line = selection.Start.Line; line <= last; line++)
				lines.Add(line);
		}
		return lines;
	}

	public bool ChangeIndent(int direction) {
		var touched = TouchedLines();
		var lines = Document.Lines.ToArray();
		var shift = new Dictionary<int, int>();

		foreach (int lineIndex in touched) {
			string line = lines[lineIndex];
			if (direction > 0) {
				lines[lineIndex] = new string(' ', TabSize) + line;
				shift[lineIndex] = TabSize;
			}
			else {
				int indentWidth = line.AsSpan().IndexOfAnyExcept(' ');
				if (indentWidth == -1)
					indentWidth = line.Length;
				int remove = Math.Min(indentWidth, TabSize);
				lines[lineIndex] = line[remove..];
				shift[lineIndex] = -remove;
			}
		}

		var newSel = new List<TextSelection>(selections.Count);
		foreach (var selection in selections) {
			bool spans = !selection.IsEmpty;
			var anchor = Move1(selection.Anchor, spans && selection.Anchor == selection.Start, shift);
			var caret = Move1(selection.Caret, spans && selection.Caret == selection.Start, shift);
			newSel.Add(new(anchor, caret));
		}

		return ReplaceAll(lines, newSel, primary);
	}

	private static TextPosition Move1(TextPosition position, bool isStartOfRange, Dictionary<int, int> shift) {
		if (!shift.TryGetValue(position.Line, out int amount))
			return position;
		if (amount > 0 && position.Column == 0 && isStartOfRange)
			return position;
		return new(position.Line, Math.Max(0, position.Column + amount));
	}

	public bool InsertLine(bool below) {
		if (!MultiLine)
			return false;
		var lines = Document.Lines.ToList();
		var newSel = new List<TextSelection>();
		int offset = 0;
		foreach (var selection in selections) {
			int line = (below ? selection.End.Line + 1 : selection.Start.Line) + offset;
			string source = lines[Math.Min(line - (below ? 1 : 0), lines.Count - 1)];
			int indentWidth = source.AsSpan().IndexOfAnyExcept(' ');
			if (indentWidth == -1)
				indentWidth = source.Length;
			string indent = AutoIndent ? new string(' ', indentWidth) : "";
			lines.Insert(line, indent);
			newSel.Add(new(new TextPosition(line, indent.Length)));
			offset++;
		}
		return ReplaceAll(lines, newSel, primary);
	}

	#endregion

	#region Clipboard

	string? lineClipboard;

	/// <summary>
	/// What copy would put on the clipboard, or null
	/// </summary>
	public string? GetCopyText(out bool wholeLines) {
		wholeLines = false;
		if (!AllowCopy)
			return null;

		if (HasSelection) {
			var parts = new List<string>();
			foreach (var selection in selections) {
				if (!selection.IsEmpty)
					parts.Add(Document.GetText(selection.Start, selection.End));
			}
			return string.Join("\n", parts);
		}

		if (MultiLine && CopyLineWhenEmpty) {
			wholeLines = true;
			var builder = new System.Text.StringBuilder();
			int lastLine = -1;
			foreach (var selection in selections) {
				if (selection.Caret.Line == lastLine) continue;
				lastLine = selection.Caret.Line;
				builder.Append(GetLine(lastLine)).Append('\n');
			}
			return builder.ToString();
		}

		return null;
	}

	public string? Copy() {
		var text = GetCopyText(out bool wholeLines);
		if (text != null)
			lineClipboard = wholeLines ? text : null;
		return text;
	}

	public string? Cut() {
		if (ReadOnly)
			return null;
		var text = Copy();
		if (text == null)
			return null;
		if (lineClipboard != null)
			DeleteLines();
		else
			DeleteSelection();
		return text;
	}

	public bool Paste(string? text) {
		if (string.IsNullOrEmpty(text) || ReadOnly)
			return false;
		text = TextDocument.Normalize(text);
		BreakUndoGroup();

		bool done;
		if (MultiLine && text == lineClipboard && !HasSelection) {
			// Whole line copies paste above the caret's line, like VS
			string paste = PrepareInsert(text);
			done = ApplyEdits((s, _) => new Edit(new(s.Caret.Line, 0), new(s.Caret.Line, 0), paste) { Keep = true },
				TextEditKind.Other);
		}
		else {
			// One line per caret if the counts match
			string[] parts = text.TrimEnd('\n').Split('\n');
			if (selections.Count > 1 && parts.Length == selections.Count)
				done = ApplyEdits((s, i) => new Edit(s.Start, s.End, PrepareInsert(parts[i])), TextEditKind.Other);
			else {
				string paste = PrepareInsert(text);
				done = ApplyEdits((s, _) => new Edit(s.Start, s.End, paste), TextEditKind.Other);
			}
		}

		BreakUndoGroup();
		return done;
	}

	#endregion

	#region Line operations

	/// <summary>
	/// Ctrl+D, duplicates the selection or the whole line
	/// </summary>
	public bool Duplicate() {
		if (HasSelection) {
			return ApplyEdits((selection, _) => {
				if (selection.IsEmpty) return null;
				string text = Document.GetText(selection.Start, selection.End);
				return new Edit(selection.End, selection.End, text) { SelectInserted = true };
			}, TextEditKind.Other);
		}

		if (!MultiLine)
			return false;

		// copy is placed above the original
		int lastLine = -1;
		return ApplyEdits((selection, _) => {
			if (selection.Caret.Line == lastLine)
				return null;
			lastLine = selection.Caret.Line;
			var lineStart = new TextPosition(selection.Caret.Line, 0);
			return new Edit(lineStart, lineStart, GetLine(selection.Caret.Line) + "\n") { Keep = true };
		}, TextEditKind.Other);
	}

	public bool DeleteLines() {
		if (!MultiLine) {
			SetText("");
			return true;
		}

		var touched = TouchedLines();
		foreach (var s in selections)
			touched.Add(s.Caret.Line);

		var lines = new List<string>();
		var newSel = new List<TextSelection>();
		for (int line = 0; line < Document.LineCount; line++) {
			if (!touched.Contains(line))
				lines.Add(GetLine(line));
		}
		if (lines.Count == 0)
			lines.Add("");

		int removedBefore = 0;
		int prev = -1;
		foreach (int line in touched) {
			int target = Math.Clamp(line - removedBefore, 0, lines.Count - 1);
			if (target != prev)
				newSel.Add(new(new TextPosition(target, Math.Min(Primary.Caret.Column, lines[target].Length))));
			prev = target;
			removedBefore++;
		}

		return ReplaceAll(lines, newSel, newSel.Count - 1);
	}

	/// <summary>
	/// Alt+Up/Down, moves the lines under each selection up or down one
	/// </summary>
	public bool MoveLines(int direction) {
		if (!MultiLine)
			return false;

		var touched = TouchedLines();
		foreach (var s in selections)
			touched.Add(s.Caret.Line);

		// Contiguous blocks of lines
		var blocks = new List<(int Start, int End)>();
		foreach (int line in touched) {
			if (blocks.Count > 0 && blocks[blocks.Count - 1].End == line - 1)
				blocks[blocks.Count - 1] = (blocks[blocks.Count - 1].Start, line);
			else
				blocks.Add((line, line));
		}

		if (direction < 0 && blocks[0].Start == 0) return false;
		if (direction > 0 && blocks[blocks.Count - 1].End == LastLine) return false;

		var lines = Document.Lines.ToList();
		if (direction < 0) {
			foreach (var block in blocks) {
				string above = lines[block.Start - 1];
				lines.RemoveAt(block.Start - 1);
				lines.Insert(block.End, above);
			}
		}
		else {
			for (int i = blocks.Count - 1; i >= 0; i--) {
				var block = blocks[i];
				string below = lines[block.End + 1];
				lines.RemoveAt(block.End + 1);
				lines.Insert(block.Start, below);
			}
		}

		var newSel = new List<TextSelection>(selections.Count);
		foreach (var s in selections)
			newSel.Add(new(new TextPosition(s.Anchor.Line + direction, s.Anchor.Column), new TextPosition(s.Caret.Line + direction, s.Caret.Column)));


		return ReplaceAll(lines, newSel, primary);
	}

	#endregion

	#region Movement

	public static int CharClass(char character) {
		if (char.IsWhiteSpace(character))
			return 0;
		if (char.IsLetterOrDigit(character) || character == '_')
			return 1;
		return 2;
	}

	/// <summary>
	/// Start of the next word, like Ctrl+Right in VS
	/// </summary>
	public TextPosition WordRight(TextPosition position) {
		string line = GetLine(position.Line);
		if (position.Column >= line.Length)
			return position.Line < LastLine ? new(position.Line + 1, 0) : position;

		int i = position.Column;
		int charClass = CharClass(line[i]);
		if (charClass != 0) {
			while (i < line.Length && CharClass(line[i]) == charClass)
				i++;
		}
		while (i < line.Length && CharClass(line[i]) == 0)
			i++;
		return new(position.Line, i);
	}

	public TextPosition WordLeft(TextPosition position) {
		if (position.Column == 0)
			return position.Line > 0 ? new(position.Line - 1, GetLine(position.Line - 1).Length) : position;

		string line = GetLine(position.Line);
		int i = Math.Min(position.Column, line.Length);
		while (i > 0 && CharClass(line[i - 1]) == 0)
			i--;
		if (i > 0) {
			int charClass = CharClass(line[i - 1]);
			while (i > 0 && CharClass(line[i - 1]) == charClass)
				i--;
		}
		return new(position.Line, i);
	}

	public (TextPosition Start, TextPosition End) WordRangeAt(TextPosition position) {
		position = Document.Clamp(position);
		string line = GetLine(position.Line);
		if (line.Length == 0)
			return (position, position);

		int i = Math.Min(position.Column, line.Length - 1);
		// Caret right after a word (or at the end) grabs the word on the left
		if (position.Column > 0 && (position.Column >= line.Length || (CharClass(line[position.Column]) == 0 && CharClass(line[position.Column - 1]) != 0)))
			i = position.Column - 1;

		int charClass = CharClass(line[i]);
		int start = i;
		int end = i + 1;
		while (start > 0 && CharClass(line[start - 1]) == charClass)
			start--;
		while (end < line.Length && CharClass(line[end]) == charClass)
			end++;
		return (new(position.Line, start), new(position.Line, end));
	}

	public (TextPosition Start, TextPosition End) LineRangeAt(TextPosition position) {
		position = Document.Clamp(position);
		var start = new TextPosition(position.Line, 0);
		var end = position.Line < LastLine ? new TextPosition(position.Line + 1, 0) : new TextPosition(position.Line, GetLine(position.Line).Length);
		return (start, end);
	}

	private TextPosition MoveVertical(TextPosition position, int lines, ref float? preferredX) {
		if (Layout != null)
			return Layout.MoveVertically(position, lines, ref preferredX);

		int target = position.Line + lines;
		if (target < 0) return Document.Start;
		if (target > LastLine) return Document.End;
		return Document.Clamp(new(target, position.Column));
	}

	public TextPosition MovePosition(TextPosition position, TextMovement movement, ref float? preferredX) {
		string line = GetLine(position.Line);
		switch (movement) {
			case TextMovement.Left:
				if (position.Column > 0)
					return new(position.Line, position.Column - 1);
				if (position.Line > 0)
					return new(position.Line - 1, GetLine(position.Line - 1).Length);
				return position;
			case TextMovement.Right:
				if (position.Column < line.Length)
					return new(position.Line, position.Column + 1);
				if (position.Line < LastLine)
					return new(position.Line + 1, 0);
				return position;
			case TextMovement.WordLeft: return WordLeft(position);
			case TextMovement.WordRight: return WordRight(position);
			case TextMovement.Up: return MoveVertical(position, -1, ref preferredX);
			case TextMovement.Down: return MoveVertical(position, 1, ref preferredX);
			case TextMovement.PageUp: return MoveVertical(position, -Math.Max(1, Layout?.PageLines ?? 10), ref preferredX);
			case TextMovement.PageDown: return MoveVertical(position, Math.Max(1, Layout?.PageLines ?? 10), ref preferredX);
			case TextMovement.Home: {
					// smart home, first non-space then column 0
					int indentWidth = line.AsSpan().IndexOfAnyExcept(' ');
					if (indentWidth == -1)
						indentWidth = line.Length;
					return new(position.Line, position.Column == indentWidth ? 0 : indentWidth);
				}
			case TextMovement.End: return new(position.Line, line.Length);
			case TextMovement.DocumentStart: return Document.Start;
			case TextMovement.DocumentEnd: return Document.End;
		}
		return position;
	}

	private static bool IsVertical(TextMovement movement) => movement is TextMovement.Up or TextMovement.Down or TextMovement.PageUp or TextMovement.PageDown;

	public void Move(TextMovement movement, bool extend) {
		for (int i = 0; i < selections.Count; i++) {
			var selection = selections[i];
			float? preferredX = IsVertical(movement) ? selection.PreferredX : null;
			TextPosition target;
			if (!extend && !selection.IsEmpty && movement is TextMovement.Left or TextMovement.Right)
				target = movement == TextMovement.Left ? selection.Start : selection.End;
			else
				target = MovePosition(selection.Caret, movement, ref preferredX);

			var moved = extend ? new TextSelection(selection.Anchor, target) : new TextSelection(target);
			moved.PreferredX = IsVertical(movement) ? preferredX : null;
			selections[i] = moved;
		}
		boxActive = false;
		SelectionMoved();
	}

	#endregion

	#region Multiple carets

	/// <summary>
	/// Ctrl+Alt+Click, adds a caret or removes one that's already there
	/// </summary>
	public void AddCaret(TextPosition position, bool toggle = true) {
		position = Document.Clamp(position);
		if (toggle && selections.Count > 1) {
			int index = selections.FindIndex(
				s => s.IsEmpty ? s.Caret == position : s.Start <= position && position <= s.End);

			if (index >= 0) {
				selections.RemoveAt(index);
				if (index < primary || primary >= selections.Count)
					primary = Math.Max(0, primary - 1);
				SelectionMoved();
				return;
			}
		}

		selections.Add(new(position));
		primary = selections.Count - 1;
		boxActive = false;
		SelectionMoved();
	}

	/// <summary>
	/// Ctrl+Alt+Up/Down, new caret above the top caret or below the bottom one
	/// </summary>
	public bool AddCaretVertical(int direction) {
		var from = direction < 0 ? selections[0] : selections[selections.Count - 1];
		float? preferredX = from.PreferredX;
		var target = MoveVertical(from.Caret, direction, ref preferredX);
		if (target.Line == from.Caret.Line)
			return false;

		selections.Add(new(target) { PreferredX = preferredX });
		primary = selections.Count - 1;
		boxActive = false;
		SelectionMoved();
		return true;
	}

	/// <summary>
	/// Alt+Shift+. like VS's "Insert Next Matching Caret".
	/// </summary>
	public bool AddNextOccurrence() {
		var current = Primary;
		if (current.IsEmpty) {
			var word = WordRangeAt(current.Caret);
			if (word.Start == word.End)
				return false;
			selections[primary] = new(word.Start, word.End);
			SelectionMoved();
			return true;
		}

		string needle = Document.GetText(current.Start, current.End);
		string text = Document.GetText();
		var starts = Document.GetLineStarts();
		int from = ToOffset(starts, current.End);

		for (int tries = 0; tries < 2; tries++) {
			int index = text.IndexOf(needle, from, StringComparison.Ordinal);
			while (index >= 0) {
				var start = FromOffset(starts, index);
				var end = FromOffset(starts, index + needle.Length);
				if (!selections.Any(x => x.Start < end && start < x.End || x.Start == start)) {
					selections.Add(new(start, end));
					primary = selections.Count - 1;
					SelectionMoved();
					return true;
				}
				index = text.IndexOf(needle, index + 1, StringComparison.Ordinal);
			}
			from = 0;
		}
		return false;
	}

	/// <summary>
	/// Alt+Shift+; (VS's "Insert Carets at All Matching")
	/// </summary>
	public bool SelectAllOccurrences() {
		var current = Primary;
		var range = current.IsEmpty ? WordRangeAt(current.Caret) : (current.Start, current.End);
		if (range.Start == range.End)
			return false;

		string needle = Document.GetText(range.Start, range.End);
		string text = Document.GetText();
		var starts = Document.GetLineStarts();
		var found = new List<TextSelection>();
		int primaryIndex = 0;
		for (int index = text.IndexOf(needle, StringComparison.Ordinal); index >= 0; index = text.IndexOf(needle, index + needle.Length, StringComparison.Ordinal)) {
			var start = FromOffset(starts, index);
			if (start == range.Start)
				primaryIndex = found.Count;
			found.Add(new(start, FromOffset(starts, index + needle.Length)));
		}
		SetSelections(found, primaryIndex);
		return true;
	}

	/// <summary>
	/// Drops every caret but the primary one (Escape), false if there was only one.
	/// </summary>
	public bool CollapseCarets() {
		if (selections.Count <= 1)
			return false;
		SetSelection(Primary);
		return true;
	}

	bool boxActive;
	int boxAnchorLine;
	int boxCaretLine;
	float boxAnchorX;
	float boxCaretX;

	/// <summary>
	/// Column (box) selection between two lines at two X positions, one selection per line
	/// </summary>
	public void BoxSelect(int anchorLine, float anchorX, int caretLine, float caretX) {
		if (Layout == null)
			return;

		anchorLine = Math.Clamp(anchorLine, 0, LastLine);
		caretLine = Math.Clamp(caretLine, 0, LastLine);

		var list = new List<TextSelection>();
		int step = caretLine >= anchorLine ? 1 : -1;
		for (int line = anchorLine; ; line += step) {
			list.Add(new(Layout.PositionAt(line, anchorX), Layout.PositionAt(line, caretX)));
			if (line == caretLine)
				break;
		}

		selections.Clear();
		selections.AddRange(list);
		primary = list.Count - 1;
		Normalize();
		BreakUndoGroup();
		SelectionChanged?.Invoke(this);

		boxActive = true;
		boxAnchorLine = anchorLine;
		boxAnchorX = anchorX;
		boxCaretLine = caretLine;
		boxCaretX = caretX;
	}

	/// <summary>
	/// Shift+Alt+Arrows, grows/shrinks a box selection from the primary caret
	/// </summary>
	public void ExtendBox(TextMovement movement) {
		if (Layout == null)
			return;

		if (!boxActive) {
			var current = Primary;
			boxAnchorLine = current.Anchor.Line;
			boxAnchorX = Layout.GetX(current.Anchor);
			boxCaretLine = current.Caret.Line;
			boxCaretX = Layout.GetX(current.Caret);
		}

		switch (movement) {
			case TextMovement.Up: boxCaretLine = Math.Max(0, boxCaretLine - 1); break;
			case TextMovement.Down: boxCaretLine = Math.Min(LastLine, boxCaretLine + 1); break;
			case TextMovement.Left:
			case TextMovement.Right: {
					var caret = Layout.PositionAt(boxCaretLine, boxCaretX);
					int length = GetLine(caret.Line).Length;
					int column = Math.Clamp(caret.Column + (movement == TextMovement.Left ? -1 : 1), 0, length);
					boxCaretX = Layout.GetX(new(caret.Line, column));
					break;
				}
		}


		BoxSelect(boxAnchorLine, boxAnchorX, boxCaretLine, boxCaretX);
	}

	#endregion
}
