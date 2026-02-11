namespace Nucleus.Types;

public enum TextAlignment : byte
{
	Left = 0,
	Top = 0,
	Middle = 1,
	Center = 1,
	Right = 2,
	Bottom = 2
}

public struct TextAlignment2D {
	public TextAlignment Horizontal;
	public TextAlignment Vertical;

	public TextAlignment2D(TextAlignment horizontal, TextAlignment vertical){
		Horizontal = horizontal;
		Vertical = vertical;
	}
	public readonly Anchor ToAnchor() => (Anchor)(1 + ((int)Vertical * 3) + (int)Horizontal);
}
