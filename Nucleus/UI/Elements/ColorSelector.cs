using Nucleus.Core;
using Nucleus.Engine;
using Nucleus.Types;
using Raylib_cs;

namespace Nucleus.UI.Elements
{
	public class ColorSelector : Element
	{
		private Color selectedColor = Color.White;
		public Color SelectedColor {
			get => selectedColor;
			set {
				selectedColor = value;
				ColorChanged?.Invoke(this, value);
			}
		}
		public ColorSelectorDialog CurrentDialog { get; protected set; }

		public delegate void OnColorChange(ColorSelector self, Color newColor);
		public event OnColorChange? ColorChanged;

		public override void MouseRelease(Element self, FrameState state, Types.MouseButton button) {
			if (IValidatable.IsValid(CurrentDialog))
				return;

			CurrentDialog = UI.Add<ColorSelectorDialog>();
			CurrentDialog.Position = state.MouseState.MousePos;
			CurrentDialog.Setup(this);
			CurrentDialog.FitToParent(8);
		}
		public override void Paint(float width, float height) {
			if (Hovered) {
				if (Depressed)
					Graphics2D.SetDrawColor(50, 50, 50);
				else
					Graphics2D.SetDrawColor(190, 190, 190);

				Graphics2D.DrawRectangle(1, 1, width - 2, height - 2);
			}

			Graphics2D.SetTexture(checkerboardTex);
			Graphics2D.SetDrawColor(255, 255, 255);

			Vector2F pos = new(3, 3), size = new(width / 2, height - 6);

			Raylib.DrawTexturePro(checkerboardTex, new(0, 0, size.W, size.H), new(Graphics2D.Offset.X + pos.X, Graphics2D.Offset.Y + pos.Y, size.W, size.H), new(0), 0, Color.White);

			Graphics2D.SetDrawColor(SelectedColor);
			Graphics2D.DrawRectangle(3, 3, (width / 2) - 2, height - 6);

			Graphics2D.SetDrawColor(SelectedColor, 255);
			Graphics2D.DrawRectangle(1 + (width / 2), 3, (width / 2) - 4, height - 6);
		}

		private static Raylib_cs.Texture2D checkerboardTex;
		static ColorSelector() {
			var img = Raylib.GenImageChecked(64, 64, 4, 4, Color.Gray, Color.DarkGray);
			checkerboardTex = Raylib.LoadTextureFromImage(img);
			Raylib.UnloadImage(img);
		}
	}

	public class ColorSelectorDialog : Panel
	{
		public Color SelectedColor {
			get => selector.SelectedColor;
			set {
				selector.SelectedColor = value;
				UpdateNumsliders();
			}
		}

		private void UpdateNumsliders() {
			var hsv = SelectedColor.ToHSV();
			HSlider.SetValueNoUpdate(hsv.X);
			SSlider.SetValueNoUpdate(hsv.Y);
			VSlider.SetValueNoUpdate(hsv.Z);
									
			RSlider.SetValueNoUpdate(SelectedColor.R);
			GSlider.SetValueNoUpdate(SelectedColor.G);
			BSlider.SetValueNoUpdate(SelectedColor.B);
			ASlider.SetValueNoUpdate(SelectedColor.A / 255f);

			Hexbox.Text = $"{SelectedColor.ToHex(true)}";
		}

		Panel ColorWheel;

		Texture2D ColorWheelTex;
		Texture2D HueWheelTex;
		Texture2D ColorPickerTex;
		Texture2D ColorSatValTex;
		Texture2D ColorSatValInnerTex;

		private float _workingHue = 0;
		private float _workingSat = 0;
		private float _workingVal = 0;


		public enum ColorSelectorDragMode
		{
			None,
			Hue,
			SatVal,
		}

		public ColorSelectorDragMode DragMode { get; set; } = ColorSelectorDragMode.None;

