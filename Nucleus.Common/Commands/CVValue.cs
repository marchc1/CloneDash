namespace Nucleus.Commands;

public struct CVValue
{
	public static CVValue Null => new();

	public char[]? Chars;
	public int StringLength;
	public double Double;
	public int Int;

	public readonly ReadOnlySpan<char> GetString() => (StringLength == 0 || Chars == null) ? default : Chars.AsSpan()[..StringLength];
}