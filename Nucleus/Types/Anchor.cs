namespace Nucleus.Types
{
    public record Anchor(int Anchoring)
    {
        public static Anchor TopLeft = new(1);
        public static Anchor TopMiddle = new(2);
        public static Anchor TopCenter = new(2);
        public static Anchor TopRight = new(3);

        public static Anchor MiddleLeft = new(4);
        public static Anchor CenterLeft = new(4);
        public static Anchor Middle = new(5);
        public static Anchor Center = new(5);
        public static Anchor MiddleRight = new(6);
        public static Anchor CenterRight = new(6);

        public static Anchor BottomLeft = new(7);
        public static Anchor BottomMiddle = new(8);
        public static Anchor BottomCenter = new(8);
        public static Anchor BottomRight = new(9);

		public int Deconstruct() => Anchoring;

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
        public static Vector2F CalculatePosition(Vector2F position, Vector2F size, Anchor anchoring, bool subtract = false) {
            var mul = subtract ? -1 : 1;
            switch (anchoring.Anchoring) {
                case 1: return position;
                case 2: return new(position.X + ((size.X / 2) * mul), position.Y);
                case 3: return new(position.X + (size.X * mul), position.Y);

                case 4: return new(position.X, position.Y + ((size.Y / 2) * mul));
                case 5: return new(position.X + ((size.X / 2) * mul), position.Y + ((size.Y / 2) * mul));
                case 6: return new(position.X + (size.X * mul), position.Y + ((size.Y / 2) * mul));

                case 7: return new(position.X, position.Y + (size.Y * mul));
                case 8: return new(position.X + ((size.X / 2) * mul), position.Y + (size.Y * mul));
                case 9: return new(position.X + (size.X * mul), position.Y + (size.Y * mul));
            }

            throw new NotImplementedException();
        }
        public (TextAlignment horizontal, TextAlignment vertical) ToTextAlignment() {
            switch (Anchoring) {
                case 1: return (TextAlignment.Left, TextAlignment.Top);
                case 2: return (TextAlignment.Center, TextAlignment.Top);
                case 3: return (TextAlignment.Right, TextAlignment.Top);

                case 4: return (TextAlignment.Left, TextAlignment.Center);
                case 5: return (TextAlignment.Center, TextAlignment.Center);
                case 6: return (TextAlignment.Right, TextAlignment.Center);

                case 7: return (TextAlignment.Left, TextAlignment.Bottom);
                case 8: return (TextAlignment.Center, TextAlignment.Bottom);
                case 9: return (TextAlignment.Right, TextAlignment.Bottom);
            }

            throw new NotImplementedException();
        }

        public static Vector2F CornerEdgeOffset(Anchor anchor, Vector2F textPadding) {
            var offset = Vector2F.Zero;
            switch (anchor.Horizontal) {
                case 0: offset.X = textPadding.X; break;
                case 1: offset.X = -textPadding.X; break;
            }
            switch (anchor.Vertical) {
                case 0: offset.Y = textPadding.Y; break;
                case 1: offset.Y = -textPadding.Y; break;
            }
            return offset;
        }

        public static Vector2F GetPositionGivenAlignment(Anchor alignment, RectangleF bounds, Vector2F padding) {
            Vector2F drawPos = CalculatePosition(bounds.Pos, bounds.Size, alignment);
            var offset = CornerEdgeOffset(alignment, padding);

            return drawPos + offset;
        }
        public static Vector2F GetPositionGivenAlignment(Anchor alignment, Vector2F bounds, Vector2F padding) => GetPositionGivenAlignment(alignment, RectangleF.FromPosAndSize(new(0), bounds), padding);

        public float Horizontal {
            get {
                switch (Anchoring) {
                    case 1: return 0f;
                    case 2: return 0.5f;
                    case 3: return 1f;

                    case 4: return 0f;
                    case 5: return 0.5f;
                    case 6: return 1f;

                    case 7: return 0f;
                    case 8: return 0.5f;
                    case 9: return 1f;
                }
                throw new NotImplementedException();
            }
        }
        public float Vertical {
            get {
                switch (Anchoring) {
                    case 1: return 0f;
                    case 2: return 0f;
                    case 3: return 0f;

                    case 4: return 0.5f;
                    case 5: return 0.5f;
                    case 6: return 0.5f;

                    case 7: return 1f;
                    case 8: return 15f;
                    case 9: return 1f;
                }
                throw new NotImplementedException();
            }
        }
    }
}
