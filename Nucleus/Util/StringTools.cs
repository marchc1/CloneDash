using System.Buffers;
using System.Text;

namespace Nucleus.Util;

public static class RuneExtensions
{
	public static Rune GetRuneAt(this ReadOnlySpan<char> text, int index) {
		if ((uint)index >= (uint)text.Length)
			throw new ArgumentOutOfRangeException(nameof(index));

		var status = Rune.DecodeFromUtf16(text[index..], out Rune rune, out int charsConsumed);
		if (status != OperationStatus.Done)
			throw new ArgumentException("Invalid UTF-16 data in span.", nameof(text));

		return rune;
	}
}