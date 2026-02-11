using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Types
{
	public struct Triangle2D
	{
		public Vector2F a;
		public Vector2F b;
		public Vector2F c;

		public Vector2F A {
			get => a;
			set => a = value;
		}
		public Vector2F B {
			get => b;
			set => b = value;
		}
		public Vector2F C {
			get => c;
			set => c = value;
		}

		public static float Sign(Vector2F p1, Vector2F p2, Vector2F p3) {
			return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
		}

		public bool IsPointInTriangle(Vector2F point) {
			float d1, d2, d3;
			bool has_neg, has_pos;

			d1 = Sign(point, a, b);
			d2 = Sign(point, b, c);
			d3 = Sign(point, c, a);

			has_neg = (d1 < 0) || (d2 < 0) || (d3 < 0);
			has_pos = (d1 > 0) || (d2 > 0) || (d3 > 0);

			return !(has_neg && has_pos);
		}

		public Triangle2D RotateAroundPoint(Vector2F center, float degrees) =>
			new(
				this.A.RotateAroundPoint(center, degrees),
				this.B.RotateAroundPoint(center, degrees),
				this.C.RotateAroundPoint(center, degrees)
			);

		public Triangle2D() {
			a = Vector2F.Zero;
			b = Vector2F.Zero;
			c = Vector2F.Zero;
		}

		public Triangle2D(Vector2F a, Vector2F b, Vector2F c) {
			this.a = a;
			this.b = b;
			this.c = c;
		}
	}
}
