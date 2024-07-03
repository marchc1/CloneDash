using Nucleus.Core;
using Nucleus.Types;
using Raylib_cs;


namespace Nucleus.UI
{
    public class Button : Label
    {
        protected override void Initialize() {
            base.Initialize();
        }
        protected override void OnThink(FrameState frameState) {
            if (Hovered)
                EngineCore.SetMouseCursor(MouseCursor.MOUSE_CURSOR_POINTING_HAND);
            if (TriggeredWhenEnterPressed && frameState.KeyboardState.KeyPressed(KeyboardLayout.USA.Enter)) {
                MouseReleaseOccur(frameState, Types.MouseButton.MouseLeft, true);
            }
        }

        public bool TriggeredWhenEnterPressed { get; set; } = false;

        public override void Paint(float width, float height) {
            var backpre = BackgroundColor;
            if (TriggeredWhenEnterPressed) {
                backpre = backpre.Adjust(0, 0, 1 + (Math.Sin(Lifetime * 6) * 1.3));
            }
            var back = MixColorBasedOnMouseState(this, backpre, new(0, 0.8f, 1.8f, 1f), new(0, 1.2f, 0.6f, 1f));
            var fore = MixColorBasedOnMouseState(this, ForegroundColor, new(0, 0.8f, 1.8f, 1f), new(0, 1.2f, 0.6f, 1f));

            Graphics2D.SetDrawColor(back);
            Graphics2D.DrawRectangle(0, 0, width, height);
            ImageDrawing();
            if (BorderSize > 0) {
                Graphics2D.SetDrawColor(fore);
                Graphics2D.DrawRectangleOutline(0, 0, width, height, BorderSize);
            }
            base.Paint(width, height);
        }
    }
}
