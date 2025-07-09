using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Poly2Tri
{
    /// <summary>
    /// Define a RectangleF. This structure is slightly different from System.Drawing.RectangleF as it is
    /// internally storing Left,Top,Right,Bottom instead of Left,Top,Width,Height.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Rectangle : IEquatable<Rectangle>
    {
        public double MinX;
        public double MinY;
        public double MaxX;
        public double MaxY;

        /// <summary>
        /// An empty rectangle.
        /// </summary>
        public static readonly Rectangle Empty = new Rectangle();

        /// <summary>
        /// Initializes a new instance of the <see cref="RectangleF"/> struct.
        /// </summary>
        /// <param name="x">The left.</param>
        /// <param name="y">The top.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        public Rectangle(double x, double y, double width, double height)
        {
            MinX = x;
            MinY = y;
            MaxX = x + width;
            MaxY = y + height;
        }

        /// <summary>
        /// Expands the rectangle as needed so that the given point falls within it's bounds.
        /// </summary>
        /// <param name="p"></param>
        public void Encapsulate(Vector2 p)
        {
            if (p.X < MinX)
                MinX = Math.Floor(p.X);
            if (p.X > MaxX)
                MaxX = Math.Ceiling(p.X);

            if (p.Y < MinY)
                MinY = Math.Floor(p.Y);
            if (p.Y > MaxY)
                MaxY = Math.Ceiling(p.Y);
        }

        /// <summary>
        /// Expands the rectangle as needed so that the given point falls within it's bounds.
        /// </summary>
        /// <param name="p"></param>
        public void Encapsulate(Rectangle p)
        {
            if (p.MinX < MinX)
                MinX = p.MinX;
            if (p.MaxX > MaxX)
                MaxX = p.MaxX;

            if (p.MinY < MinY)
                MinY = p.MinY;
            if (p.MaxY > MaxY)
                MaxY = p.MaxY;
        }

        /// <summary>
        /// Gets or sets the X position.
        /// </summary>
        /// <value>The X position.</value>
        public double X
        {
            get
            {
                return MinX;
            }
            set
            {
                MaxX = value + Width;
                MinX = value;
            }
        }

        /// <summary>
        /// Gets or sets the Y position.
        /// </summary>
        /// <value>The Y position.</value>
        public double Y
        {
            get
            {
                return MinY;
            }
            set
            {
                MaxY = value + Height;
                MinY = value;
            }
        }

        /// <summary>
        /// Gets or sets the width.
        /// </summary>
        /// <value>The width.</value>
        public double Width
        {
            get { return MaxX - MinX; }
            set { MaxX = MinX + value; }
        }

        /// <summary>
        /// Gets or sets the height.
        /// </summary>
        /// <value>The height.</value>
        public double Height
        {
            get { return MaxY - MinY; }
            set { MaxY = MinY + value; }
        }

        /// <summary>
        /// Gets the Point that specifies the center of the rectangle.
        /// </summary>
        /// <value>
        /// The center.
        /// </value>
        public Vector2 Center
        {
            get
            {
                return new Vector2(X + (Width / 2), Y + (Height / 2));
            }
        }

        /// <summary>
        /// Gets a value that indicates whether the rectangle is empty.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [is empty]; otherwise, <c>false</c>.
        /// </value>
        public bool IsEmpty
        {
            get
            {
                return (Width == 0.0f) && (Height == 0.0f) && (X == 0.0f) && (Y == 0.0f);
            }
        }

        /// <summary>
        /// Gets or sets the size of the rectangle.
        /// </summary>
        /// <value>The size of the rectangle.</value>
        public Vector2 Size
        {
            get
            {
                return new Vector2(Width, Height);
            }
            set
            {
                Width = value.X;
                Height = value.Y;
            }
        }

        /// <summary>
        /// Gets the position of the top-left corner of the rectangle.
        /// </summary>
        /// <value>The top-left corner of the rectangle.</value>
        public Vector2 TopLeft { get { return new Vector2(MinX, MinY); } }

        /// <summary>
        /// Gets the position of the top-right corner of the rectangle.
        /// </summary>
        /// <value>The top-right corner of the rectangle.</value>
        public Vector2 TopRight { get { return new Vector2(MaxX, MinY); } }

        /// <summary>
        /// Gets the position of the bottom-left corner of the rectangle.
        /// </summary>
        /// <value>The bottom-left corner of the rectangle.</value>
        public Vector2 BottomLeft { get { return new Vector2(MinX, MaxY); } }

        /// <summary>
        /// Gets the position of the bottom-right corner of the rectangle.
        /// </summary>
        /// <value>The bottom-right corner of the rectangle.</value>
        public Vector2 BottomRight { get { return new Vector2(MaxX, MaxY); } }

        /// <summary>Determines whether this rectangle contains a specified Point.</summary>
        /// <param name="value">The Point to evaluate.</param>
        /// <param name="result">[OutAttribute] true if the specified Point is contained within this rectangle; false otherwise.</param>
        public void Contains(ref Vector2 value, out bool result)
        {
            result = (X <= value.X) && (value.X < MaxX) && (Y <= value.Y) && (value.Y < MaxY);
        }

        /// <summary>Determines whether this rectangle entirely contains a specified rectangle.</summary>
        /// <param name="value">The rectangle to evaluate.</param>
        public bool Contains(Rectangle value)
        {
            return (X <= value.X) && (value.MaxX <= MaxX) && (Y <= value.Y) && (value.MaxY <= MaxY);
        }

        /// <summary>Determines whether this rectangle entirely contains a specified rectangle.</summary>
        /// <param name="value">The rectangle to evaluate.</param>
        /// <param name="result">[OutAttribute] On exit, is true if this rectangle entirely contains the specified rectangle, or false if not.</param>
        public void Contains(ref Rectangle value, out bool result)
        {
            result = (X <= value.X) && (value.MaxX <= MaxX) && (Y <= value.Y) && (value.MaxY <= MaxY);
        }

        /// <summary>
        /// Checks, if specified point is inside <see cref="RectangleF"/>.
        /// </summary>
        /// <param name="x">X point coordinate.</param>
        /// <param name="y">Y point coordinate.</param>
        /// <returns><c>true</c> if point is inside <see cref="RectangleF"/>, otherwise <c>false</c>.</returns>
        public bool Contains(double x, double y)
        {
            return (x >= MinX && x <= MaxX && y >= MinY && y <= MaxY);
        }

        /// <summary>
        /// Checks, if specified <see cref="Vector2F"/> is inside <see cref="RectangleF"/>.
        /// </summary>
        /// <param name="vector2D">Coordinate <see cref="Vector2F"/>.</param>
        /// <returns><c>true</c> if <see cref="Vector2F"/> is inside <see cref="RectangleF"/>, otherwise <c>false</c>.</returns>
        public bool Contains(Vector2 vector2D)
        {
            return Contains(vector2D.X, vector2D.Y);
        }

        /// <summary>Determines whether a specified rectangle intersects with this rectangle.</summary>
        /// <param name="value">The rectangle to evaluate.</param>
        public bool Intersects(Rectangle value)
        {
            bool result;
            Intersects(ref value, out result);
            return result;
        }

        /// <summary>
        /// Determines whether a specified rectangle intersects with this rectangle.
        /// </summary>
        /// <param name="value">The rectangle to evaluate</param>
        /// <param name="result">[OutAttribute] true if the specified rectangle intersects with this one; false otherwise.</param>
        public void Intersects(ref Rectangle value, out bool result)
        {
            result = (value.X < MaxX) && (X < value.MaxX) && (value.Y < MaxY) && (Y < value.MaxY);
        }

        /// <summary>
        /// Creates a rectangle defining the area where one rectangle overlaps with another rectangle.
        /// </summary>
        /// <param name="value1">The first Rectangle to compare.</param>
        /// <param name="value2">The second Rectangle to compare.</param>
        /// <returns>The intersection rectangle.</returns>
        public static Rectangle Intersect(Rectangle value1, Rectangle value2)
        {
            Rectangle result;
            Intersect(ref value1, ref value2, out result);
            return result;
        }

        /// <summary>Creates a rectangle defining the area where one rectangle overlaps with another rectangle.</summary>
        /// <param name="value1">The first rectangle to compare.</param>
        /// <param name="value2">The second rectangle to compare.</param>
        /// <param name="result">[OutAttribute] The area where the two first parameters overlap.</param>
        public static void Intersect(ref Rectangle value1, ref Rectangle value2, out Rectangle result)
        {
            double newLeft = (value1.X > value2.X) ? value1.X : value2.X;
            double newTop = (value1.Y > value2.Y) ? value1.Y : value2.Y;
            double newRight = (value1.MaxX < value2.MaxX) ? value1.MaxX : value2.MaxX;
            double newBottom = (value1.MaxY < value2.MaxY) ? value1.MaxY : value2.MaxY;

            if ((newRight > newLeft) && (newBottom > newTop))
                result = new Rectangle(newLeft, newTop, newRight - newLeft, newBottom - newTop);
            else
                result = Empty;
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
        /// <returns>
        /// 	<c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (!(obj is Rectangle))
                return false;

            Rectangle other = (Rectangle)obj;
            return Equals(ref other);
        }

        /// <summary>
        /// Determines whether the specified <see cref="RectangleF"/> is equal to this instance.
        /// </summary>
        /// <param name="other">The <see cref="RectangleF"/> to compare with this instance.</param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="RectangleF"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(ref Rectangle other)
        {
            return MinX == other.MinX && MinY == other.MinY && MaxX == other.MaxX && MaxY == other.MaxY;
        }

        /// <summary>
        /// Determines whether the specified <see cref="RectangleF"/> is equal to this instance.
        /// </summary>
        /// <param name="other">The <see cref="RectangleF"/> to compare with this instance.</param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="RectangleF"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Rectangle other)
        {
            return Equals(ref other);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.
        /// </returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int result = MinX.GetHashCode();
                result = (result * 397) ^ MinY.GetHashCode();
                result = (result * 397) ^ MaxX.GetHashCode();
                result = (result * 397) ^ MaxY.GetHashCode();
                return result;
            }
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "X:{0} Y:{1} Width:{2} Height:{3}", X, Y, Width, Height);
        }

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>The result of the operator.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Rectangle left, Rectangle right)
        {
            return left.Equals(ref right);
        }

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>The result of the operator.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Rectangle left, Rectangle right)
        {
            return !left.Equals(ref right);
        }
    }
}
