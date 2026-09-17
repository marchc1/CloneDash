using System;
using System.Runtime.InteropServices;

namespace Nucleus.AudioSystem.Raylib;

/// <summary>
/// Converts text to a Ansi buffer for passing to native code
/// </summary>
internal readonly ref struct AnsiBuffer
{
    private readonly IntPtr _data;

    public AnsiBuffer(string text)
    {
        _data = Marshal.StringToHGlobalAnsi(text);
    }

    public unsafe sbyte* AsPointer()
    {
        return (sbyte*)_data.ToPointer();
    }

    public void Dispose()
    {
        Marshal.FreeHGlobal(_data);
    }
}

internal static class AnsiStringUtils
{
	internal static AnsiBuffer ToAnsiBuffer(this string text)
    {
        return new AnsiBuffer(text);
    }
}
