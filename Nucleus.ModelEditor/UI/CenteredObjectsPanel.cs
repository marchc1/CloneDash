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
		protected override void PostLayoutChildren() {
			float sizeOfAllChildren = 0;
			foreach (var child in this.GetChildren()) {
				sizeOfAllChildren += child.RenderBounds.W;
			}
			var center = (this.RenderBounds.W / 2) - (sizeOfAllChildren / 2);
			foreach (var child in this.GetChildren()) {
				var h = MathF.Min(child.Size.Y, this.RenderBounds.H);
				child.Position = new(center, ForceHeight ? 0 : (this.RenderBounds.H - h));
				child.Size = new(child.Size.X, ForceHeight ? this.RenderBounds.H : h);
				center += child.Size.X;
			}
		}
		public override bool HoverTest(RectangleF bounds, Vector2F mousePos) {
			return false;
		}
	}
}
