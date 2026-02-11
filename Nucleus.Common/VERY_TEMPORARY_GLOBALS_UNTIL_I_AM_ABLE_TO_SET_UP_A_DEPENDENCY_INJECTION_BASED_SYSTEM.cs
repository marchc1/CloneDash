using Nucleus.Common.Commands;

namespace Nucleus.Common;

// Mark any dependencies that set these globals with VERY_TEMPORARY_GLOBALS_UNTIL_I_AM_ABLE_TO_SET_UP_A_DEPENDENCY_INJECTION_BASED_SYSTEM so
// they're easy to find later.
public static class VERY_TEMPORARY_GLOBALS_UNTIL_I_AM_ABLE_TO_SET_UP_A_DEPENDENCY_INJECTION_BASED_SYSTEM{
	public static readonly GlobalVariablesBase globals = new();
	public static ICvar cvar = null!;
}
