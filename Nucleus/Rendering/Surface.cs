using Nucleus.Types;
using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Rendering
{
	/// <summary>
	/// Screenspace rendering operations that aren't 2D or 3D specific drawing operations, such as viewports (todo; move scissor rectangles here?)
	/// </summary>
	public static class Surface
	{
		public static void SetViewport(float x, float y, float w, float h) {
			// Why is Windows like this?????????????????
			var DPIFactor = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? Vector2F.One : EngineCore.Window.GetWindowScaleDPI();
			x *= DPIFactor.X;
			y *= DPIFactor.Y;
			w *= DPIFactor.X;
			h *= DPIFactor.Y;

			Rlgl.DrawRenderBatchActive();
			EngineCore.Window.Viewport((int)x, (int)y, (int)w, (int)h);
		}

		public static void SetViewport(Vector2F pos, Vector2F size) => SetViewport(pos.X, pos.Y, size.W, size.H);
		public static void SetViewport(RectangleF bounds) => SetViewport(bounds.X, bounds.Y, bounds.W, bounds.H);

		public static void ResetViewport() => SetViewport(EngineCore.GetScreenBounds());

		public static void Clear(Color c) => EngineCore.Window.ClearBackground(c);
		public static void Clear(int r, int g, int b, int a) => EngineCore.Window.ClearBackground(r, g, b, a);
		public static void Clear(int r, int g, int b) => EngineCore.Window.ClearBackground(r, g, b, 255);
		public static void Clear(int rgb) => EngineCore.Window.ClearBackground(rgb, rgb, rgb, 255);

		public static void Spin() {
			Rlgl.DrawRenderBatchActive();
			EngineCore.Window.SwapScreenBuffer();
		}
	}
}
