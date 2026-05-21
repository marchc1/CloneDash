namespace Nucleus.Types;

public enum Axis : byte
{
	None = 0,
	Horizontal = 1 << 0,
	Vertical = 1 << 1,
	Both = Horizontal | Vertical
}
