namespace Nucleus.ManagedMemory;

public enum MemoryRealm
{
	/// <summary>
	/// CPU memory
	/// </summary>
	CPU,

	/// <summary>
	/// GPU memory
	/// </summary>
	GPU
}

public interface IMemoryManager<T> where T : IManagedMemoryUnit
{
	ulong GetTotalBits(MemoryRealm realm);
}

public interface IManagedMemoryUnit : IValidatable, IDisposable
{
	public static Span<char> MergePath(ReadOnlySpan<char> pathID, ReadOnlySpan<char> path, Span<char> output) {
		if (output.Length < (pathID.Length + path.Length + 1))
			throw new Exception("Did not allocate enough space for this operation!");
		pathID.CopyTo(output[0..]);
		output[pathID.Length] = '@';
		path.CopyTo(output[(pathID.Length + 1)..]);
		return output[(pathID.Length + path.Length + 1)..];
	}

	/// <summary>
	/// Helper method for use alongside a stackalloc (probably)
	/// </summary>
	/// <param name="pathID"></param>
	/// <param name="path"></param>
	/// <returns></returns>
	public static int MergePathSize(ReadOnlySpan<char> pathID, ReadOnlySpan<char> path) => pathID.Length + path.Length + 1;

	ulong GetUsedBits(MemoryRealm realm);

	public virtual ulong GetUsedBytes(MemoryRealm realm) => GetUsedBits(realm) / 8;
}