		private void UpdateHSVValues() {
			var hsv = SelectedColor.ToHSV();
			_workingHue = hsv.X;
			_workingSat = hsv.Y;
			_workingVal = hsv.Z;
		}

		public float Hue {
			get => _workingHue;
			set {
				SelectedColor = (SelectedColor.ToHSV().SetHSV(value, _workingSat, _workingVal).ToRGB());
				_workingHue = value;
			}
		}
		public float Saturation {
			get => _workingSat;
			set {
				SelectedColor = (SelectedColor.ToHSV().SetHSV(_workingHue, value, _workingVal).ToRGB());
				_workingSat = value;

			}
		}
		public float Value {
			get => _workingVal;
			set {
				SelectedColor = (SelectedColor.ToHSV().SetHSV(_workingHue, _workingSat, value).ToRGB());
				_workingVal = value;
			}
		}
		ColorSelector selector;
		public void Setup(ColorSelector selector) {
			this.selector = selector;
			var hsv = SelectedColor.ToHSV();
			_workingHue = hsv.X;
			_workingSat = hsv.Y;
			_workingVal = hsv.Z;
			UpdateNumsliders();
		}
		NumSlider RSlider;
		NumSlider GSlider;
		NumSlider BSlider;
		NumSlider HSlider;
		NumSlider SSlider;
		NumSlider VSlider;
		NumSlider ASlider;
		Textbox Hexbox;
		FlexPanel SepPanel;

