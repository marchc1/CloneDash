namespace Nucleus.UI;

/// <summary>
/// A spot in a <see cref="TextDocument"/>, column is a character index into the line
/// </summary>
public readonly record struct TextPosition(int Line, int Column) : IComparable<TextPosition>
{
	public int CompareTo(TextPosition other) => Line != other.Line ? Line.CompareTo(other.Line) : Column.CompareTo(other.Column);

	public static bool operator <(TextPosition a, TextPosition b) => a.CompareTo(b) < 0;
	public static bool operator >(TextPosition a, TextPosition b) => a.CompareTo(b) > 0;
	public static bool operator <=(TextPosition a, TextPosition b) => a.CompareTo(b) <= 0;
	public static bool operator >=(TextPosition a, TextPosition b) => a.CompareTo(b) >= 0;

	public static TextPosition Min(TextPosition a, TextPosition b) => a <= b ? a : b;
	public static TextPosition Max(TextPosition a, TextPosition b) => a >= b ? a : b;
}

/// <summary>
/// One caret and the selection hanging off of it
/// </summary>
public struct TextSelection
{
	public TextPosition Anchor;
	public TextPosition Caret;
	/// <summary>
	/// Remembered X for up/down movement
	/// </summary>
	public float? PreferredX;

	public TextSelection(TextPosition caret) {
		Anchor = caret;
		Caret = caret;
	}

	public TextSelection(TextPosition anchor, TextPosition caret) {
		Anchor = anchor;
		Caret = caret;
	}

	public readonly TextPosition Start => TextPosition.Min(Anchor, Caret);
	public readonly TextPosition End => TextPosition.Max(Anchor, Caret);
	public readonly bool IsEmpty => Anchor == Caret;

	public override readonly string ToString() => IsEmpty ? $"{Caret}" : $"{Anchor} -> {Caret}";
}
