using System;
using System.Collections.Generic;
using System.Text;

namespace Nucleus.Common;

public static class CFormatting {
	public static Span<char> SliceNullTerminatedString(this Span<char> span) {
		int index = System.MemoryExtensions.IndexOf(span, '\0');
		if (index == -1)
			return span;
		return span[..index];
	}
	public static ReadOnlySpan<char> SliceNullTerminatedString(this ReadOnlySpan<char> span) {
		int index = System.MemoryExtensions.IndexOf(span, '\0');
		if (index == -1)
			return span;
		return span[..index];
	}
	public static ReadOnlySpan<byte> SliceNullTerminatedString(this ReadOnlySpan<byte> span) {
		int index = System.MemoryExtensions.IndexOf(span, (byte)0);
		if (index == -1)
			return span;
		return span[..index];
	}
	public static ReadOnlySpan<char> SliceSafe(this ReadOnlySpan<char> span, int to) {
		return span[..Math.Min(span.Length, to)];
	}

	public static int strcmp(scoped ReadOnlySpan<char> a, scoped ReadOnlySpan<char> b) => a.SliceNullTerminatedString().CompareTo(b.SliceNullTerminatedString(), StringComparison.Ordinal);
	public static int strncmp(scoped ReadOnlySpan<char> a, scoped ReadOnlySpan<char> b, int c) => a.SliceNullTerminatedString().SliceSafe(c).CompareTo(b.SliceNullTerminatedString().SliceSafe(c), StringComparison.Ordinal);
	public static int stricmp(scoped ReadOnlySpan<char> a, scoped ReadOnlySpan<char> b) => a.SliceNullTerminatedString().CompareTo(b.SliceNullTerminatedString(), StringComparison.OrdinalIgnoreCase);

	public static bool streq(scoped ReadOnlySpan<char> a, scoped ReadOnlySpan<char> b) => a.SliceNullTerminatedString().Equals(b.SliceNullTerminatedString(), StringComparison.Ordinal);
	public static bool strieq(scoped ReadOnlySpan<char> a, scoped ReadOnlySpan<char> b) => a.SliceNullTerminatedString().Equals(b.SliceNullTerminatedString(), StringComparison.OrdinalIgnoreCase);
	public static nint strlen(scoped ReadOnlySpan<char> str) {
		int i = 0;
		for (i = 0; i < str.Length; i++) {
			if (str[i] == '\0')
				return i;
		}
		return i;
	}

	public static nint strlen(scoped ReadOnlySpan<byte> str) {
		int i = 0;
		for (i = 0; i < str.Length; i++) {
			if (str[i] == '\0')
				return i;
		}
		return i;
	}
	public static int strcpy(scoped Span<byte> target, scoped ReadOnlySpan<byte> str) {
		str = str.SliceNullTerminatedString();
		int len = Math.Min(target.Length, str.Length);
		str[..len].CopyTo(target);
		target[Math.Min(target.Length - 1, str.Length)] = 0;
		return len;
	}
	public static ReadOnlySpan<char> strstr(ReadOnlySpan<char> target, scoped ReadOnlySpan<char> str) {
		if (str.Length == 0) return target;
		if (str.Length > target.Length) return ReadOnlySpan<char>.Empty;

		char first = str[0];

		for (int i = 0; i <= target.Length - str.Length; i++) {
			if (target[i] != first)
				continue;

			if (target.Slice(i, str.Length).Equals(str, StringComparison.Ordinal))
				return target.Slice(i);
		}

		return ReadOnlySpan<char>.Empty;
	}
	public static Span<char> strstr(Span<char> target, scoped ReadOnlySpan<char> str) {
		if (str.Length == 0) return target;
		if (str.Length > target.Length) return Span<char>.Empty;

		char first = str[0];

		for (int i = 0; i <= target.Length - str.Length; i++) {
			if (target[i] != first)
				continue;

			if (target.Slice(i, str.Length).Equals(str, StringComparison.Ordinal))
				return target.Slice(i);
		}

		return Span<char>.Empty;
	}
	public static ReadOnlySpan<char> stristr(ReadOnlySpan<char> target, scoped ReadOnlySpan<char> str) {
		if (str.Length == 0) return target;
		if (str.Length > target.Length) return ReadOnlySpan<char>.Empty;

		char first = char.ToUpperInvariant(str[0]);

		for (int i = 0; i <= target.Length - str.Length; i++) {
			if (char.ToUpperInvariant(target[i]) != first)
				continue;

			if (target.Slice(i, str.Length).Equals(str, StringComparison.OrdinalIgnoreCase))
				return target.Slice(i);
		}

		return ReadOnlySpan<char>.Empty;
	}
	public static ReadOnlySpan<char> strchr(ReadOnlySpan<char> target, char c) {
		for (int i = 0; i < target.Length; i++) {
			if (target[i] == c)
				return target.Slice(i);
		}


		return ReadOnlySpan<char>.Empty;
	}
	public static Span<char> strchr(Span<char> target, char c) {
		for (int i = 0; i < target.Length; i++) {
			if (target[i] == c)
				return target.Slice(i);
		}


		return Span<char>.Empty;
	}
	public static ReadOnlySpan<char> strrchr(ReadOnlySpan<char> target, char c) {
		for (int i = target.Length - 1; i >= 0; i--) {
			if (target[i] == c)
				return target.Slice(i);
		}


		return ReadOnlySpan<char>.Empty;
	}
	public static Span<char> strrchr(Span<char> target, char c) {
		for (int i = target.Length - 1; i >= 0; i--) {
			if (target[i] == c)
				return target.Slice(i);
		}


		return Span<char>.Empty;
	}
	public static Span<char> stristr(Span<char> target, scoped ReadOnlySpan<char> str) {
		if (str.Length == 0) return target;
		if (str.Length > target.Length) return Span<char>.Empty;

		char first = char.ToUpperInvariant(str[0]);

		for (int i = 0; i <= target.Length - str.Length; i++) {
			if (char.ToUpperInvariant(target[i]) != first)
				continue;

			if (target.Slice(i, str.Length).Equals(str, StringComparison.OrdinalIgnoreCase))
				return target.Slice(i);
		}

		return Span<char>.Empty;
	}
	public static int strcpy(scoped Span<char> target, scoped ReadOnlySpan<char> str) {
		str = str.SliceNullTerminatedString();
		int len = Math.Min(target.Length, str.Length);
		str[..len].CopyTo(target);
		target[Math.Min(target.Length - 1, str.Length)] = '\0';
		return len;
	}
	public static int strcat(scoped Span<char> target, scoped ReadOnlySpan<char> str) {
		int targetLen;
		for (targetLen = 0; targetLen < target.Length; targetLen++) {
			if (target[targetLen] == '\0')
				break;
		}

		str = str.SliceNullTerminatedString();
		int len = Math.Min(target.Length - targetLen, str.Length);
		str[..len].CopyTo(target[targetLen..]);
		target[Math.Min(target.Length - 1, targetLen + str.Length)] = '\0';
		return len;
	}
}