namespace Nucleus.UI;

/// <summary>
/// Plain text stored as lines, no carets or undo
/// </summary>
public class TextDocument
{
	public SafeArray<string> Lines { get; private set; } = [""];

	public int LineCount => Lines.Count;
	public string GetLine(int line) => Lines[line] ?? "";

	public TextPosition Start => new(0, 0);
	public TextPosition End => new(LineCount - 1, GetLine(LineCount - 1).Length);

	public int Length {
		get {
			int length = LineCount - 1;
			foreach (var line in Lines)
				length += line.Length;
			return length;
		}
	}

	public static string Normalize(string text) => text.Replace("\r\n", "\n").Replace('\r', '\n');

	public string GetText() => string.Join("\n", Lines);

	public void SetText(string? text) {
		Lines = new(Normalize(text ?? "").Split('\n'));
	}

	public void SetLines(IEnumerable<string> lines) {
		Lines = new(lines);
		if (Lines.Count == 0)
			Lines.Add("");
	}

	public TextPosition Clamp(TextPosition position) {
		int line = Math.Clamp(position.Line, 0, LineCount - 1);
		return new(line, Math.Clamp(position.Column, 0, GetLine(line).Length));
	}

	public string GetText(TextPosition start, TextPosition end) {
		start = Clamp(start);
		end = Clamp(end);
		if (end < start)
			(start, end) = (end, start);

		if (start.Line == end.Line)
			return GetLine(start.Line).Substring(start.Column, end.Column - start.Column);

		var builder = new System.Text.StringBuilder();
		builder.Append(GetLine(start.Line).AsSpan(start.Column));
		for (int i = start.Line + 1; i < end.Line; i++) {
			builder.Append('\n');
			builder.Append(GetLine(i));
		}
		builder.Append('\n');
		builder.Append(GetLine(end.Line).AsSpan(0, end.Column));
		return builder.ToString();
	}

	/// <summary>
	/// Replaces start-end with <paramref name="text"/>
	/// </summary>
	/// <returns>Where the inserted text ends</returns>
	public TextPosition Replace(TextPosition start, TextPosition end, string text) {
		start = Clamp(start);
		end = Clamp(end);
		if (end < start)
			(start, end) = (end, start);

		string prefix = GetLine(start.Line).Substring(0, start.Column);
		string suffix = GetLine(end.Line).Substring(end.Column);
		string[] inserted = Normalize(text).Split('\n');

		Lines.RemoveRange(start.Line, end.Line - start.Line + 1);

		var newLines = new string[inserted.Length];
		for (int i = 0; i < inserted.Length; i++)
			newLines[i] = inserted[i];
		newLines[0] = prefix + newLines[0];
		newLines[newLines.Length - 1] += suffix;
		Lines.InsertRange(start.Line, newLines);

		int lastLine = start.Line + inserted.Length - 1;
		int lastCol = (inserted.Length == 1 ? prefix.Length : 0) + inserted[inserted.Length - 1].Length;
		return new(lastLine, lastCol);
	}

	/// <summary>
	/// Offset of each line's first character in <see cref="GetText()"/>
	/// </summary>
	public int[] GetLineStarts() {
		var starts = new int[LineCount];
		int offset = 0;
		for (int i = 0; i < LineCount; i++) {
			starts[i] = offset;
			offset += GetLine(i).Length + 1;
		}
		return starts;
	}

	public int GetOffset(TextPosition position) {
		position = Clamp(position);
		int offset = 0;
		for (int i = 0; i < position.Line; i++)
			offset += GetLine(i).Length + 1;
		return offset + position.Column;
	}

	public TextPosition GetPosition(int offset) {
		offset = Math.Max(0, offset);
		for (int i = 0; i < LineCount; i++) {
			int lineLength = GetLine(i).Length;
			if (offset <= lineLength)
				return new(i, offset);
			offset -= lineLength + 1;
		}
		return End;
	}

	private static TextPosition PositionFromStarts(int[] starts, int offset, TextDocument document) {
		int line = Array.BinarySearch(starts, offset);
		if (line < 0)
			line = ~line - 1;
		line = Math.Clamp(line, 0, starts.Length - 1);
		return new(line, Math.Clamp(offset - starts[line], 0, document.GetLine(line).Length));
	}

	internal TextPosition GetPosition(int[] starts, int offset) => PositionFromStarts(starts, offset, this);
}
