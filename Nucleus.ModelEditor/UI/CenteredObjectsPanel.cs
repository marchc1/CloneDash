using Nucleus.Types;
using Nucleus.UI;

namespace Nucleus.ModelEditor
{
	public class CenteredObjectsPanel : Panel {

		public bool ForceHeight { get; set; } = true;
		protected override void Initialize() {
			base.Initialize();
			DrawPanelBackground = false;
		}
		public float XSeparation { get; set; } = 0;
		public float YSeparation { get; set; } = 0;
		protected override void PostLayoutChildren() {
			float sizeOfAllChildren = 0;
			foreach (var child in this.GetChildren()) {
				sizeOfAllChildren += child.RenderBounds.W + XSeparation;
			}
			var center = (this.RenderBounds.W / 2) - (sizeOfAllChildren / 2);
			foreach (var child in this.GetChildren()) {
				var h = MathF.Min(child.Size.Y, this.RenderBounds.H - YSeparation);
				child.Position = new(center, ForceHeight ? (YSeparation / 2f) : (this.RenderBounds.H - h));
				child.Size = new(child.RenderBounds.W, ForceHeight ? this.RenderBounds.H - YSeparation : h);
				center += child.RenderBounds.W + XSeparation;
			}
		}
		public override bool HoverTest(RectangleF bounds, Vector2F mousePos) {
			return false;
		}
	}
}
