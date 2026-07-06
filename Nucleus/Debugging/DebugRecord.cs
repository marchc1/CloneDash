using Nucleus.Util;

namespace Nucleus.Debugging
{
public struct DebugRecord
{
	InlineArray512<char> KeyValueData;
	public int KeySize;
	public int ValueSize;
	public int Spacing;
	public bool HasValue;

	public bool GetText(Span<char> output) => KeyValueData[..KeySize].TryCopyTo(output);
	public bool GetKey(Span<char> output) => KeyValueData[..KeySize].TryCopyTo(output);
	public bool GetValue(Span<char> output) => KeyValueData[KeySize..][..ValueSize].TryCopyTo(output);

	public DebugRecord(int spacing, ReadOnlySpan<char> key, ReadOnlySpan<char> value = default, bool valueless = false) {
		if (!key.TryCopyTo(KeyValueData) || !value.TryCopyTo(KeyValueData[key.Length..])) {
			Logs.Warn("DebugRecord overflow (store less text)");
		}

		Spacing = spacing;
		KeySize = key.Length;
		ValueSize = value.Length;
		HasValue = !valueless;
	}

	static readonly char[] tempprintbuffer = new char[1024];
	public ReadOnlySpan<char> Print(in DebugRecordState state) {
		Span<char> buffer = tempprintbuffer.AsSpan();
		int charIdx = 0;
		for (int i = 0; i < Spacing; i++) tempprintbuffer[charIdx++] = ' ';
		GetKey(buffer[charIdx..]); charIdx += KeySize;
		if (!HasValue)
			return buffer[..charIdx];

		int fillSpacesUntil = state.MaxKeyPlusSpacingSize - (KeySize - Spacing);
		for (int i = 0; i < fillSpacesUntil; i++) tempprintbuffer[charIdx++] = ' ';
		tempprintbuffer[charIdx++] = ' ';
		tempprintbuffer[charIdx++] = ':';
		tempprintbuffer[charIdx++] = ' ';
		GetValue(buffer[charIdx..]); charIdx += ValueSize;

		return buffer[..charIdx];
	}

	// public static implicit operator DebugRecord(string from) {
	// 	bool containsValue = false;
	// 	ReadOnlySpan<char> key = "";
	// 	ReadOnlySpan<char> value = null;
	// 
	// 	var colon = from.IndexOf(':');
	// 	if (colon == -1)
	// 		containsValue = false;
	// 	else {
	// 		if (colon == from.Length - 1)
	// 			containsValue = false;
	// 		else
	// 			containsValue = true;
	// 	}
	// 
	// 	if (containsValue) {
	// 		key = from.AsSpan()[..colon];
	// 		value = from.AsSpan()[(colon + 1)..].Trim();
	// 	}
	// 	else
	// 		key = from;
	// 
	// 	return new(0, key, value, !containsValue);
	// }
}
}