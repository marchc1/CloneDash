using Nucleus.Types;

namespace Nucleus.UI;

public class Panel : Element
{
	public Panel(Element? parent, ReadOnlySpan<char> name = default) : base(parent, name) {
		DockPadding = RectangleF.TLRB(2);
		AddFlag(ElementFlags.PaintBackgroundEnabled | ElementFlags.PaintBorderEnabled | ElementFlags.PaintEnabled);
	}

	public override void Paint(float width, float height) {

	}
}
