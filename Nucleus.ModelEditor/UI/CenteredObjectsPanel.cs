using Nucleus.Types;
using Nucleus.UI;

namespace Nucleus.ModelEditor
{
	public class CenteredObjectsPanel : Panel
	{
		public CenteredObjectsPanel(Element parent) : base(parent) {
			SetPaintBackgroundEnabled(false);
			SetBorderSize(0);
		}
		public bool ForceHeight { get; set; } = true;
		public float XSeparation { get; set; } = 0;
		public float YSeparation { get; set; } = 0;

		protected override void PerformLayout(float width, float height) {
			float sizeOfAllChildren = 0;
			foreach (var child in this.GetChildren()) {
				sizeOfAllChildren += child.GetRenderBounds().W + XSeparation;
			}
			var center = (this.GetRenderBounds().W / 2) - (sizeOfAllChildren / 2);
			foreach (var child in this.GetChildren()) {
				var h = MathF.Min(child.GetSize().Y, this.GetRenderBounds().H - YSeparation);
				child.SetPos(new(center, ForceHeight ? (YSeparation / 2f) : (this.GetRenderBounds().H - h)));
				child.SetSize(new(child.GetRenderBounds().W, ForceHeight ? this.GetRenderBounds().H - YSeparation : h));
				center += child.GetRenderBounds().W + XSeparation;
			}
		}
		public override bool HoverTest(RectangleF bounds, Vector2F mousePos) {
			return false;
		}
	}
}
