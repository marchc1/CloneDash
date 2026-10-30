using Nucleus.Common.Input;
using Nucleus.Common.Types;
using Nucleus.Common.UI;
using Nucleus.Core;
using Nucleus.Extensions;
using Nucleus.Types;

using Raylib_cs;

using System.Numerics;

namespace Nucleus.UI.Elements;

public delegate void ColorChangedFn(ColorSelector selector, ref Color color);
public delegate void ColorPickedFn(ColorPicker picker, Color color);

internal static class ColorDraw
{
	public static void Checkerboard(float x, float y, float width, float height, float cell = 6) {
		Graphics2D.SetDrawColor(150, 150, 150);
		Graphics2D.DrawRectangle(x, y, width, height);
		Graphics2D.SetDrawColor(95, 95, 95);
		for (float cellY = 0; cellY < height; cellY += cell) {
			for (float cellX = ((int)(cellY / cell) % 2) * cell; cellX < width; cellX += cell * 2)
				Graphics2D.DrawRectangle(x + cellX, y + cellY, MathF.Min(cell, width - cellX), MathF.Min(cell, height - cellY));
		}
	}

	public static void Vertex(float x, float y, Color color) {
		Rlgl.Color4ub(color.R, color.G, color.B, color.A);
		Rlgl.Vertex2f(x, y);
	}

	/// <summary>
	/// Gradient through evenly spaced stops, left to right or top to bottom, local coordinates
	/// </summary>
	public static void Gradient(float x, float y, float width, float height, ReadOnlySpan<Color> stops, bool vertical = false) {
		var offset = Graphics2D.Offset;
		x += offset.X;
		y += offset.Y;
		Rlgl.SetTexture(Rlgl.GetTextureIdDefault());
		Rlgl.Begin(DrawMode.TRIANGLES);
		int segments = stops.Length - 1;
		for (int i = 0; i < segments; i++) {
			Color from = stops[i];
			Color to = stops[i + 1];
			if (vertical) {
				float y0 = y + height * i / segments, y1 = y + height * (i + 1) / segments;
				Vertex(x, y0, from); Vertex(x, y1, to); Vertex(x + width, y1, to);
				Vertex(x, y0, from); Vertex(x + width, y1, to); Vertex(x + width, y0, from);
			}
			else {
				float x0 = x + width * i / segments, x1 = x + width * (i + 1) / segments;
				Vertex(x0, y, from); Vertex(x0, y + height, from); Vertex(x1, y + height, to);
				Vertex(x0, y, from); Vertex(x1, y + height, to); Vertex(x1, y, to);
			}
		}
		Rlgl.End();
		Rlgl.SetTexture(0);
	}
}

public enum ColorSwatch
{
	/// <summary> Left half with alpha, right half without </summary>
	Both,
	/// <summary> All of it with alpha </summary>
	WithAlpha,
	/// <summary> All of it without alpha </summary>
	Opaque
}

/// <summary>
/// A color swatch button that opens a <see cref="ColorPicker"/>
/// </summary>
public class ColorSelector(Element? parent, ReadOnlySpan<char> name = default) : Element(parent, name)
{
	public ColorSwatch Swatch { get; set; } = ColorSwatch.Both;

	public event ColorChangedFn? OnColorChanged;

	Color color = Color.White;
	public Color SelectedColor {
		get => color;
		set {
			if (color == value)
				return;
			color = value;
			OnColorChanged?.Invoke(this, ref color);
			if (IValidatable.IsValid(CurrentDialog))
				CurrentDialog.Picker.Color = color;
		}
	}

	/// <summary>
	/// Sets the color without firing <see cref="OnColorChanged"/>
	/// </summary>
	public void SetColorNoUpdate(Color value) {
		color = value;
		if (IValidatable.IsValid(CurrentDialog))
			CurrentDialog.Picker.Color = color;
	}

	public ColorSelectorDialog? CurrentDialog { get; protected set; }

	protected override bool MouseRelease(Element self, FrameState state, ButtonCode button) {
		if (!IsHovered()) return true;
		if (IValidatable.IsValid(CurrentDialog))
			CurrentDialog.Remove();
		else
			CurrentDialog = new ColorSelectorDialog(UI, this);
		return true;
	}

