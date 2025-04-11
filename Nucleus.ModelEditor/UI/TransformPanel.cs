using Nucleus.Types;
using Nucleus.UI;

namespace Nucleus.ModelEditor
{
	public class TransformPanel : Panel
	{
		public delegate void FloatChange(int i, float value);
		public event FloatChange? FloatChanged;

		public delegate void Keyframed(KeyframeProperty property, int index);

		public event Element.MouseEventDelegate? OnSelected;
		public event Keyframed? OnKeyframe;
		private NumSlider[] sliders;
		private Button button;
		private KeyframeButton keyframe;
		private KeyframeButton keyframeX;
		private KeyframeButton keyframeY;

		public bool SeparatedProperties {
			get => !keyframe.Enabled;
			set {
				keyframe.Enabled = !value;
				keyframeX.Enabled = value;
				keyframeY.Enabled = value;
			}
		}

		private bool enableSliders = true;
		public bool EnableSliders {
			get => enableSliders;
			set {
				enableSliders = value;
				foreach (var slider in sliders) {
					var c = slider.TextColor;
					slider.TextColor = new(c.R, c.G, c.B, value ? 255 : 0);
				}
			}
		}
		public NumSlider GetNumSlider(int index) => sliders[index];
		public Button GetButton() => button;

		public static TransformPanel New(Element parent, string text, int floats, KeyframeProperty property = KeyframeProperty.None) {
			var panel = parent.Add<TransformPanel>();
			panel.DockPadding = RectangleF.TLRB(2);
			panel.BorderSize = 2;

			panel.button = panel.Add<Button>();
			panel.button.Dock = Dock.Left;
			panel.button.Text = text;
			panel.button.Size = new(96);
			panel.button.MouseReleaseEvent += (v1, v2, v3) => panel.OnSelected?.Invoke(panel, v2, v3);
			panel.button.BorderSize = 0;

			panel.keyframeY = panel.Add<KeyframeButton>();
			panel.keyframeY.Dock = Dock.Right;
			panel.keyframeY.Size = new(26);
			panel.keyframeY.Property = property;
			panel.keyframeY.MouseReleaseEvent += (v1, v2, v3) => panel.OnKeyframe?.Invoke(property, 1);
			panel.keyframeY.BorderSize = 0;
			panel.keyframeY.Enabled = false;
			panel.keyframeY.TooltipText = "Keyframe Y";

			panel.keyframeX = panel.Add<KeyframeButton>();
			panel.keyframeX.Dock = Dock.Right;
			panel.keyframeX.Size = new(26);
			panel.keyframeX.Property = property;
			panel.keyframeX.MouseReleaseEvent += (v1, v2, v3) => panel.OnKeyframe?.Invoke(property, 0);
			panel.keyframeX.BorderSize = 0;
			panel.keyframeX.Enabled = false;
			panel.keyframeX.TooltipText = "Keyframe X";

			panel.keyframe = panel.Add<KeyframeButton>();
			panel.keyframe.Dock = Dock.Right;
			panel.keyframe.Size = new(26);
			panel.keyframe.Property = property;
			panel.keyframe.MouseReleaseEvent += (v1, v2, v3) => panel.OnKeyframe?.Invoke(property, -1);
			panel.keyframe.BorderSize = 0;

			//ModelEditor.Active.File.PropertySeparatedOrCombined += (b, prop, separated) => {
			//	if (prop == property)
			//		panel.keyframe2.Enabled = separated;
			//};

			var floatparts = panel.Add<FlexPanel>();
			floatparts.Dock = Dock.Fill;
			floatparts.Direction = Directional180.Horizontal;
			floatparts.ChildrenResizingMode = FlexChildrenResizingMode.StretchToFit;
			floatparts.DockPadding = RectangleF.Zero;
			floatparts.BorderSize = 0;

			panel.sliders = new NumSlider[floats];
			for (int i = 0; i < floats; i++) {
				var floatEdit = floatparts.Add<NumSlider>();
				panel.sliders[i] = floatEdit;
				floatEdit.HelperText = "";
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
