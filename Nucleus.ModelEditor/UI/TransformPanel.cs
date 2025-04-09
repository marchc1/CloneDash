using Nucleus.Types;
using Nucleus.UI;
using Raylib_cs;

namespace Nucleus.ModelEditor
{
	public class KeyframeButton : Button
	{
		public static readonly Raylib_cs.Color KEYFRAME_COLOR_NO_PENDING_KEYFRAME = new Raylib_cs.Color(90, 240, 120);
		public static readonly Raylib_cs.Color KEYFRAME_COLOR_PENDING_KEYFRAME = new Raylib_cs.Color(240, 90, 20);
		public static readonly Raylib_cs.Color KEYFRAME_COLOR_ACTIVE_KEYFRAME = new Raylib_cs.Color(240, 30, 5);

		public override void Paint(float width, float height) {
			bool canInput = CanInput();
			bool isAnimateMode = ModelEditor.Active.AnimationMode;
			var activeAnimation = ModelEditor.Active.File.Models.First().ActiveAnimation;

			bool canUse = (canInput && isAnimateMode && activeAnimation != null);

			if (canUse) {
				// todo: determine background color
				BackgroundColor = KEYFRAME_COLOR_NO_PENDING_KEYFRAME;
				ImageColor = Color.Black;
			}
			else {
				BackgroundColor = DefaultBackgroundColor;
				ImageColor = Color.Gray;
			}

			base.Paint(width, height);
		}

		public override bool CanInput() {
			if (ModelEditor.Active.File.Models.First().ActiveAnimation == null) return false;

			return base.CanInput();
		}

		protected override void Initialize() {
			Text = "";
			Image = Textures.LoadTextureFromFile("models/keyframe.png");
			ImageOrientation = ImageOrientation.Centered;
		}
	}
	public class TransformPanel : Panel
	{
		public delegate void FloatChange(int i, float value);
		public event FloatChange? FloatChanged;

		public event Element.MouseEventDelegate? OnSelected;
		public event Element.MouseEventDelegate? OnKeyframe;
		private NumSlider[] sliders;
		private Button button;
		private KeyframeButton keyframe;

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
		public KeyframeButton GetKeyframeButton() => keyframe;

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

			panel.keyframe = panel.Add<KeyframeButton>();
			panel.keyframe.Dock = Dock.Right;
			panel.keyframe.Size = new(26);
			panel.keyframe.MouseReleaseEvent += (v1, v2, v3) => panel.OnKeyframe?.Invoke(panel, v2, v3);
			panel.keyframe.BorderSize = 0;

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
