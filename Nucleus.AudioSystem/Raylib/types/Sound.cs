using System.Runtime.InteropServices;

namespace Nucleus.AudioSystem.Raylib;

/// <summary>
/// Sound source type
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public partial struct Sound
{
    /// <summary>
    /// Audio stream
    /// </summary>
    public AudioStream Stream;

    /// <summary>
    /// Total number of frames (considering channels)
    /// </summary>
    public uint FrameCount;
}
