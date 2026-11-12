using Nucleus.Common.Input;
using Nucleus.Types;
using Nucleus.UI;

namespace CloneDash.Scenes;

public class MuseDash1PauseButton : Element
{
	public MuseDash1PauseButton(Element? parent) : base(parent) {
		SetPaintBackgroundEnabled(false);
		SetPaintBorderEnabled(false);
	}

	public Action? Clicked;

	protected override bool MouseClick(FrameState state, ButtonCode button) {
		if (button != ButtonCode.MouseLeft)
			return false;

		Clicked?.Invoke();
		return true;
	}
}