	public override void Paint(float width, float height) {
		float x = 3, y = 3;
		float innerWidth = width - 6, innerHeight = height - 6;
		float half = Swatch switch {
			ColorSwatch.WithAlpha => innerWidth,
			ColorSwatch.Opaque => 0,
			_ => MathF.Round(innerWidth / 2)
		};

		ColorDraw.Checkerboard(x, y, half, innerHeight);
		Graphics2D.SetDrawColor(color);
		Graphics2D.DrawRectangle(x, y, half, innerHeight);
		Graphics2D.SetDrawColor(color, 255);
		Graphics2D.DrawRectangle(x + half, y, innerWidth - half, innerHeight);
	}

	public override void PaintBorder(float width, float height) {
		int light;
		if (!IsHovered())
			light = 120;
		else if (IsDepressed())
			light = 90;
		else
			light = 200;
		Graphics2D.SetDrawColor(light, light, light, 255);
		Graphics2D.DrawRectangleOutline(0, 0, width, height, 2);
	}
}

public class ColorSelectorDialog : Panel
{
	public readonly ColorSelector Selector;
	public readonly ColorPicker Picker;

	public ColorSelectorDialog(Element parent, ColorSelector selector) : base(parent) {
		Selector = selector;
		DockPadding = RectangleF.TLRB(8);

		Picker = new ColorPicker(this);
		Picker.Dock = Dock.Fill;
		Picker.Color = selector.SelectedColor;
		Picker.ColorChanged += (_, c) => Selector.SelectedColor = c;

		Size = new(ColorPicker.PreferredSize.W + 16, ColorPicker.PreferredSize.H + 16);
		var anchor = selector.GetGlobalPosition();
		Position = new(anchor.X, anchor.Y + selector.GetRenderBounds().H + 4);
		MakePopup();
		MoveToFront();
		FitToParent(8);

		UI.Input.OnClick += UI_OnClick;
	}

	public override void ApplySchemeSettings(IScheme scheme) {
		base.ApplySchemeSettings(scheme);
		SetBgSchemeColor(scheme.GetColor("Nucleus.Menu.Background", new(20, 30, 45, 250)));
		SetFgSchemeColor(scheme.GetColor("Nucleus.Menu.Border", new(190, 195, 195, 114)));
	}

	private void UI_OnClick(Element? element) {
		if (element == null || (element != this && !element.IsIndirectChildOf(this) && element != Selector))
			Remove();
	}

	protected override void OnRemoval() {
		base.OnRemoval();
		if (UI != null)
			UI.Input.OnClick -= UI_OnClick;
	}
}

/// <summary>
/// HSV/RGB/alpha fields, a hue ring with a saturation/value triangle, a before/after swatch and an alpha bar
/// </summary>
public class ColorPicker : Panel
{
	public static readonly Vector2F PreferredSize = new(520, 312);

	public event ColorPickedFn? ColorChanged;

	Color color = Color.White;
	Color original = Color.White;
	float hue;
	float sat;
	float val;

	/// <summary>
	/// Setting this doesn't fire <see cref="ColorChanged"/>, and it becomes the "before" color too
	/// </summary>
	public Color Color {
		get => color;
		set {
			color = value;
			original = value;
			ReadHSV();
			UpdateFields();
		}
	}
	public Color OriginalColor => original;
	public float Hue => hue;
	public float Saturation => sat;
	public float Value => val;

	public readonly ColorWheel Wheel;
	public readonly ColorCompare Compare;
	public readonly AlphaBar Alpha;
	public readonly ChannelSlider[] Channels;
	public readonly Textbox Hex;

