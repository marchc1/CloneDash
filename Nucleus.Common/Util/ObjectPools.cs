using System.Collections.Concurrent;
using System.Diagnostics;

namespace Nucleus.Common.Util;

public interface IPoolableObject
{
	void Init();
	void Reset();
}

public class PoolableList<T> : List<T>, IPoolableObject
{
	public void Init() { }
	public void Reset() => Clear();
}

public class ListPool<T>
{
	public static readonly ListPool<T> Shared = new();
	readonly ObjectPool<PoolableList<T>> pool = new();

	public List<T> Alloc(int capacity = 0) {
		List<T> list = pool.Alloc();
		if (capacity > 0)
			list.EnsureCapacity(capacity);
		return list;
	}

	public void Free(List<T> list) {
		if (list is not PoolableList<T> pooledList)
			throw new InvalidCastException("Got a non-poolable list!");
		pool.Free(pooledList);
	}
}

public class ObjectPool<T> where T : IPoolableObject, new()
{
	public static readonly ObjectPool<T> Shared = new();

	readonly ConcurrentBag<T> _free = new();
	readonly ConcurrentDictionary<T, byte> _allocated = new(ReferenceEqualityComparer.Instance);

	public T Alloc() {
		T instance;
		if (_free.TryTake(out instance)) {
			instance.Init();
			return instance;
		}

		instance = new T();
		_allocated[instance] = 0;
		instance.Init();
		return instance;
	}

	public bool IsMemoryPoolAllocated(T value) => value != null && _allocated.ContainsKey(value);

	public void Free(T value) {
		if (value == null)
			return;

		Debug.Assert(_allocated.ContainsKey(value), $"Passed an instance of {typeof(T).Name} to {nameof(Free)}(T value) that was not allocated by {nameof(Alloc)}()");

		value.Reset();
		_free.Add(value);
	}

	sealed class ReferenceEqualityComparer : IEqualityComparer<T>
	{
		public static readonly ReferenceEqualityComparer Instance = new();
		public bool Equals(T x, T y) => ReferenceEquals(x, y);
		public int GetHashCode(T obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
	}
}