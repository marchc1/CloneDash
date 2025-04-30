using System.Numerics;

namespace Nucleus
{
    public static partial class NMath
    {
		public static void CubicBezier(float p0x, float p0y, float p1x, float p1y, float p2x, float p2y, float p3x, float p3y, float t, out float rx, out float ry) {
			float u = 1f - t;
			float t2 = t * t;
			float u2 = u * u;
			float u3 = u2 * u;
			float t3 = t2 * t;

			rx = (u3) * p0x + (3f * u2 * t) * p1x + (3f * u * t2) * p2x + (t3) * p3x;
			ry = (u3) * p0y + (3f * u2 * t) * p1y + (3f * u * t2) * p2y + (t3) * p3y;
		}

		public static void CubicBezier(double p0x, double p0y, double p1x, double p1y, double p2x, double p2y, double p3x, double p3y, double t, out double rx, out double ry) {
			double u = 1d - t;
			double t2 = t * t;
			double u2 = u * u;
			double u3 = u2 * u;
			double t3 = t2 * t;

			rx = (u3) * p0x + (3d * u2 * t) * p1x + (3d * u * t2) * p2x + (t3) * p3x;
			ry = (u3) * p0y + (3d * u2 * t) * p1y + (3d * u * t2) * p2y + (t3) * p3y;
		}


		public static class Ease
        {
            public static float Linear(float t) => t;

            public static float InQuad(float t) => t * t;
            public static float OutQuad(float t) => 1 - InQuad(1 - t);
            public static float InOutQuad(float t) {
                if (t < 0.5) return InQuad(t * 2) / 2;
                return 1 - InQuad((1 - t) * 2) / 2;
            }

            public static float InCubic(float t) => t * t * t;
            public static float OutCubic(float t) => 1 - InCubic(1 - t);
            public static float InOutCubic(float t) {
                if (t < 0.5) return InCubic(t * 2) / 2;
                return 1 - InCubic((1 - t) * 2) / 2;
            }

            public static float InQuart(float t) => t * t * t * t;
            public static float OutQuart(float t) => 1 - InQuart(1 - t);
            public static float InOutQuart(float t) {
                if (t < 0.5) return InQuart(t * 2) / 2;
                return 1 - InQuart((1 - t) * 2) / 2;
            }

            public static float InQuint(float t) => t * t * t * t * t;
            public static float OutQuint(float t) => 1 - InQuint(1 - t);
            public static float InOutQuint(float t) {
                if (t < 0.5) return InQuint(t * 2) / 2;
                return 1 - InQuint((1 - t) * 2) / 2;
            }

            public static float InSine(float t) => (float)-Math.Cos(t * Math.PI / 2);
            public static float OutSine(float t) => (float)Math.Sin(t * Math.PI / 2);
            public static float InOutSine(float t) => (float)(Math.Cos(t * Math.PI) - 1) / -2;

            public static float InExpo(float t) => (float)Math.Pow(2, 10 * (t - 1));
            public static float OutExpo(float t) => 1 - InExpo(1 - t);
            public static float InOutExpo(float t) {
                if (t < 0.5) return InExpo(t * 2) / 2;
                return 1 - InExpo((1 - t) * 2) / 2;
            }

            public static float InCirc(float t) => -((float)Math.Sqrt(1 - t * t) - 1);
            public static float OutCirc(float t) => 1 - InCirc(1 - t);
            public static float InOutCirc(float t) {
                if (t < 0.5) return InCirc(t * 2) / 2;
                return 1 - InCirc((1 - t) * 2) / 2;
            }

            public static float InElastic(float t) => 1 - OutElastic(1 - t);
            public static float OutElastic(float t) {
                float p = 0.3f;
                return (float)Math.Pow(2, -10 * t) * (float)Math.Sin((t - p / 4) * (2 * Math.PI) / p) + 1;
            }
            public static float InOutElastic(float t) {
                if (t < 0.5) return InElastic(t * 2) / 2;
                return 1 - InElastic((1 - t) * 2) / 2;
            }

            public static float InBack(float t) {
                float s = 1.70158f;
                return t * t * ((s + 1) * t - s);
            }
            public static float OutBack(float t) => 1 - InBack(1 - t);
            public static float InOutBack(float t) {
                if (t < 0.5) return InBack(t * 2) / 2;
                return 1 - InBack((1 - t) * 2) / 2;
            }

