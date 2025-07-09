using Nucleus.Types;

namespace Nucleus.UI.Elements
{
	/// <summary>
	/// Lays out all children in order, depending on direction.
	/// </summary>
	public class DirectionalLayoutPanel : ScrollPanel
	{
		private Directional180 direction = Directional180.Vertical;
		public Directional180 Direction {
			get => direction;
			set {
				direction = value;
				InvalidateLayout();
			}
		}

		public float WidthOffset { get; set; } = 0;

		private float padding = 4;
		public float Padding {
			get => padding;
			set {
				padding = value;
				InvalidateLayout();
			}
		}

		private bool sizetoedge = false;
		// big name
		public bool SizeChildrensOppositeSideToEdge {
			get => sizetoedge;
			set {
				sizetoedge = value;
				InvalidateLayout();
			}
		}

		protected override void Initialize() {
			base.Initialize();

			MainPanel.OnChildParented += MainPanel_OnChildParented;
		}

		private void MainPanel_OnChildParented(Element parent, Element child) {
			child.Dock = Dock.Top;
		}

		private bool autosize = false;
		public bool AutoSize {
			get => autosize;
			set {
				autosize = value;
				InvalidateLayout();
			}
		}

		protected override void OnThink(FrameState frameState) {
			if (!AutoSize) {
				base.OnThink(frameState);
			}
			else {
				base.OnThink(frameState);
				VerticalScrollbar.Visible = false;
				VerticalScrollbar.Enabled = false;
				HorizontalScrollbar.Visible = false;
				HorizontalScrollbar.Enabled = false;
			}
		}

		protected override void PostLayoutChildren() {
			base.PostLayoutChildren();
			float size = 0;

			foreach(var child in AddParent.Children) {
				size = MathF.Max(size, child.RenderBounds.Y + child.RenderBounds.H + 8);
			}

			if (AutoSize) {
				this.SetRenderBounds(h: size + 8);
				this.MainPanel.SetRenderBounds(h: size + 8);
			}
		}
	}
}
