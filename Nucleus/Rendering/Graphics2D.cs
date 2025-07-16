using Nucleus.Extensions;
using Nucleus.Files;
using Nucleus.Rendering;
using Nucleus.Types;
using Nucleus.UI;
using Raylib_cs;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Nucleus.Core
{
	static unsafe class __graphics2Dunsafe
	{
		[DllImport("raylib", CallingConvention = CallingConvention.Cdecl)]
		public static extern void* glfwGetCurrentContext();
		[DllImport("raylib", CallingConvention = CallingConvention.Cdecl)]
		public static extern void glTexImage2DMultisample(int target, int level, int format, int width, int height, bool fixedsamplelocs);
	}
	public record FontEntry(string Path, string PathID);
	public static class Graphics2D
	{
		public static FontManager FontManager { get; private set; } = new(new() {
			{ "Consolas", new FontEntry("MonaspaceNeon-Regular.otf", "fonts") },
			{ "Open Sans", new FontEntry("open-sans.ttf", "fonts") },
			{ "Noto Sans", new FontEntry("noto-sans-en-jp.ttf", "fonts") },
			{ "Noto Sans Mono", new FontEntry("NotoSansMono-VariableFont_wdth,wght.ttf", "fonts") },
		});
		private static Vector2F __offset = new Vector2F(0, 0);
		private static Color ___drawColor = Color.White;
		private static Color __drawColor {
			get {
				if (CurAlpha != 1)
					return new(___drawColor.R, ___drawColor.G, ___drawColor.B, (byte)(___drawColor.A * CurAlpha));
				else
					return ___drawColor;
			}
			set {
				___drawColor = value;
			}
		}
		public const int MAX_ALPHA_STACK = 2048;
		private static int ALPHA_STACK_POINTER = 0;
		public static float[] Alphas { get; private set; } = new float[MAX_ALPHA_STACK];
		public static float CurAlpha { get; private set; } = 1;

		public static void PushAlpha(float v) {
			Alphas[ALPHA_STACK_POINTER] = v;
			ALPHA_STACK_POINTER++;
			CalcAlpha();
		}

		public static void ClearAlphaStack() => ALPHA_STACK_POINTER = 0;


		private static void CalcAlpha() {
			if (ALPHA_STACK_POINTER == 0) {
				CurAlpha = 1;
				return;
			}
			float alphaNow = 1;
			for (int i = 0; i < ALPHA_STACK_POINTER; i++) {
				alphaNow *= (Alphas[i] / 255f);
			}
			CurAlpha = Math.Clamp(alphaNow, 0, 1);
		}

		public static float? PopAlpha() {
			if (ALPHA_STACK_POINTER <= 0)
				return null;

			float alpha = Alphas[ALPHA_STACK_POINTER - 1];
			ALPHA_STACK_POINTER--;
			CalcAlpha();
			return alpha;
		}

		public static Shader shader_hsvtransform = Filesystem.ReadFragmentShader("shaders", "change_color.fshader"); 
		public static float Hue { get; set; } = 0;
		public static float Saturation { get; set; } = 1;
		public static float Value { get; set; } = 1;
		public static System.Numerics.Vector3 HSV {
			get {
				return new(Hue, Saturation, Value);
			}
			set {
				Hue = value.X;
				Saturation = value.Y;
				Value = value.Z;

			}
		}

		/// <summary>
		/// This should be done before starting EngineCore or loading a level.
		/// </summary>
		/// <param name="codepointsStr"></param>
		public static void RegisterCodepoints(string codepointsStr) => FontManager.RegisterCodepoints(codepointsStr);

		private static void beginShading() {
			if (HSV.Equals(new(0, 1, 1)))
				return;

			int swirlCenterLoc = Raylib.GetShaderLocation(shader_hsvtransform, "inputHSV");
			Raylib.SetShaderValue(shader_hsvtransform, swirlCenterLoc, HSV, ShaderUniformDataType.SHADER_UNIFORM_VEC3);
			Raylib.BeginShaderMode(shader_hsvtransform);
		}
		private static void endShading() {
			Raylib.EndShaderMode();
		}

		public static void SetHSV(float h, float s, float v) {
			HSV = new(h, s, v);
			int swirlCenterLoc = Raylib.GetShaderLocation(shader_hsvtransform, "inputHSV");
		}
		public static void SetHSV(Vector3 hsv) => SetHSV(hsv.X, hsv.Y, hsv.Z);

		public static Vector2F Offset => __offset;

		/// <summary>
		/// Note this OFFSETS the returning vector by _offset as well automatically
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		private static Vector2 AFV2ToSNV2(Vector2F input) => new Vector2(input.X + __offset.X, input.Y + __offset.Y);
		private static Vector2 NoOffset_AFV2ToSNV2(Vector2F input) => new Vector2(input.X, input.Y);

		private static Vector2F SNV2ToAFV2(Vector2 input) => new Vector2F(__offset.X, __offset.Y);

		private static Rectangle NoOffset_AFRToRLR(RectangleF input) => new Rectangle(input.X, input.Y, input.Width, input.Height);
		private static Rectangle AFRToRLR(RectangleF input) => new Rectangle(input.X + __offset.X, input.Y + __offset.Y, input.Width, input.Height);

		private static float offsetXF(float input) => input + __offset.X;
		private static float offsetYF(float input) => input + __offset.Y;
		private static int offsetX(float input) => (int)(input + __offset.X);
		private static int offsetY(float input) => (int)(input + __offset.Y);

		public static void ResetDrawingOffset() => __offset = new Vector2F(0, 0);
		public static void OffsetDrawing(Vector2F by) => __offset = __offset + by;
		public static void SetOffset(Vector2F offset) => __offset = offset;

		public static Font GetFont(string fontName, float fontSize) {
			return FontManager["", fontName, (int)fontSize];
		}
		public static Vector2F GetTextSize(string message, string font, float fontSize) {
			var s = Raylib.MeasureTextEx(FontManager[message, font, (int)fontSize], message, (int)fontSize, 0);
			return new(s.X, s.Y);
		}
		public static void DrawDottedLine(Vector2F start, Vector2F end, float segmentLength = 4) {
			var dist = start.Distance(end);
			Vector2 v1, v2;
			for (float i = 0; i < dist; i += (segmentLength * 2)) {
				v1 = AFV2ToSNV2(Vector2F.Lerp(i / dist, start, end));
				v2 = AFV2ToSNV2(Vector2F.Lerp(Math.Clamp(i + segmentLength, 0, dist) / dist, start, end));

				Raylib.DrawLineV(v1, v2, __drawColor);
			}
		}

		public static void DrawText(Vector2F pos, string message, string font, float fontSize) => Raylib.DrawTextEx(FontManager[message, font, (int)fontSize], message, AFV2ToSNV2(pos), (int)fontSize, 0, __drawColor);
		public static void DrawText(float x, float y, string message, string font, float fontSize) => Raylib.DrawTextEx(FontManager[message, font, (int)fontSize], message, new Vector2(offsetX(x), offsetY(y)), (int)fontSize, 0, __drawColor);
		public static void DrawText(float x, float y, string message, string font, float fontSize, TextAlignment horizontal, TextAlignment vertical) {
			int fontSizeI = (int)fontSize;
			var size = Raylib.MeasureTextEx(FontManager[message, font, fontSizeI], message, fontSize, 0);
			float xOffset = 0f, yOffset = 0f;

			switch (horizontal.Alignment) {
				case 1:
					xOffset = -size.X / 2f;
					break;
				case 2:
					xOffset = -size.X;
					break;
			}

			switch (vertical.Alignment) {
				case 1:
					yOffset = -size.Y / 2f;
					break;
				case 2:
					yOffset = -size.Y;
					break;
			}

			DrawText(x + xOffset, y + yOffset, message, font, fontSize);
		}
		public static void DrawText(Vector2F pos, string message, string font, float fontSize, TextAlignment horizontal, TextAlignment vertical) => DrawText(pos.x, pos.y, message, font, fontSize, horizontal, vertical);
		public static void DrawText(float x, float y, string message, string font, float fontSize, Anchor drawingAnchor) => DrawText(x, y, message, font, fontSize, drawingAnchor.ToTextAlignment().horizontal, drawingAnchor.ToTextAlignment().vertical);
		public static void DrawText(Vector2F pos, string message, string font, float fontSize, Anchor drawingAnchor) => DrawText(pos.x, pos.y, message, font, fontSize, drawingAnchor);

		// Untested; need to make sure these all work as expected
		/// <summary>
		/// Draws a gradient. Does not use <see cref="SetDrawColor(Color)"/> due to needing two colors.
		/// </summary>
		/// <param name="pos"></param>
		/// <param name="size"></param>
		/// <param name="direction">The direction the gradient goes from start -> end. So if you chose <see cref="Dock.Left"/>, the gradient will start at <paramref name="start"/> on the right side of the rectangle, and end at <paramref name="end"/> on the left side of the rectangle.</param>
		public static void DrawGradient(Vector2F pos, Vector2F size, Color start, Color end, Dock direction) {
			switch (direction) {
				case Dock.Left: Raylib.DrawRectangleGradientH(offsetX(pos.X), offsetY(pos.Y), (int)size.X, (int)size.Y, end, start); break;
				case Dock.Right: Raylib.DrawRectangleGradientH(offsetX(pos.X), offsetY(pos.Y), (int)size.X, (int)size.Y, start, end); break;

				case Dock.Top: Raylib.DrawRectangleGradientV(offsetX(pos.X), offsetY(pos.Y), (int)size.X, (int)size.Y, start, end); break;
				case Dock.Bottom: Raylib.DrawRectangleGradientV(offsetX(pos.X), offsetY(pos.Y), (int)size.X, (int)size.Y, end, start); break;
			}
		}

		public static unsafe void SetScreenpixelShader(bool unset = false) {
			Rlgl.SetShader(Rlgl.GetShaderIdDefault(), Rlgl.GetShaderLocsDefault());
		}

		private static Texture2D __texture;
		public static Texture2D GetTexture() => __texture;
		public static void SetTexture(Texture2D tex) => __texture = tex;
		public static void SetTexture(RenderTexture2D tex) => __texture = tex.Texture;

		public static Color GetDrawColor() => __drawColor;
		public static void SetDrawColor(in Color c) => __drawColor = c;
		public static void SetDrawColor(in Color c, int alpha) => __drawColor = new(c.R, c.G, c.B, alpha);
		public static void SetDrawColor(int r, int g, int b) => __drawColor = new Color(r, g, b, 255);
		public static void SetDrawColor(int r, int g, int b, int a) => __drawColor = new Color(r, g, b, a);

		/// <summary>
		/// Set the current draw color from a hue, saturation and value. Alpha is optional and must be between 0-255
		/// </summary>
		/// <param name="hue"></param>
		/// <param name="saturation"></param>
		/// <param name="value"></param>
		/// <param name="alpha"></param>
		public static void SetDrawColorHSV(float hue, float saturation, float value, int alpha = 255) {
			var c = Raylib.ColorFromHSV(hue, saturation, value);
			c.A = (byte)Math.Clamp(alpha, 0, 255);
			__drawColor = c;
		}

		/// <summary>
		/// Set the current draw color based on a color with hue additive and saturation/value multiplicative.
		/// </summary>
		/// <param name="c"></param>
		/// <param name="hue"></param>
		/// <param name="saturation"></param>
		/// <param name="value"></param>
		public static void SetDrawColor(Color c, float hue, float saturation, float value) {
			var hsv = Raylib.ColorToHSV(c);
			hsv.X += hue;
			hsv.Y *= saturation;
			hsv.Z *= value;
			__drawColor = hsv.ToRGB(c.A / 255);
		}

		public static void DrawPixel(int x, int y) => Raylib.DrawPixel(offsetX(x), offsetY(x), __drawColor);
		public static void DrawPixel(Vector2F pos) => Raylib.DrawPixelV(AFV2ToSNV2(pos), __drawColor);

		public static void DrawLine(int startX, int startY, int endX, int endY) => Raylib.DrawLine(offsetX(startX), offsetY(startY), offsetX(endX), offsetY(endY), __drawColor);
		public static void DrawLine(float startX, float startY, float endX, float endY) => Raylib.DrawLine(offsetX(startX), offsetY(startY), offsetX(endX), offsetY(endY), __drawColor);
		public static void DrawLine(int startX, int startY, int endX, int endY, float thick) => Raylib.DrawLineEx(new Vector2(offsetX(startX), offsetY(startY)), new Vector2(offsetX(endX), offsetY(endY)), thick, __drawColor);
		public static void DrawLine(float startX, float startY, float endX, float endY, float thick) => Raylib.DrawLineEx(new Vector2(offsetXF(startX), offsetYF(startY)), new Vector2(offsetXF(endX), offsetYF(endY)), thick, __drawColor);
		public static void DrawLine(Vector2F start, Vector2F end) => Raylib.DrawLineV(AFV2ToSNV2(start), AFV2ToSNV2(end), __drawColor);
		public static void DrawLine(Vector2F start, Vector2F end, float width) => Raylib.DrawLineEx(AFV2ToSNV2(start), AFV2ToSNV2(end), width, __drawColor);

		public static void DrawLine(Vector2F startPos, Color startColor, Vector2F endPos, Color endColor, float width = 1) {
			var _startPos = AFV2ToSNV2(startPos.Round());
			var _endPos = AFV2ToSNV2(endPos.Round());
			Rlgl.DrawRenderBatchActive();
			Rlgl.SetLineWidth(width);
			Rlgl.Begin(DrawMode.LINES);
			Rlgl.Color4ub(startColor.R, startColor.G, startColor.B, startColor.A);
			Rlgl.Vertex2f(_startPos.X + 0.5f, _startPos.Y + 0.5f);
			Rlgl.Color4ub(endColor.R, endColor.G, endColor.B, startColor.A);
			Rlgl.Vertex2f(_endPos.X + 0.5f, _endPos.Y + 0.5f);
			Rlgl.End();
			Rlgl.DrawRenderBatchActive();
			Rlgl.SetLineWidth(1);
		}

		public static void DrawLineStrip(Vector2F[] points) => Raylib.DrawLineStrip(Array.ConvertAll<Vector2F, Vector2>(points, AFV2ToSNV2), points.Length, __drawColor);
		public static void DrawLineStrip(Vector2F[] points, float ratio = 1) {
			if (points.Length <= 1) {
				Logs.Warn("DrawLineStrip: expected point array of length > 1, ignoring.");
				return;
			}
			if (ratio <= 0)
				return;
			if (ratio >= 1) {
				Raylib.DrawLineStrip(Array.ConvertAll(points, AFV2ToSNV2), points.Length, __drawColor);
				return;
			}
			float length = ratio * (points.Length - 1);
			int pointsNeeded = (int)MathF.Max(MathF.Ceiling(ratio * points.Length), 2);

			Vector2[] finalPoints = new Vector2[pointsNeeded];
			finalPoints[0] = AFV2ToSNV2(points[0]);
			for (int i = 1; i < pointsNeeded; i++) {
				var lastPoint = points[i - 1];
				var currPoint = points[i];

				var r = (float)Math.Clamp(NMath.Remap(length, i - 1, i, 0, 1), 0, 1);

				finalPoints[i] = AFV2ToSNV2(Vector2F.Lerp(
					r, lastPoint, currPoint
					));
			}

			Raylib.DrawLineStrip(finalPoints, finalPoints.Length, __drawColor);
		}

		public static void DrawLineBezier(Vector2F start, Vector2F end, float width = 1f) => Raylib.DrawLineBezier(AFV2ToSNV2(start), AFV2ToSNV2(end), width, __drawColor);
		public static void DrawCubicBezier(Vector2F p1, Vector2F c1, Vector2F c3, Vector2F p4, float width = 1f) => Raylib.DrawSplineSegmentBezierCubic(AFV2ToSNV2(p1), AFV2ToSNV2(p4), AFV2ToSNV2(c1), AFV2ToSNV2(c3), width, __drawColor);

		public static void DrawCircle(int centerX, int centerY, float radius) => Raylib.DrawCircle(offsetX(centerX), offsetY(centerY), radius, __drawColor);
		public static void DrawCircle(Vector2F pos, float radius) => Raylib.DrawCircleV(AFV2ToSNV2(pos), radius, __drawColor);
		public static void DrawTriangle(Vector2F v1, Vector2F v2, Vector2F v3) {
			Raylib.DrawTriangle(AFV2ToSNV2(v1), AFV2ToSNV2(v2), AFV2ToSNV2(v3), __drawColor);
		}
		public static void DrawCircleSector(int centerX, int centerY, float radius, float startAngle, float endAngle, int segments = 32) => Raylib.DrawCircleSector(new Vector2(offsetX(centerX), offsetY(centerY)), radius, startAngle, endAngle, segments, __drawColor);
		public static void DrawCircleSector(float centerX, float centerY, float radius, float startAngle, float endAngle, int segments = 32) => Raylib.DrawCircleSector(new Vector2(offsetXF(centerX), offsetYF(centerY)), radius, startAngle, endAngle, segments, __drawColor);
		public static void DrawCircleSector(Vector2F pos, float radius, float startAngle, float endAngle, int segments = 32) => Raylib.DrawCircleSector(AFV2ToSNV2(pos), radius, startAngle, endAngle, segments, __drawColor);

		public static void DrawCircleSectorLines(int centerX, int centerY, float radius, float startAngle, float endAngle, int segments = 32) => Raylib.DrawCircleSectorLines(new Vector2(offsetX(centerX), offsetY(centerY)), radius, startAngle, endAngle, segments, __drawColor);
		public static void DrawCircleSectorLines(float centerX, float centerY, float radius, float startAngle, float endAngle, int segments = 32) => Raylib.DrawCircleSectorLines(new Vector2(offsetXF(centerX), offsetYF(centerY)), radius, startAngle, endAngle, segments, __drawColor);
		public static void DrawCircleSectorLines(Vector2F pos, float radius, float startAngle, float endAngle, int segments = 32) => Raylib.DrawCircleSectorLines(AFV2ToSNV2(pos), radius, startAngle, endAngle, segments, __drawColor);

		//add draw circle gradient, if we ever need it

		public static void DrawCircleLines(int centerX, int centerY, float radius) => Raylib.DrawCircleLines(offsetX(centerX), offsetY(centerY), radius, __drawColor);
		//draw ellipse, draw ellipse lines, draw ring, draw ring lines need implementations later

		public static void DrawRectangle(int x, int y, int width, int height) => Raylib.DrawRectangleV(new(offsetXF(x), offsetYF(y)), new(width, height), __drawColor);
		public static void DrawRectangle(float x, float y, float width, float height) => Raylib.DrawRectangleV(new(offsetXF(x), offsetYF(y)), new((int)width, (int)height), __drawColor);
		public static void DrawRectangle(Vector2F pos, Vector2F size) => Raylib.DrawRectangleV(AFV2ToSNV2(pos), NoOffset_AFV2ToSNV2(size), __drawColor);
		public static void DrawRectangle(RectangleF rect) => Raylib.DrawRectangleRec(AFRToRLR(rect), __drawColor);
		public static void DrawRectangle(RectangleF rect, Vector2F origin, float rotation) => Raylib.DrawRectanglePro(NoOffset_AFRToRLR(rect), AFV2ToSNV2(origin), rotation, __drawColor);

		//notimplemented: drawrectanglegradientV, H, Ex

		public static void DrawRectangleOutline(int x, int y, int width, int height, float thickness = 1) => Raylib.DrawRectangleLinesEx(AFRToRLR(RectangleF.XYWH(x, y, width, height)), thickness, __drawColor);
		public static void DrawRectangleOutline(float x, float y, float width, float height, float thickness = 1) => Raylib.DrawRectangleLinesEx(AFRToRLR(RectangleF.XYWH(x, y, width, height)), thickness, __drawColor);
		public static void DrawRectangleOutline(Vector2F pos, Vector2F size, float thickness = 1) => Raylib.DrawRectangleLinesEx(AFRToRLR(RectangleF.FromPosAndSize(pos, size)), thickness, __drawColor);
		public static void DrawRectangleOutline(RectangleF rect, float thickness = 1) => Raylib.DrawRectangleLinesEx(AFRToRLR(rect), thickness, __drawColor);

		public static void DrawRectangleRounded(int x, int y, int width, int height, float roundness, int segments) => Raylib.DrawRectangleRounded(AFRToRLR(RectangleF.XYWH(x, y, width, height)), roundness, segments, __drawColor);
		public static void DrawRectangleRounded(float x, float y, float width, float height, float roundness, int segments) => Raylib.DrawRectangleRounded(AFRToRLR(RectangleF.XYWH(x, y, width, height)), roundness, segments, __drawColor);
		public static void DrawRectangleRounded(Vector2F pos, Vector2F size, float roundness, int segments) => Raylib.DrawRectangleRounded(AFRToRLR(RectangleF.FromPosAndSize(pos, size)), roundness, segments, __drawColor);
		public static void DrawRectangleRounded(RectangleF rect, float roundness, int segments) => Raylib.DrawRectangleRounded(AFRToRLR(rect), roundness, segments, __drawColor);

		public static void DrawRectangleRoundedOutline(int x, int y, int width, int height, float roundness, float thickness, int segments) => Raylib.DrawRectangleRoundedLines(AFRToRLR(RectangleF.XYWH(x, y, width, height)), roundness, segments, thickness, __drawColor);
		public static void DrawRectangleRoundedOutline(float x, float y, float width, float height, float roundness, float thickness, int segments) => Raylib.DrawRectangleRoundedLines(AFRToRLR(RectangleF.XYWH(x, y, width, height)), roundness, segments, thickness, __drawColor);
		public static void DrawRectangleRoundedOutline(Vector2F pos, Vector2F size, float roundness, float thickness, int segments) => Raylib.DrawRectangleRoundedLines(AFRToRLR(RectangleF.FromPosAndSize(pos, size)), roundness, segments, thickness, __drawColor);
		public static void DrawRectangleRoundedOutline(RectangleF rect, float roundness, float thickness, int segments) => Raylib.DrawRectangleRoundedLines(AFRToRLR(rect), roundness, segments, thickness, __drawColor);

		public static void DrawCircle(Vector2F pos, Vector2F size) {
			var local = AFV2ToSNV2(pos);
			Raylib.DrawEllipse((int)local.X, (int)local.Y, size.W, size.H, __drawColor);
		}
		public static void DrawCircleLines(Vector2F pos, Vector2F size) {
			var local = AFV2ToSNV2(pos);
			Raylib.DrawEllipseLines((int)local.X, (int)local.Y, size.W, size.H, __drawColor);
		}

		public static void DrawRendertarget(float x, float y, float width, float height, float rotation = 0) => Raylib.DrawTexturePro(__texture,
				new Rectangle(0, 0, __texture.Width, -__texture.Height),
				new Rectangle(__offset.X + x, __offset.Y + y, width, height),
				new System.Numerics.Vector2(0, 0),
				rotation,
				__drawColor); //=> Raylib.DrawTextureRec(__texture, new Rectangle(x + __offset.X, y + __offset.Y, width, height), new Vector2(x, y), __drawColor);

		private static RectangleF __scissorRect;
		private static Stack<RectangleF> ScissorRects = [];

		public static RectangleF ActiveScissorRect => ScissorRects.Count == 0 ? RectangleF.FromPosAndSize(new(0, 0), EngineCore.GetScreenSize()) : ScissorRects.Peek();

		public static void ScissorRect() {
			EngineCore.Window.EndScissorMode();
			if (ScissorRects.Count > 0) {
				var sR = ScissorRects.Pop();
				__scissorRect = sR;
			}
			else {
				__scissorRect = RectangleF.FromPosAndSize(new(0, 0), EngineCore.GetScreenSize());
			}
		}
		public static void ScissorRect(RectangleF rect) {
			var r = rect.FitInto(ActiveScissorRect);
			ScissorRects.Push(r);
			EngineCore.Window.BeginScissorMode((int)r.X, (int)r.Y, (int)r.W, (int)r.H);
			__scissorRect = RectangleF.XYWH(r.X, r.Y, rect.W, r.H);
		}
		public static RectangleF GetScissorRect() => __scissorRect;

		public static void DrawImage(RectangleF space, Vector2F? origin = null, float rotation = 0, bool flipX = false, bool flipY = false, Vector3? hsvTransform = null) {
			if (hsvTransform.HasValue)
				SetHSV(hsvTransform.Value);
			beginShading();

			Raylib.DrawTexturePro(__texture, new Rectangle(
				(flipX ? 1 : 0) * __texture.Width, (flipY ? 1 : 0) * __texture.Height,
				(flipX ? -1 : 1) * __texture.Width, (flipY ? -1 : 1) * __texture.Height), AFRToRLR(space.AddPosition(new(Offset.X, Offset.Y))), AFV2ToSNV2(origin.HasValue ? origin.Value : Vector2F.Zero), rotation, __drawColor);

			if (hsvTransform.HasValue)
				SetHSV(0, 1, 1);

			endShading();
		}
		public static void DrawImage(Vector2F pos, Vector2F size, Vector2F? origin = null, float rotation = 0, bool flipX = false, bool flipY = false) => DrawImage(RectangleF.FromPosAndSize(pos, size), origin, rotation, flipX, flipY);
		public static void DrawRing(Vector2F center, float innerRadius, float outerRadius, float startAngle = 0, float endAngle = 360, int segments = 32) {
			Raylib.DrawRing(AFV2ToSNV2(center), innerRadius, outerRadius, startAngle, endAngle, segments, __drawColor);
		}
		public static RenderTexture2D CreateRenderTarget(float wF, float hF, PixelFormat pixelFormat = PixelFormat.PIXELFORMAT_UNCOMPRESSED_R8G8B8A8, int mipmaps = 1) {
			int w = (int)wF;
			int h = (int)hF;

			RenderTexture2D target = new();
			target.Id = Rlgl.LoadFramebuffer(w, h);
			if (target.Id > 0) {
				Rlgl.EnableFramebuffer(target.Id);
				unsafe {
					target.Texture.Id = Rlgl.LoadTexture(null, w, h, pixelFormat, mipmaps);
					target.Texture.Width = w;
					target.Texture.Height = h;
					target.Texture.Format = pixelFormat;
					target.Texture.Mipmaps = mipmaps;

					target.Depth.Id = Rlgl.LoadTextureDepth(w, h, true);
					target.Texture.Width = w;
					target.Texture.Height = h;
					target.Texture.Format = PixelFormat.PIXELFORMAT_COMPRESSED_PVRT_RGBA;
					target.Texture.Mipmaps = mipmaps;

					Rlgl.FramebufferAttach(target.Id, target.Texture.Id, FramebufferAttachType.RL_ATTACHMENT_COLOR_CHANNEL0, FramebufferAttachTextureType.RL_ATTACHMENT_TEXTURE2D, 0);
					Rlgl.FramebufferAttach(target.Id, target.Depth.Id, FramebufferAttachType.RL_ATTACHMENT_DEPTH, FramebufferAttachTextureType.RL_ATTACHMENT_RENDERBUFFER, 0);

					Rlgl.DisableFramebuffer();
				}
			}
			else
				Logs.Warn("Rendertarget failed to initialize");

			return target;
		}
		public static void DestroyRenderTarget(RenderTexture2D target) => Raylib.UnloadRenderTexture(target);

		public static void BeginRenderTarget(RenderTexture2D texture) {
			// https://registry.khronos.org/OpenGL-Refpages/gl4/html/glBlendFunc.xhtml
			// https://registry.khronos.org/OpenGL-Refpages/gl4/html/glBlendEquation.xhtml

			EngineCore.Window.BeginTextureMode(texture);
			Surface.Clear(0, 0, 0, 0);
			Rlgl.SetBlendFactorsSeparate(GLEnum.SRC_ALPHA, GLEnum.ONE_MINUS_SRC_ALPHA, GLEnum.ONE, GLEnum.DST_ALPHA, GLEnum.FUNC_ADD, GLEnum.FUNC_ADD);
			Rlgl.SetBlendMode(BlendMode.BLEND_CUSTOM_SEPARATE);
		}

		public static void EndRenderTarget() {
			EngineCore.Window.EndTextureMode();
		}

		public static void DrawRenderTexture(RenderTexture2D texture, Vector2F size) {
			SetTexture(texture);
			SetDrawColor(__drawColor);
			Rlgl.SetBlendMode(BlendMode.BLEND_ALPHA_PREMULTIPLY);
			//DrawRectangleOutline(0, 0, size.W, size.H, 2);
			DrawRendertarget(0, 0, size.W, size.H);
			Rlgl.SetBlendMode(BlendMode.BLEND_ALPHA);
		}

		// TODO: ensure non-breaking changes
		public static void DrawTexture(Vector2F pos, Vector2F size, Vector2F? TL = null, Vector2F? TR = null, Vector2F? BL = null, Vector2F? BR = null) {
			var texture = __texture;
			var source = RectangleF.XYWH(0, 0, texture.Width, texture.Height);
			var dest = RectangleF.FromPosAndSize(new(pos.X, pos.Y), size);
			float rotation = 0;
			Vector2F origin = new(0, 0);
			var tint = __drawColor;

			Vector2F tl = TL ?? new(0, 0);
			Vector2F tr = TR ?? new(1, 0);
			Vector2F bl = BL ?? new(0, 1);
			Vector2F br = BR ?? new(1, 1);

			if (texture.Id > 0) {
				float width = (float)texture.Width;
				float height = (float)texture.Height;

				bool flipX = false;

				if (source.Width < 0) { flipX = true; source.Width *= -1; }
				if (source.Height < 0) source.y -= source.Height;

				Vector2F topLeft = Vector2F.Zero;
				Vector2F topRight = Vector2F.Zero;
				Vector2F bottomLeft = Vector2F.Zero;
				Vector2F bottomRight = Vector2F.Zero;

				if (rotation == 0.0f) {
					float x = dest.x - origin.x;
					float y = dest.y - origin.y;
					topLeft = new Vector2F(x, y);
					topRight = new Vector2F(x + dest.Width, y);
					bottomLeft = new Vector2F(x, y + dest.Height);
					bottomRight = new Vector2F(x + dest.Width, y + dest.Height);
				}
				else {
					float sinRotation = MathF.Sin(rotation * (MathF.PI / 180f));
					float cosRotation = MathF.Cos(rotation * (MathF.PI / 180f));
					float x = dest.x;
					float y = dest.y;
					float dx = -origin.x;
					float dy = -origin.y;

					topLeft.x = x + dx * cosRotation - dy * sinRotation;
					topLeft.y = y + dx * sinRotation + dy * cosRotation;

					topRight.x = x + (dx + dest.Width) * cosRotation - dy * sinRotation;
					topRight.y = y + (dx + dest.Width) * sinRotation + dy * cosRotation;

					bottomLeft.x = x + dx * cosRotation - (dy + dest.Height) * sinRotation;
					bottomLeft.y = y + dx * sinRotation + (dy + dest.Height) * cosRotation;

					bottomRight.x = x + (dx + dest.Width) * cosRotation - (dy + dest.Height) * sinRotation;
					bottomRight.y = y + (dx + dest.Width) * sinRotation + (dy + dest.Height) * cosRotation;
				}

				Rlgl.SetTexture(texture.Id);
				Rlgl.Begin(DrawMode.QUADS);

				Rlgl.Color4ub(tint.R, tint.G, tint.B, tint.A);
				Rlgl.Normal3f(0.0f, 0.0f, 1.0f);                          // Normal vector pointing towards viewer

				// Top-left corner for texture and quad
				if (flipX) Rlgl.TexCoord2f((source.x + source.Width) / width, source.y / height);
				else Rlgl.TexCoord2f(tl.X, tl.Y);
				Rlgl.Vertex2f(topLeft.x, topLeft.y);

				// Bottom-left corner for texture and quad
				if (flipX) Rlgl.TexCoord2f((source.x + source.Width) / width, (source.y + source.Height) / height);
				else Rlgl.TexCoord2f(bl.X, bl.Y);
				Rlgl.Vertex2f(bottomLeft.x, bottomLeft.y);

				// Bottom-right corner for texture and quad
				if (flipX) Rlgl.TexCoord2f(source.x / width, (source.y + source.Height) / height);
				else Rlgl.TexCoord2f(br.X, br.Y);
				Rlgl.Vertex2f(bottomRight.x, bottomRight.y);

				// Top-right corner for texture and quad
				if (flipX) Rlgl.TexCoord2f(source.x / width, source.y / height);
				else Rlgl.TexCoord2f(tr.X, tr.Y);
				Rlgl.Vertex2f(topRight.x, topRight.y);

				Rlgl.End();
				Rlgl.SetTexture(0);

				//Raylib.DrawTexturePro(__texture, new(0, 0, width, height), new(pos.X, pos.Y, size.X, size.Y), new(0, 0), 0, __drawColor);
			}
		}

		public static void DrawLoader(float x, float y, float inner = 14, float outer = 21, int segments = 16, bool useRealtime = false, double? time = null) {
			for (float i = 0; i < segments; i++) {
				var r = i / segments;
				var rn = r * MathF.PI * 2;

				var sO = MathF.Sin(rn) * 1;
				var cO = MathF.Cos(rn) * 1;

				var c = (int)((1 - (r + time ?? (useRealtime ? (float)(DateTime.Now - EngineCore.Level.Start).TotalSeconds : EngineCore.Level.CurtimeF)) % 1) * 155);

				SetDrawColor(c, c, c);
				DrawLine(x + (sO * inner), y + (cO * inner), x + (sO * outer), y + (cO * outer), 1);
			}
		}
	}
}
