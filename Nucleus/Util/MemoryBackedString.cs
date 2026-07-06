using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Nucleus.Util;

/// <summary>
/// A string helper with an internally backed char[]
/// This is mostly useful if you use it on class fields that live a while and don't want to create a bunch of garbage strings
/// </summary>
public struct MemoryBackedString
{
	char[]? stringBacking;
	int stringLength;

	public readonly long MemorySize => stringBacking?.LongLength ?? 0;
	public readonly int Length => stringLength;

	public MemoryBackedString(int initialSize) {
		stringBacking = new char[initialSize];
	}

	public MemoryBackedString(nuint initialSize) {
		stringBacking = new char[initialSize];
	}

	public readonly Span<char> ToSpan() => stringBacking == null ? default : stringBacking.AsSpan()[..stringLength];
	public readonly ReadOnlySpan<char> ToReadOnlySpan() => stringBacking == null ? default : stringBacking.AsSpan()[..stringLength];

	/// <summary>
	/// Concatenates text on the left hand of the string
	/// </summary>
	public void ConcatLefthand(ReadOnlySpan<char> text) {
		int textLength = text.Length;
		if (textLength == 0) return;

		nuint finalSize = (nuint)textLength + (nuint)stringLength;
		EnsureCharacters(finalSize);

		Array.Reverse(stringBacking, 0, stringLength);
		text.CopyTo(stringBacking.AsSpan());
		Array.Reverse(stringBacking, 0, textLength);
		Array.Reverse(stringBacking, 0, stringLength + textLength);
		stringLength += textLength;
	}

	/// <summary>
	/// Concatenates text on the right hand of the string
	/// </summary>
	public void ConcatRighthand(ReadOnlySpan<char> text) {
		int textLength = text.Length;
		if (textLength == 0) return;

		nuint finalSize = (nuint)textLength + (nuint)stringLength;
		EnsureCharacters(finalSize);

		text.CopyTo(stringBacking.AsSpan()[stringLength..]);
		stringLength += textLength;
	}

	public bool ConcatLefthand<T>(in T t, ReadOnlySpan<char> format = default, IFormatProvider? formatProvider = null) where T : ISpanFormattable {
		Span<char> copyBuffer = stackalloc char[8000];
		if (!t.TryFormat(copyBuffer, out int charsWritten, format, formatProvider))
			return false;
		ConcatLefthand(copyBuffer[..charsWritten]);
		return true;
	}

	public bool ConcatRighthand<T>(in T t, ReadOnlySpan<char> format = default, IFormatProvider? formatProvider = null) where T : ISpanFormattable {
		Span<char> copyBuffer = stackalloc char[8000];
		if (!t.TryFormat(copyBuffer, out int charsWritten, format, formatProvider))
			return false;
		ConcatRighthand(copyBuffer[..charsWritten]);
		return true;
	}

	public void SetText(ReadOnlySpan<char> text) {
		Clear();
		ConcatRighthand(text);
	}

	public void SetText<T>(in T t, ReadOnlySpan<char> format = default, IFormatProvider? formatProvider = null) where T : ISpanFormattable {
		Clear();
		ConcatRighthand(in t, format, formatProvider);
	}


	public void Clear() {
		stringLength = 0;
	}

	/// <summary> Grow the array by this amount each time no matter what </summary>
	const int MIN_GROW_SIZE = 128;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[MemberNotNull(nameof(stringBacking))]
	private void EnsureCharacters(nuint finalSize) {
		nuint currentArraySize = (nuint)(stringBacking?.LongLength ?? 0);
		if (stringBacking == null || currentArraySize < finalSize) {
			nuint growBy = Math.Max(finalSize - currentArraySize, MIN_GROW_SIZE);
			nuint finalArrayLength = currentArraySize + growBy;

			char[]? prev = stringBacking;
			char[] now = new char[finalArrayLength];

			prev?.CopyTo(now, 0);
			stringBacking = now;
		}
	}
}