using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Raylib_cs;

/// <summary>
/// Converts text to a UTF8 buffer for passing to native code
/// </summary>
public readonly ref struct Utf8Buffer
{
	private readonly IntPtr Data;
	private readonly int Length;

	public unsafe Utf8Buffer(ReadOnlySpan<char> text) {
		if(text == null || text.IsEmpty) {
			Data = 0;
			Length = 0;
			return;
		}

		Length = Encoding.UTF8.GetByteCount(text);
		Data = Marshal.AllocCoTaskMem(Length + 1);

		var span = new Span<byte>((void*)Data, Length + 1);
		span.Clear();
		Encoding.UTF8.GetBytes(text, span);
	}

	public unsafe sbyte* AsPointer() => (sbyte*)Data.ToPointer();

	public unsafe void Dispose() {
		if (Data == 0)
			return;

		new Span<byte>((void*)Data, Length + 1).Clear();
		Marshal.FreeCoTaskMem(Data);
	}
}

public static class Utf8StringUtils
{
	public static Utf8Buffer ToUtf8Buffer(this string text) {
		return new Utf8Buffer(text);
	}
	public static Utf8Buffer ToUtf8Buffer(this ReadOnlySpan<char> text) {
		return new Utf8Buffer(text);
	}

	public static byte[] ToUtf8String(this string text)
    {
        if (text == null)
        {
            return null;
        }

        var length = Encoding.UTF8.GetByteCount(text);

        var byteArray = new byte[length + 1];
        var wrote = Encoding.UTF8.GetBytes(text, 0, text.Length, byteArray, 0);
        byteArray[wrote] = 0;

        return byteArray;
    }

    public static unsafe string GetUTF8String(sbyte* bytes)
    {
        return Marshal.PtrToStringUTF8((IntPtr)bytes);
    }

    public static byte[] GetUTF8Bytes(this string text)
    {
        return Encoding.UTF8.GetBytes(text);
    }
}
