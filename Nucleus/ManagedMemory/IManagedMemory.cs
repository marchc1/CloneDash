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


/// <summary>
/// Represents the multiplier to convert a value of Data Unit into bits. 
/// <br></br>
/// So Bit == 1, Byte == 8, etc...
/// </summary>
public enum DataUnit : ulong
{
	Bit = 1,
	Byte = 8,

	Kilobit = 1000,
	Kilobyte = 8000,

	Megabit = 1000000,
	Megabyte = 8000000,

	Gigabit = 1000000000,
	Gigabyte = 8000000000,

	b = Bit,
	B = Byte,
	Mb = Megabit,
	MB = Megabyte,
	Gb = Gigabit,
	GB = Gigabyte
}
public interface IManagedMemory : IValidatable, IDisposable
{
	public ulong UsedBits { get; }
	public ulong UsedBytes => UsedBits / 8;

	public static ulong Convert(ulong data, DataUnit from, DataUnit to) {
		return (ulong)(((double)data * (double)from) / (double)to);
	}
	public static string NiceBytes(ulong data, DataUnit unit = DataUnit.Byte) {
		if (unit != DataUnit.Byte) data = Convert(data, unit, DataUnit.Byte);
		if (data < (ulong)DataUnit.Kilobyte) return $"{data:0.000}B";
		else if (data < (ulong)DataUnit.Megabyte) return $"{data / (double)DataUnit.Kilobyte:0.000}KB";
		else if (data < (ulong)DataUnit.Gigabyte) return $"{data / (double)DataUnit.Megabyte:0.000}MB";
		else return $"{data / (ulong)DataUnit.Gigabyte:0.000}GB";
	}
	public static string NiceBytes(IManagedMemory inf) => NiceBytes(inf.UsedBytes);

	/// <summary>
	/// Helper method for use alongside a stackalloc (probably)
	/// </summary>
	/// <param name="pathID"></param>
	/// <param name="path"></param>
	/// <returns></returns>
	public static int MergePathSize(ReadOnlySpan<char> pathID, ReadOnlySpan<char> path) => pathID.Length + path.Length + 1;
	public static void MergePath(ReadOnlySpan<char> pathID, ReadOnlySpan<char> path, Span<char> output) {
		if (output.Length < (pathID.Length + path.Length + 1))
			throw new Exception("Did not allocate enough space for this operation!");
		pathID.CopyTo(output[0..]);
		output[pathID.Length] = '@';
		path.CopyTo(output[(pathID.Length + 1)..]);
	}
}
