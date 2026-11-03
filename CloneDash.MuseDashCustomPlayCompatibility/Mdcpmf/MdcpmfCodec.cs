using System.Text;

namespace CloneDash.MuseDashCustomPlayCompatibility.Mdcpmf;

public static class MdcpmfCodec
{
	public static byte[] Decode(ReadOnlySpan<byte> file) {
		if (file.Length % 2 != 0)
			throw new FormatException($"mdcpmf length {file.Length} is odd, expected 2 bytes per char");

		var text = new byte[file.Length / 2];
		for (int i = 0; i < text.Length; i++)
			text[i] = (byte)(file[i * 2] - file[i * 2 + 1]);
		return text;
	}

	public static byte[] Encode(ReadOnlySpan<byte> text, Random? rng = null) {
		rng ??= Random.Shared;
		var file = new byte[text.Length * 2];
		for (int i = 0; i < text.Length; i++) {
			int pad = rng.Next(256 - text[i]);
			file[i * 2] = (byte)(pad + text[i]);
			file[i * 2 + 1] = (byte)pad;
		}
		return file;
	}

	public static string DecodeToString(ReadOnlySpan<byte> file) {
		var sub = Decode(file);
		if (LooksLikeText(sub))
			return Encoding.UTF8.GetString(sub);

		var xor = new byte[sub.Length];
		for (int i = 0; i < xor.Length; i++)
			xor[i] = (byte)(file[i * 2] ^ file[i * 2 + 1]);

		if (LooksLikeText(xor))
			return Encoding.UTF8.GetString(xor);

		throw new FormatException("mdcpmf file does not decode to text");
	}

	public static bool LooksLikeText(ReadOnlySpan<byte> data) {
		if (data.Length == 0)
			return false;

		int ok = 0;
		foreach (var b in data)
			if (b is >= 32 and < 127 or 9 or 10 or 13 or >= 0x80)
				ok++;
		return ok / (double)data.Length > 0.9 && data.StartsWith("//"u8);
	}
}
