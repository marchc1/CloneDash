using Nucleus.Common.Input;
using Nucleus.Types;
using Nucleus.UI;

namespace Nucleus.ModelEditor
{
	public class TransformPanel : Panel
	{
		public TransformPanel(Element parent) : base(parent) {
			BorderSize = 0;
			SetPaintBackgroundEnabled(false);
		}
		public delegate void FloatChange(int i, float value);
		public event FloatChange? FloatChanged;

		public delegate void Keyframed(KeyframeProperty property, int index);

		public event Action<Element, ButtonCode>? OnSelected;
		public event Keyframed? OnKeyframe;
		private NumSlider[] sliders;
		private Button button;
		private KeyframeButton keyframe;
		private KeyframeButton keyframeX;
		private KeyframeButton keyframeY;

		public bool SeparatedProperties {
			get => !keyframe.IsMouseInputEnabled();
			set {
				keyframe.SetMouseInputEnabled(!value);
				keyframeX.SetMouseInputEnabled(value);
				keyframeY.SetMouseInputEnabled(value);
			}
		}

		private bool enableSliders = true;
		public bool EnableSliders {
			get => enableSliders;
			set {
				enableSliders = value;
				foreach (var slider in sliders) {
					var c = slider.GetTextColor();
					slider.SetTextColor(new Common.Types.Color(c.R, c.G, c.B, value ? 255 : 0));
				}
			}
		}
		public NumSlider GetNumSlider(int index) => sliders[index];
		public Button GetButton() => button;

		public static TransformPanel New(Element parent, string text, int floats, KeyframeProperty property = KeyframeProperty.None) {
			var panel = new TransformPanel(parent);
			panel.			DockPadding = RectangleF.TLRB(2);
			panel.BorderSize = 2;

			panel.button = new Button(panel);
			panel.button.			Dock = Dock.Left;
			panel.button.			Text = text;
			panel.button.			Size = new(96);
			panel.button.BorderSize = 0;
			panel.button.OnButtonClick += (v1, v3) => panel.OnSelected?.Invoke(panel, v3);

			panel.keyframeY = new KeyframeButton(panel);
			panel.keyframeY.			Dock = Dock.Right;
			panel.keyframeY.ArrayIndex = 1;
			panel.keyframeY.Property = property;
			panel.keyframeY.OnButtonClick += (_, _) => ModelEditor.Active.File.InsertKeyframe(ModelEditor.Active.LastSelectedObject, property, 1);
			panel.keyframeY.			Size = new(26);
			panel.keyframeY.BorderSize = 0;
			panel.keyframeY.SetMouseInputEnabled(false);
			panel.keyframeY.			TooltipText = "Keyframe Y";

			panel.keyframeX = new KeyframeButton(panel);
			panel.keyframeX.			Dock = Dock.Right;
			panel.keyframeX.ArrayIndex = 0;
			panel.keyframeX.Property = property;
			panel.keyframeX.OnButtonClick += (_, _) => ModelEditor.Active.File.InsertKeyframe(ModelEditor.Active.LastSelectedObject, property, 0);
			panel.keyframeX.			Size = new(26);
			panel.keyframeX.BorderSize = 0;
			panel.keyframeX.SetMouseInputEnabled(false);
			panel.keyframeX.			TooltipText = "Keyframe X";

			panel.keyframe = new KeyframeButton(panel);
			panel.keyframe.			Dock = Dock.Right;
			panel.keyframe.			Size = new(26);
			panel.keyframe.ArrayIndex = -1;
			panel.keyframe.Property = property;
			panel.keyframe.OnButtonClick += (_, _) => ModelEditor.Active.File.InsertKeyframe(ModelEditor.Active.LastSelectedObject, property, -1);
			panel.keyframe.BorderSize = 0;

			ModelEditor.Active.File.PropertySeparatedOrCombined += (b, prop, separated) => {
				if (prop == property)
					panel.SeparatedProperties = separated;
			};

			var floatparts = new FlexPanel(panel);
			floatparts.			Dock = Dock.Fill;
			floatparts.Direction = Axis.Horizontal;
			floatparts.ChildrenResizingMode = FlexChildrenResizingMode.StretchToFit;
			floatparts.			DockPadding = RectangleF.Zero;
			floatparts.BorderSize = 0;

			panel.sliders = new NumSlider[floats];
			for (int i = 0; i < floats; i++) {
				var floatEdit = new NumSlider(floatparts);
				panel.sliders[i] = floatEdit;
				floatEdit.SetHelperText("");
				floatEdit.Value = 0;
				floatEdit.BorderSize = 0;
				floatEdit.OnValueChanged += (self, oldV, newV) => {
					panel.FloatChanged?.Invoke(i, (float)newV);
				};
			}

			return panel;
		}
	}
}
