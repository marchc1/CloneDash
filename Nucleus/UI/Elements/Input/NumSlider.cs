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
		label.		Dock = Dock.Left;
		label.SetAutoSize(true);
		label.		Text = "Num";
		label.BorderSize = 0;
		label.SetTextPadding(new(12));
		label.SetBgColor(Color.Blank);
		label.		DockMargin = RectangleF.XYWH(0, 0, 16, 0);

		numslider = new NumSlider(this);
		numslider.		Dock = Dock.Fill;
		numslider.Digits = 3;
	}

	public override void Paint(float width, float height) {

	}

	public ReadOnlySpan<char> Font { get => ((ITextElement)label).Font; set => ((ITextElement)label).Font = value; }
	public ReadOnlySpan<char> Text { get => ((ITextElement)label).Text; set => ((ITextElement)label).Text = value; }

	public float TextSize { get => ((ITextElement)label).TextSize; set => ((ITextElement)label).TextSize = value; }
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
		Text = GetTextVariant();
	}

	public delegate void OnValueChangedDelegate(NumSlider self, double oldValue, double newValue);
	public event OnValueChangedDelegate? OnValueChanged;
	public double? MinimumValue {
		get;
		set { field = value; SetValueNoUpdate(_value); }
	} = null;
	public double? MaximumValue {
		get;
		set { field = value; SetValueNoUpdate(_value); }
	} = null;
	private int _digits = 5;
	public int Digits {
		get => _digits;
		set {
			_digits = value;
			SetValueNoUpdate(_value);
		}
	}
	public string Prefix { get; set; } = "";
	public string Suffix { get; set; } = "";

	public NumSlider(Element? parent) : base(parent) {
		SetValueNoUpdate(Value);
	}
	protected override void OnThink() {
		base.OnThink();
		if ((didDrag && IsDepressed()) || (IsHovered() && !IsKeyboardFocused()))
			EngineCore.SetMouseCursor(MouseCursor.MOUSE_CURSOR_RESIZE_EW);
	}
	// click started while already typing in it
	bool editingClick = false;

	protected override bool MouseClick(FrameState state, ButtonCode button) {
		if (IsKeyboardFocused()) {
			editingClick = true;
			return base.MouseClick(state, button);
		}

		editingClick = false;
		dragStart = state.Mouse.MousePos;
		return true;
	}

	public event Action<NumSlider>? FocusGained;
	public event Action<NumSlider>? FocusLost;

	protected override bool OnGainingKeyboardFocus(Element? lastFocus, ref Element? passTo) {
		FocusGained?.Invoke(this);
		Text = $"{Value}";
		return base.OnGainingKeyboardFocus(lastFocus, ref passTo);
	}
	public virtual double? ParseString(ReadOnlySpan<char> input) {
		double t;

		if (double.TryParse(input, out t))
			return t;

		return null;
	}

	/// <summary>
	/// Typed +n, +-n, *n and /n change the current value
	/// </summary>
	public bool RelativeInput { get; set; } = true;

	public double? ParseInput(ReadOnlySpan<char> input) {
		input = input.Trim();
		if (!RelativeInput || input.Length < 2 || input[0] is not ('+' or '*' or '/'))
			return ParseString(input);
		if (ParseString(input.Slice(1)) is not double amount) return null;
		return input[0] switch {
			'+' => Value + amount,
			'*' => Value * amount,
			_ => amount == 0 ? null : Value / amount
		};
	}
	bool didDrag = false;

	protected override bool OnLosingKeyboardFocus(Element? lostTo) {
		if (ParseInput(Text) is double parsed)
			Value = parsed;
		else
			SetValueNoUpdate(Value);
		bool result = base.OnLosingKeyboardFocus(lostTo);
		FocusLost?.Invoke(this);
		return result;
	}
	protected override bool KeyPressed(in KeyboardState keyboardState, ButtonCode key) {
		if (key == ButtonCode.KeyEnter || key == ButtonCode.KeyPadEnter || key == ButtonCode.KeyTab) {
			double? parsed = ParseInput(Text);
			if (parsed != null) {
				Value = parsed.Value;
				KeyboardUnfocus();
			}
		}
		else {
			return base.KeyPressed(in keyboardState, key);
		}
		return true;
	}


	Vector2F dragStart;
	double dragValue;
	double dragSpeed;

	/// <summary>
	/// Value change per pixel when dragging an unbounded slider, right or up.<br/>
	/// Shift = 10x finer, Ctrl = 10x coarser
	/// </summary>
	public virtual double GetDragSpeed(double startValue) => Math.Max(1, Math.Sqrt(Math.Abs(startValue))) * 0.03;

	protected override bool MouseDrag(Element self, FrameState state, Vector2F delta) {
		if (editingClick)
			return base.MouseDrag(self, state, delta);

		if (dragStart.Distance(state.Mouse.MousePos) > 5 || didDrag) {
			bool bounded = MinimumValue.HasValue && MaximumValue.HasValue;
			if (!didDrag) {
				dragStart = state.Mouse.MousePos;
				// unrounded copy of Value
				dragValue = double.IsFinite(Value) ? Value : 0;
				dragSpeed = GetDragSpeed(dragValue);
			}
			else if (!bounded)
				EngineCore.Window.SetMousePosition(dragStart);

			didDrag = true;
			if (bounded) {
				RectangleF content = GetContentRect();
				Value = NMath.Remap(self.GetMousePos().X - content.X, BarPadding, content.W - BarPadding, MinimumValue!.Value, MaximumValue!.Value, true);
			}
			else {
				double multiplier = 1;
				if (state.Keyboard.ShiftDown)
					multiplier = 0.1;
				else if (state.Keyboard.ControlDown)
					multiplier = 10;
				dragValue += (delta.X - delta.Y) * dragSpeed * multiplier;
				Value = dragValue;
			}
		}
		return true;
	}

	protected override bool MouseRelease(Element self, FrameState state, ButtonCode button) {
		if (editingClick) {
			editingClick = false;
			return base.MouseRelease(self, state, button);
		}

		bool wasDrag = didDrag;
		didDrag = false;
		if (!wasDrag && IsHovered()) {
			bool wasFocused = IsKeyboardFocused();
			base.MouseRelease(self, state, button);
			if (!wasFocused && IsKeyboardFocused())
				SelectAll();
		}
		return true;
	}
	public bool ScrollToChange { get; set; } = false;
	/// <summary>
	/// Value change per wheel notch. Shift = 10x finer
	/// </summary>
	public double ScrollStep { get; set; } = 1;

	protected override bool MouseScroll(Element self, FrameState state, Vector2F delta) {
		if (!ScrollToChange || IsKeyboardFocused() || delta.Y == 0) return false;
		double step = ScrollStep * (state.Keyboard.ShiftDown ? 0.1 : 1);
		Value = (double.IsFinite(Value) ? Value : 0) + Math.Sign(delta.Y) * step;
		return true;
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
		if (MinimumValue != null && MaximumValue != null)
			PaintBar(width, height);
		base.Paint(width, height);
	}

	/// <summary>
	/// The fill behind the text of a bounded slider
	/// </summary>
	protected virtual void PaintBar(float width, float height) {
		var draw = (float)Math.Max(
			NMath.Remap(Value, MinimumValue!.Value, MaximumValue!.Value, 0, width - (BarPadding * 2)),
			BarPadding / 2);
		Graphics2D.SetDrawColor(100, 149, 237);
		Graphics2D.DrawRectangle(BarPadding, BarPadding, draw, height - (BarPadding * 2));
	}
}
