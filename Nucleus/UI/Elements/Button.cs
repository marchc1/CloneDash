using Nucleus.Common.Audio;
using Nucleus.Common.Input;
using Nucleus.Common.Types;
using Nucleus.Core;
using Nucleus.Extensions;
using Nucleus.Input;
using Nucleus.Types;

using Raylib_cs;

using System.Numerics;


namespace Nucleus.UI;

public delegate void ButtonActionFn(Button button, ButtonCode mouseButton);

public class Button : Label
{
	public event ButtonActionFn? OnButtonClick;

	public Button(Element? parent, ReadOnlySpan<char> text = "Button", ReadOnlySpan<char> name = default) : base(parent, text, name) {
		BackgroundColor = new(20, 25, 32, 220);
		SetPaintBackgroundEnabled(true);
		SetPaintBorderEnabled(true);
	}
	protected override void OnThink(FrameState frameState) {
		if (Hovered)
			EngineCore.SetMouseCursor(MouseCursor.MOUSE_CURSOR_POINTING_HAND);
	}
	protected override void KeyPressed(in KeyboardState keyboardState, ButtonCode key) {
		if (!TriggeredWhenEnterPressed) {
			UI.MarkKeyEventNotConsumed();
			return;
		}

		if (key == ButtonCode.KeyEnter || key == ButtonCode.KeyPadEnter)
			MouseReleaseOccur(Level.FrameState, ButtonCode.MouseLeft, true);
	}

	protected override void MouseClick(FrameState state, ButtonCode button) {
		base.MouseClick(state, button);
		audiosystem.PlaySound("click.wav", AudioPlaybackSettings.Unaltered);
	}

	protected override void MouseRelease(Element self, FrameState state, ButtonCode button) {
		base.MouseRelease(self, state, button);
		OnButtonClick?.Invoke(this, button);
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

	public bool PulsePreservesAlpha;

	public bool DrawAsCircle { get; set; } = false;

	public Vector4 HoveredMultiplier = new(0, 0.8f, 2.5f, 1f);
	public Vector4 DepressedMultiplier = new(0, 1.2f, 0.6f, 1f);

	public bool DrawBackgroundWhenMouseIdle = true;

	public static void ColorStateSetup(Button b, out Color back, out Color fore) {
		var backpre = b.BackgroundColor;
		var forepre = b.ForegroundColor;

		var canInput = b.IsMouseInputEnabled();

		if ((b.TriggeredWhenEnterPressed || b.Pulsing) && canInput) {
			double val = ((Math.Sin(b.PulseTime * 6) + 1) / 2);
			backpre = backpre.Adjust(0, 0, 1 + (val * 1.9));
			forepre = forepre.Adjust(0, 0, 1 + (val * 0.1f));
			if (!b.PulsePreservesAlpha)
				backpre.A = (byte)(int)(float)Math.Clamp(backpre.A * val, byte.MinValue, byte.MaxValue);
		}

		back = MixColorBasedOnMouseState(b, backpre, b.HoveredMultiplier, b.DepressedMultiplier);
		fore = MixColorBasedOnMouseState(b, forepre, b.HoveredMultiplier, b.DepressedMultiplier);

		if (!canInput) {
			back = back.Adjust(0, 0, -0.5f);
			fore = fore.Adjust(0, 0, -0.5f);
		}

		if (!b.DrawBackgroundWhenMouseIdle && !b.Hovered && !b.Pulsing) {
			back.A = 0;
			fore.A = 0;
		}
	}

	public override void Paint(float width, float height) {
		ColorStateSetup(this, out var back, out var fore);

		Graphics2D.SetDrawColor(back);
		if (DrawAsCircle) {
			var whd2 = new Vector2F(width / 2, width / 2);
			var whd3 = new Vector2F(width / 3, width / 3);
			Graphics2D.DrawCircle(whd2, whd3);
		}
		else
			Graphics2D.DrawRectangle(0, 0, width, height);

		Vector2F posOffset = new(0);

		Vector2F textDrawingPosition = TextAlignment.GetPositionGivenAlignment(RenderBounds.Size, TextPadding);
		if (ImageFollowsText && Image != null) {
			posOffset = new Vector2F(textDrawingPosition.X - (width / 2) - (Image.Width / 2) - 2, 0);
		}

		ImageDrawing(posOffset);

		base.Paint(width, height);
	}
	public override void PaintBorder(float width, float height) {
		ColorStateSetup(this, out var back, out var fore);

		if (BorderSize > 0) {
			Graphics2D.SetDrawColor(fore);
			if (DrawAsCircle) {
				var whd2 = new Vector2F(width / 2, width / 2);
				var whd3 = new Vector2F(width / 3, width / 3);
				Graphics2D.DrawCircleLines(whd2, whd3);
			}
			else
				base.PaintBorder(width, height);
		}
	}
}
