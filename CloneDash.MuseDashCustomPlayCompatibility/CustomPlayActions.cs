using CloneDash.Game.Input;
using Nucleus;

namespace CloneDash.MuseDashCustomPlayCompatibility;

[MarkForStaticConstruction]
public static class CustomPlayActions
{
	public static readonly InputAction Middle = InputAction.Register("mdcp/middle");
}