	public ColorPicker(Element? parent) : base(parent) {
		SetPaintBackgroundEnabled(false);
		SetPaintBorderEnabled(false);
		Size = PreferredSize;

		Channels = [
			new ChannelSlider(this, ColorChannel.Hue),
			new ChannelSlider(this, ColorChannel.Saturation),
			new ChannelSlider(this, ColorChannel.Value),
			new ChannelSlider(this, ColorChannel.Red),
			new ChannelSlider(this, ColorChannel.Green),
			new ChannelSlider(this, ColorChannel.Blue),
			new ChannelSlider(this, ColorChannel.Alpha),
		];

		Hex = new Textbox(this);
		Hex.TextSize = 19;
		Hex.OnUserPressedEnter += (_, _, text) => {
			if (((ReadOnlySpan<char>)text).TryParseHexToColor(out Color parsed, out _)) {
				if (text.TrimStart().TrimStart('#').Length < 8)
					parsed.A = color.A;
				SetRGB(parsed);
			}
			UpdateFields();
		};

		Compare = new ColorCompare(this);
		Wheel = new ColorWheel(this);
		Alpha = new AlphaBar(this);

		ReadHSV();
		UpdateFields();
	}

	private void ReadHSV() {
		var hsv = color.RGBubToHSVf();
		if (!(color.R == color.G && color.G == color.B))
			hue = hsv.X;
		if (color.R != 0 || color.G != 0 || color.B != 0)
			sat = hsv.Y;
		val = hsv.Z;
	}

	private void Apply(Color newColor, bool fromRGB = false) {
		bool changed = newColor != color;
		color = newColor;
		if (fromRGB)
			ReadHSV();
		UpdateFields();
		if (changed)
			ColorChanged?.Invoke(this, newColor);
	}

	public void SetHSV(float newHue, float newSat, float newVal) {
		hue = (newHue % 360 + 360) % 360;
		sat = Math.Clamp(newSat, 0, 1);
		val = Math.Clamp(newVal, 0, 1);
		Apply(new Vector3(hue, sat, val).HSVfToRGBub(color.A / 255f));
	}

	public void SetRGB(Color newColor) => Apply(newColor, true);

	public void SetAlpha(byte alpha) {
		var updated = color;
		updated.A = alpha;
		Apply(updated);
	}

	internal void SetChannel(ColorChannel channel, double value) {
		var updated = color;
		byte byteValue = (byte)Math.Clamp(Math.Round(value), 0, 255);
		switch (channel) {
			case ColorChannel.Hue: SetHSV((float)value, sat, val); return;
			case ColorChannel.Saturation: SetHSV(hue, (float)value / 100, val); return;
			case ColorChannel.Value: SetHSV(hue, sat, (float)value / 100); return;
			case ColorChannel.Alpha: SetAlpha(byteValue); return;
			case ColorChannel.Red: updated.R = byteValue; break;
			case ColorChannel.Green: updated.G = byteValue; break;
			case ColorChannel.Blue: updated.B = byteValue; break;
		}
		SetRGB(updated);
	}

	internal double ChannelValue(ColorChannel channel) => channel switch {
		ColorChannel.Hue => hue,
		ColorChannel.Saturation => sat * 100,
		ColorChannel.Value => val * 100,
		ColorChannel.Red => color.R,
		ColorChannel.Green => color.G,
		ColorChannel.Blue => color.B,
		_ => color.A,
	};

	private void UpdateFields() {
		if (Channels == null) return;
		foreach (var slider in Channels) {
			if (!slider.IsKeyboardFocused())
				slider.SetValueNoUpdate(ChannelValue(slider.Channel));
		}
		if (!Hex.IsKeyboardFocused())
			Hex.Text = color.ToHex(true).ToString();
	}

	public const float GAP = 4;
	public const float MIN_ROW = 22;
	public const float MAX_ROW = 34;
	public const float COMPARE_W = 28;
	public const float ALPHA_W = 24;
	public const float MIN_WHEEL = 120;

	private static float RowHeight(float space, int rows) => MathF.Floor((space - GAP * (rows - 1)) / rows);

