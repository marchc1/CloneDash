using System.Buffers;
using System.Text;

namespace Nucleus.Common.Extensions;

public static class StringExtensions
{
	extension(ReadOnlySpan<char> text)
	{
		public Rune GetRuneAt(int index) {
			if ((uint)index >= (uint)text.Length)
				throw new ArgumentOutOfRangeException(nameof(index));

			var status = Rune.DecodeFromUtf16(text[index..], out Rune rune, out int charsConsumed);
			if (status != OperationStatus.Done)
				throw new ArgumentException("Invalid UTF-16 data in span.", nameof(text));

			return rune;
		}
	}
}