		protected override void Initialize() {
			base.Initialize();

			ColorWheelTex = Level.Textures.LoadTextureFromFile("colorwheel.png");
			HueWheelTex = Level.Textures.LoadTextureFromFile("huewheel.png");
			ColorPickerTex = Level.Textures.LoadTextureFromFile("colorpicker.png");
			ColorSatValTex = Level.Textures.LoadTextureFromFile("colorsatval.png");
			ColorSatValInnerTex = Level.Textures.LoadTextureFromFile("colorsatvalinner.png");

			Raylib.GenTextureMipmaps(ref ColorWheelTex);
			Raylib.GenTextureMipmaps(ref HueWheelTex);
			Raylib.GenTextureMipmaps(ref ColorPickerTex);
			Raylib.GenTextureMipmaps(ref ColorSatValTex);
			Raylib.GenTextureMipmaps(ref ColorSatValInnerTex);

			Raylib.SetTextureFilter(ColorWheelTex, TextureFilter.TEXTURE_FILTER_ANISOTROPIC_16X);
			Raylib.SetTextureFilter(HueWheelTex, TextureFilter.TEXTURE_FILTER_ANISOTROPIC_16X);
			Raylib.SetTextureFilter(ColorPickerTex, TextureFilter.TEXTURE_FILTER_ANISOTROPIC_16X);
			Raylib.SetTextureFilter(ColorSatValTex, TextureFilter.TEXTURE_FILTER_ANISOTROPIC_16X);
			Raylib.SetTextureFilter(ColorSatValInnerTex, TextureFilter.TEXTURE_FILTER_ANISOTROPIC_16X);

			this.Origin = Anchor.BottomCenter;
			this.UI.OnElementClicked += delegate (Element el, FrameState fs, Types.MouseButton mb) {
				if (el == null) return;

				if (!el.IsIndirectChildOf(this)) {
					this.Remove();
				}
			};
			this.Size = new(180, 320);
			ColorWheel = this.Add<Panel>();
			ColorWheel.PaintOverride += ColorWheel_PaintOverride;
			ColorWheel.MouseClickEvent += ColorWheel_MouseClickEvent;
			ColorWheel.MouseDragEvent += ColorWheel_MouseDragEvent;
			ColorWheel.MouseReleaseEvent += ColorWheel_MouseReleaseEvent;

			SepPanel = Add<FlexPanel>();
			SepPanel.ChildrenResizingMode = FlexChildrenResizingMode.StretchToFit;
			SepPanel.Direction = Directional180.Horizontal;

			var rgbPanel = SepPanel.Add<FlexPanel>();
			rgbPanel.Direction = Directional180.Vertical;
			rgbPanel.ChildrenResizingMode = FlexChildrenResizingMode.StretchToFit;

			var hsvPanel = SepPanel.Add<FlexPanel>();
			hsvPanel.Direction = Directional180.Vertical;
			hsvPanel.ChildrenResizingMode = FlexChildrenResizingMode.StretchToFit;

			RSlider = rgbPanel.Add<NumSlider>();
			GSlider = rgbPanel.Add<NumSlider>();
			BSlider = rgbPanel.Add<NumSlider>();

			RSlider.MinimumValue = 0; RSlider.MaximumValue = 255; RSlider.Digits = 0;
			GSlider.MinimumValue = 0; GSlider.MaximumValue = 255; GSlider.Digits = 0;
			BSlider.MinimumValue = 0; BSlider.MaximumValue = 255; BSlider.Digits = 0;
			RSlider.Prefix = "R: ";
			GSlider.Prefix = "G: ";
			BSlider.Prefix = "B: ";
			RSlider.Value = 0;
			GSlider.Value = 0;
			BSlider.Value = 0;

			HSlider = hsvPanel.Add<NumSlider>();
			SSlider = hsvPanel.Add<NumSlider>();
			VSlider = hsvPanel.Add<NumSlider>();

			HSlider.Digits = 3;
			SSlider.MinimumValue = 0; SSlider.MaximumValue = 1; SSlider.Digits = 3;
			VSlider.MinimumValue = 0; VSlider.MaximumValue = 1; VSlider.Digits = 3;
			HSlider.Prefix = "H: ";
			SSlider.Prefix = "S: ";
			VSlider.Prefix = "V: ";
			HSlider.Value = 0;
			SSlider.Value = 0;
			VSlider.Value = 0;

			ASlider = Add<NumSlider>();
			ASlider.MinimumValue = 0;
			ASlider.MaximumValue = 1;
			ASlider.Digits = 3;
			ASlider.TextFormat = "Alpha: {0:0.000}";
			ASlider.Value = 0;

			Hexbox = Add<Textbox>();

			HSlider.OnValueChanged += (_, _, v) => Hue = (float)v;
			SSlider.OnValueChanged += (_, _, v) => Saturation = (float)Math.Clamp(v, 0, 1);
			VSlider.OnValueChanged += (_, _, v) => Value = (float)Math.Clamp(v, 0, 1);
			

			RSlider.OnValueChanged += (_, _, v) => {
				Color c = SelectedColor;
				c.R = (byte)(Math.Clamp(v, 0, 255));
				SelectedColor = c;
				UpdateHSVValues();
			};

			GSlider.OnValueChanged += (_, _, v) => {
				Color c = SelectedColor;
				c.G = (byte)(Math.Clamp(v, 0, 255));
				SelectedColor = c;
				UpdateHSVValues();
			};

			BSlider.OnValueChanged += (_, _, v) => {
				Color c = SelectedColor;
				c.B = (byte)(Math.Clamp(v, 0, 255));
				SelectedColor = c;
				UpdateHSVValues();
			};

			ASlider.OnValueChanged += (_, _, v) => {
				Color c = SelectedColor;
				c.A = (byte)(Math.Clamp(v, 0, 1) * 255f);
				SelectedColor = c;
			};

			Hexbox.TextChangedEvent += (_, _, txt) => {
				if (txt.TryParseHexToColor(out Color c))
					SelectedColor = c;
			};

			BackgroundColor = new Color(BackgroundColor.R, BackgroundColor.G, BackgroundColor.B, (byte)225);
		}


		private void ColorWheel_MouseReleaseEvent(Element self, FrameState state, Types.MouseButton button) {
			DragMode = ColorSelectorDragMode.None;
		}

