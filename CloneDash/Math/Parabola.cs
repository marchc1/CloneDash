namespace CloneDash
{
    public abstract class Parabola {
        /// <summary>
        /// Creates a parabolic curve from three two-dimensional points
        /// </summary>
        public static ParabolaFrom3Points From3Points(Vector2F start, Vector2F middle, Vector2F end) => new ParabolaFrom3Points(start, middle, end);
    }
    public class ParabolaFrom3Points : Parabola
    {
        private Vector2F S, M, E;
        private double A, B, C;

        public Vector2F Start {
            get { return S; }
            set { S = value; Build(); }
        }
        public Vector2F Middle {
            get { return M; }
            set { M = value; Build(); }
        }
        public Vector2F End {
            get { return E; }
            set { E = value; Build(); }
        }

        public ParabolaFrom3Points(Vector2F start, Vector2F middle, Vector2F end) {
            S = start;
            M = middle;
            E = end;

            Build();
        }

        private void Build() {
            double X1 = (double)Start.X, Y1 = (double)Start.Y;
            double X2 = (double)Middle.X, Y2 = (double)Middle.Y;
            double X3 = (double)End.X, Y3 = (double)End.Y;

            double D = (X1 - X2) * (X1 - X3) * (X2 - X3);
            A = (X3 * (Y2 - Y1) + X2 * (Y1 - Y3) + X1 * (Y3 - Y2)) / D;
            B = (X3 * X3 * (Y1 - Y2) + X2 * X2 * (Y3 - Y1) + X1 * X1 * (Y2 - Y3)) / D;
            C = (X2 * X3 * (X2 - X3) * Y1 + X3 * X1 * (X3 - X1) * Y2 + X1 * X2 * (X1 - X2) * Y3) / D;
        }

        public float CalculateY(float X) => (float)((A * (Math.Pow(X, 2))) + (B * X) + C);
    }
}