	// Fields as one column of 8 rows, or two columns (H|R, S|G, V|B, A, hex)
	private void LayoutFields(float x, float y, float width, float height, bool twoColumns) {
		int rows = twoColumns ? 5 : Channels.Length + 1;
		float rowH = Math.Clamp(RowHeight(height, rows), MIN_ROW, MAX_ROW);
		float colW = twoColumns ? MathF.Floor((width - GAP) / 2) : width;

		if (twoColumns) {
			for (int i = 0; i < 3; i++) {
				Place(Channels[i], i, 0, x, y, width, rowH, colW);
				Place(Channels[i + 3], i, 1, x, y, width, rowH, colW);
			}
			Place(Channels[6], 3, 0, x, y, width, rowH, colW, true);
			Place(Hex, 4, 0, x, y, width, rowH, colW, true);
		}
		else {
			for (int i = 0; i < Channels.Length; i++)
				Place(Channels[i], i, 0, x, y, width, rowH, colW);
			Place(Hex, Channels.Length, 0, x, y, width, rowH, colW);
		}
	}

	private static void Place(Element element, int row, int col, float x, float y, float width, float rowH, float colW, bool span = false) {
		element.Position = new(x + col * (colW + GAP), y + row * (rowH + GAP));
		element.Size = new(span ? width : colW, rowH);
	}

	private float FieldsHeight(bool twoColumns, float rowH) {
		int rows = twoColumns ? 5 : Channels.Length + 1;
		return rows * rowH + (rows - 1) * GAP;
	}

	protected override void PerformLayout(float width, float height) {
		base.PerformLayout(width, height);
		const float SIDE = COMPARE_W + ALPHA_W + GAP * 3;

		// Side by side with one or two columns of fields, whichever leaves the bigger wheel
		float best = -1;
		bool bestTwo = false;
		foreach (bool two in new[] { false, true }) {
			if (RowHeight(height, two ? 5 : 8) < MIN_ROW) continue;
			float wheelSize = MathF.Min(height, width - (two ? 184 : 110) - SIDE);
			if (wheelSize > best) {
				best = wheelSize;
				bestTwo = two;
			}
		}
		float stacked = MathF.Min(width - SIDE, height - FieldsHeight(true, MIN_ROW) - GAP);

		if (best >= MIN_WHEEL || (best > 0 && best >= stacked)) {
			// fields | compare | wheel | alpha
			float wheel = best;
			bool oneColumn = !bestTwo;
			float fieldsW = MathF.Min(width - SIDE - wheel, oneColumn ? 200 : 300);
			float fieldsH = MathF.Min(height, FieldsHeight(!oneColumn, MAX_ROW));
			float barH = MathF.Max(wheel, fieldsH);
			LayoutFields(0, MathF.Round((barH - fieldsH) / 2), fieldsW, fieldsH, !oneColumn);

			float x = fieldsW + GAP;
			Compare.Position = new(x, 0);
			Compare.Size = new(COMPARE_W, barH);
			x += COMPARE_W + GAP;
			Wheel.Position = new(x, MathF.Round((barH - wheel) / 2));
			Wheel.Size = new(wheel, wheel);
			x += wheel + GAP;
			Alpha.Position = new(x, 0);
			Alpha.Size = new(ALPHA_W, barH);
			return;
		}

		// Stacked: compare | wheel | alpha on top, two columns of fields below
		float fieldsH2 = FieldsHeight(true, MIN_ROW);
		float wheel2 = MathF.Max(0, MathF.Min(width - SIDE, height - fieldsH2 - GAP));
		float top = wheel2;
		float fieldsH3 = MathF.Min(height - top - GAP, FieldsHeight(true, MAX_ROW));
		float y0 = MathF.Max(0, MathF.Round((height - top - GAP - fieldsH3) / 2));
		float total = COMPARE_W + GAP + wheel2 + GAP + ALPHA_W;
		float left = MathF.Max(0, MathF.Round((width - total) / 2));

		Compare.Position = new(left, y0);
		Compare.Size = new(COMPARE_W, top);
		Wheel.Position = new(left + COMPARE_W + GAP, y0);
		Wheel.Size = new(wheel2, wheel2);
		Alpha.Position = new(left + COMPARE_W + GAP + wheel2 + GAP, y0);
		Alpha.Size = new(ALPHA_W, top);
		LayoutFields(0, y0 + top + GAP, width, fieldsH3, true);
	}
}

public enum ColorChannel { Hue, Saturation, Value, Red, Green, Blue, Alpha }

/// <summary>
/// A <see cref="NumSlider"/> showing what its channel does to the current color
/// </summary>
public class ChannelSlider : NumSlider
{
	public readonly ColorChannel Channel;
	readonly ColorPicker picker;

