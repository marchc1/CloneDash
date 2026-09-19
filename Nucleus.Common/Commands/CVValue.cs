namespace Nucleus.Commands;

public struct CVValue
{
	public char[]? Chars;
	public double Double;
	public int Int;
	public int StringLength;
	public static CVValue Null => new();

	public readonly ReadOnlySpan<char> GetString() => (StringLength == 0 || Chars == null) ? default : Chars.AsSpan()[..StringLength];
}