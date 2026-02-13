global using static Nucleus.NucleusDllMain;
using Nucleus.Common.Engine;

namespace Nucleus;

public static class NucleusDllMain
{
	[Dependency] public static IGameDLL gameDLL = null!;
	[Dependency] public static IEngine engine = null!;
	[Dependency] public static IEngineAPI engineAPI = null!;
}
