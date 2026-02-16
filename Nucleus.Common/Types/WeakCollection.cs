using System.Collections;

namespace Nucleus.ManagedMemory;

public class WeakCollection<T> : ICollection<T?> where T : class
{
	readonly List<WeakReference<T?>> references = [];

	public T? this[int index] { get => references[index].TryGetTarget(out T? target) ? target : null; set => references[index].SetTarget(value); }
	public int Count => references.Count;
	public int ReferencedCount => references.Count(static x => x.TryGetTarget(out _));
	public bool IsReadOnly => false;

	public void Add(T? item) {
		if (item == null)
			return;
		references.Add(new(item));
	}

	public void Clear() {
		foreach (var reference in references)
			reference.SetTarget(null);
		references.Clear();
	}

	public bool Contains(T? item) {
		foreach (var reference in references)
			if (reference.TryGetTarget(out T? t) && t == item)
				return true;
		return false;
	}

	public void CopyTo(T?[] array, int arrayIndex) {
		for (int i = 0; i < references.Count; i++)
			if (references[i].TryGetTarget(out T? t))
				array[arrayIndex++] = t;
	}

	public IEnumerator<T?> GetEnumerator() {
		foreach (var reference in references)
			if (reference.TryGetTarget(out T? t))
				yield return t;
	}

	public bool Remove(T? item) {
		for (int i = 0; i < references.Count; i++) {
			WeakReference<T?> reference = references[i];
			if (reference.TryGetTarget(out T? t) && t == item) {
				references.RemoveAt(i);
				return true;
			}
		}

		return false;
	}

	IEnumerator IEnumerable.GetEnumerator() {
		return GetEnumerator();
	}
}
