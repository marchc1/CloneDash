using Nucleus.ManagedMemory;

namespace Nucleus.Common.Graphics;

public interface ITexture : IManagedMemoryUnit
{
	public uint HardwareID { get; }
	public int Width { get; }
	public int Height { get; }
	public ImageFormat Format { get; }
	public ulong UsedBits_CPU { get; }
}
