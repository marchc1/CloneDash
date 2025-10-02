namespace Nucleus.Types
{
	public struct RectangleF
	{
		public static readonly RectangleF Zero = new(0, 0, 0, 0);
		public float top;
		public float left;
		public float right;
		public float bottom;

		/// <summary>
		/// Convenience property, internally equivalent to <see cref="top"/>
		/// </summary>
		public float Top { get { return top; } set { top = value; } }
		/// <summary>
		/// Convenience property, internally equivalent to <see cref="left"/>
		/// </summary>
		public float Left { get { return left; } set { left = value; } }
		/// <summary>
		/// Convenience property, internally equivalent to <see cref="right"/>
		/// </summary>
		public float Right { get { return right; } set { right = value; } }
		/// <summary>
		/// Convenience property, internally equivalent to <see cref="bottom"/>
		/// </summary>
		public float Bottom { get { return bottom; } set { bottom = value; } }

		/// <summary>
		/// Convenience property, internally equivalent to <see cref="left"/>
		/// </summary>
		public float X { get { return left; } set { left = value; } }
		/// <summary>
		/// Convenience property, internally equivalent to <see cref="top"/>
		/// </summary>
		public float Y { get { return top; } set { top = value; } }
		/// <summary>
		/// Convenience property, internally equivalent to <see cref="right"/>
		/// </summary>
		public float W { get { return right; } set { right = value; } }
		/// <summary>
		/// Convenience property, internally equivalent to <see cref="bottom"/>
		/// </summary>
		public float H { get { return bottom; } set { bottom = value; } }

#pragma warning disable IDE1006 // Naming Styles
		/// <summary>
		/// Convenience property, internally equivalent to <see cref="left"/>
		/// </summary>
		public float x { get { return left; } set { left = value; } }
		/// <summary>
		/// Convenience property, internally equivalent to <see cref="top"/>
		/// </summary>
		public float y { get { return top; } set { top = value; } }
		/// <summary>
		/// Convenience property, internally equivalent to <see cref="right"/>
		/// </summary>
		public float w { get { return right; } set { right = value; } }
		/// <summary>
		/// Convenience property, internally equivalent to <see cref="bottom"/>
		/// </summary>
		public float h { get { return bottom; } set { bottom = value; } }
#pragma warning restore IDE1006 // Naming Styles

		public override string ToString() {
			return $"RectangleF [X: {X}, Y: {Y}, W: {W}, H: {H}]";
		}

		public static RectangleF From2Points(Vector2F p1, Vector2F p2) {
			Vector2F tl = new(Math.Min(p1.X, p2.X), Math.Min(p1.Y, p2.Y));
			Vector2F br = new(Math.Max(p1.X, p2.X), Math.Max(p1.Y, p2.Y));
			Vector2F size = br - tl;
			return FromPosAndSize(tl, size);
		}

		/// <summary>
		/// Convenience property, internally equivalent to <see cref="right"/>
		/// </summary>
		public float Width {
			get { return right; }
			set { right = value; }
		}

		/// <summary>
		/// Convenience property, internally equivalent to <see cref="bottom"/>
		/// </summary>
		public float Height {
			get { return bottom; }
			set { bottom = value; }
		}

		/// <summary>
		/// Checks if all four sides are zero
		/// </summary>
		public bool IsZero => (Math.Abs(top) + Math.Abs(left) + Math.Abs(right) + Math.Abs(bottom)) == 0;

		/// <summary>
		/// Returns <see cref="left"/> and <see cref="top"/> as <c><see cref="Vector2F"/>(<see cref="left"/>, <see cref="top"/>)</c>
		/// </summary>
		public Vector2F Pos {
			get {
				return new(left, top);
			}
			set {
				this.left = value.X;
				this.top = value.Y;
			}
		}
		/// <summary>
		/// Returns <see cref="right"/> and <see cref="bottom"/> as <c><see cref="Vector2F"/>(<see cref="right"/>, <see cref="bottom"/>)</c>
		/// </summary>
		public Vector2F Size {
			get {
				return new(right, bottom);
			}
			set {
				this.right = value.X;
				this.bottom = value.Y;
			}
		}

		public RectangleF(float top, float left, float right, float bottom) { this.top = top; this.bottom = bottom; this.left = left; this.right = right; }

		/// <summary>
		/// Creates a rectangle from X, Y, W, and H
		/// </summary>
		/// <param name="X"></param>
		/// <param name="Y"></param>
		/// <param name="Width"></param>
		/// <param name="Height"></param>
		/// <returns>RectangleF with X, Y, Width, Height set</returns>
		public static RectangleF XYWH(float X, float Y, float Width, float Height) => new RectangleF() { top = Y, left = X, right = Width, bottom = Height };

		/// <summary>
		/// Creates a rectangle from top, left, right, and bottom
		/// </summary>
		/// <param name="Top"></param>
		/// <param name="Left"></param>
		/// <param name="Right"></param>
		/// <param name="Bottom"></param>
		/// <returns>RectangleF with top, left, right, bottom set</returns>
		public static RectangleF TLRB(float Top, float Left, float Right, float Bottom) => new RectangleF() { top = Top, left = Left, right = Right, bottom = Bottom };

		/// <summary>
		/// Creates a rectangle that takes all four sides from <paramref name="All"/>.
		/// <br></br>
		/// <br></br>
		/// ex. <c>new <see cref="RectangleF"/>(<paramref name="All"/>, <paramref name="All"/>, <paramref name="All"/>, <paramref name="All"/>)</c>
		/// </summary>
		/// <param name="All"></param>
		/// <returns></returns>
		public static RectangleF TLRB(float All) => new RectangleF() { top = All, left = All, right = All, bottom = All };

		/// <summary>
		/// Converts a <see cref="Vector2F"/> position and a <see cref="Vector2F"/> size into a <see cref="RectangleF"/>.
		/// <br></br>
		/// <br></br>
		/// ex. <c>new <see cref="RectangleF"/>(<paramref name="pos"/>.Y, <paramref name="pos"/>.X, <paramref name="size"/>.W, <paramref name="size"/>.H)</c>
		/// </summary>
		/// <param name="pos"></param>
		/// <param name="size"></param>
		/// <returns></returns>
		public static RectangleF FromPosAndSize(Vector2F pos, Vector2F size) => new RectangleF() { top = pos.Y, left = pos.X, right = size.W, bottom = size.H };

		/// <summary>
		/// Checks if a <see cref="Vector2F"/> <paramref name="point"/> is within the bounds of x, y, w, h
		/// </summary>
		public static bool ContainsPoint(float x, float y, float w, float h, Vector2F point) => point.X >= x && point.X <= x + w && point.Y >= y && point.Y <= y + h;
		/// <summary>
		/// Checks if a <see cref="Vector2F"/> <paramref name="point"/> is within the <see cref="RectangleF"/>
		/// </summary>
		public bool ContainsPoint(Vector2F point) => ContainsPoint(X, Y, Width, Height, point);
		/// <summary>
		/// Checks if a <see cref="Vector2F"/> <paramref name="point"/> is within a rectangle from <paramref name="pos"/> and <paramref name="size"/>
		/// </summary>
		public static bool ContainsPoint(Vector2F pos, Vector2F size, Vector2F point) => point.X >= pos.X && point.X <= pos.X + size.W && point.Y >= pos.Y && point.Y <= pos.Y + size.H;

		/// <summary>
		/// Tests if <paramref name="subrect"/> is within <paramref name="rect"/> whatsoever.
		/// </summary>
		/// <param name="rect"></param>
		/// <param name="subrect"></param>
		/// <returns></returns>
		public static bool IsSubrectangleWithinRectangle(RectangleF rect, RectangleF subrect) {
			// four points of subrect
			var TL = new Vector2F(subrect.X, subrect.Y);
			var TR = new Vector2F(subrect.X + subrect.W, subrect.Y);
			var BL = new Vector2F(subrect.X, subrect.Y + subrect.H);
			var BR = new Vector2F(subrect.X + subrect.W, subrect.Y + subrect.H);

			return
					rect.ContainsPoint(TL) ||
					rect.ContainsPoint(TR) ||
					rect.ContainsPoint(BL) ||
					rect.ContainsPoint(BR);
		}


		public static RectangleF operator +(RectangleF from, float by) => new RectangleF(from.top + by, from.left + by, from.right + by, from.bottom + by);
		public static RectangleF operator -(RectangleF from, float by) => new RectangleF(from.top - by, from.left - by, from.right - by, from.bottom - by);
		public static RectangleF operator *(RectangleF from, float by) => new RectangleF(from.top * by, from.left * by, from.right * by, from.bottom * by);
		public static RectangleF operator /(RectangleF from, float by) => new RectangleF(
			(float)((double)from.top / (double)by),
			(float)((double)from.left / (double)by),
			(float)((double)from.right / (double)by),
			(float)((double)from.bottom / (double)by)
		);

		public static RectangleF operator +(RectangleF from, RectangleF by) => new RectangleF(from.top + by.top, from.left + by.left, from.right + by.right, from.bottom + by.bottom);
		public static RectangleF operator -(RectangleF from, RectangleF by) => new RectangleF(from.top - by.top, from.left - by.left, from.right - by.right, from.bottom - by.bottom);
		public static RectangleF operator *(RectangleF from, RectangleF by) => new RectangleF(from.top * by.top, from.left * by.left, from.right * by.right, from.bottom * by.bottom);
		public static RectangleF operator /(RectangleF from, RectangleF by) => new RectangleF(
			(float)((double)from.top / (double)by.top),
			(float)((double)from.left / (double)by.left),
			(float)((double)from.right / (double)by.right),
			(float)((double)from.bottom / (double)by.bottom)
		);

		private static bool equality(RectangleF a, RectangleF b) {
			return a.X == b.X && a.Y == b.Y && a.W == b.W && a.H == b.H;
		}

		public static RectangleF Round(RectangleF rect) => new() {
			X = MathF.Round(rect.X),
			Y = MathF.Round(rect.Y),
			W = MathF.Round(rect.W),
			H = MathF.Round(rect.H)
		};

		public void RoundInPlace() {
			X = MathF.Round(X);
			Y = MathF.Round(Y);
			W = MathF.Round(W);
			H = MathF.Round(H);
		}

		public static bool operator ==(RectangleF a, RectangleF b) => equality(a, b);
		public static bool operator !=(RectangleF a, RectangleF b) => !equality(a, b);

		public System.Drawing.Rectangle ToDrawing() => new((int)X, (int)Y, (int)W, (int)H);
		public System.Drawing.RectangleF ToDrawingF() => new(X, Y, W, H);

		public RectangleF AddPosition(Vector2F v) {
			return new RectangleF() {
				x = this.X + v.X,
				y = this.Y + v.Y,
				w = this.W,
				h = this.H
			};
		}

		public RectangleF AddSize(Vector2F v) {
			return new RectangleF() {
				x = this.X,
				y = this.Y,
				w = this.W + v.X,
				h = this.H + v.Y
			};
		}

		public RectangleF FitInto(RectangleF cropRect) {
			RectangleF newR = this;

			if (newR.X < cropRect.X) {
				newR.W -= (cropRect.X - newR.X);
				newR.X = cropRect.X;
			}
			if ((newR.X + newR.W) > (cropRect.X + cropRect.W)) {
				newR.W -= (newR.X + newR.W) - (cropRect.X + cropRect.W);
			}

			if (newR.Y < cropRect.Y) {
				newR.H -= (cropRect.Y - newR.Y);
				newR.Y = cropRect.Y;
			}
			if ((newR.Y + newR.H) > (cropRect.Y + cropRect.H)) {
				newR.H -= (newR.Y + newR.H) - (cropRect.Y + cropRect.H);
			}

			return newR;
		}

		public RectangleF Inflate(float v) {
			return new() {
				X = X - v,
				Y = Y - v,
				W = W + v + v,
				H = H + v + v
			};
		}
		public RectangleF Deflate(float v) {
			return new() {
				X = X + v,
				Y = Y + v,
				W = W - v - v,
				H = H - v - v
			};
		}


		public Vector2F GetOverflow(RectangleF rect, float padding = 0) {
			Vector2F overflow = new();
			var thisP = this.Deflate(padding);

			if (rect.X < thisP.X) overflow.X = thisP.X - rect.X;
			else if ((rect.X + rect.W) > (thisP.X + thisP.W)) overflow.X = (thisP.X + thisP.W) - (rect.X + rect.W);
			if (rect.Y < thisP.Y) overflow.Y = thisP.Y - rect.Y;
			else if ((rect.Y + rect.H) > (thisP.Y + thisP.H)) overflow.Y = (thisP.Y + thisP.H) - (rect.Y + rect.H);

			return overflow;
		}
	}
}