	public ChannelSlider(ColorPicker picker, ColorChannel channel) : base(picker) {
		this.picker = picker;
		Channel = channel;
		MinimumValue = 0;
		MaximumValue = channel switch {
			ColorChannel.Hue => 360,
			ColorChannel.Saturation or ColorChannel.Value => 100,
			_ => 255
		};
		Digits = 0;
		TextSize = 19;
		BarPadding = 3;
		OnValueChanged += (_, _, v) => picker.SetChannel(Channel, v);
	}

	private static string Letter(ColorChannel channel) => channel switch {
		ColorChannel.Hue => "H", ColorChannel.Saturation => "S", ColorChannel.Value => "V",
		ColorChannel.Red => "R", ColorChannel.Green => "G", ColorChannel.Blue => "B", _ => "A"
	};

	private Color At(float t) {
		var current = picker.Color;
		current.A = 255;
		return Channel switch {
			ColorChannel.Hue => new Vector3(t * 360, MathF.Max(picker.Saturation, 0.15f), MathF.Max(picker.Value, 0.35f)).HSVfToRGBub(),
			ColorChannel.Saturation => new Vector3(picker.Hue, t, picker.Value).HSVfToRGBub(),
			ColorChannel.Value => new Vector3(picker.Hue, picker.Saturation, t).HSVfToRGBub(),
			ColorChannel.Red => new Color((int)(t * 255), current.G, current.B, 255),
			ColorChannel.Green => new Color(current.R, (int)(t * 255), current.B, 255),
			ColorChannel.Blue => new Color(current.R, current.G, (int)(t * 255), 255),
			_ => new Color(current.R, current.G, current.B, (int)(t * 255)),
		};
	}

	private static Color Faded(Color color, int alpha) => new(color.R, color.G, color.B, alpha * color.A / 255);

	protected override void PaintBar(float width, float height) {
		float x = BarPadding;
		float barWidth = width - BarPadding * 2;
		float stripH = 5;
		float stripY = height - BarPadding - stripH;
		int steps = Channel == ColorChannel.Hue ? 12 : 1;
		Span<Color> stops = stackalloc Color[steps + 1];
		Span<Color> faded = stackalloc Color[steps + 1];
		for (int i = 0; i <= steps; i++) {
			stops[i] = At((float)i / steps);
			faded[i] = Faded(stops[i], 55);
		}

		// Tint behind the text, full color strip along the bottom
		if (Channel == ColorChannel.Alpha)
			ColorDraw.Checkerboard(x, stripY, barWidth, stripH, 2.5f);
		ColorDraw.Gradient(x, BarPadding, barWidth, stripY - BarPadding, faded);
		ColorDraw.Gradient(x, stripY, barWidth, stripH, stops);

		float t = (float)NMath.Remap(Value, MinimumValue!.Value, MaximumValue!.Value, 0, 1, true);
		float markerX = MathF.Round(x + t * barWidth);
		Graphics2D.SetDrawColor(15, 15, 15);
		Graphics2D.DrawRectangle(markerX - 2, stripY - 3, 4, stripH + 3);
		Graphics2D.SetDrawColor(255, 255, 255);
		Graphics2D.DrawRectangle(markerX - 1, stripY - 2, 2, stripH + 1);

		Graphics2D.SetDrawColor(225, 230, 235);
		Graphics2D.DrawText(new(BarPadding + 6, (height - stripH) / 2), Letter(Channel), Graphics2D.UI_FONT_NAME, 18, Anchor.CenterLeft);
	}
}

/// <summary>
/// Before/after swatch. Clicking the bottom half resets to the starting color.
/// </summary>
public class ColorCompare : Element
{
	readonly ColorPicker picker;

	public ColorCompare(ColorPicker picker) : base(picker) {
		this.picker = picker;
	}

	bool OverOriginal => GetMousePos().Y > GetRenderBounds().H / 2;

	protected override void OnThink() {
		if (IsHovered() && OverOriginal)
			EngineCore.SetMouseCursor(MouseCursor.MOUSE_CURSOR_POINTING_HAND);
	}

