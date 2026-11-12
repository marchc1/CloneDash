using CloneDash.Game.Input;
using CloneDash.Settings;
using Nucleus;
using Nucleus.Common.Input;
using Nucleus.Engine;

namespace CloneDash.Compatibility.UnbeatableWhiteLabel;

[MarkForStaticConstruction]
public static class UnbeatableWhiteLabelActions
{
	public const string SET = "unbeatable";

	public static readonly InputAction Low = InputAction.Register("unbeatable/low");
	public static readonly InputAction Mid = InputAction.Register("unbeatable/mid");
	public static readonly InputAction Top = InputAction.Register("unbeatable/top");

	public static KeyBindSet Binds => Key.GetSet(SET);

	static UnbeatableWhiteLabelActions() {
		Top.BindDefault(SET, ButtonCode.KeyS, ButtonCode.KeyD, ButtonCode.KeyF, ButtonCode.KeyG, ButtonCode.MouseRight);
		Low.BindDefault(SET, ButtonCode.KeyH, ButtonCode.KeyJ, ButtonCode.KeyK, ButtonCode.KeyL, ButtonCode.MouseLeft);
		InputSettings.ShareLegacyBinds(SET, Top, Low, Mid);
	}
}
