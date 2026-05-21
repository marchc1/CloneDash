using Nucleus.Common.Input;
using Nucleus.Common.Types;
using Nucleus.Core;
using Nucleus.Input;
using Nucleus.Types;
using Raylib_cs;
using System.Diagnostics.CodeAnalysis;

namespace Nucleus.UI;

public interface INumSlider
{
	double Value { get; set; }
	double? MinimumValue { get; set; }
	double? MaximumValue { get; set; }
	int Digits { get; set; }
	string Prefix { get; set; }
	string Suffix { get; set; }
}
public class LabeledNumSlider : Panel, INumSlider, ITextElement
{
	private Label label;
	private NumSlider numslider;

	public double Value { get => numslider.Value; set => numslider.Value = value; }
	public double? MinimumValue { get => numslider.MinimumValue; set => numslider.MinimumValue = value; }
	public double? MaximumValue { get => numslider.MaximumValue; set => numslider.MaximumValue = value; }
	public int Digits { get => numslider.Digits; set => numslider.Digits = value; }
	public string Prefix { get => numslider.Prefix; set => numslider.Prefix = value; }
	public string Suffix { get => numslider.Suffix; set => numslider.Suffix = value; }
	public string? TextFormat { get => numslider.TextFormat; set => numslider.TextFormat = value; }


	public LabeledNumSlider(Element? parent) : base(parent){
		label = new Label(this);
		label.SetDock(Dock.Left);
		label.SetAutoSize(true);
		label.SetText("Num");
		label.SetBorderSize(0);
		label.SetBgColor(Color.Blank);
		label.SetDockMargin(RectangleF.XYWH(0, 0, 16, 0));

		numslider = new NumSlider(this);
		numslider.SetDock(Dock.Fill);
		numslider.Digits = 3;
	}

	public override void Paint(float width, float height) {

	}

	public ReadOnlySpan<char> GetFont() {
		return ((ITextElement)label).GetFont();
	}

	public float GetTextSize() {
		return ((ITextElement)label).GetTextSize();
	}

	public void SetFont(ReadOnlySpan<char> font) {
		((ITextElement)label).SetFont(font);
	}

	public void SetTextSize(float textSize) {
		((ITextElement)label).SetTextSize(textSize);
	}
}
public class NumSlider : Textbox, INumSlider
{
	private double _value = 0;
	private bool firstSet = true;
	public double Value {
		get => _value;
		set {
			if (_value == value && !firstSet)
				return;
			firstSet = false;

			var oldV = _value;
			SetValueNoUpdate(value);
			OnValueChanged?.Invoke(this, oldV, _value);
		}
	}
	public void SetValueNoUpdate(double value) {
		_value = Math.Round(value, Digits);
		if (MinimumValue.HasValue) _value = Math.Max(MinimumValue.Value, _value);
		if (MaximumValue.HasValue) _value = Math.Min(MaximumValue.Value, _value);
		SetText(GetTextVariant());
	}

	public delegate void OnValueChangedDelegate(NumSlider self, double oldValue, double newValue);
	public event OnValueChangedDelegate? OnValueChanged;
	public double? MinimumValue { get; set; } = null;
	public double? MaximumValue { get; set; } = null;
	private int _digits = 5;
	public int Digits {
		get => _digits;
		set {
			_digits = value;
			_value = Math.Round(_value, value);
		}
	}
	public string Prefix { get; set; } = "";
	public string Suffix { get; set; } = "";

	public NumSlider(Element? parent) : base(parent) {
		SetValueNoUpdate(Value);
	}
	protected override void OnThink() {
		base.OnThink();
		if (didDrag && IsDepressed())
			EngineCore.SetMouseCursor(MouseCursor.MOUSE_CURSOR_RESIZE_EW);
		else if (IsHovered() && !IsKeyboardFocused())
			EngineCore.SetMouseCursor(MouseCursor.MOUSE_CURSOR_POINTING_HAND);
	}
	string? workType = null;
	int caret = 0;
	protected override bool MouseClick(FrameState state, ButtonCode button) {
		KeyboardUnfocus();
		dragStart = state.Mouse.MousePos;
		return true;
	}

	protected override bool OnGainingKeyboardFocus(Element? lastFocus, ref Element? passTo) {
		SetText($"{Value}");
		caret = 0;
		return base.OnGainingKeyboardFocus(lastFocus, ref passTo);
	}
	public virtual double? ParseString(ReadOnlySpan<char> input) {
		double t;

		if (double.TryParse(input, out t))
			return t;

		return null;
	}
	bool didDrag = false;

	protected override bool OnLosingKeyboardFocus(Element? lostTo) {
		double? v = ParseString(workType);
		if (v != null) {
			Value = v.Value;
		}
		workType = null;
		return true;
	}
	protected override bool KeyPressed(in KeyboardState keyboardState, ButtonCode key) {
		if (key == ButtonCode.KeyEnter || key == ButtonCode.KeyPadEnter) {
			double? v = ParseString(GetText());
			if (v != null) {
				Value = v.Value;
				KeyboardUnfocus();
			}
			workType = null;
		}
		else {
			return base.KeyPressed(in keyboardState, key);
		}
		return true;
	}


	Vector2F dragStart;
	protected override bool MouseDrag(Element self, FrameState state, Vector2F delta) {
		if (dragStart.Distance(state.Mouse.MousePos) > 5 || didDrag) {
			if (!didDrag)
				dragStart = state.Mouse.MousePos;
			else
				EngineCore.Window.SetMousePosition(dragStart);

			didDrag = true;
			if (MinimumValue.HasValue && MaximumValue.HasValue) {
				Value = NMath.Remap(self.GetMousePos().X, BarPadding, self.GetRenderBounds().Width - (BarPadding * 2), MinimumValue.Value, MaximumValue.Value);
			}
			else Value += delta.X / MathF.Pow(1.5f, Digits);
		}
		return true;
	}

	protected override bool MouseRelease(Element self, FrameState state, ButtonCode button) {
		if (!IsHovered()) return true;
		if (!didDrag)
			base.MouseRelease(self, state, button);
		didDrag = false;
		return true;
	}
	protected override bool MouseScroll(Element self, FrameState state, Vector2F delta) {
		return false;
	}
	public bool TriggeredWhenEnterPressed { get; set; } = false;

	public string GetTextVariant() {
		string nmStr;
		if (double.IsNaN(Value))
			nmStr = "(not specified)";
		else if (double.IsPositiveInfinity(Value)) nmStr = "+Infinity";
		else if (double.IsNegativeInfinity(Value)) nmStr = "-Infinity";
		else if (TextFormat != null)
			nmStr = string.Format(TextFormat, Value);
		else nmStr = string.Format($"{{0:0.{new string('0', Digits)}}}", Value);
		string text = Prefix + nmStr + Suffix;

		return text;
	}

	[StringSyntax(StringSyntaxAttribute.NumericFormat)]
	public string? TextFormat { get; set; }

	public float BarPadding = 6;
	public override void Paint(float width, float height) {
		if (MinimumValue != null && MaximumValue != null) {
			var draw = (float)Math.Max(NMath.Remap(Value, MinimumValue.Value, MaximumValue.Value, 0, width - (BarPadding * 2)), BarPadding / 2);
			Graphics2D.SetDrawColor(100, 149, 237);
			Graphics2D.DrawRectangle(BarPadding, BarPadding, draw, height - (BarPadding * 2));
		}
		base.Paint(width, height);
	}
}