	protected override bool MouseRelease(Element self, FrameState state, ButtonCode button) {
		if (IsHovered() && OverOriginal)
			picker.SetRGB(picker.OriginalColor);
		return true;
	}

	public override void Paint(float width, float height) {
		float half = MathF.Round(height / 2);
		ColorDraw.Checkerboard(0, 0, width, height);
		Graphics2D.SetDrawColor(picker.Color);
		Graphics2D.DrawRectangle(0, 0, width, half);
		Graphics2D.SetDrawColor(picker.OriginalColor);
		Graphics2D.DrawRectangle(0, half, width, height - half);

		if (IsHovered() && OverOriginal) {
			Graphics2D.SetDrawColor(255, 255, 255, 60);
			Graphics2D.DrawRectangle(0, half, width, height - half);
		}
		Graphics2D.SetDrawColor(15, 15, 15, 200);
		Graphics2D.DrawLine(0, half, width, half, 2);
	}

	public override void PaintBorder(float width, float height) {
		Graphics2D.SetDrawColor(120, 125, 130, 255);
		Graphics2D.DrawRectangleOutline(0, 0, width, height, 1);
	}
}

/// <summary>
/// Vertical alpha slider, opaque at the top
/// </summary>
public class AlphaBar : Element
{
	readonly ColorPicker picker;
	bool dragging;

	public AlphaBar(ColorPicker picker) : base(picker) {
		this.picker = picker;
	}

	private void SetFromMouse() {
		float barHeight = GetRenderBounds().H;
		float t = Math.Clamp(1 - GetMousePos().Y / MathF.Max(barHeight, 1), 0, 1);
		picker.SetAlpha((byte)MathF.Round(t * 255));
	}

	protected override void OnThink() {
		if (IsHovered() || dragging)
			EngineCore.SetMouseCursor(MouseCursor.MOUSE_CURSOR_RESIZE_NS);
	}

	protected override bool MouseClick(FrameState state, ButtonCode button) {
		dragging = true;
		SetFromMouse();
		return true;
	}

	protected override bool MouseDrag(Element self, FrameState state, Vector2F delta) {
		if (dragging)
			SetFromMouse();
		return true;
	}

	protected override bool MouseRelease(Element self, FrameState state, ButtonCode button) {
		dragging = false;
		return true;
	}

	public override void Paint(float width, float height) {
		var color = picker.Color;
		ColorDraw.Checkerboard(0, 0, width, height);
		ColorDraw.Gradient(0, 0, width, height, [new Color((int)color.R, color.G, color.B, 255), new Color((int)color.R, color.G, color.B, 0)], vertical: true);

		float y = MathF.Round((1 - color.A / 255f) * height);
		Graphics2D.SetDrawColor(15, 15, 15);
		Graphics2D.DrawRectangle(-2, y - 3, width + 4, 6);
		Graphics2D.SetDrawColor(255, 255, 255);
		Graphics2D.DrawRectangle(-1, y - 1, width + 2, 2);
	}

	public override void PaintBorder(float width, float height) {
		Graphics2D.SetDrawColor(120, 125, 130, 255);
		Graphics2D.DrawRectangleOutline(0, 0, width, height, 1);
	}
}

/// <summary>
/// Hue ring with a saturation/value triangle inside it
/// </summary>
public class ColorWheel : Element
{
	public enum DragTarget { None, Hue, SatVal }

	readonly ColorPicker picker;
	public DragTarget Dragging { get; private set; }

	public ColorWheel(ColorPicker picker) : base(picker) {
		this.picker = picker;
	}

	// hue 0 points up and goes clockwise
	private static Vector2F Direction(float degrees) {
		float radians = degrees * (MathF.PI / 180f);
		return new(MathF.Sin(radians), -MathF.Cos(radians));
	}

	Vector2F Center => GetRenderBounds().Size / 2;
	float OuterRadius => MathF.Min(GetRenderBounds().W, GetRenderBounds().H) / 2 - 1;
	float InnerRadius => OuterRadius * 0.8f;
	float TriangleRadius => InnerRadius - 3;

