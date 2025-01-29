using Nucleus.Core;
using Nucleus.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.UI
{
	public class Checkbox : Button
	{
		public bool Checked { get; set; } = false;
		protected override void Initialize() {
			Text = "";
		}
		public delegate void CheckboxClicked(Checkbox self);
		public event CheckboxClicked? OnCheckedChanged;

		private float? CheckAnim = null;

		public override void Paint(float width, float height) {
			float c = CheckAnim ?? (Checked ? 1 : 0);
			c = Math.Clamp(c + (EngineCore.FrameTime * 6f * (Checked ? 1 : -1)), 0, 1);
			CheckAnim = c;

			base.Paint(width, height);
			if (c > 0) {
				c = NMath.Ease.InQuad(c);
				Graphics2D.SetDrawColor(TextColor);
				Graphics2D.DrawLineStrip([new(width * 0.25f, height * 0.55f), new(width / 2f, height * 0.8f), new(width * 0.75f, height * 0.28f)], c);
			}
		}

		public override void MouseRelease(Element self, FrameState state, MouseButton button) {
			Checked = !Checked;
			OnCheckedChanged?.Invoke(this);
		}
	}
}
