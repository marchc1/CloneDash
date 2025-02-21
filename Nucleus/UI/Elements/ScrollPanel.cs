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
	public class ScrollPanel : Panel
	{
		public Scrollbar VerticalScrollbar { get; private set; }
		public Scrollbar HorizontalScrollbar { get; private set; }
		public Panel MainPanel { get; private set; }

		protected override void Initialize() {
			base.Initialize();
			VerticalScrollbar = base.Add<Scrollbar>();
			VerticalScrollbar.Alignment = ScrollbarAlignment.Vertical;
			VerticalScrollbar.Enabled = true;

			HorizontalScrollbar = base.Add<Scrollbar>();
			HorizontalScrollbar.Alignment = ScrollbarAlignment.Horizontal;
			HorizontalScrollbar.Enabled = true;

			MainPanel = this.Add<Panel>();
			MainPanel.Dock = Dock.Fill;
			MainPanel.PaintOverride += delegate (Element self, float width, float height) {

			};
			MainPanel.DockMargin = RectangleF.TLRB(4);
			AddParent = MainPanel;
			MainPanel.Clipping = false;

			MouseScrollEvent += ScrollPanel_MouseScrollEvent;
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
		public override T Add<T>(T? toAdd = default) where T : class {
			var ret = base.Add<T>(toAdd);
			return ret;
		}
		protected override void PerformLayout(float width, float height) {
			base.PerformLayout(width, height);
		}
		public virtual bool ShouldItemBeVisible(Element e) {
			return true;
		}
		protected override void OnThink(FrameState frameState) {
			base.OnThink(frameState);

			VerticalScrollbar.PageContents = AddParent.SizeOfAllChildren;
			VerticalScrollbar.PageSize = AddParent.RenderBounds.Size;
			HorizontalScrollbar.PageContents = AddParent.SizeOfAllChildren;
			HorizontalScrollbar.PageSize = AddParent.RenderBounds.Size;

			VerticalScrollbar.Update(AddParent.SizeOfAllChildren, AddParent.RenderBounds.Size);

			foreach (Element child in MainPanel.Children) {
				if (ShouldItemBeVisible(child)) {
					child.EngineDisabled = RectangleF.IsSubrectangleWithinRectangle(MainPanel.RenderBounds.AddPosition(MainPanel.ChildRenderOffset), child.RenderBounds);
					child.EngineInvisible = child.EngineDisabled;
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
