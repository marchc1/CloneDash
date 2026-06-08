using Nucleus.Extensions;
using Nucleus.Types;
using System.Runtime.InteropServices;

namespace Nucleus.Core;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ConsoleMessage
{
	public const int CONSOLE_MESSAGE_HEADER_SIZE = sizeof(ulong) + sizeof(LogLevel) + sizeof(int);
	public DateTime Time;
	public LogLevel Level;
	public int Length;
	// ... and the message follows, until reaching Length.

	public double GetAge() => (DateTime.Now - Time).TotalSeconds;

	/// <summary>
	/// Parses a console message out of a contiguous memory chunk and forwards it to the next message.
	/// </summary>
	public static bool ParseOneConsoleMessage(scoped ref ReadOnlySpan<byte> rawBytes, out ConsoleMessage header, scoped out ReadOnlySpan<char> message) {
		if (rawBytes.Length < CONSOLE_MESSAGE_HEADER_SIZE) {
			header = default;
			message = default;
			return false;
		}
		// header readable, lets try
		header = MemoryMarshal.Cast<byte, ConsoleMessage>(rawBytes[0..CONSOLE_MESSAGE_HEADER_SIZE])[0];
		// the message will be at CONSOLE_MESSAGE_HEADER_SIZE until length. Length is the size in CHARACTERS so that should be noted
		message = MemoryMarshal.Cast<byte, char>(rawBytes[CONSOLE_MESSAGE_HEADER_SIZE..][..(header.Length * sizeof(char))]);
		rawBytes = rawBytes[(CONSOLE_MESSAGE_HEADER_SIZE + header.Length * sizeof(char))..];
		return true;
	}
}

public ref struct LiveConsoleMessage
{
	public ConsoleMessage Header;
	public ReadOnlySpan<char> Text;
}


/// <summary>
/// A more memory efficient way of storing console messages
/// </summary>
public class ConsoleMessageList
{
	byte[] mem = new byte[4096];
	int start;
	int end;

	int _iterating;
	public void BeginRead() => _iterating++;
	public void EndRead() => _iterating--;

	public LiveConsoleMessage AddToEnd(LogLevel level, DateTime time, ReadOnlySpan<char> text) {
		int headerSize = ConsoleMessage.CONSOLE_MESSAGE_HEADER_SIZE;
		int messageBytes = text.Length * sizeof(char);
		int totalNeeded = headerSize + messageBytes;

		EnsureSpace(totalNeeded);

		var header = new ConsoleMessage { Level = level, Time = time, Length = text.Length };
		MemoryMarshal.Write(mem.AsSpan(end), in header);
		end += headerSize;
		MemoryMarshal.Cast<char, byte>(text).CopyTo(mem.AsSpan(end));
		end += messageBytes;

		return new() {
			Header = header,
			Text = text
		};
	}

	public bool RemoveFromStart() {
		ReadOnlySpan<byte> span = mem.AsSpan(start, end - start);
		if (!ConsoleMessage.ParseOneConsoleMessage(ref span, out _, out var message))
			return false;
		start = end - span.Length;
		CompactIfNeeded();
		return true;
	}

	// Compact + reallocate if needed
	private void EnsureSpace(int needed) {
		if (end + needed <= mem.Length)
			return;
		CompactIfNeeded();
		if (end + needed > mem.Length)
			Array.Resize(ref mem, Math.Max(mem.Length * 2, end + needed));
	}

	// Compact the backing memory if needed
	private void CompactIfNeeded() {
		if (_iterating > 0) return; // defer
		if (start == 0) return;
		if (start == end) { start = end = 0; return; }
		mem.AsSpan(start, end - start).CopyTo(mem);
		end -= start;
		start = 0;
	}

	public int ComputeCount() {
		int count = 0;
		ReadOnlySpan<byte> span = mem.AsSpan(start, end - start);
		while (ConsoleMessage.ParseOneConsoleMessage(ref span, out _, out _))
			count++;
		return count;
	}

	public int GetMessages(Span<int> offsets, out int maxMessageLength) {
		int count = 0;
		ReadOnlySpan<byte> span = mem.AsSpan(start, end - start);
		int pos = start;
		maxMessageLength = 0;
		while (count < offsets.Length && ConsoleMessage.ParseOneConsoleMessage(ref span, out var header, out _)) {
			offsets[count++] = pos;
			pos = end - span.Length;
			if (header.Length > maxMessageLength)
				maxMessageLength = header.Length;
		}
		return count;
	}

	public bool GetMessageAt(ReadOnlySpan<int> offsets, int index, out ConsoleMessage header, out ReadOnlySpan<char> message) {
		if (index < 0 || index >= offsets.Length) {
			header = default;
			message = default;
			return false;
		}
		ReadOnlySpan<byte> span = mem.AsSpan(offsets[index], end - offsets[index]);
		return ConsoleMessage.ParseOneConsoleMessage(ref span, out header, out message);
	}

	public void RemoveExpired(double maxAgeSeconds) {
		ReadOnlySpan<byte> span = mem.AsSpan(start, end - start);
		while (ConsoleMessage.ParseOneConsoleMessage(ref span, out var header, out _)) {
			if (header.GetAge() > maxAgeSeconds)
				start = end - span.Length;
			else
				break;
		}
		CompactIfNeeded();
	}
}