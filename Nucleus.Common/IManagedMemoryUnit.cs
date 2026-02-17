namespace Nucleus.ManagedMemory;

public interface IManagedMemoryUnit : IValidatable, IDisposable
{
	public ulong UsedBits { get; }
	public ulong UsedBytes => UsedBits / 8;


	/// <summary>
	/// Helper method for use alongside a stackalloc (probably)
	/// </summary>
	/// <param name="pathID"></param>
	/// <param name="path"></param>
	/// <returns></returns>
	public static int MergePathSize(ReadOnlySpan<char> pathID, ReadOnlySpan<char> path) => pathID.Length + path.Length + 1;
	public static Span<char> MergePath(ReadOnlySpan<char> pathID, ReadOnlySpan<char> path, Span<char> output) {
		if (output.Length < (pathID.Length + path.Length + 1))
			throw new Exception("Did not allocate enough space for this operation!");
		pathID.CopyTo(output[0..]);
		output[pathID.Length] = '@';
		path.CopyTo(output[(pathID.Length + 1)..]);
		return output[(pathID.Length + path.Length + 1)..];
	}
}