using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Types;

/// <summary>
/// A list that never shrinks or reallocates, which also means its contents can be safely accessed by reference
/// <br/>
/// Uses fragment-based storage based on <see cref="MAX_FRAGMENT_SIZE"/>
/// </summary>
public class NeverShrinkingList<T> : IEnumerable, IEnumerable<T?>, ICollection<T?>
{
	const int MAX_FRAGMENT_SIZE = 32;

	List<T?[]> fragments = [];
	int count = 0;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	void fragAddr(int index, out int fragmentAbsIndex, out int fragmentLocalIndex) {
		fragmentAbsIndex = index / MAX_FRAGMENT_SIZE;
		fragmentLocalIndex = index % MAX_FRAGMENT_SIZE;
	}


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	bool overflows(int index, out int abs, out int local) {
		fragAddr(index, out abs, out local);
		return abs >= fragments.Count || local >= fragments[abs].Length;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	void allocateFragment(out int abs, out int local) {
		// Given count, do we need to allocate a fragment
		if (overflows(count, out abs, out local))
			fragments.Add(new T[MAX_FRAGMENT_SIZE]);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	T? get(int index) {
		fragAddr(index, out int abs, out int local);
		return fragments[abs][local];
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	void set(int index, T? value) {
		fragAddr(index, out int abs, out int local);
		fragments[abs][local] = value;
	}

	public ref T? this[int index] {
		get {
			fragAddr(index, out int abs, out int local);
			return ref fragments[abs][local];
		}
	}
	public int Count => count;

	public bool IsReadOnly => false;

	public void Add(T? item) {
		allocateFragment(out int abs, out int local);
		fragments[abs][local] = item;
		count++;
	}

	public ref T? Add() {
		allocateFragment(out int abs, out int local);
		fragments[abs][local] = default;
		count++;

		return ref fragments[abs][local];
	}

	public ref T? AddNoZero() {
		allocateFragment(out int abs, out int local);
		count++;

		return ref fragments[abs][local];
	}


	public void Clear() => Clear(false);

	public void Clear(bool zeroOut = true) {
		if (zeroOut)
			for (int i = 0; i < count; i++)
				this[i] = default;

		count = 0;
	}

	public bool Contains(T? item) => IndexOf(item) != -1;

	public void CopyTo(T?[] array, int arrayIndex) {
		for (int i = 0; i < count; i++) {
			array[arrayIndex++] = this[i];
		}
	}

	public IEnumerator<T?> GetEnumerator() {
		for (int i = 0; i < count; i++) {
			yield return get(i);
		}
	}

	public int IndexOf(T? item) {
		for (int i = 0; i < count; i++) {
			var iT = get(i);
			if (get(i)?.Equals(item) ?? item == null) return i;
		}
		return -1;
	}

	public void Insert(int index, T item) {
		throw new NotSupportedException("Inserting would break reference guarantees");
	}

	public bool Remove(T? item) {
		throw new NotSupportedException("Removing would break reference guarantees");
	}

	public void RemoveAt(int index) {
		throw new NotSupportedException("Removing would break reference guarantees");
	}

	IEnumerator IEnumerable.GetEnumerator() {
		return GetEnumerator();
	}
}
