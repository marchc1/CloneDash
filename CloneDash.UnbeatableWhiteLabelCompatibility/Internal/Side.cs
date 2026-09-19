using System.Numerics;

namespace CloneDash.Unbeatable.Internal;

public enum Side
{
	None,
	Left,
	Right
}

public static class SideExtension
{
	public static float GetFlipScalar(this Side side) {
		if (side == Side.Left) {
			return 1f;
		}
		if (side != Side.Right) {
			return 0f;
		}
		return -1f;
	}

	public static Vector3 GetFlipVector(this Side side) {
		return new Vector3(1f, 0f, 0f) * -side.GetFlipScalar();
	}

	public static Vector3 GetFlipVector(this Side side, float magnitude) {
		return new Vector3(1f, 0f, 0f) * -side.GetFlipScalar() * magnitude;
	}

	public static Side GetOpposite(this Side side) {
		if (side == Side.Left) {
			return Side.Right;
		}
		if (side != Side.Right) {
			return Side.None;
		}
		return Side.Left;
	}

	public static T Pick<T>(this Side side, T lhs, T rhs) {
		if (side == Side.Left) {
			return lhs;
		}
		if (side != Side.Right) {
			return default(T);
		}
		return rhs;
	}
}