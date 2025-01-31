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
			var childrenCount = AddParent.Children.Count;
			var addOffset = 0f;
			for (int i = 0; i < childrenCount; i++) {
				Element child = AddParent.Children[i];
				switch (Direction) {
					case Directional180.Vertical:
						child.Position = new Vector2F(child.Position.X, addOffset);
						addOffset += child.Size.H + padding;
						break;
					case Directional180.Horizontal:
						child.Position = new Vector2F(addOffset, child.Position.Y);
						addOffset += child.RenderBounds.W + padding;
						break;
				}

				if (SizeChildrensOppositeSideToEdge) {
					switch (Direction) {
						case Directional180.Vertical:
							var x = child.RenderBounds.X;
							var uW = AddParent.RenderBounds.W;
							child.Size = new Vector2F(uW - x, child.Size.H);
							break;
						case Directional180.Horizontal:
							var y = child.RenderBounds.Y;
							var uH = AddParent.RenderBounds.H;
							child.Size = new Vector2F(child.Size.W, uH - y);
							break;
					}
				}
			}
			if (AutoSize) {
				switch (Direction) {
					case Directional180.Vertical:
						SetRenderBounds(h: addOffset);
						this.Size = new(this.Size.W, addOffset);
						break;

					case Directional180.Horizontal:

						break;
				}
			}
		}
	}
}
