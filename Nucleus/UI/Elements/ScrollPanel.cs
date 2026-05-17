using Nucleus.Core;
using Nucleus.Types;
using Nucleus.UI.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.UI
{
	public class ScrollMainPanel(Element? parent, ReadOnlySpan<char> name = default) : Panel(parent, name);
	public class ScrollPanel : Panel
	{
		public ScrollPanel(Element? parent, ReadOnlySpan<char> name = default) : base(parent, name) {
			VerticalScrollbar = new Scrollbar(this);
			VerticalScrollbar.Alignment = ScrollbarAlignment.Vertical;
			VerticalScrollbar.Enabled = true;

			HorizontalScrollbar = new Scrollbar(this);
			HorizontalScrollbar.Alignment = ScrollbarAlignment.Horizontal;
			HorizontalScrollbar.Enabled = true;

			MainPanel = new ScrollMainPanel(this);
			MainPanel.Dock = Dock.Fill;
			MainPanel.DrawPanelBackground = false;
			MainPanel.PaintOverride += delegate (Element self, float width, float height) {

			};
			MainPanel.DockMargin = RectangleF.TLRB(4);
			AddParent = MainPanel;
			MainPanel.Clipping = false;

			MouseScrollEvent += ScrollPanel_MouseScrollEvent;
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

		protected override void PostLayoutChildren() {

		}

		private void ScrollPanel_MouseScrollEvent(Element self, FrameState state, Vector2F delta) {
			ConsumeScrollEvent();

			if (delta.X != 0)
				HorizontalScrollbar.MouseScrolled(HorizontalScrollbar, state, delta);
			if (delta.Y != 0)
				VerticalScrollbar.MouseScrolled(VerticalScrollbar, state, delta);
		}
		protected override void PerformLayout(float width, float height) {
			base.PerformLayout(width, height);
		}
		public virtual bool ShouldItemBeVisible(Element e) {
			return true;
		}
		protected override void OnThink(FrameState frameState) {
			base.OnThink(frameState);

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

			foreach (Element child in MainPanel.Children) {
				if (ShouldItemBeVisible(child)) {
					child.EngineDisabled = false;
					child.EngineInvisible = !RectangleF.IsSubrectangleWithinRectangle(MainPanel.RenderBounds.AddPosition(-MainPanel.ChildRenderOffset), child.RenderBounds);
				}
				else {
					child.EngineDisabled = true;
					child.EngineInvisible = true;
				}
			}

			MainPanel.ChildRenderOffset = new Vector2F(HorizontalScrollbar.Scroll, -VerticalScrollbar.Scroll).Round();
		}
		protected override void PostLayoutChild(Element element) {

		}
		public override void Paint(float width, float height) {
			Graphics2D.SetDrawColor(ForegroundColor);
			Graphics2D.DrawRectangleOutline(0, 0, width, height, 1);
		}
	}
}
