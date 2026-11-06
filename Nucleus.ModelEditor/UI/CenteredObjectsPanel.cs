using Nucleus.Types;
using Nucleus.UI;

namespace Nucleus.ModelEditor
{
	public class CenteredObjectsPanel : Panel
	{
		public CenteredObjectsPanel(Element parent) : base(parent) {
			SetPaintBackgroundEnabled(false);
			BorderSize = 0;
		}
		public bool ForceHeight { get; set; } = true;
		public float XSeparation { get; set; } = 0;
		public float YSeparation { get; set; } = 0;

		/// <summary>
		/// Hide children from the end until the rest fit
		/// </summary>
		public bool DropOverflow { get; set; } = false;

		/// <summary>
		/// Lay children out in centered rows of <see cref="RowHeight"/>, growing the panel to fit
		/// Children wider than the panel get shrunk, only <see cref="IconButton"/>s grow back
		/// </summary>
		public bool Wrap { get; set; } = false;
		public float RowHeight { get; set; } = 32;

		private static float NaturalWidth(Element child) =>
			child is IconButton button ? button.GetNaturalWidth() : child.Size.X;

		protected override void PerformLayout(float width, float height) {
			using var children = this.GetChildren();
			if (Wrap) {
				WrapLayout(children.AsSpan());
				return;
			}

			float available = this.GetRenderBounds().W;
			float sizeOfAllChildren = 0;
			int fits = children.Length;
			for (int i = 0; i < children.Length; i++) {
				var next = sizeOfAllChildren + children[i].Size.X + XSeparation;
				if (DropOverflow && next > available) {
					fits = i;
					break;
				}
				sizeOfAllChildren = next;
			}
			var center = (available / 2) - (sizeOfAllChildren / 2);
			for (int i = 0; i < children.Length; i++) {
				var child = children[i];
				if (DropOverflow)
					child.SetVisible(i < fits);
				if (i >= fits) continue;

				var h = MathF.Min(child.Size.Y, this.GetRenderBounds().H - YSeparation);
				child.				Position = new(center, ForceHeight ? (YSeparation / 2f) : (this.GetRenderBounds().H - h));
				child.Size = new(child.Size.X, ForceHeight ? this.GetRenderBounds().H - YSeparation : h);
				center += child.Size.X + XSeparation;
			}
		}

		float wrappedHeight = -1;
		private void WrapLayout(ReadOnlySpan<Element> children) {
			float available = MathF.Max(GetRenderBounds().W, 1);
			float y = YSeparation / 2;
			int rows = 0;
			int start = 0;
			while (start < children.Length) {
				// fill a row
				int end = start;
				float rowWidth = 0;
				for (; end < children.Length; end++) {
					if (!children[end].IsVisible()) continue;
					float width = MathF.Min(NaturalWidth(children[end]), available);
					float next = rowWidth + (rowWidth > 0 ? XSeparation : 0) + width;
					if (rowWidth > 0 && next > available) break;
					rowWidth = next;
				}

				float x = MathF.Round((available - rowWidth) / 2);
				bool any = false;
				for (int i = start; i < end; i++) {
					var child = children[i];
					if (!child.IsVisible()) continue;
					float width = MathF.Min(NaturalWidth(child), available);
					child.Position = new(x, y);
					child.Size = new(width, RowHeight);
					x += width + XSeparation;
					any = true;
				}
				if (any) {
					rows++;
					y += RowHeight + XSeparation;
				}
				start = end;
			}

			wrappedHeight = rows == 0 ? 0 : YSeparation + rows * RowHeight + (rows - 1) * XSeparation;
		}

		// autosized children settle after the first layout
		float laidOutWidths = -1;
		protected override void OnThink() {
			base.OnThink();
			float widths = 0;
			using (var children = GetChildren()) {
				foreach (var child in children) {
					if (!Wrap)
						widths += child.Size.X;
					else if (child.IsVisible())
						widths += NaturalWidth(child);
				}
			}
			if (widths != laidOutWidths) {
				laidOutWidths = widths;
				InvalidateLayout();
			}

			if (Wrap && wrappedHeight >= 0 && MathF.Abs(wrappedHeight - Size.Y) > 0.5f)
				Size = new(Size.X, wrappedHeight);
		}

		public override bool HoverTest(RectangleF bounds, Vector2F mousePos) {
			return false;
		}
	}
}
