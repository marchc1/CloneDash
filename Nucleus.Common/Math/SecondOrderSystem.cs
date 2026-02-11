using Nucleus.Types;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus
{
	public interface ISecondOrderSystem<T>
	{
		public T Value { get; set; }
		public T Update(T x, float? xd_ = null);
	}
	public class SecondOrderSystem : ISecondOrderSystem<float>
	{
		private const float PI = MathF.PI;
		private float xp;
		private float y, yd;
		private float k1, k2, k3;
		private float T_crit;
		private double last = globals.CurTime;
		/// <summary>
		/// Entirely from https://www.youtube.com/watch?v=KPoeNZZ6H4s
		/// </summary>
		/// <param name="f">Natural frequency, the speed that the system will respond to changes, as well as frequency of vibrations</param>
		/// <param name="z">Damping coefficient, describes how the system comes to settle at the target. When Z is 0, vibration will never die down. When greater then 1, the system will not vibrate and will slowly reach the target.</param>
		/// <param name="r">Initial response, when 0, the system takes time to begin accelerating. When positive, it reacts immediately. When greater then 1, it will overshoot. When negative, it will anticipate.</param>
		/// <param name="x0"></param>

		private float f;
		private float z;
		private float r;
		public void ResetTo(float x0) {
			k1 = z / (PI * f);
			k2 = 1 / (2 * PI * f * (2 * PI * f));
			k3 = r * z / (2 * PI * f);

			T_crit = 0.8f * ((float)Math.Sqrt(4 * k2 + k1 * k1) - k1);

			xp = x0;
			y = x0;

			yd = 0;
			last = globals.CurTime;
		}
		public SecondOrderSystem(float f, float z, float r, float x0) {
			this.f = f;
			this.z = z;
			this.r = r;
			ResetTo(x0);
		}
		public float Update(float x) {
			float deltatime = (float)(globals.CurTime - last);
			return Update(deltatime, x);
		}
		public float Update(float x, float? xdIn = null) {
			float deltatime = (float)(globals.CurTime - last);
			return Update(deltatime, x, xdIn);
		}
		public float Update(float T, float x, float? xdIn = null) {
			float xd = 0f;

			if (!xdIn.HasValue) {
				xd = (x - xp) / T;
				xp = x;
			}
			else
				xd = xdIn.Value;

			int iterations = (int)Math.Ceiling(T / T_crit);
			T = T / iterations;

			for (int i = 0; i < iterations; i++) {
				y = y + T * yd;
				yd = yd + T * (x + k3 * xd - y - k1 * yd) / k2;
			}

			last = globals.CurTime;
			return y;
		}

		public float Out => y;
		public float Value { get => y; set { } } // :( - make it just use Out!!! Or Value!!!!! Just be consistent!!!!!!!!
	}

	public class SecondOrderSystem2F : ISecondOrderSystem<Vector2F>
	{
		public SecondOrderSystem X;
		public SecondOrderSystem Y;
		public SecondOrderSystem2F(float f, float z, float r, Vector2F? t_ = null) {
			var t = t_ ?? Vector2F.Zero;
			X = new(f, z, r, t.X);
			Y = new(f, z, r, t.Y);
		}
		public Vector2F Value { get; set; }
		public Vector2F Update(Vector2F x, float? xd_ = null) {
			Value = new(
				X.Update(x.X, xd_),
				Y.Update(x.Y, xd_)
			);
			return Value;
		}
	}
	public class SecondOrderSystem3F : ISecondOrderSystem<Vector3>
	{
		public SecondOrderSystem X;
		public SecondOrderSystem Y;
		public SecondOrderSystem Z;
		public SecondOrderSystem3F(float f, float z, float r, Vector3? t_ = null) {
			var t = t_ ?? Vector3.Zero;
			X = new(f, z, r, t.X);
			Y = new(f, z, r, t.Y);
			Z = new(f, z, r, t.Z);
		}
		public Vector3 Value { get; set; }
		public Vector3 Update(Vector3 x, float? xd_ = null) {
			Value = new(
				X.Update(x.X, xd_),
				Y.Update(x.Y, xd_),
				Z.Update(x.Z, xd_)
			);
			return Value;
		}
	}
}