	/// <summary>
	/// Pure hue, white and black corners for the current hue, local to the wheel
	/// </summary>
	public (Vector2F Hue, Vector2F White, Vector2F Black) Triangle() {
		var center = Center;
		float radius = TriangleRadius;
		return (
			center + Direction(picker.Hue) * radius,
			center + Direction(picker.Hue + 120) * radius,
			center + Direction(picker.Hue + 240) * radius);
	}

	public Vector2F SatValPosition() {
		var triangle = Triangle();
		float saturation = picker.Saturation;
		float value = picker.Value;
		return triangle.Hue * (saturation * value)
			+ triangle.White * ((1 - saturation) * value)
			+ triangle.Black * (1 - value);
	}

	private static Vector3 Barycentric(Vector2F p, Vector2F a, Vector2F b, Vector2F c) {
		var v0 = b - a;
		var v1 = c - a;
		var v2 = p - a;
		float d00 = v0.Dot(v0);
		float d01 = v0.Dot(v1);
		float d11 = v1.Dot(v1);
		float d20 = v2.Dot(v0);
		float d21 = v2.Dot(v1);
		float denom = d00 * d11 - d01 * d01;
		if (MathF.Abs(denom) < 1e-6f) return new(1, 0, 0);
		float v = (d11 * d20 - d01 * d21) / denom;
		float w = (d00 * d21 - d01 * d20) / denom;
		return new(1 - v - w, v, w);
	}

	private static Vector2F ClosestOnSegment(Vector2F p, Vector2F a, Vector2F b) {
		var ab = b - a;
		float t = Math.Clamp((p - a).Dot(ab) / MathF.Max(ab.Dot(ab), 1e-6f), 0, 1);
		return a + ab * t;
	}

	private bool InTriangle(Vector2F point) {
		var triangle = Triangle();
		var weights = Barycentric(point, triangle.Hue, triangle.White, triangle.Black);
		return weights.X >= 0 && weights.Y >= 0 && weights.Z >= 0;
	}

	/// <summary>
	/// Saturation and value at a point, clamped to the triangle
	/// </summary>
	public (float Saturation, float Value) SatValAt(Vector2F point) {
		var triangle = Triangle();
		if (!InTriangle(point)) {
			Vector2F best = ClosestOnSegment(point, triangle.Hue, triangle.White);
			Vector2F candidate = ClosestOnSegment(point, triangle.White, triangle.Black);
			if ((candidate - point).Length < (best - point).Length)
				best = candidate;
			candidate = ClosestOnSegment(point, triangle.Black, triangle.Hue);
			if ((candidate - point).Length < (best - point).Length)
				best = candidate;
			point = best;
		}

		var weights = Barycentric(point, triangle.Hue, triangle.White, triangle.Black);
		float hueWeight = Math.Clamp(weights.X, 0, 1);
		float whiteWeight = Math.Clamp(weights.Y, 0, 1);
		float value = Math.Clamp(hueWeight + whiteWeight, 0, 1);
		float saturation = value > 1e-4f ? Math.Clamp(hueWeight / value, 0, 1) : picker.Saturation;
		return (saturation, value);
	}

	public float HueAt(Vector2F point) {
		var offset = point - Center;
		float degrees = MathF.Atan2(offset.X, -offset.Y) * (180f / MathF.PI);
		return (degrees + 360) % 360;
	}

	public DragTarget TargetAt(Vector2F point) {
		if (InTriangle(point)) return DragTarget.SatVal;
		float dist = (point - Center).Length;
		if (dist >= InnerRadius - 3 && dist <= OuterRadius + 3) return DragTarget.Hue;
		return DragTarget.None;
	}

	public override bool HoverTest(RectangleF bounds, Vector2F mousePos) {
		var local = mousePos - bounds.Pos;
		return bounds.ContainsPoint(mousePos) && (local - bounds.Size / 2).Length <= MathF.Min(bounds.W, bounds.H) / 2 + 2;
	}

	private void DragTo(Vector2F point) {
		switch (Dragging) {
			case DragTarget.Hue:
				picker.SetHSV(HueAt(point), picker.Saturation, picker.Value);
				break;
			case DragTarget.SatVal:
				var satVal = SatValAt(point);
				picker.SetHSV(picker.Hue, satVal.Saturation, satVal.Value);
				break;
		}
	}

