using Nucleus.Core;
using Nucleus.Types;
using Nucleus.UI.Elements;
using System.Xml.Linq;

namespace Nucleus.UI;

public class ScrollPanel : Panel
{
	public class ScrollMainPanel : Panel
	{
		ScrollPanel parent;
		public ScrollMainPanel(ScrollPanel parent, ReadOnlySpan<char> name = default) : base(parent, name) {
			this.parent = parent;
			SetPaintBackgroundEnabled(false);
			SetPaintBorderEnabled(false);
		}
		public Dock? ChildDock;
		protected override void ChildParented(Element parent, Element child) {
			base.ChildParented(parent, child);
			if (ChildDock.HasValue)
				child.Dock = ChildDock.Value;
		}
		protected override bool MouseScroll(Element self, FrameState state, Vector2F delta) {
			if (delta.X != 0)
				parent.HorizontalScrollbar.MouseScrolled(parent.HorizontalScrollbar, state, delta);
			if (delta.Y != 0)
				parent.VerticalScrollbar.MouseScrolled(parent.VerticalScrollbar, state, delta);

			return true;
		}
	}

	public ScrollPanel(Element? parent, ReadOnlySpan<char> name = default) : base(parent, name) {
		VerticalScrollbar = new Scrollbar(this);
		VerticalScrollbar.Alignment = ScrollbarAlignment.Vertical;
		VerticalScrollbar.SetVisible(true);

		HorizontalScrollbar = new Scrollbar(this);
		HorizontalScrollbar.Alignment = ScrollbarAlignment.Horizontal;
		HorizontalScrollbar.SetVisible(true);

		MainPanel = new ScrollMainPanel(this);
		MainPanel.Dock = Dock.Fill;
		MainPanel.SetPaintBackgroundEnabled(false);
		MainPanel.DockMargin = RectangleF.TLRB(4);
		AddParent = MainPanel;
		MainPanel.Clipping = false;
	}
	public Scrollbar VerticalScrollbar { get; private set; }
	public Scrollbar HorizontalScrollbar { get; private set; }
	public ScrollMainPanel MainPanel { get; private set; }

	bool horizontalOverflow = true, verticalOverflow = true;

	public bool HorizontalOverflow {
		get => horizontalOverflow; set {
			horizontalOverflow = value;
			InvalidateLayout();
		}
	}

	public bool VerticalOverflow {
		get => verticalOverflow; set {
			verticalOverflow = value;
			InvalidateLayout();
		}
	}

	protected override void PerformLayout(float width, float height) {
		base.PerformLayout(width, height);
	}
	public virtual bool ShouldItemBeVisible(Element e) {
		return true;
	}
	protected override void OnThink() {
		base.OnThink();

		if (VerticalOverflow) {
			VerticalScrollbar.PageContents = AddParent.SizeOfAllChildren;
			VerticalScrollbar.PageSize = AddParent.RenderBounds.Size;
		}
		else {
			VerticalScrollbar.PageContents = new(0);
		}

		if (HorizontalOverflow) {
			HorizontalScrollbar.PageContents = AddParent.SizeOfAllChildren;
			HorizontalScrollbar.PageSize = AddParent.RenderBounds.Size;
		}
		else {
			HorizontalScrollbar.PageContents = new(0);
		}

		MainPanel.Clipping = true;

		VerticalScrollbar.Update(AddParent.SizeOfAllChildren, AddParent.RenderBounds.Size);
		HorizontalScrollbar.Update(AddParent.SizeOfAllChildren, AddParent.RenderBounds.Size);

		foreach (Element child in MainPanel.Children)
			child.SetVisible(ShouldItemBeVisible(child));

		MainPanel.ChildRenderOffset = new Vector2F(HorizontalScrollbar.Scroll, -VerticalScrollbar.Scroll).Round();
	}
	protected override void PostLayoutChild(Element element) {

	}
	public override void PaintBorder(float width, float height) {
		Graphics2D.SetDrawColor(GetFgColor());
		Graphics2D.DrawRectangleOutline(0, 0, width, height, 1);
	}
}
