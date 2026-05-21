using Nucleus;
using Nucleus.Common.Graphics;
using Nucleus.Common.Input;
using Nucleus.Common.Types;
using Nucleus.Common.UI;
using Nucleus.Core;
using Nucleus.Extensions;
using Nucleus.Types;
using Nucleus.UI;

namespace CloneDash.UI;

public class NumberPickerCarousel : Element
{
	SchemeableSetting<Color> textColor = SchemeableSetting<Color>.Default(DefaultTextColor);

	SchemeableSetting<float> TextSize = SchemeableSetting<float>.Default(DefaultTextSize);
	SchemeableSetting<string> Font = SchemeableSetting<string>.Default(Graphics2D.UI_FONT_NAME);
	public Color GetTextColor() => textColor.Get();
	public void SetTextColor(Color value) => textColor.SetUserValue(value);

	public override void ApplySchemeSettings(IScheme scheme) {
		base.ApplySchemeSettings(scheme);

		textColor.SetSchemeValue(scheme.GetColor("Nucleus.Text"));
		var fontStyle = scheme.GetFontStyle("Nucleus.Default");
		Font.SetSchemeValue(fontStyle.Name);
		TextSize.SetSchemeValue(fontStyle.Tall);
	}


	public int MinimumValue { get; set; } = 1;
	public int MaximumValue { get; set; } = 2;
	private int _value = 1;
	public int Value {
		get => _value;
		set {
			int range = MaximumValue - MinimumValue + 1;
			int wrapped = ((value - MinimumValue) % range + range) % range + MinimumValue;
			if (wrapped == _value) return;
			int old = _value;
			_value = wrapped;
			ValueChanged?.Invoke(this, old, _value);
		}
	}

	public int VisibleSideCount { get; set; } = 2;
	public float SelectedFontSize { get; set; } = 24;
	public float UnselectedFontSize { get; set; } = 18;
	public float DividerThickness { get; set; } = 2;

	public delegate void ValueChangedDelegate(NumberPickerCarousel self, int oldValue, int newValue);
	public event ValueChangedDelegate? ValueChanged;

	private float _scrollOffset = 0f;
	private float _targetOffset = 0f;
	private float _scrollSpeed = 10f;

	public NumberPickerCarousel(Element? parent) : base(parent){
		SetSize(new Vector2F(320, 48));
		Clipping = true;
	}

	protected override bool MouseScroll(Element self, FrameState state, Vector2F delta) {
		if (delta.Y > 0)
			Value++;
		else if (delta.Y < 0)
			Value--;
		return true;
	}

	private float _totalDragDistance = 0f;
	private bool _isDragging = false;
	public float DragThreshold { get; set; } = 5f;

	protected override bool MouseClick(FrameState state, Nucleus.Common.Input.ButtonCode button) {
		if (button != Nucleus.Common.Input.ButtonCode.MouseLeft) return false;
		_totalDragDistance = 0f;
		_isDragging = false;
		return true;
	}

	protected override bool MouseDrag(Element self, FrameState state, Vector2F delta) {
		_totalDragDistance += MathF.Abs(delta.X);

		if (!_isDragging && _totalDragDistance > DragThreshold)
			_isDragging = true;

		if (_isDragging) {
			_scrollOffset += delta.X;

			float cellWidth = GetRenderBounds().Width / (VisibleSideCount * 2 + 1);
			while (_scrollOffset > cellWidth / 2f) {
				_scrollOffset -= cellWidth;
				Value--;
			}
			while (_scrollOffset < -cellWidth / 2f) {
				_scrollOffset += cellWidth;
				Value++;
			}
		}
		return true;
	}
	protected override bool MouseRelease(Element self, FrameState state, ButtonCode button) {
		if (button != Nucleus.Common.Input.ButtonCode.MouseLeft) return false;
		if (_isDragging) {
			_isDragging = false;
			float cellWidth = GetRenderBounds().Width / (VisibleSideCount * 2 + 1);
			int snap = (int)MathF.Round(_scrollOffset / cellWidth);
			Value -= snap;
			_scrollOffset = 0;
			return true;
		}

		float cw = GetRenderBounds().Width / (VisibleSideCount * 2 + 1);
		var mousePos = GetMousePos();
		int cellIndex = (int)(mousePos.X / cw);
		int offset = cellIndex - VisibleSideCount;
		if (offset != 0) {
			_scrollOffset = 0;
			Value += offset;
		}
		return true;
	}

	protected override void OnThink() {
		base.OnThink();
	}

	private int WrapValue(int v) {
		int range = MaximumValue - MinimumValue + 1;
		return ((v - MinimumValue) % range + range) % range + MinimumValue;
	}


	public ReadOnlySpan<char> GetFont() => Font.Get();
	public void SetFont(ReadOnlySpan<char> font) => Font.SetUserValue(new(font));

	public float GetTextSize() => TextSize.Get();
	public void SetTextSize(float textSize) => TextSize.SetUserValue(textSize);

	char[] text = new char[32];

	public override void Paint(float width, float height) {
		int totalVisible = VisibleSideCount * 2 + 1;
		float cellWidth = width / totalVisible;

		Graphics2D.SetDrawColor(GetBgColor());
		Graphics2D.DrawRectangle(0, 0, width, height);

		float selectedX = VisibleSideCount * cellWidth + _scrollOffset;
		Graphics2D.SetDrawColor(GetBgColor().Adjust(0, 0, .4f));
		Graphics2D.DrawRectangle(selectedX, 0, cellWidth, height);

		Span<char> text = this.text;
		for (int i = 0; i < totalVisible; i++) {
			int offset = i - VisibleSideCount;
			int numberValue = WrapValue(Value + offset);

			float cellX = i * cellWidth + _scrollOffset;

			if (cellX + cellWidth < 0 || cellX > width)
				continue;

			bool isSelected = offset == 0;
			float fontSize = isSelected ? SelectedFontSize : UnselectedFontSize;
			Color textCol = isSelected ? GetTextColor() : GetFgColor().Adjust(0, 0, .3f);

			numberValue.TryFormat(text, out int written);
			var textSize = Graphics2D.GetTextSize(text[..written], GetFont(), fontSize);
			float tx = cellX + (cellWidth - textSize.X) / 2f;
			float ty = (height - textSize.Y) / 2f;

			Graphics2D.SetDrawColor(textCol);
			Graphics2D.DrawText(new Vector2F(tx, ty), text[..written], GetFont(), fontSize);
		}

		Graphics2D.SetDrawColor(GetFgColor());
		float divLeft = selectedX;
		float divRight = selectedX + cellWidth;
		float divPadY = height * 0.1f;

		Graphics2D.DrawRectangle(divLeft - DividerThickness / 2f, divPadY, DividerThickness, height - divPadY * 2);
		Graphics2D.DrawRectangle(divRight - DividerThickness / 2f, divPadY, DividerThickness, height - divPadY * 2);

		Graphics2D.SetDrawColor(GetFgColor());
		Graphics2D.DrawRectangleOutline(0, 0, width, height, BorderSize);
	}
}