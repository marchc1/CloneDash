using CloneDash.Game.Input;
using Nucleus;
using Nucleus.Common.Input;
using Nucleus.Engine;

namespace CloneDash.Common.Gamemodes.MuseDash;

[MarkForStaticConstruction]
public static class MuseDashActions
{
	public const string SET = "musedash";

	public static readonly InputAction Air = InputAction.Register("musedash/air");
	public static readonly InputAction Fever = InputAction.Register("musedash/fever");
	public static readonly InputAction Ground = InputAction.Register("musedash/ground");

	public static KeyBindSet Binds => Key.GetSet(SET);

	static MuseDashActions() {
		Air.BindDefault(SET, ButtonCode.KeyS, ButtonCode.KeyD, ButtonCode.KeyF, ButtonCode.KeyG, ButtonCode.MouseRight);
		Ground.BindDefault(SET, ButtonCode.KeyH, ButtonCode.KeyJ, ButtonCode.KeyK, ButtonCode.KeyL, ButtonCode.MouseLeft);
		Fever.BindDefault(SET, ButtonCode.KeySpace);
	}
}
