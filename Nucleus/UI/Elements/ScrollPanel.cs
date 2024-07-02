using Nucleus.Core;
using Nucleus.Types;
using Nucleus.UI.Elements;

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

            VerticalScrollbar.MinScroll = 5;
            VerticalScrollbar.MaxScroll = 20;

            HorizontalScrollbar.MinScroll = 5;
            HorizontalScrollbar.MaxScroll = 20;

            MouseScrollEvent += ScrollPanel_MouseScrollEvent;
        }

        private void ScrollPanel_MouseScrollEvent(Element self, FrameState state, Vector2F delta) {
            if (delta.X != 0)
                HorizontalScrollbar.MouseScrolled(null, state, delta);
            if (delta.Y != 0)
                VerticalScrollbar.MouseScrolled(null, state, delta);
        }
        public override T Add<T>(T? toAdd = null) where T : class {
            var ret = base.Add<T>(toAdd);
            ret.MouseScrollEvent += ScrollPanel_MouseScrollEvent;
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
            HorizontalScrollbar.Enabled = MainPanel.SizeOfAllChildren.W > this.RenderBounds.Size.W;
            VerticalScrollbar.Enabled = MainPanel.SizeOfAllChildren.H > this.RenderBounds.Size.H;

            HorizontalScrollbar.MinScroll = this.RenderBounds.Size.W;
            HorizontalScrollbar.MaxScroll = MainPanel.SizeOfAllChildren.W;

            VerticalScrollbar.MinScroll = this.RenderBounds.Size.H;
            VerticalScrollbar.MaxScroll = MainPanel.SizeOfAllChildren.H;

            foreach (Element child in MainPanel.Children) {
                if (!ShouldItemBeVisible(child))
                    child.Enabled = false;
                else {
                    child.Enabled = true;

                    if ((child.RenderBounds.Y + child.RenderBounds.H) < VerticalScrollbar.Scroll)
                        child.Visible = false;
                    else if (child.RenderBounds.Y > VerticalScrollbar.Scroll + RenderBounds.H)
                        child.Visible = false;
                    else
                        child.Visible = true;
                }
            }
            MainPanel.ChildRenderOffset = new Vector2F(HorizontalScrollbar.Scroll, -VerticalScrollbar.Scroll).Round();
        }
        public override void Paint(float width, float height) {
            VerticalScrollbar.Scroll = 5;
            Graphics2D.SetDrawColor(ForegroundColor);
            Graphics2D.DrawRectangleOutline(0, 0, width, height, 1);
        }
    }
}
