using Nucleus.Util;

namespace Nucleus.Debugging;

public struct DebugRecord
{
	private InlineArray512<char> _keyValueData;
	public readonly int KeySize;
	public readonly int ValueSize;
	public readonly int Spacing;
	public readonly bool HasValue;

	public bool GetText(Span<char> output) => _keyValueData[..KeySize].TryCopyTo(output);
	public bool GetKey(Span<char> output) => _keyValueData[..KeySize].TryCopyTo(output);
	public bool GetValue(Span<char> output) => _keyValueData[KeySize..][..ValueSize].TryCopyTo(output);

	public DebugRecord(int spacing, ReadOnlySpan<char> key, ReadOnlySpan<char> value = default, bool valueless = false) {
		if (!key.TryCopyTo(_keyValueData) || !value.TryCopyTo(_keyValueData[key.Length..])) {
			Logs.Warn("DebugRecord overflow (store less text)");
		}

		Spacing = spacing;
		KeySize = key.Length;
		ValueSize = value.Length;
		HasValue = !valueless;
	}

	static readonly char[] TempPrintBuffer = new char[1024];

	public ReadOnlySpan<char> Print(in DebugRecordState state) {
		Span<char> buffer = TempPrintBuffer.AsSpan();
		int charIdx = 0;
		for (int i = 0; i < Spacing; i++) TempPrintBuffer[charIdx++] = ' ';
		GetKey(buffer[charIdx..]);
		charIdx += KeySize;
		if (!HasValue)
			return buffer[..charIdx];

		int fillSpacesUntil = state.MaxKeyPlusSpacingSize - (KeySize - Spacing);
		for (int i = 0; i < fillSpacesUntil; i++) TempPrintBuffer[charIdx++] = ' ';
		TempPrintBuffer[charIdx++] = ' ';
		TempPrintBuffer[charIdx++] = ':';
		TempPrintBuffer[charIdx++] = ' ';
		GetValue(buffer[charIdx..]);
		charIdx += ValueSize;

		return buffer[..charIdx];
	}
}