		private void ColorWheel_MouseDragEvent(Element self, FrameState state, Vector2F delta) {
			switch (DragMode) {
				case ColorSelectorDragMode.Hue:
					SetHueToMousePos();
					break;
				case ColorSelectorDragMode.SatVal:
					SetSatValToMousePos();
					break;
			}
		}
		protected float GetColorWheelWidth() => ColorWheel.RenderBounds.Width;
		protected float GetColorWheelWidthRatio() => ColorWheel.RenderBounds.Width / 180;
		protected Vector2F GetColorWheelCenterPos() => new Vector2F(ColorWheel.RenderBounds.Width) / 2;

		protected float GetTriangleTop() => -48 * GetColorWheelWidthRatio();
		protected float GetTriangleBottom() => 24 * GetColorWheelWidthRatio();
		protected float GetTriangleNoSatSide() => 41 * GetColorWheelWidthRatio();
		protected float GetTriangleSatSide() => GetTriangleNoSatSide() * (1 - Saturation);
		protected float GetTriangleSatSide(float saturation) => GetTriangleNoSatSide() * (1 - saturation);
		protected float GetOuterRing() => 86 * GetColorWheelWidthRatio();
		protected float GetInnerRing() => 44 * GetColorWheelWidthRatio();

		public Triangle2D GetSatValTri() {
			var center = GetColorWheelCenterPos();
			return new Triangle2D(
				center + new Vector2F(0, GetTriangleTop()),
				center + new Vector2F(-GetTriangleNoSatSide(), GetTriangleBottom()),
				center + new Vector2F(GetTriangleNoSatSide(), GetTriangleBottom())
			);
		}
		public ColorSelectorDragMode DetermineDragMode() {
			var ret = ColorSelectorDragMode.None;
			var center = GetColorWheelCenterPos();
			var mousepos = ColorWheel.GetMousePos();
			var rotation = Hue;

			var tri = GetSatValTri().RotateAroundPoint(center, rotation);

			if (mousepos.InTriangle(tri))
				ret = ColorSelectorDragMode.SatVal;
			else if (mousepos.InRing(center, GetOuterRing(), GetInnerRing())) {
				ret = ColorSelectorDragMode.Hue;
			}

			return ret;
		}

		private void SetHueToMousePos() {
			var center = GetColorWheelCenterPos();
			var mousepos = GetMousePos();
			Hue = mousepos.GetRotationFromCenter(center);
			//Console.WriteLine(Hue);
		}
		private Vector2F GetSatvalXYFromCurrentColor() {
			// start at sat = 1, light = 1
			var ret = new Vector2F(0, GetTriangleTop());
			ret = Vector2F.Lerp(Saturation, new(GetTriangleNoSatSide(), GetTriangleBottom()), ret);
			ret = Vector2F.Lerp(Value, new(-GetTriangleNoSatSide(), GetTriangleBottom()), ret);
			return ret;
		}

		private static float TriangleArea(Vector2F v1, Vector2F v2, Vector2F v3) {
			return Math.Abs((v1.X * (v2.Y - v3.Y) + v2.X * (v3.Y - v1.Y) + v3.X * (v1.Y - v2.Y)) / 2.0f);
		}

		public static (float Saturation, float Value) CalculateSVFromPosition(Triangle2D colorTri, Vector2F position) {
			float area = TriangleArea(colorTri.A, colorTri.C, colorTri.B);
			float alpha = TriangleArea(position, colorTri.C, colorTri.B) / area;
			float beta = TriangleArea(position, colorTri.A, colorTri.B) / area;
			float gamma = TriangleArea(position, colorTri.A, colorTri.C) / area;

			if (alpha < 0 || beta < 0 || gamma < 0) {
				return (0, 0);
			}

			float saturation = alpha * 1.0f + beta * 0.0f + gamma * 0.0f;
			float value = alpha * 1.0f + beta * 1.0f + gamma * 0.0f;

			return (Math.Clamp(saturation, 0, 1), Math.Clamp(value, 0, 1));
		}

