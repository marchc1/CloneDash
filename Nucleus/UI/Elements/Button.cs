using Nucleus.Core;

namespace Nucleus.UI
{
    public class Button : Label
    {
        protected override void Initialize() {
            base.Initialize();
        }
        public override void Paint(float width, float height) {
            var back = MixColorBasedOnMouseState(this, BackgroundColor, new(0, 0.8f, 1.8f, 1f), new(0, 1.2f, 0.6f, 1f));
            var fore = MixColorBasedOnMouseState(this, ForegroundColor, new(0, 0.8f, 1.8f, 1f), new(0, 1.2f, 0.6f, 1f));

            Graphics2D.SetDrawColor(back);
            Graphics2D.DrawRectangle(0, 0, width, height);
            ImageDrawing();
            Graphics2D.SetDrawColor(fore);
            Graphics2D.DrawRectangleOutline(0, 0, width, height, 2);

            base.Paint(width, height);
        }
    }
}
