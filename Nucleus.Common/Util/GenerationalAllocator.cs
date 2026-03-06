using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Nucleus.Common.Util;

public readonly record struct GenerationalHandle(ulong Handle, ulong Generation);
public class GenerationalAllocator
{
	private ulong _generation;
	private readonly ConcurrentDictionary<ulong, ulong> _generations = new();

	public ulong GetGeneration() => _generation;

	public GenerationalHandle Alloc(ulong handle) {
		var gen = Interlocked.Increment(ref _generation);
		_generations[handle] = gen;
		return new GenerationalHandle(handle, gen);
	}

	public void Free(in GenerationalHandle handle) {
		_generations.TryRemove(new KeyValuePair<ulong, ulong>(handle.Handle, handle.Generation));
		Interlocked.Increment(ref _generation);
	}

	public bool IsValid(in GenerationalHandle handle) =>
		_generations.TryGetValue(handle.Handle, out var gen) && gen == handle.Generation;

	public GenerationalHandle Alloc(long handle) => Alloc((ulong)handle);
	public GenerationalHandle Alloc(uint handle) => Alloc((ulong)handle);
	public GenerationalHandle Alloc(int handle) => Alloc((ulong)(uint)handle);
}