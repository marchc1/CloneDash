using Nucleus.Common.Commands;

namespace Nucleus.Common;

[EngineComponent] 
public static class CommonDependencies{
	[Dependency] public static GlobalVariablesBase globals = new();
	[Dependency] public static ICvar cvar = null!;
}
