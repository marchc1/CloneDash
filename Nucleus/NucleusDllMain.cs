global using static Nucleus.NucleusDllMain;
using Nucleus.Common.Engine;

namespace Nucleus;

public static class NucleusDllMain
{
	// ReSharper disable InconsistentNaming
	// ReSharper disable FieldCanBeMadeReadOnly.Global
#pragma warning disable CA2211
	[Dependency] public static IGameDLL gameDLL = null!;
	[Dependency] public static IEngine engine = null!;
	[Dependency] public static IEngineAPI engineAPI = null!;
#pragma warning restore CA2211
	// ReSharper restore FieldCanBeMadeReadOnly.Global
	// ReSharper restore InconsistentNaming
}
