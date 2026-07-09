using Nucleus.Common.Graphics;
using Nucleus.Common.Types;
using Nucleus.Extensions;
using Nucleus.Files;
using Nucleus.ManagedMemory;
using Nucleus.Rendering;
using Nucleus.Types;
using Nucleus.UI;
using Raylib_cs;
using System.Globalization;
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
	public static class Graphics2D
	{
		public const string UI_FONT_NAME = "Noto Sans";

		public const string UI_MONO_BOLD_FONT_NAME = "Noto Sans Mono Bold";

		// See here for possible values of CultureInfo.Name:
		// https://learn.microsoft.com/zh-cn/openspecs/windows_protocols/ms-lcid/a9eac961-e77d-41a6-90a5-ce1a8b0cdb9c
		public readonly static string UI_CN_JP_FONT_NAME = CultureInfo.CurrentCulture.Name switch {
			"zh-Hant" => "Noto Sans TC",
			"zh-HK" => "Noto Sans HK",
			"zh-MO" => "Noto Sans HK",
			"zh-TW" => "Noto Sans TC",
			"ja" => "Noto Sans JP",
			"ja-JP" => "Noto Sans JP",
			_ => "Noto Sans SC"
		};

		public static FontManager FontManager { get; private set; } = new(new() {
			{ "Consolas", new FontEntry("MonaspaceNeon-Regular.otf", "fonts") },
			{ "Open Sans", new FontEntry("open-sans.ttf", "fonts") },
			{ "Noto Sans", new FontEntry("NotoSans-Regular.ttf", "fonts") },
			{ "Noto Sans Bold", new FontEntry("NotoSans-Bold.ttf", "fonts") },
			{ "Noto Sans Arabic", new FontEntry("NotoSansArabic-Regular.ttf", "fonts") },
			{ "Noto Sans Arabic Bold", new FontEntry("NotoSansArabic-Bold.ttf", "fonts") },
			{ "Noto Sans HK", new FontEntry("NotoSansHK-Regular.ttf", "fonts") },
			{ "Noto Sans HK Bold", new FontEntry("NotoSansHK-Bold.ttf", "fonts") },
			{ "Noto Sans JP", new FontEntry("NotoSansJP-Regular.ttf", "fonts") },
			{ "Noto Sans JP Bold", new FontEntry("NotoSansJP-Bold.ttf", "fonts") },
			{ "Noto Sans KR", new FontEntry("NotoSansKR-Regular.ttf", "fonts") },
			{ "Noto Sans KR Bold", new FontEntry("NotoSansKR-Regular.ttf", "fonts") },
			{ "Noto Sans SC", new FontEntry("NotoSansSC-Regular.ttf", "fonts") },
			{ "Noto Sans SC Bold", new FontEntry("NotoSansSC-Bold.ttf", "fonts") },
			{ "Noto Sans TC", new FontEntry("NotoSansTC-Regular.ttf", "fonts") },
			{ "Noto Sans TC Bold", new FontEntry("NotoSansTC-Bold.ttf", "fonts") },
			{ "Noto Sans Mono", new FontEntry("NotoSansMono-Regular.ttf", "fonts") },
			{ "Noto Sans Mono Bold", new FontEntry("NotoSansMono-Bold.ttf", "fonts") },
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

		/// <summary>
		/// This should be done before starting EngineCore or loading a level.
		/// </summary>
		/// <param name="codepointsStr"></param>
		public static void RegisterCodepoints(ReadOnlySpan<char> codepointsStr) => FontManager.RegisterCodepoints(codepointsStr);

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

		static void __SetOffset(Vector2F o) => __offset = o.Round(3);
		public static void ResetDrawingOffset() => __offset = new Vector2F(0, 0);
		public static void OffsetDrawing(Vector2F by) => __SetOffset(__offset + by);
		public static void SetOffset(Vector2F offset) => __SetOffset(offset);

		public static Vector2F GetTextSize(ReadOnlySpan<char> message, ReadOnlySpan<char> font, float fontSize) {
			var s = Raylib.MeasureTextEx(FontManager[message, font, (int)fontSize].GetFont(), message, (int)fontSize, 0);
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

		public struct TextChunk
		{
			public string Text;
			public string Font;
			public TextChunk(ReadOnlySpan<char> text, ReadOnlySpan<char> font) {
				Text = new(text);
				Font = new(font);
			}
		}

		public struct MappedText
		{
			public string Text;
			public string Font;
			public Vector2F RelativePos;
		}

		public static void DrawText(Vector2F pos, ReadOnlySpan<char> message, ReadOnlySpan<char> font, float fontSize)
			=> Raylib.DrawTextEx(FontManager[message, font, (int)fontSize].GetFont(), message, AFV2ToSNV2(pos), (int)fontSize, 0, __drawColor);
		public static void DrawText(float x, float y, ReadOnlySpan<char> message, ReadOnlySpan<char> font, float fontSize)
			=> Raylib.DrawTextEx(FontManager[message, font, (int)fontSize].GetFont(), message, new Vector2(offsetX(x), offsetY(y)), (int)fontSize, 0, __drawColor);
		public static Vector2F DrawText(float x, float y, ReadOnlySpan<char> message, ReadOnlySpan<char> font, float fontSize, TextAlignment horizontal, TextAlignment vertical)
			=> DrawText(x, y, [new(message, font)], 1, fontSize, horizontal, vertical);
		public static Vector2F DrawText(float x, float y, ReadOnlySpan<char> message, ReadOnlySpan<char> font, float fontSize, TextAlignment2D alignment)
			=> DrawText(x, y, [new(message, font)], 1, fontSize, alignment.Horizontal, alignment.Vertical);
		public static Vector2F DrawText(Vector2F pos, ReadOnlySpan<char> message, ReadOnlySpan<char> font, float fontSize, TextAlignment horizontal, TextAlignment vertical)
			=> DrawText(pos.x, pos.y, message, font, fontSize, horizontal, vertical);
		public static Vector2F DrawText(Vector2F pos, ReadOnlySpan<char> message, ReadOnlySpan<char> font, float fontSize, TextAlignment2D alignment)
			=> DrawText(pos.x, pos.y, message, font, fontSize, alignment.Horizontal, alignment.Vertical);
		public static Vector2F DrawText(float x, float y, ReadOnlySpan<char> message, ReadOnlySpan<char> font, float fontSize, Anchor drawingAnchor)
			=> DrawText(x, y, message, font, fontSize, drawingAnchor.ToTextAlignment());
		public static Vector2F DrawText(Vector2F pos, ReadOnlySpan<char> message, ReadOnlySpan<char> font, float fontSize, Anchor drawingAnchor)
			=> DrawText(pos.x, pos.y, message, font, fontSize, drawingAnchor);
		public static Vector2F DrawText(Vector2F pos, Span<TextChunk> textsFontsMap, int chunkCount, float fontSize, Anchor drawAnchor)
			=> DrawText(pos.x, pos.y, textsFontsMap, chunkCount, fontSize, drawAnchor.ToTextAlignment());
		public static Vector2F DrawText(float x, float y, Span<TextChunk> textsFontsMap, int chunkCount, float fontSize, TextAlignment horizontal, TextAlignment vertical)
			=> DrawText(x, y, textsFontsMap, chunkCount, 0, 0, fontSize, horizontal, vertical);
		public static Vector2F DrawText(float x, float y, Span<TextChunk> textsFontsMap, int chunkCount, float fontSize, TextAlignment2D alignment)
			=> DrawText(x, y, textsFontsMap, chunkCount, 0, 0, fontSize, alignment.Horizontal, alignment.Vertical);


		static readonly NeverShrinkingList<MappedText> mappedTextsCache = [];

		public static Vector2F DrawText(float x, float y, Span<TextChunk> textsFontsMap, int chunkCount, int fontSpacing, int lineSpacing, float fontSize, TextAlignment horizontal, TextAlignment vertical) {
			Vector2F combinedSize = new();
			mappedTextsCache.Clear();

			for (int i = 0; i < textsFontsMap.Length; i += chunkCount) {
				Vector2F chunkedSize = new();
				Span<TextChunk> chunk = textsFontsMap[i..Math.Min(i + chunkCount, textsFontsMap.Length)];

				for (int j = 0; j < chunk.Length; j++) {
					ref TextChunk piece = ref chunk[j];
					string textPart = piece.Text;
					string fontName = piece.Font;
					ref Font font = ref FontManager[textPart, fontName, (int)fontSize].GetFont();
					Vector2F measuredSize = Raylib.MeasureTextEx(font, textPart, fontSize, fontSpacing).ToNucleus();

					ref MappedText textPiece = ref mappedTextsCache.Add();
					textPiece.Text = textPart;
					textPiece.Font = fontName;
					textPiece.RelativePos = chunkedSize;

					chunkedSize.X += measuredSize.X + fontSpacing;
					chunkedSize.Y = Math.Max(chunkedSize.Y, measuredSize.Y);
				}

				combinedSize.X = Math.Max(combinedSize.X, chunkedSize.X);
				combinedSize.Y += chunkedSize.Y + lineSpacing;
			}

			switch (horizontal) {
				case TextAlignment.Center:
					x += -combinedSize.X / 2;
					break;
				case TextAlignment.Right:
					x += -combinedSize.X;
					break;
			}
			switch (vertical) {
				case TextAlignment.Center:
					y += -combinedSize.Y / 2;
					break;
				case TextAlignment.Bottom:
					y += -combinedSize.Y;
					break;
			}

			for (int i = 0; i < mappedTextsCache.Count; i++) {
				ref MappedText mappedText = ref mappedTextsCache[i];
				Vector2F relativePos = mappedText.RelativePos;
				DrawText(x + relativePos.X, y + relativePos.Y, mappedText.Text, mappedText.Font, fontSize);
			}

			return combinedSize;
		}

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
		private static bool __textureFlippedY;
		public static void SetTexture(ITexture? tex) {
			if (tex == null) {
				__texture = default;
				__textureFlippedY = false;
				return;
			}
			__texture = new() {
				Id = tex.GetTextureHandle(),
				Width = tex.Width,
				Height = tex.Height,
				Format = tex.Format,
				Mipmaps = tex.GetMipmapCount()
			};
			__textureFlippedY = tex.HasPublicFlags(PublicTextureFlags.RequiresFlippedV);
		}

		/// <summary>
		/// This will go in a future Nucleus update when texture management is more uniform!!
		/// </summary>
		/// <param name="tex"></param>
		public static void SetTexture(RenderTexture2D tex) { __texture = tex.Texture; __textureFlippedY = true; }

		public static void SetBlendMode(BlendMode mode) => Rlgl.SetBlendMode(mode);

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
			__drawColor = hsv.HSVfToRGBub(c.A / 255);
		}

		public static void DrawPixel(int x, int y) => Raylib.DrawPixel(offsetX(x), offsetY(x), __drawColor);
		public static void DrawPixel(Vector2F pos) => Raylib.DrawPixelV(AFV2ToSNV2(pos), __drawColor);

		static float potentialLineWidthFlush(float newWidth) {
			if (MathF.Abs(Rlgl.GetLineWidth() - newWidth) > 0.01f) {
				Rlgl.DrawRenderBatchActive();
				Rlgl.SetLineWidth(newWidth);
			}
			return newWidth;
		}

		public static void DrawLine(int startX, int startY, int endX, int endY) => Raylib.DrawLine(offsetX(startX), offsetY(startY), offsetX(endX), offsetY(endY), __drawColor);
		public static void DrawLine(float startX, float startY, float endX, float endY) => Raylib.DrawLine(offsetX(startX), offsetY(startY), offsetX(endX), offsetY(endY), __drawColor);
		public static void DrawLine(int startX, int startY, int endX, int endY, float thick) => Raylib.DrawLineEx(new Vector2(offsetX(startX), offsetY(startY)), new Vector2(offsetX(endX), offsetY(endY)), thick, __drawColor);
		public static void DrawLine(float startX, float startY, float endX, float endY, float thick) => Raylib.DrawLineEx(new Vector2(offsetXF(startX), offsetYF(startY)), new Vector2(offsetXF(endX), offsetYF(endY)), thick, __drawColor);
		public static void DrawLine(Vector2F start, Vector2F end) => Raylib.DrawLineV(AFV2ToSNV2(start), AFV2ToSNV2(end), __drawColor);
		public static void DrawLine(Vector2F start, Vector2F end, float width) => Raylib.DrawLineEx(AFV2ToSNV2(start), AFV2ToSNV2(end), potentialLineWidthFlush(width), __drawColor);

		public static void DrawLine(Vector2F startPos, Color startColor, Vector2F endPos, Color endColor, float width = 1) {
			var _startPos = AFV2ToSNV2(startPos.Round());
			var _endPos = AFV2ToSNV2(endPos.Round());
			potentialLineWidthFlush(width);

			Rlgl.Begin(DrawMode.LINES);
			Rlgl.Color4ub(startColor.R, startColor.G, startColor.B, startColor.A);
			Rlgl.Vertex2f(_startPos.X + 0.5f, _startPos.Y + 0.5f);
			Rlgl.Color4ub(endColor.R, endColor.G, endColor.B, startColor.A);
			Rlgl.Vertex2f(_endPos.X + 0.5f, _endPos.Y + 0.5f);
			Rlgl.End();
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

		public static float ConvertRoundnessToRelative(float w, float h, float roundness) {
			float ratio = Math.Min(w, h);
			if (ratio <= 0f) return 0f;
			return Math.Clamp((roundness * 2f) / ratio, 0f, 1f);
		}

		public static void DrawRectangleRounded(int x, int y, int width, int height, float roundness, int segments) => Raylib.DrawRectangleRounded(AFRToRLR(RectangleF.XYWH(x, y, width, height)), ConvertRoundnessToRelative(width, height, roundness), segments, __drawColor);
		public static void DrawRectangleRounded(float x, float y, float width, float height, float roundness, int segments) => Raylib.DrawRectangleRounded(AFRToRLR(RectangleF.XYWH(x, y, width, height)), ConvertRoundnessToRelative(width, height, roundness), segments, __drawColor);
		public static void DrawRectangleRounded(Vector2F pos, Vector2F size, float roundness, int segments) => Raylib.DrawRectangleRounded(AFRToRLR(RectangleF.FromPosAndSize(pos, size)), ConvertRoundnessToRelative(size.W, size.H, roundness), segments, __drawColor);
		public static void DrawRectangleRounded(RectangleF rect, float roundness, int segments) => Raylib.DrawRectangleRounded(AFRToRLR(rect), ConvertRoundnessToRelative(rect.W, rect.H, roundness), segments, __drawColor);

		public static void DrawRectangleRoundedOutline(int x, int y, int width, int height, float roundness, float thickness, int segments) => Raylib.DrawRectangleRoundedLinesEx(AFRToRLR(RectangleF.XYWH(x, y, width, height)), ConvertRoundnessToRelative(width, height, roundness), segments, thickness, __drawColor);
		public static void DrawRectangleRoundedOutline(float x, float y, float width, float height, float roundness, float thickness, int segments) => Raylib.DrawRectangleRoundedLinesEx(AFRToRLR(RectangleF.XYWH(x, y, width, height)), ConvertRoundnessToRelative(width, height, roundness), segments, thickness, __drawColor);
		public static void DrawRectangleRoundedOutline(Vector2F pos, Vector2F size, float roundness, float thickness, int segments) => Raylib.DrawRectangleRoundedLinesEx(AFRToRLR(RectangleF.FromPosAndSize(pos, size)), ConvertRoundnessToRelative(size.W, size.H, roundness), segments, thickness, __drawColor);
		public static void DrawRectangleRoundedOutline(RectangleF rect, float roundness, float thickness, int segments) => Raylib.DrawRectangleRoundedLinesEx(AFRToRLR(rect), ConvertRoundnessToRelative(rect.W, rect.H, roundness), segments, thickness, __drawColor);

		public static void DrawCircle(Vector2F pos, Vector2F size) {
			var local = AFV2ToSNV2(pos);
			Raylib.DrawEllipse((int)local.X, (int)local.Y, size.W, size.H, __drawColor);
		}
		public static void DrawCircleLines(Vector2F pos, Vector2F size) {
			var local = AFV2ToSNV2(pos);
			Raylib.DrawEllipseLines((int)local.X, (int)local.Y, size.W, size.H, __drawColor);
		}

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

		public static void DrawRing(Vector2F center, float innerRadius, float outerRadius, float startAngle = 0, float endAngle = 360, int segments = 32) {
			Raylib.DrawRing(AFV2ToSNV2(center), innerRadius, outerRadius, startAngle, endAngle, segments, __drawColor);
		}

		public static RenderTexture2D CreateRenderTarget(float wF, float hF, ImageFormat pixelFormat = ImageFormat.R8G8B8A8, int mipmaps = 1) {
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
					target.Texture.Format = ImageFormat.PVRT_RGBA;
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
			Rlgl.SetBlendFactorsSeparate(GLEnum.SRC_ALPHA, GLEnum.ONE_MINUS_SRC_ALPHA, GLEnum.ONE, GLEnum.ONE_MINUS_SRC_ALPHA, GLEnum.FUNC_ADD, GLEnum.FUNC_ADD);
			SetBlendMode(BlendMode.CustomSeparate);
		}

		public static void EndRenderTarget() {
			EngineCore.Window.EndTextureMode();
		}

		public static void CalculateUVCoordinatesFromRects(ITexture tex, in RectangleF source, in RectangleF dest, out float sU, out float sV, out float eU, out float eV){
			float texW = tex.Width;
			float texH = tex.Height;

			sU = source.X / texW;
			sV = source.Y / texH;
			eU = (source.X + source.Width) / texW;
			eV = (source.Y + source.Height) / texH;
		}

		public static void DrawTexturedRectangle(RectangleF bounds, float rotation = 0, Vector2F origin = default, float sU = 0, float sV = 0, float eU = 1, float eV = 1, bool flipX = false, bool flipY = false)
			=> DrawTexturedRectangle(bounds.X, bounds.Y, bounds.W, bounds.H, rotation, origin, sU, sV, eU, eV, flipX, flipY);

		public static void DrawTexturedRectangle(Vector2F pos, Vector2F size, float rotation = 0, Vector2F origin = default, float sU = 0, float sV = 0, float eU = 1, float eV = 1, bool flipX = false, bool flipY = false)
			=> DrawTexturedRectangle(pos.X, pos.Y, size.W, size.H, rotation, origin, sU, sV, eU, eV, flipX, flipY);

		public static void DrawTexturedProgress(Vector2F pos, Vector2F size, float progress, Axis axis, bool reversed = false) {
			if (reversed) {
				switch (axis) {
					case Axis.Horizontal:
						DrawTexturedRectangle(new Vector2F(pos.X + (size.X * (1 - progress)), pos.Y), new Vector2F(size.X * progress, size.Y), sU: 1 - progress);
						return;
					case Axis.Vertical:
						DrawTexturedRectangle(new Vector2F(pos.X, pos.Y + (size.Y * (1 - progress))), new Vector2F(size.X, size.Y * progress), sV: 1 - progress); return;
				}
			}
			else {
				switch (axis) {
					case Axis.Horizontal:
						DrawTexturedRectangle(pos, new(size.X * progress, size.Y), eU: progress);
						return;
					case Axis.Vertical:
						DrawTexturedRectangle(pos, new(size.X, size.Y * progress), eV: progress);
						return;
				}
			}

			Logs.Warn($"DrawTexturedProgress: {nameof(axis)} argument invalid (got {axis}, expected Horizontal or Vertical)");
		}

		public static void DrawTexturedRectangle(float x, float y, float w, float h, float rotation = 0, Vector2F rotationOrigin = default, float sU = 0, float sV = 0, float eU = 1, float eV = 1, bool flipX = false, bool flipY = false) {
			if (flipX) (eU, sU) = (sU, eU);
			if (flipY) (eV, sV) = (sV, eV);

			if (__textureFlippedY) 
				(eV, sV) = (sV, eV);

			x += __offset.X;
			y += __offset.Y;

			Rlgl.PushMatrix();

			Rlgl.Translatef(x, y, 0);
			if (rotation != 0)
				Rlgl.Rotatef(rotation, 0, 0, 1);
			Rlgl.Translatef(-rotationOrigin.X, -rotationOrigin.Y, 0);

			Rlgl.SetTexture(__texture.Id);
			Rlgl.Begin(DrawMode.QUADS);
			Rlgl.Color4ub(__drawColor.R, __drawColor.G, __drawColor.B, __drawColor.A); Rlgl.Normal3f(0, 0, 1);
			{
				Rlgl.TexCoord2f(sU, sV); Rlgl.Vertex2f(0, 0);
				Rlgl.TexCoord2f(sU, eV); Rlgl.Vertex2f(0, h);
				Rlgl.TexCoord2f(eU, eV); Rlgl.Vertex2f(w, h);
				Rlgl.TexCoord2f(eU, sV); Rlgl.Vertex2f(w, 0);
			}
			Rlgl.End();
			Rlgl.SetTexture(0);

			Rlgl.PopMatrix();
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
