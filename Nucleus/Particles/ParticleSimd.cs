using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace Nucleus.Particles;

public static class ParticleSimd
{
	public const int WIDTH = 4;

	public static int Pad(int count) => (count + WIDTH - 1) & ~(WIDTH - 1);

	public static void Fill(Span<float> destination, float value, int count) {
		var v = Vector128.Create(value);
		ref float d = ref MemoryMarshal.GetReference(destination);
		for (int i = 0; i < count; i += WIDTH)
			v.StoreUnsafe(ref d, (nuint)i);
	}

	public static void MultiplyAdd(Span<float> destination, ReadOnlySpan<float> source, float scale, int count) {
		var s = Vector128.Create(scale);
		ref float d = ref MemoryMarshal.GetReference(destination);
		ref float src = ref MemoryMarshal.GetReference(source);
		for (int i = 0; i < count; i += WIDTH)
			(Vector128.LoadUnsafe(ref d, (nuint)i) + Vector128.LoadUnsafe(ref src, (nuint)i) * s).StoreUnsafe(ref d, (nuint)i);
	}

	public static void Scale(Span<float> destination, ReadOnlySpan<float> scale, int count) {
		ref float d = ref MemoryMarshal.GetReference(destination);
		ref float s = ref MemoryMarshal.GetReference(scale);
		for (int i = 0; i < count; i += WIDTH)
			(Vector128.LoadUnsafe(ref d, (nuint)i) * Vector128.LoadUnsafe(ref s, (nuint)i)).StoreUnsafe(ref d, (nuint)i);
	}
}
