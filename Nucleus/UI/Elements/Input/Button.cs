using Nucleus.Common.Audio;
using Nucleus.Common.Graphics;
using Nucleus.Common.Input;
using Nucleus.Common.Types;
using Nucleus.Core;
using Nucleus.Extensions;
using Nucleus.Input;
using Nucleus.Types;
using Nucleus.UI.Elements;
using Raylib_cs;

using System.Numerics;


namespace Nucleus.UI;

public delegate void ButtonActionFn(Button button, ButtonCode mouseButton);

public class Button : Label
{
	public event ButtonActionFn? OnButtonClick;

	public Button(Element? parent, ReadOnlySpan<char> text = "Button", ReadOnlySpan<char> name = default) : base(parent, text, name) {
		SetBgColor(new Color(20, 25, 32, 220));
		SetPaintBackgroundEnabled(true);
		SetPaintBorderEnabled(true);
	}

	protected override void OnThink() {
		if (IsHovered())
			EngineCore.SetMouseCursor(MouseCursor.MOUSE_CURSOR_POINTING_HAND);
	}

	protected override bool KeyPressed(in KeyboardState keyboardState, ButtonCode key) {
		return true;
	}

	protected override bool MouseClick(FrameState state, ButtonCode button) {
		base.MouseClick(state, button);
		audiosystem.PlaySound("click.wav", AudioPlaybackSettings.Unaltered);
		return true;
	}

	protected override bool MouseRelease(Element self, FrameState state, ButtonCode button) {
		if (!IsHovered()) return true;
		base.MouseRelease(self, state, button);
		OnButtonClick?.Invoke(this, button);
		return true;
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
	public bool ImageFollowsText { get; set; } = false;

	public Vector4 HoveredMultiplier = new(0, 0.8f, 2.5f, 1f);
	public Vector4 DepressedMultiplier = new(0, 1.2f, 0.6f, 1f);

	public bool DrawBackgroundWhenMouseIdle = true;

	// kinda a hack to get what we want here for hovering
	Color painttimeBgColor;
	Color painttimeFgColor;
	bool switchToPaintTimeColors = false;
	double lastRenderTime;

	public override Color GetBgColor() => switchToPaintTimeColors ? painttimeBgColor :base.GetBgColor();
	public override Color GetFgColor() => switchToPaintTimeColors ? painttimeFgColor : base.GetFgColor();
	

	public void ColorStateSetup(out Color back, out Color fore) {
		if (lastRenderTime != globals.CurTime) {
			lastRenderTime = globals.CurTime;
			var backpre = GetBgColor();
			var forepre = GetFgColor();

			var canInput = IsMouseInputEnabled();

			if ((TriggeredWhenEnterPressed || Pulsing) && canInput) {
				double val = ((Math.Sin(PulseTime * 6) + 1) / 2);
				backpre = backpre.Adjust(0, 0, 1 + (val * 1.9));
				forepre = forepre.Adjust(0, 0, 1 + (val * 0.1f));
				if (!PulsePreservesAlpha)
					backpre.A = (byte)(int)(float)Math.Clamp(backpre.A * val, byte.MinValue, byte.MaxValue);
			}

			back = MixColorBasedOnMouseState(this, backpre, HoveredMultiplier, DepressedMultiplier);
			fore = MixColorBasedOnMouseState(this, forepre, HoveredMultiplier, DepressedMultiplier);

			if (!canInput) {
				back = back.Adjust(0, 0, -0.5f);
				fore = fore.Adjust(0, 0, -0.5f);
			}

			if (!DrawBackgroundWhenMouseIdle && !IsHovered() && !Pulsing) {
				back.A = 0;
				fore.A = 0;
			}

			painttimeBgColor = back;
			painttimeFgColor = fore;
		}
		else { // re-entrants in the current frame will use cached colors
			back = painttimeBgColor;
			fore = painttimeFgColor;
		}
	}

	public override void Paint(float width, float height) {
		ColorStateSetup(out var back, out var fore);

		Graphics2D.SetDrawColor(back);
		if (DrawAsCircle) {
			var whd2 = new Vector2F(width / 2, width / 2);
			var whd3 = new Vector2F(width / 3, width / 3);
			Graphics2D.DrawCircle(whd2, whd3);
		}
		else
			Graphics2D.DrawRectangle(0, 0, width, height);

		Vector2F posOffset = new(0);

		Vector2F textDrawingPosition = GetTextAlignment().GetPositionGivenAlignment(RenderBounds.Size, GetTextPadding());
		switchToPaintTimeColors = true;
		base.Paint(width, height);
		switchToPaintTimeColors = false;
	}
	public override void PaintBorder(float width, float height) {
		ColorStateSetup(out var back, out var fore);

		if (BorderSize > 0) {
			Graphics2D.SetDrawColor(fore);
			if (DrawAsCircle) {
				var whd2 = new Vector2F(width / 2, width / 2);
				var whd3 = new Vector2F(width / 3, width / 3);
				Graphics2D.DrawCircleLines(whd2, whd3);
			}
			else {
				switchToPaintTimeColors = true;
				base.PaintBorder(width, height);
				switchToPaintTimeColors = false;
			}
		}
	}
}