		private void SetSatValToMousePos() {
			var counterrotated = GetMousePos().RotateAroundPoint(GetColorWheelCenterPos(), -Hue);
			var center = GetColorWheelCenterPos();
			var result = CalculateSVFromPosition(GetSatValTri(), counterrotated);
			//Console.WriteLine($"{result.Saturation}, {result.Value}");
			Saturation = result.Saturation;
			Value = result.Value;
		}

		private void ColorWheel_MouseClickEvent(Element self, FrameState state, Types.MouseButton button) {
			DragMode = DetermineDragMode();
			switch (DragMode) {
				case ColorSelectorDragMode.Hue:
					SetHueToMousePos();
					break;
				case ColorSelectorDragMode.SatVal:
					SetSatValToMousePos();
					break;
			}
		}

		private void ColorWheel_PaintOverride(Element self, float width, float height) {
			Graphics2D.SetDrawColor(255, 255, 255);
			var hovermode = DetermineDragMode();

			var huewheelColor = DragMode == ColorSelectorDragMode.Hue ? 160 : hovermode == ColorSelectorDragMode.Hue ? 255 : 200;
			var satvalwheelColor = DragMode == ColorSelectorDragMode.SatVal ? 160 : hovermode == ColorSelectorDragMode.SatVal ? 255 : 200;

			Graphics2D.SetDrawColor(huewheelColor, huewheelColor, huewheelColor);
			Graphics2D.SetTexture(ColorWheelTex);
			Graphics2D.DrawImage(new(0, 0), new(width, height));

			Graphics2D.SetDrawColor(255, 255, 255);
			Graphics2D.SetTexture(HueWheelTex);
			Graphics2D.DrawImage(new(0, 0), new(width, height));

			Rlgl.PushMatrix();
			var pos = self.GetGlobalPosition();
			Rlgl.Translatef(pos.X + (width / 2), pos.Y + (height / 2), 0);

			Rlgl.Rotatef(Hue, 0, 0, 1);
			var offset = Graphics2D.Offset;
			Graphics2D.ResetDrawingOffset();

			Graphics2D.SetHSV(Hue, 1, 1);
			Graphics2D.SetTexture(ColorPickerTex);
			var centerPos = new Vector2F(-width / 2, -height / 2);
			Graphics2D.DrawImage(centerPos, new(width, height), new(0, 0), 0);
			Graphics2D.SetHSV(0, 1, 1);

			//var targetPos = new Vector2F();
			//targetPos.X = (float)NMath.Remap(Value, 0, 1, -1, 1) * GetTriangleSatSide();
			//targetPos.Y = (float)NMath.Remap(Saturation, 0, 1, GetTriangleBottom(), GetTriangleTop());
			var targetPos = GetSatvalXYFromCurrentColor();
			Graphics2D.SetTexture(ColorSatValTex);
			Graphics2D.SetDrawColor(satvalwheelColor, satvalwheelColor, satvalwheelColor);
			Graphics2D.DrawImage(centerPos + targetPos, new(width, height));
			Graphics2D.SetHSV(Hue, Saturation, Value);
			Graphics2D.SetDrawColor(255, 255, 255);
			Graphics2D.SetTexture(ColorSatValInnerTex);
			Graphics2D.DrawImage(centerPos + targetPos, new(width, height));
			Graphics2D.SetHSV(0, 1, 1);

			Graphics2D.OffsetDrawing(offset);
			Rlgl.PopMatrix();
		}

		protected override void PerformLayout(float width, float height) {
			base.PerformLayout(width, height);
			ColorWheel.Size = new(width, width);
			SepPanel.Position = new(0, width + 4);
			SepPanel.Size = new(width, 80);
			ASlider.Position = new(4, width + 4 + 80);
			ASlider.Size = new(width - 8, 24);
			Hexbox.Position = new(4, width + 8 + 104);
			Hexbox.Size = new(width - 8, 24);
		}
	}
}
