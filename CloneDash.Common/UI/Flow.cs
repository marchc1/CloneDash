using Nucleus.Types;
using Nucleus.UI;

namespace CloneDash.Common.UI
{
	public class Flow : Element
	{
		public FlowDirection Direction { get; set; } = FlowDirection.Horizontal;
		public float Spacing { get; set; } = 0;
		public Axis AutoSize { get; set; } = Axis.None;

		public Flow(Element? parent) : base(parent) {
			SetBorderSize(0);
		}

		protected override void OnThink() {
			base.OnThink();
			
			// putting this in PerformLayout() only seems to actually update the sizing
			// when the window is resized, not when the items get added
			
			ReadOnlySpan<Element> children = GetChildren();
			if (children.Length == 0) return;

			Vector2F current = Vector2F.Zero;

			for (int i = 0; i < children.Length; i++) {
				if (i != 0) {
					if (Direction == FlowDirection.Horizontal) current.X += Spacing;
					else current.Y += Spacing;
				}

				Element element = children[i];
				Vector2F s = element.GetRenderBounds().Size;
				Vector2F p = element.GetPos();

				if (Direction == FlowDirection.Horizontal) {
					element.SetPos(new Vector2F(current.X, p.Y));
					current.X += s.X;
					current.Y = Math.Max(s.Y, current.Y);
				}
				else {
					element.SetPos(new Vector2F(p.X, current.Y));
					current.X = Math.Max(s.X, current.X);
					current.Y += s.Y;
				}
			}

			if (AutoSize == Axis.None)
				return;

			Vector2F size = GetSize();
			if (AutoSize.HasFlag(Axis.Horizontal)) size.X = current.X;
			if (AutoSize.HasFlag(Axis.Vertical)) size.Y = current.Y;
			SetSize(size);
		}
	}

	public enum FlowDirection
	{
		Horizontal,
		Vertical
	}
}