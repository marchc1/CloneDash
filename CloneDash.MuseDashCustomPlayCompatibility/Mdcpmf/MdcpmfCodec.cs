using System.Text;

namespace CloneDash.MuseDashCustomPlayCompatibility.Mdcpmf;

internal static class MdcpmfCodec
{
	static readonly ulong[] _q = [0xE53FB922A8E8482A, 0xBDE853B871A2FEAF, 0x08352277E66F3081, 0xA4053AF05D021129, 0xBC131D95D2847782];

	static ulong _f(ulong z) {
		z += 0x9E3779B97F4A7C15;
		z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9;
		z = (z ^ (z >> 27)) * 0x94D049BB133111EB;
		return z ^ (z >> 31);
	}

	static int _g(ReadOnlySpan<ulong> t, int v) => (int)(t[v >> 6] >> (v & 63)) & 1;

	static int _h(int a, int b, int s) {
		int u = a + ~b + 1, w = (a | b) - (a & b);
		return ((u & ~-s) | (w & -s)) & 0xFF;
	}

	public static string DecodeToString(ReadOnlySpan<byte> file) {
		Span<ulong> t = stackalloc ulong[5];
		ulong z = 0x6A09E667F3BCC908;
		for (int i = 0; i < t.Length; i++)
			t[i] = _q[i] ^ (z = _f(z));

		Span<long> c = stackalloc long[2];
		int n = file.Length, h = n >> 1, k = (int)(t[4] & 0xFF), s = -1, st = 3;
		byte[]? o = null;

		while (true) {
			switch (st) {
				case 3:
					st = (n & 1) == 0 && h >= 2 ? 7 : 0;
					break;
				case 7:
					for (int i = 0; i < h; i++) {
						int a = file[i << 1], b = file[(i << 1) | 1];
						c[0] += _g(t, _h(a, b, 0));
						c[1] += _g(t, _h(a, b, 1));
					}
					st = 5;
					break;
				case 5:
					for (int j = 0; j < 2 && s < 0; j++)
						if (c[j] * 10 > (long)h * 9 && _h(file[0], file[1], j) == k && _h(file[2], file[3], j) == k)
							s = j;
					st = s < 0 ? 0 : 6;
					break;
				case 6:
					o = new byte[h];
					for (int i = 0; i < h; i++)
						o[i] = (byte)_h(file[i << 1], file[(i << 1) | 1], s);
					st = 1;
					break;
				case 1:
					return Encoding.UTF8.GetString(o!);
				default:
					throw new FormatException("Not a valid .mdcpmf file");
			}
		}
	}
}
