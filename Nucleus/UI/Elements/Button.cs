using Nucleus.Core;
using Nucleus.Types;
using Raylib_cs;


namespace Nucleus.UI
{
	public class Button : Label
	{
		protected override void Initialize() {
			base.Initialize();
			BackgroundColor = new(20, 25, 32, 220);
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

		public bool DrawAsCircle { get; set; } = false;

		public override void Paint(float width, float height) {
			var backpre = BackgroundColor;
			var forepre = ForegroundColor;

			var canInput = CanInput();

			if ((TriggeredWhenEnterPressed || Pulsing) && canInput) {
				backpre = backpre.Adjust(0, 0, 1 + (Math.Sin(PulseTime * 6) * 1.9));
				forepre = forepre.Adjust(0, 0, 1 + (Math.Sin(PulseTime * 6) * 0.1f));
			}
			var back = MixColorBasedOnMouseState(this, backpre, new(0, 0.8f, 2.5f, 1f), new(0, 1.2f, 0.6f, 1f));
			var fore = MixColorBasedOnMouseState(this, forepre, new(0, 0.8f, 1.8f, 1f), new(0, 1.2f, 0.6f, 1f));

			if (!canInput) {
				back = back.Adjust(0, 0, -0.5f);
				fore = fore.Adjust(0, 0, -0.5f);
			}

			Graphics2D.SetDrawColor(back);
			var whd2 = new Vector2F(width / 2, width / 2);
			var whd3 = new Vector2F(width / 3, width / 3);
			if (DrawAsCircle)
				Graphics2D.DrawCircle(whd2, whd3);
			else
				Graphics2D.DrawRectangle(0, 0, width, height);

			Vector2F posOffset = new(0);

			Vector2F textDrawingPosition = Anchor.GetPositionGivenAlignment(TextAlignment, RenderBounds.Size, TextPadding);
			if (ImageFollowsText && Image != null) {
				posOffset = new Vector2F(textDrawingPosition.X - (width / 2) - (Image.Width / 2) - 2, 0);
			}

			ImageDrawing(posOffset);
			if (BorderSize > 0) {
				Graphics2D.SetDrawColor(fore);
				if (DrawAsCircle)
					Graphics2D.DrawCircleLines(whd2, whd3);
				else
					Graphics2D.DrawRectangleOutline(0, 0, width, height, BorderSize);
			}

			base.Paint(width, height);
		}
	}
}
