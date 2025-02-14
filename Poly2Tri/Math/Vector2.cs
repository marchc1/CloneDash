namespace Poly2Tri
{
    public struct Vector2
    {
        public static readonly Vector2 Zero = new Vector2();
        public double X;

        public double Y;

        public Vector2(double x, double y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// Multiplies a vector with another by performing component-wise multiplication."/>.
        /// </summary>
        /// <param name="left">The first vector to multiply.</param>
        /// <param name="right">The second vector to multiply.</param>
        /// <returns>The multiplication of the two vectors.</returns>
        public static Vector2 operator *(Vector2 left, Vector2 right)
        {
            return new Vector2(left.X * right.X, left.Y * right.Y);
        }

        /// <summary>
        /// Scales a vector by the given value.
        /// </summary>
        /// <param name="value">The vector to scale.</param>
        /// <param name="scale">The amount by which to scale the vector.</param>
        /// <returns>The scaled vector.</returns>
        public static Vector2 operator *(Vector2 value, double scale)
        {
            return new Vector2(value.X * scale, value.Y * scale);
        }

        /// <summary>
        /// Scales a vector by the given value.
        /// </summary>
        /// <param name="value">The vector to scale.</param>
        /// <param name="scale">The amount by which to scale the vector.</param>
        /// <returns>The scaled vector.</returns>
        public static Vector2 operator /(Vector2 value, double scale)
        {
            return new Vector2(value.X / scale, value.Y / scale);
        }

        /// <summary>
        /// Scales a vector by the given value.
        /// </summary>
        /// <param name="scale">The amount by which to scale the vector.</param>
        /// <param name="value">The vector to scale.</param>  
        /// <returns>The scaled vector.</returns>
        public static Vector2 operator /(double scale, Vector2 value)
        {
            return new Vector2(scale / value.X, scale / value.Y);
        }

        /// <summary>
        /// Perform a component-wise addition
        /// </summary>
        /// <param name="value">The input vector.</param>
        /// <param name="scalar">The scalar value to be added on elements</param>
        /// <returns>The vector with added scalar for each element.</returns>
        public static Vector2 operator +(Vector2 value, double scalar)
        {
            return new Vector2(value.X + scalar, value.Y + scalar);
        }

        /// <summary>
        /// Perform a component-wise addition
        /// </summary>
        /// <param name="value">The input vector.</param>
        /// <param name="scalar">The scalar value to be added on elements</param>
        /// <returns>The vector with added scalar for each element.</returns>
        public static Vector2 operator +(Vector2 value, Vector2 scalar)
        {
            return new Vector2(scalar.X + value.X, scalar.Y + value.Y);
        }
    }
}
