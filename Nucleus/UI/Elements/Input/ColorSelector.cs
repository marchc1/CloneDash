using Nucleus.Common.Input;
using Nucleus.Common.Types;
using Nucleus.Core;
using Nucleus.Engine;
using Nucleus.Extensions;
using Nucleus.Types;

using Raylib_cs;

using System.Numerics;

using static Nucleus.UI.Elements.ColorSelector;

namespace Nucleus.UI.Elements;

public delegate void ColorChangedFn(ColorSelector selector, ref Color color);
public class ColorSelector(Element? parent, ReadOnlySpan<char> name = default) : Element(parent, name)
{
	public event ColorChangedFn? OnColorChanged;
	public Color SelectedColor {
		get {
			return field;
		}
		set {
			if (field == value)
				return;

			field = value;
			OnColorChanged?.Invoke(this, ref field);
		}
	} = Color.White;

	public ColorSelectorDialog CurrentDialog { get; protected set; }

	protected override bool MouseRelease(Element self, FrameState state, ButtonCode button) {
		if (!IsHovered()) return true;
		if (IValidatable.IsValid(CurrentDialog))
			return true;

		CurrentDialog = new ColorSelectorDialog(UI);
		CurrentDialog.SetPos(state.Mouse.MousePos);
		CurrentDialog.Setup(this);
		CurrentDialog.FitToParent(8);
		return true;
	}
	public override void Paint(float width, float height) {
		if (IsHovered()) {
			if (IsDepressed())
				Graphics2D.SetDrawColor(50, 50, 50);
			else
				Graphics2D.SetDrawColor(190, 190, 190);

			Graphics2D.DrawRectangle(1, 1, width - 2, height - 2);
		}

		Graphics2D.SetDrawColor(SelectedColor);
		Graphics2D.DrawRectangle(3, 3, width - 6, height - 6);
	}
	public override void PaintBorder(float width, float height) {
		Graphics2D.SetDrawColor(120 + (SelectedColor.R / 2), 120 + (SelectedColor.G / 2), 120 + (SelectedColor.B / 2), 255);
		Graphics2D.DrawRectangleOutline(0, 0, width, height, 2);
	}
}

