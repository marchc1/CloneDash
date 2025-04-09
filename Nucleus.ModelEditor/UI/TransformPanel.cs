using Nucleus.Types;
using Nucleus.UI;
using Raylib_cs;

namespace Nucleus.ModelEditor
{
	public class TransformPanel : Panel
	{
		public delegate void FloatChange(int i, float value);
		public event FloatChange? FloatChanged;

		public event Element.MouseEventDelegate? OnSelected;
		public event Element.MouseEventDelegate? OnKeyframe;
		private NumSlider[] sliders;
		private Button button;
		private Button keyframe;

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
		public Button GetKeyframeButton() => keyframe;

		public static TransformPanel New(Element parent, string text, int floats) {
			var panel = parent.Add<TransformPanel>();
			panel.DockPadding = RectangleF.TLRB(2);
			panel.BorderSize = 2;

			panel.button = panel.Add<Button>();
			panel.button.Dock = Dock.Left;
			panel.button.Text = text;
			panel.button.Size = new(96);
			panel.button.MouseReleaseEvent += (v1, v2, v3) => panel.OnSelected?.Invoke(panel, v2, v3);
			panel.button.BorderSize = 0;

			panel.keyframe = panel.Add<Button>();
			panel.keyframe.Dock = Dock.Right;
			panel.keyframe.Text = "";
			panel.keyframe.Image = panel.Textures.LoadTextureFromFile("models/keyframe.png");
			panel.keyframe.ImageOrientation = ImageOrientation.Centered;
			panel.keyframe.Size = new(26);
			panel.keyframe.MouseReleaseEvent += (v1, v2, v3) => panel.OnKeyframe?.Invoke(panel, v2, v3);
			panel.keyframe.BorderSize = 0;
			panel.keyframe.PaintOverride += (self, w, h) => {
				bool canInput = self.CanInput();
				bool isAnimateMode = ModelEditor.Active.AnimationMode;

				bool canUse = (canInput && isAnimateMode);

				if (canUse) {
					// todo: determine background color
					self.BackgroundColor = ModelEditor.KEYFRAME_COLOR_NO_PENDING_KEYFRAME;
					self.ImageColor = Color.Black;
				}
				else {
					self.BackgroundColor = Element.DefaultBackgroundColor;
					self.ImageColor = Color.Gray;
				}

				self.Paint(w, h);
			};

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
