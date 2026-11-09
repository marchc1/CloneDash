using CloneDash.Common.Gamemodes.MuseDash;
using CloneDash.Game.Input;
using Nucleus;
using Nucleus.Common.Input;

namespace CloneDash.MuseDashCustomPlayCompatibility;

[MarkForStaticConstruction]
public static class CustomPlayActions
{
	public static readonly InputAction Middle = InputAction.Register("mdcp/middle");

	static CustomPlayActions() {
		Middle.BindDefault(MuseDashActions.SET, ButtonCode.KeyW, ButtonCode.KeyE, ButtonCode.KeyO, ButtonCode.KeyP);
	}
}
