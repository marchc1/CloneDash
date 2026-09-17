namespace CloneDash.Unbeatable.Internal;

public record struct Lane
{
	public static Lane Top(Side side) {
		return new Lane {
			height = Height.Top,
			side = side
		};
	}

	public static Lane Mid(Side side) {
		return new Lane {
			height = Height.Mid,
			side = side
		};
	}

	public static Lane Low(Side side) {
		return new Lane {
			height = Height.Low,
			side = side
		};
	}

	public Height height;
	public Side side;

	public static readonly Lane TopLeft = Lane.Top(Side.Left);
	public static readonly Lane MidLeft = Lane.Mid(Side.Left);
	public static readonly Lane LowLeft = Lane.Low(Side.Left);
	public static readonly Lane TopRight = Lane.Top(Side.Right);
	public static readonly Lane MidRight = Lane.Mid(Side.Right);
	public static readonly Lane LowRight = Lane.Low(Side.Right);
}