	protected override void OnThink() {
		if (Dragging != DragTarget.None || (IsHovered() && TargetAt(GetMousePos()) != DragTarget.None))
			EngineCore.SetMouseCursor(MouseCursor.MOUSE_CURSOR_CROSSHAIR);
	}

	protected override bool MouseClick(FrameState state, ButtonCode button) {
		if (button != ButtonCode.MouseLeft) return true;
		Dragging = TargetAt(GetMousePos());
		DragTo(GetMousePos());
		return true;
	}

	protected override bool MouseDrag(Element self, FrameState state, Vector2F delta) {
		DragTo(GetMousePos());
		return true;
	}

	protected override bool MouseRelease(Element self, FrameState state, ButtonCode button) {
		Dragging = DragTarget.None;
		return true;
	}

	public override void Paint(float width, float height) {
		var offset = Graphics2D.Offset;
		var center = Center + offset;
		float outer = OuterRadius, inner = InnerRadius;
		const int SEGMENTS = 120;

		// backing disc behind the triangle
		Graphics2D.SetDrawColor(12, 16, 24, 200);
		Graphics2D.DrawCircle(Center, inner);

		Rlgl.SetTexture(Rlgl.GetTextureIdDefault());
		Rlgl.Begin(DrawMode.TRIANGLES);
		for (int i = 0; i < SEGMENTS; i++) {
			float startAngle = i * 360f / SEGMENTS, endAngle = (i + 1) * 360f / SEGMENTS;
			Color startColor = new Vector3(startAngle, 1, 1).HSVfToRGBub();
			Color endColor = new Vector3(endAngle % 360, 1, 1).HSVfToRGBub();
			Vector2F start = Direction(startAngle), end = Direction(endAngle);
			ColorDraw.Vertex(center.X + start.X * outer, center.Y + start.Y * outer, startColor);
			ColorDraw.Vertex(center.X + start.X * inner, center.Y + start.Y * inner, startColor);
			ColorDraw.Vertex(center.X + end.X * inner, center.Y + end.Y * inner, endColor);
			ColorDraw.Vertex(center.X + start.X * outer, center.Y + start.Y * outer, startColor);
			ColorDraw.Vertex(center.X + end.X * inner, center.Y + end.Y * inner, endColor);
			ColorDraw.Vertex(center.X + end.X * outer, center.Y + end.Y * outer, endColor);
		}

		var triangle = Triangle();
		ColorDraw.Vertex(triangle.Hue.X + offset.X, triangle.Hue.Y + offset.Y, new Vector3(picker.Hue, 1, 1).HSVfToRGBub());
		ColorDraw.Vertex(triangle.Black.X + offset.X, triangle.Black.Y + offset.Y, Color.Black);
		ColorDraw.Vertex(triangle.White.X + offset.X, triangle.White.Y + offset.Y, Color.White);
		Rlgl.End();
		Rlgl.SetTexture(0);

		// hue marker: a notch across the ring
		var hueDirection = Direction(picker.Hue);
		Graphics2D.SetDrawColor(15, 15, 15);
		Graphics2D.DrawLine(Center + hueDirection * (inner - 2), Center + hueDirection * (outer + 1), 6);
		Graphics2D.SetDrawColor(255, 255, 255);
		Graphics2D.DrawLine(Center + hueDirection * (inner - 1), Center + hueDirection * outer, 2);

		// sat/val marker
		var marker = SatValPosition();
		Graphics2D.SetDrawColor(15, 15, 15);
		Graphics2D.DrawCircleLines((int)MathF.Round(marker.X), (int)MathF.Round(marker.Y), 7);
		Graphics2D.SetDrawColor(255, 255, 255);
		Graphics2D.DrawCircleLines((int)MathF.Round(marker.X), (int)MathF.Round(marker.Y), 6);
		Graphics2D.DrawCircleLines((int)MathF.Round(marker.X), (int)MathF.Round(marker.Y), 5);
	}

	public override void PaintBorder(float width, float height) { }
}
