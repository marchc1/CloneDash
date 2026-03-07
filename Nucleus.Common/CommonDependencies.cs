using Nucleus.Common.Audio;
using Nucleus.Common.Commands;
using Nucleus.Common.FileSystem;
using Nucleus.Common.Graphics;

namespace Nucleus.Common;

[EngineComponent] 
public static class CommonDependencies{
#pragma warning disable CA2211 // Non-constant fields should not be visible
	[Dependency] public static IAudioSystem audiosystem = null!;
	[Dependency] public static ICvar cvar = null!;
	[Dependency] public static IGraphicsHardwareConfig gfxHardwareConfig = null!;
	[Dependency] public static GlobalVariablesBase globals = new();
	[Dependency] public static IFileSystem filesystem = null!;
#pragma warning restore CA2211 // Non-constant fields should not be visible
}
