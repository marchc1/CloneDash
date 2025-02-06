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

        public bool TriggeredWhenEnterPressed {
			get => __triggeredWhenEnterPressed;
			set {
				__triggeredWhenEnterPressed = value;
				startPulse = DateTime.UtcNow;
			}
		}

		private bool __triggeredWhenEnterPressed = false;
		private bool __pulsing = false;
		private DateTime startPulse;
		public float PulseTime => (float)(DateTime.UtcNow - startPulse).TotalSeconds;
        public bool Pulsing {
			get => __pulsing;
			set {
				__pulsing = value;
				startPulse = DateTime.UtcNow;
			}
		}

        public override void Paint(float width, float height) {
            var backpre = BackgroundColor;
            var forepre = ForegroundColor;
            if (TriggeredWhenEnterPressed || Pulsing) {
                backpre = backpre.Adjust(0, 0, 1 + (Math.Sin(PulseTime * 6) * 1.9));
				forepre = forepre.Adjust(0, 0, 1 + (Math.Sin(PulseTime * 6) * 0.1f));
            }
            var back = MixColorBasedOnMouseState(this, backpre, new(0, 0.8f, 2.5f, 1f), new(0, 1.2f, 0.6f, 1f));
            var fore = MixColorBasedOnMouseState(this, forepre, new(0, 0.8f, 1.8f, 1f), new(0, 1.2f, 0.6f, 1f));

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
