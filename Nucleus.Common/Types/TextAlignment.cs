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
	public TextAlignment Vertical;
	public TextAlignment Horizontal;

	public TextAlignment2D(TextAlignment vertical, TextAlignment horizontal){
		Vertical = vertical;
		Horizontal = horizontal;
	}
	public readonly Anchor ToAnchor() => (Anchor)(1 + ((int)Vertical * 3) + (int)Horizontal);
}
