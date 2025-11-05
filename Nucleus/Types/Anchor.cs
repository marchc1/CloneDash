namespace Nucleus.Types
{
	public enum Anchor
	{
		TopLeft = 1,
		TopMiddle = 2,
		TopCenter = 2,
		TopRight = 3,

		MiddleLeft = 4,
		CenterLeft = 4,
		Middle = 5,
		Center = 5,
		MiddleRight = 6,
		CenterRight = 6,

		BottomLeft = 7,
		BottomMiddle = 8,
		BottomCenter = 8,
		BottomRight = 9
	}
	public static class AnchorTools
	{
		/// <summary>
		/// Calculates the position coordinates given a position, size and anchor.
		/// <br></br>
		/// <br></br>
		/// Addition adds the size argument its position, subtraction subtracts from it. For example, you would use addition for cases where
		/// you want to center a position within a box. You would use subtraction for centering coordinates (ie. automatically positioning a box to place its position
		/// in the center given its size).
		/// </summary>
		/// <param name="position"></param>
		/// <param name="size"></param>
		/// <param name="anchoring"></param>
		/// <param name="subtract"></param>
		/// <returns></returns>
		/// <exception cref="NotImplementedException"></exception>
		public static Vector2F CalculatePosition(this Anchor anchoring, Vector2F position, Vector2F size,bool subtract = false) {
			var mul = subtract ? -1 : 1;
			switch (anchoring) {
				case Anchor.TopLeft: return position;
				case Anchor.TopCenter: return new(position.X + ((size.X / 2) * mul), position.Y);
				case Anchor.TopRight: return new(position.X + (size.X * mul), position.Y);

				case Anchor.CenterLeft: return new(position.X, position.Y + ((size.Y / 2) * mul));
				case Anchor.Center: return new(position.X + ((size.X / 2) * mul), position.Y + ((size.Y / 2) * mul));
				case Anchor.CenterRight: return new(position.X + (size.X * mul), position.Y + ((size.Y / 2) * mul));

				case Anchor.BottomLeft: return new(position.X, position.Y + (size.Y * mul));
				case Anchor.BottomCenter: return new(position.X + ((size.X / 2) * mul), position.Y + (size.Y * mul));
				case Anchor.BottomRight: return new(position.X + (size.X * mul), position.Y + (size.Y * mul));
			}

			throw new NotImplementedException();
		}
		public static (TextAlignment horizontal, TextAlignment vertical) ToTextAlignment(this Anchor anchor) {
			switch (anchor) {
				case Anchor.TopLeft: return (TextAlignment.Left, TextAlignment.Top);
				case Anchor.TopCenter: return (TextAlignment.Center, TextAlignment.Top);
				case Anchor.TopRight: return (TextAlignment.Right, TextAlignment.Top);
				case Anchor.CenterLeft: return (TextAlignment.Left, TextAlignment.Center);
				case Anchor.Center: return (TextAlignment.Center, TextAlignment.Center);
				case Anchor.CenterRight: return (TextAlignment.Right, TextAlignment.Center);
				case Anchor.BottomLeft: return (TextAlignment.Left, TextAlignment.Bottom);
				case Anchor.BottomCenter: return (TextAlignment.Center, TextAlignment.Bottom);
				case Anchor.BottomRight: return (TextAlignment.Right, TextAlignment.Bottom);
			}

			throw new NotImplementedException();
		}
		public static Vector2F CornerEdgeOffset(this Anchor anchor, Vector2F textPadding) {
			var offset = Vector2F.Zero;
			switch (anchor.GetHorizontalRatio()) {
				case 0f: offset.X = textPadding.X; break;
				case 1f: offset.X = -textPadding.X; break;
			}
			switch (anchor.GetVerticalRatio()) {
				case 0f: offset.Y = textPadding.Y; break;
				case 1f: offset.Y = -textPadding.Y; break;
			}
			return offset;
		}

		public static Vector2F GetPositionGivenAlignment(this Anchor alignment, RectangleF bounds, Vector2F padding) {
			Vector2F drawPos = alignment.CalculatePosition(bounds.Pos, bounds.Size);
			var offset = CornerEdgeOffset(alignment, padding);

			return drawPos + offset;
		}
		public static Vector2F GetPositionGivenAlignment(this Anchor alignment, Vector2F bounds, Vector2F padding) => GetPositionGivenAlignment(alignment, RectangleF.FromPosAndSize(new(0), bounds), padding);
		public static float GetHorizontalRatio(this Anchor anchor) {
			switch (anchor) {
				case Anchor.TopLeft: return 0f;
				case Anchor.TopCenter: return 0.5f;
				case Anchor.TopRight: return 1f;
				case Anchor.CenterLeft: return 0f;
				case Anchor.Center: return 0.5f;
				case Anchor.CenterRight: return 1f;
				case Anchor.BottomLeft: return 0f;
				case Anchor.BottomCenter: return 0.5f;
				case Anchor.BottomRight: return 1f;
			}
			throw new NotImplementedException();
		}
		public static float GetVerticalRatio(this Anchor anchor) {
			switch (anchor) {
				case Anchor.TopLeft: return 0f;
				case Anchor.TopCenter: return 0f;
				case Anchor.TopRight: return 0f;
				case Anchor.CenterLeft: return 0.5f;
				case Anchor.Center: return 0.5f;
				case Anchor.CenterRight: return 0.5f;
				case Anchor.BottomLeft: return 1f;
				case Anchor.BottomCenter: return 1f;
				case Anchor.BottomRight: return 1f;
			}
			throw new NotImplementedException();
		}

		public static Vector2F ToVector2(this Anchor anchor) {
			return new Vector2F(GetHorizontalRatio(anchor), GetVerticalRatio(anchor));
		}
	}
}
