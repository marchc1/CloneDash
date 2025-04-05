using Newtonsoft.Json.Linq;
using Nucleus.Core;
using Nucleus.Types;
using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.UI
{
	public interface INumSlider
	{
		double Value { get; set; }
		double? MinimumValue { get; set; }
		double? MaximumValue { get; set; }
		int Digits { get; set; }
		string Prefix { get; set; }
		string Suffix { get; set; }
	}
	public class LabeledNumSlider : Panel, INumSlider
	{
		private Label label;
		private NumSlider numslider;

		public double Value { get => numslider.Value; set => numslider.Value = value; }
		public double? MinimumValue { get => numslider.MinimumValue; set => numslider.MinimumValue = value; }
		public double? MaximumValue { get => numslider.MaximumValue; set => numslider.MaximumValue = value; }
		public int Digits { get => numslider.Digits; set => numslider.Digits = value; }
		public string Prefix { get => numslider.Prefix; set => numslider.Prefix = value; }
		public string Suffix { get => numslider.Suffix; set => numslider.Suffix = value; }

		public new string Text { get => label.Text; set => label.Text = value; }
		protected override void Initialize() {
			label = Add<Label>();
			label.Dock = Dock.Left;
			label.AutoSize = true;
			label.Text = "Num";
			label.BorderSize = 0;
			label.BackgroundColor = Color.Blank;
			label.DockMargin = RectangleF.XYWH(0, 0, 16, 0);

			numslider = Add<NumSlider>();
			numslider.Dock = Dock.Fill;
			numslider.Digits = 3;
		}

		public override void Paint(float width, float height) {

		}
	}
	public class NumSlider : Textbox, INumSlider
	{
		private double _value = 0;
		public double Value {
			get => _value;
			set {
				if (_value == value)
					return;

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
		public double? MinimumValue { get; set; } = null;
		public double? MaximumValue { get; set; } = null;
		private int _digits = 5;
		public int Digits {
			get => _digits;
			set {
				_digits = value;
				Math.Round(_value, value);
			}
		}
		public string Prefix { get; set; } = "";
		public string Suffix { get; set; } = "";

		protected override void Initialize() {
			base.Initialize();
			SetValueNoUpdate(Value);
		}
		protected override void OnThink(FrameState frameState) {
			if (Hovered)
				EngineCore.SetMouseCursor(MouseCursor.MOUSE_CURSOR_POINTING_HAND);
			if (TriggeredWhenEnterPressed && frameState.KeyboardState.KeyPressed(KeyboardLayout.USA.Enter)) {
				MouseReleaseOccur(frameState, Types.MouseButton.MouseLeft, true);
			}
		}
		string? workType = null;
		int caret = 0;
		public override void MouseClick(FrameState state, Types.MouseButton button) {
			KeyboardUnfocus();
		}

		public override void KeyboardFocusGained(bool demanded) {
			Text = $"{Value}";
			caret = 0;
		}
		public virtual double? ParseString(string? input) {
			double t;

			if (double.TryParse(input, out t))
				return t;

			return null;
		}
		bool didDrag = false;
		public override void KeyboardFocusLost(Element lostTo, bool demanded) {
			double? v = ParseString(workType);
			if (v != null) {
				Value = v.Value;
			}
			workType = null;
		}
		public override void KeyPressed(KeyboardState keyboardState, Types.KeyboardKey key) {
			if (key == KeyboardLayout.USA.Enter || key == KeyboardLayout.USA.NumpadEnter) {
				double? v = ParseString(Text);
				if (v != null) {
					Value = v.Value;
					KeyboardUnfocus();
				}
				workType = null;
			}
			else {
				base.KeyPressed(keyboardState, key);
			}
		}

		public override void MouseDrag(Element self, FrameState state, Vector2F delta) {
			if (delta.Length > 2 || didDrag) {
				didDrag = true;
				Value += delta.X / (MathF.Pow(1.5f, Digits));
			}
		}

		public override void MouseRelease(Element self, FrameState state, Types.MouseButton button) {
			if (!didDrag)
				base.MouseRelease(self, state, button);
			didDrag = false;
		}
		public override void MouseScroll(Element self, FrameState state, Vector2F delta) {
			base.MouseScroll(self, state, delta);
		}
		public bool TriggeredWhenEnterPressed { get; set; } = false;

		public string GetTextVariant() {
			string nmStr;
			if (double.IsNaN(Value))
				nmStr = "(not specified)";
			else if (double.IsPositiveInfinity(Value)) nmStr = "+Infinity";
			else if (double.IsNegativeInfinity(Value)) nmStr = "-Infinity";
			else nmStr = string.Format($"{{0:0.{new string('0', Digits)}}}", Value);
			string text = Prefix + nmStr + Suffix;

			return text;
		}

		public override void Paint(float width, float height) {
			base.Paint(width, height);
		}
	}
}