            public static float InBounce(float t) => 1 - OutBounce(1 - t);
            public static float OutBounce(float t) {
                float div = 2.75f;
                float mult = 7.5625f;

                if (t < 1 / div) {
                    return mult * t * t;
                }
                else if (t < 2 / div) {
                    t -= 1.5f / div;
                    return mult * t * t + 0.75f;
                }
                else if (t < 2.5 / div) {
                    t -= 2.25f / div;
                    return mult * t * t + 0.9375f;
                }
                else {
                    t -= 2.625f / div;
                    return mult * t * t + 0.984375f;
                }
            }

            public static float InOutBounce(float t) {
                if (t < 0.5) return InBounce(t * 2) / 2;
                return 1 - InBounce((1 - t) * 2) / 2;
            }

            public static double Linear(double t) => t;

            public static double InQuad(double t) => t * t;
            public static double OutQuad(double t) => 1 - InQuad(1 - t);
            public static double InOutQuad(double t) {
                if (t < 0.5) return InQuad(t * 2) / 2;
                return 1 - InQuad((1 - t) * 2) / 2;
            }

            public static double InCubic(double t) => t * t * t;
            public static double OutCubic(double t) => 1 - InCubic(1 - t);
            public static double InOutCubic(double t) {
                if (t < 0.5) return InCubic(t * 2) / 2;
                return 1 - InCubic((1 - t) * 2) / 2;
            }

            public static double InQuart(double t) => t * t * t * t;
            public static double OutQuart(double t) => 1 - InQuart(1 - t);
            public static double InOutQuart(double t) {
                if (t < 0.5) return InQuart(t * 2) / 2;
                return 1 - InQuart((1 - t) * 2) / 2;
            }

            public static double InQuint(double t) => t * t * t * t * t;
            public static double OutQuint(double t) => 1 - InQuint(1 - t);
            public static double InOutQuint(double t) {
                if (t < 0.5) return InQuint(t * 2) / 2;
                return 1 - InQuint((1 - t) * 2) / 2;
            }

            public static double InSine(double t) => (double)-Math.Cos(t * Math.PI / 2);
            public static double OutSine(double t) => (double)Math.Sin(t * Math.PI / 2);
            public static double InOutSine(double t) => (double)(Math.Cos(t * Math.PI) - 1) / -2;

            public static double InExpo(double t) => (double)Math.Pow(2, 10 * (t - 1));
            public static double OutExpo(double t) => 1 - InExpo(1 - t);
            public static double InOutExpo(double t) {
                if (t < 0.5) return InExpo(t * 2) / 2;
                return 1 - InExpo((1 - t) * 2) / 2;
            }

            public static double InCirc(double t) => -((double)Math.Sqrt(1 - t * t) - 1);
            public static double OutCirc(double t) => 1 - InCirc(1 - t);
            public static double InOutCirc(double t) {
                if (t < 0.5) return InCirc(t * 2) / 2;
                return 1 - InCirc((1 - t) * 2) / 2;
            }

            public static double InElastic(double t) => 1 - OutElastic(1 - t);
            public static double OutElastic(double t) {
                double p = 0.3f;
                return Math.Pow(2, -10 * t) * Math.Sin((t - p / 4) * (2 * Math.PI) / p) + 1;
            }
            public static double InOutElastic(double t) {
                if (t < 0.5) return InElastic(t * 2) / 2;
                return 1 - InElastic((1 - t) * 2) / 2;
            }

            public static double InBack(double t) {
                double s = 1.70158;
                return t * t * ((s + 1) * t - s);
            }
            public static double OutBack(double t) => 1 - InBack(1 - t);
            public static double InOutBack(double t) {
                if (t < 0.5) return InBack(t * 2) / 2;
                return 1 - InBack((1 - t) * 2) / 2;
            }

            public static double InBounce(double t) => 1 - OutBounce(1 - t);
            public static double OutBounce(double t) {
                double div = 2.75;
                double mult = 7.5625;

                if (t < 1 / div) {
                    return mult * t * t;
                }
                else if (t < 2 / div) {
                    t -= 1.5 / div;
                    return mult * t * t + 0.75;
                }
                else if (t < 2.5 / div) {
                    t -= 2.25 / div;
                    return mult * t * t + 0.9375;
                }
                else {
                    t -= 2.625 / div;
                    return mult * t * t + 0.984375;
                }
            }
            public static double InOutBounce(double t) {
                if (t < 0.5) return InBounce(t * 2) / 2;
                return 1 - InBounce((1 - t) * 2) / 2;
            }
        }
    }
}
