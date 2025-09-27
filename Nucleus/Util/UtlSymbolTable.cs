using K4os.Hash.xxHash;
using System.Buffers;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Nucleus.Util;

public static class HashingUtils {
	public static unsafe ulong Hash(this ReadOnlySpan<char> str, bool invariant = true) {
		if (str == null || str.Length == 0)
			return 0;

		ulong hash;

		if (invariant) {
			bool veryLarge = str.Length > 1024;
			if (veryLarge) {
				char[] lowerBuffer = ArrayPool<char>.Shared.Rent(str.Length);
				str.ToLowerInvariant(lowerBuffer);
				hash = XXH64.DigestOf(MemoryMarshal.Cast<char, byte>(lowerBuffer));
				ArrayPool<char>.Shared.Return(lowerBuffer, true);
			}
			else {
				Span<char> lowerBuffer = stackalloc char[str.Length];
				str.ToLowerInvariant(lowerBuffer);
				hash = XXH64.DigestOf(MemoryMarshal.Cast<char, byte>(lowerBuffer));
			}
		}
		else
			hash = XXH64.DigestOf(MemoryMarshal.Cast<char, byte>(str));

		return hash;
	}

	public static unsafe ulong Hash<T>(this T target) where T : unmanaged {
		ref T t = ref target;
		fixed (T* ptr = &t) {
			Span<byte> data = new(ptr, Unsafe.SizeOf<T>());
			return XXH64.DigestOf(data);
		}
	}

	public static unsafe ulong Hash<T>(this Span<T> target) where T : unmanaged {
		if (target == null) return 0;
		ReadOnlySpan<byte> data = MemoryMarshal.Cast<T, byte>(target);
		return XXH64.DigestOf(data);
	}
}

public interface ISymbolTable {
	UtlSymId_t AddString(ReadOnlySpan<char> str);
	UtlSymId_t Find(ReadOnlySpan<char> str);
	string? String(UtlSymId_t symbol);
	nint GetNumStrings();
	void RemoveAll();
}

public class UtlSymbolTable(bool caseInsensitive = false) : ISymbolTable
{
	readonly Dictionary<UtlSymId_t, string> Symbols = [];

	public int Count => Symbols.Count;
	public void Clear() => Symbols.Clear();

	public UtlSymId_t AddString(ReadOnlySpan<char> str) {
		UtlSymId_t hash = str.Hash(invariant: caseInsensitive);
		if (!Symbols.ContainsKey(hash))
			Symbols[hash] = new(str);
		return hash;
	}

	public UtlSymId_t Find(ReadOnlySpan<char> str) {
		UtlSymId_t hash = str.Hash(invariant: caseInsensitive);
		if (Symbols.ContainsKey(hash))
			return hash;
		return 0;
	}

	public string? String(UtlSymId_t symbol) {
		if (Symbols.TryGetValue(symbol, out string? str))
			return str;
		return null;
	}

	public virtual nint GetNumStrings() => Symbols.Count;
	public virtual void RemoveAll() => Symbols.Clear();
}

public class UtlSymbolTableMT(bool caseInsensitive = false) : ISymbolTable
{
	readonly ConcurrentDictionary<UtlSymId_t, string> Symbols = [];

	public UtlSymId_t AddString(ReadOnlySpan<char> str) {
		UtlSymId_t hash = str.Hash(invariant: caseInsensitive);
		if (!Symbols.ContainsKey(hash))
			Symbols[hash] = new(str);
		return hash;
	}

	public UtlSymId_t Find(ReadOnlySpan<char> str) {
		UtlSymId_t hash = str.Hash(invariant: caseInsensitive);
		if (Symbols.ContainsKey(hash))
			return hash;
		return 0;
	}

	public string? String(UtlSymId_t symbol) {
		if (Symbols.TryGetValue(symbol, out string? str))
			return str;
		return null;
	}
	public nint GetNumStrings() => Symbols.Count;
	public void RemoveAll() => Symbols.Clear();
}