public class ColorSelectorDialog : Panel
{
	public class ColorSelectorWheel(ColorSelectorDialog dialog) : Panel(dialog)
	{
		public override void Paint(float width, float height) {
			Graphics2D.SetDrawColor(255, 255, 255);
			var hovermode = dialog.DetermineDragMode();
			var dragmode = dialog.DragMode;

			var huewheelColor = dragmode == ColorSelectorDragMode.Hue ? 160 : hovermode == ColorSelectorDragMode.Hue ? 255 : 200;
			var satvalwheelColor = dragmode == ColorSelectorDragMode.SatVal ? 160 : hovermode == ColorSelectorDragMode.SatVal ? 255 : 200;

			Graphics2D.SetDrawColor(huewheelColor, huewheelColor, huewheelColor);
			Graphics2D.SetTexture(dialog.ColorWheelTex);
			Graphics2D.DrawImage(new(0, 0), new(width, height));

			Graphics2D.SetDrawColor(255, 255, 255);
			Graphics2D.SetTexture(dialog.HueWheelTex);
			Graphics2D.DrawImage(new(0, 0), new(width, height));

			Rlgl.PushMatrix();
			var pos = dialog.GetGlobalPosition();
			Rlgl.Translatef(pos.X + (width / 2), pos.Y + (height / 2), 0);

			Rlgl.Rotatef(dialog.Hue, 0, 0, 1);
			var offset = Graphics2D.Offset;
			Graphics2D.ResetDrawingOffset();

			var rgb = new Vector3(dialog.Hue, dialog.Saturation, dialog.Value).HSVfToRGBub();

			Graphics2D.SetDrawColor(rgb);
			Graphics2D.SetTexture(dialog.ColorPickerTex);
			var centerPos = new Vector2F(-width / 2, -height / 2);
			Graphics2D.DrawImage(centerPos, new(width, height), new(0, 0), 0);
			Graphics2D.SetDrawColor(255, 255, 255);

			//var targetPos = new Vector2F();
			//targetPos.X = (float)NMath.Remap(Value, 0, 1, -1, 1) * GetTriangleSatSide();
			//targetPos.Y = (float)NMath.Remap(Saturation, 0, 1, GetTriangleBottom(), GetTriangleTop());
			var targetPos = dialog.GetSatvalXYFromCurrentColor();
			Graphics2D.SetTexture(dialog.ColorSatValTex);
			Graphics2D.SetDrawColor(satvalwheelColor, satvalwheelColor, satvalwheelColor);
			Graphics2D.DrawImage(centerPos + targetPos, new(width, height));
			Graphics2D.SetDrawColor(rgb);
			Graphics2D.SetTexture(dialog.ColorSatValInnerTex);
			Graphics2D.DrawImage(centerPos + targetPos, new(width, height));
			Graphics2D.SetDrawColor(255, 255, 255);

			Graphics2D.OffsetDrawing(offset);
			Rlgl.PopMatrix();
		}
		protected override bool MouseClick(FrameState state, ButtonCode button) {
			dialog.DragMode = dialog.DetermineDragMode();
			switch (dialog.DragMode) {
				case ColorSelectorDragMode.Hue:
					dialog.SetHueToMousePos();
					break;
				case ColorSelectorDragMode.SatVal:
					dialog.SetSatValToMousePos();
					break;
			}
			return true;
		}
		protected override bool MouseDrag(Element self, FrameState state, Vector2F delta) {
			switch (dialog.DragMode) {
				case ColorSelectorDragMode.Hue:
					dialog.SetHueToMousePos();
					break;
				case ColorSelectorDragMode.SatVal:
					dialog.SetSatValToMousePos();
					break;
			}
			return true;
		}
		protected override bool MouseRelease(Element self, FrameState state, ButtonCode button) {
			if (!IsHovered()) return true;
			dialog.DragMode = ColorSelectorDragMode.None;
			return true;
		}
	}
	public ColorSelectorDialog(Element? parent, ReadOnlySpan<char> name = default) : base(parent, name) {
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

		Raylib.SetTextureFilter(ColorWheelTex, TextureFilter.Anisotropic16x);
		Raylib.SetTextureFilter(HueWheelTex, TextureFilter.Anisotropic16x);
		Raylib.SetTextureFilter(ColorPickerTex, TextureFilter.Anisotropic16x);
		Raylib.SetTextureFilter(ColorSatValTex, TextureFilter.Anisotropic16x);
		Raylib.SetTextureFilter(ColorSatValInnerTex, TextureFilter.Anisotropic16x);

		this.SetOrigin(Anchor.BottomCenter);
		this.UI.Input.OnClick += delegate (Element? el) {
			if (el != null && !el.IsIndirectChildOf(this)) {
				this.Remove();
			}
		};
		this.SetSize(new(180, 320));
		ColorWheel = new Panel(this);
	}
	public ColorSelector Selector = null!;
	public Color SelectedColor { get => Selector.SelectedColor; set => Selector.SelectedColor = value; }

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

	public float Hue {
		get => _workingHue;
		set {
			SelectedColor = (SelectedColor.RGBubToHSVf().SetHSVf(value, _workingSat, _workingVal).HSVfToRGBub());
			_workingHue = value;
		}
	}
	public float Saturation {
		get => _workingSat;
		set {
			SelectedColor = (SelectedColor.RGBubToHSVf().SetHSVf(_workingHue, value, _workingVal).HSVfToRGBub());
			_workingSat = value;

		}
	}
	public float Value {
		get => _workingVal;
		set {
			SelectedColor = (SelectedColor.RGBubToHSVf().SetHSVf(_workingHue, _workingSat, value).HSVfToRGBub());
			_workingVal = value;
		}
	}
	public void Setup(ColorSelector parent) {
		Selector = parent;

		var hsv = SelectedColor.RGBubToHSVf();

		_workingHue = hsv.X;
		_workingSat = hsv.Y;
		_workingVal = hsv.Z;
	}

	protected float GetColorWheelWidth() => ColorWheel.GetRenderBounds().Width;
	protected float GetColorWheelWidthRatio() => ColorWheel.GetRenderBounds().Width / 180;
	protected Vector2F GetColorWheelCenterPos() => new Vector2F(ColorWheel.GetRenderBounds().Width) / 2;

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

	private void ColorWheel_MouseClickEvent(Element self, FrameState state, ButtonCode button) {

	}

	private void ColorWheel_PaintOverride(Element self, float width, float height) {
		
	}

	protected override void PerformLayout(float width, float height) {
		base.PerformLayout(width, height);
		ColorWheel.SetSize(new(width, width));
	}
}
