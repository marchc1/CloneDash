using Nucleus.Core;
using Nucleus.Types;
using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.UI
{
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
}
