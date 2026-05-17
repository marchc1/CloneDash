using Nucleus.Types;

namespace Nucleus.UI;

public class Panel : Element
{
	public Panel(Element? parent, ReadOnlySpan<char> name = default) : base(parent, name) {
		DockPadding = RectangleF.TLRB(2);
	}
	public bool DrawPanelBackground { get; set; } = true;

	public override void Paint(float width, float height) {
		if (!DrawPanelBackground) {
			if (ShouldDrawImage)
				ImageDrawing();

			return;
		}

		PaintBackground(this, width, height);
	}
}
