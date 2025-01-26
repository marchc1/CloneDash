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
		public bool DrawPanelBackground { get; set; } = true;
		protected override void Initialize() {
			base.Initialize();
			this.DockPadding = RectangleF.TLRB(2);
		}

		public override void Paint(float width, float height) {
			if (!DrawPanelBackground) return;

			PaintBackground(this, width, height);
		}
	}
}
