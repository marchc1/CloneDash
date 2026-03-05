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
		pooledList.Clear();
	}
}

public class ObjectPool<T> where T : IPoolableObject, new()
{
	public static readonly ObjectPool<T> Shared = new();


	readonly ConcurrentDictionary<T, bool> valueStates = [];

	public T Alloc() {
		foreach (var kvp in valueStates) {
			if (kvp.Value == false) { // We found something free
				valueStates[kvp.Key] = true;
				kvp.Key.Init();
				return kvp.Key;
			}
		}

		// Make an new instance of the class
		var instance = new T();
		valueStates[instance] = true;
		instance.Init();
		return instance;
	}

	public bool IsMemoryPoolAllocated(T value) => valueStates.TryGetValue(value, out _);
	public void Free(T value) {
		if (value == null)
			return;
		if (!valueStates.TryGetValue(value, out bool state))
			Debug.Assert(false, $"Passed an instance of {typeof(T).Name} to {nameof(Free)}(T value) that was not allocated by {nameof(Alloc)}()");
		else if (state == false)
			Debug.Assert(false, $"Attempted to free {typeof(T).Name} instance twice in ClassPool<T>, please verify");
		else {
			value.Reset();
			valueStates[value] = false;
		}
	}
}