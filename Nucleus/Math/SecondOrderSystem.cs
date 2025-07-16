using Nucleus.Commands;
using Nucleus.Core;
using Nucleus.Types;
using Nucleus.UI;
using Nucleus.UI.Elements;

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
	[Nucleus.MarkForStaticConstruction]
	public class SecondOrderSystem : ISecondOrderSystem<float>
	{
		public static ConCommand sos_tuner = ConCommand.Register(nameof(sos_tuner), (_, _) => {
			var window = EngineCore.Level.UI.Add<Window>();
			window.Size = new(480, 640);
			window.Center();

			float[] inPoints = new float[512];
			for (int i = 0; i < inPoints.Length; i++) {
				inPoints[i] = i < 50 ? 0 : i < 200 ? 1 : 0.5f;
			}

			float[] outPoints = new float[512];

			var graph = window.Add<Panel>();
			graph.Size = new(480);
			graph.Dock = Dock.Top;

			NumSlider fEdit, zEdit, rEdit;

			window.Add(out fEdit);
			window.Add(out zEdit);
			window.Add(out rEdit);

			fEdit.Value = 1;
			zEdit.Value = 1;
			rEdit.Value = 1;

			fEdit.Size = zEdit.Size = rEdit.Size = new(32);
			fEdit.Dock = zEdit.Dock = rEdit.Dock = Dock.Top;
			fEdit.Digits = zEdit.Digits = rEdit.Digits = 3;

			fEdit.OnValueChanged += (_, _, _) => modifySOSTunerData((float)fEdit.Value, (float)zEdit.Value, (float)rEdit.Value, inPoints, outPoints);
			zEdit.OnValueChanged += (_, _, _) => modifySOSTunerData((float)fEdit.Value, (float)zEdit.Value, (float)rEdit.Value, inPoints, outPoints);
			rEdit.OnValueChanged += (_, _, _) => modifySOSTunerData((float)fEdit.Value, (float)zEdit.Value, (float)rEdit.Value, inPoints, outPoints);
			modifySOSTunerData((float)fEdit.Value, (float)zEdit.Value, (float)rEdit.Value, inPoints, outPoints);

			graph.PaintOverride += (_, w, h) => {
				Graphics2D.SetDrawColor(255, 150, 150);
				for (int i = 1; i < inPoints.Length; i++) {
					getVariables(inPoints, i - 1, w, h, out var lx, out var ly);
					getVariables(inPoints, i, w, h, out var cx, out var cy);
					Graphics2D.DrawLine(lx, ly, cx, cy);
				}

				Graphics2D.SetDrawColor(150, 255, 150);
				for (int i = 1; i < outPoints.Length; i++) {
					getVariables(outPoints, i - 1, w, h, out var lx, out var ly);
					getVariables(outPoints, i, w, h, out var cx, out var cy);
					Graphics2D.DrawLine(lx, ly, cx, cy);
				}
			};
		});

		private static void getVariables(float[] points, int index, float w, float h, out float x, out float y) {
			var xPadding = 8;
			var yPadding = h * .1f;
			x = (float)NMath.Remap(index, 0, points.Length, xPadding, w - (xPadding * 2));
			y = (float)NMath.Remap(points[index], 0, 1, h - (yPadding * 2), yPadding);
		}

		private static void modifySOSTunerData(float f, float z, float r, float[] input, float[] output) {
			SecondOrderSystem sos = new SecondOrderSystem(f, z, r, 0);
			for (int i = 0; i < input.Length; i++) {
				output[i] = sos.Update(0.01f, input[i]);
			}
		}
		private static readonly float PI = (float)Math.PI;
		private float xp;
		private float y, yd;
		private float k1, k2, k3;
		private float T_crit;
		private double last = EngineCore.Level?.Curtime ?? 0;
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
			last = EngineCore.Level?.Curtime ?? 0;
		}
		public SecondOrderSystem(float f, float z, float r, float x0) {
			this.f = f;
			this.z = z;
			this.r = r;
			ResetTo(x0);
		}
		public float Update(float x) {
			float deltatime = (float)((EngineCore.Level?.Curtime ?? 0) - last);
			return Update(deltatime, x);
		}
		public float Update(float x, float? xdIn = null) {
			float deltatime = (float)((EngineCore.Level?.Curtime ?? 0) - last);
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

			last = (EngineCore.Level?.Curtime ?? 0);
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
