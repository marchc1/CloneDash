using Nucleus.Core;
using Nucleus.Types;

namespace Nucleus.UI
{
    public class Panel : Element
    {
        protected override void Initialize() {
            base.Initialize();
            this.DockPadding = RectangleF.TLRB(2);
        }

        public override void Paint(float width, float height) {
            Graphics2D.SetDrawColor(BackgroundColor);
            Graphics2D.DrawRectangle(0, 0, width, height);
            ImageDrawing();
            Graphics2D.SetDrawColor(ForegroundColor);
            Graphics2D.DrawRectangleOutline(0, 0, width, height, 2);
        }
    }
}
