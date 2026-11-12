using Nucleus;
using Nucleus.Common.Input;
using Nucleus.Engine;

namespace CloneDash.Game.Input;

[MarkForStaticConstruction]
public static class CommonActions
{
	public static readonly InputAction Pause = InputAction.Register("common/pause");

	static CommonActions() {
		Pause.BindDefault(Key.COMMON, ButtonCode.KeyEscape);
	}
}
