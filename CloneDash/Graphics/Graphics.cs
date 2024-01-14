using Raylib_cs;
using System.Numerics;

namespace CloneDash
{
    public class FontManager
    {
        public Dictionary<string, string> FontNameToFilepath = new();
        private Dictionary<string, Dictionary<int, Font>> fonttable = new();

        public FontManager(Dictionary<string, string> fonttable) {
            FontNameToFilepath = fonttable;
        }

        public Font this[string name, int size] {
            get {
                Font font;
                Dictionary<int, Font> f1;
                if (!fonttable.TryGetValue(name, out f1)) {
                    fonttable[name] = new();
                    f1 = fonttable[name];
                }

                if (!f1.TryGetValue(size, out font)) {
                    f1[size] = Raylib.LoadFontEx(FontNameToFilepath[name], size, null, 0);
                }

                return font;

            }
        }
    }

    public enum FontAlignment
    {
        Top = 0,
        Left = 0,

        Middle = 1,
        Center = 1,

        Bottom = 2,
        Right = 2
    }
    public static class Graphics
    {
        private static FontManager fontManager = new(new() {
            { "Arial", "C:\\Windows\\Fonts\\arial.ttf" },
            { "Consolas", "C:\\Windows\\Fonts\\consola.ttf" },
            { "Segoe UI", "C:\\Windows\\Fonts\\segoeui.ttf" },
        });
        private static Vector2F _offset = new Vector2F(0, 0);
        private static Color _drawColor = Color.WHITE;
        public static Shader shader_hsvtransform = Raylib.LoadShader($"", $"{Filesystem.Shaders}change_color.fs");
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

        private static void beginShading() {
            if (HSV.Equals(new(0, 1, 1)))
                return;

            int swirlCenterLoc = Raylib.GetShaderLocation(shader_hsvtransform, "inputHSV");
            Raylib.SetShaderValue<System.Numerics.Vector3>(shader_hsvtransform, swirlCenterLoc, HSV, ShaderUniformDataType.SHADER_UNIFORM_VEC3);
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

        public static Vector2F Offset => _offset;

        /// <summary>
        /// Note this OFFSETS the returning vector by _offset as well automatically
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private static Vector2 AFV2ToSNV2(Vector2F input) => new Vector2(input.X + _offset.X, input.Y + _offset.Y);
        private static Vector2 NoOffset_AFV2ToSNV2(Vector2F input) => new Vector2(input.X, input.Y);

        private static Vector2F SNV2ToAFV2(Vector2 input) => new Vector2F(_offset.X, _offset.Y);

        private static Rectangle NoOffset_AFRToRLR(RectangleF input) => new Rectangle(input.X, input.Y, input.Width, input.Height);
        private static Rectangle AFRToRLR(RectangleF input) => new Rectangle(input.X + _offset.X, input.Y + _offset.Y, input.Width, input.Height);

        private static float offsetXF(float input) => input + _offset.X;
        private static float offsetYF(float input) => input + _offset.Y;
        private static int offsetX(float input) => (int)(input + _offset.X);
        private static int offsetY(float input) => (int)(input + _offset.Y);


        public static void ResetDrawingOffset() => _offset = new Vector2F(0, 0);
        public static void OffsetDrawing(Vector2F by) => _offset = _offset + by;
        public static void SetOffset(Vector2F offset) => _offset = offset;

        public static void AssociateFont(string filepath, string nickname) {
            fontManager.FontNameToFilepath[nickname] = filepath;
        }

        public static Vector2F GetTextSize(string message, string font, int fontSize) {
            var s = Raylib.MeasureTextEx(fontManager[font, fontSize], message, fontSize, 0);
            return new(s.X, s.Y);
        }
        public static void DrawText(Vector2F pos, string message, string font, int fontSize) => Raylib.DrawTextEx(fontManager[font, fontSize], message, AFV2ToSNV2(pos), fontSize, 0, _drawColor);
        public static void DrawText(float x, float y, string message, string font, int fontSize) => Raylib.DrawTextEx(fontManager[font, fontSize], message, new Vector2(offsetX(x), offsetY(y)), fontSize, 0, _drawColor);
        public static void DrawText(float x, float y, string message, string font, int fontSize, FontAlignment horizontal, FontAlignment vertical) {
            var size = Raylib.MeasureTextEx(fontManager[font, fontSize], message, fontSize, 0);
            float xOffset = 0f, yOffset = 0f;

            switch (horizontal) {
                case FontAlignment.Middle:
                    xOffset = -size.X * 0.5f;
                    break;
                case FontAlignment.Right:
                    xOffset = -size.X;
                    break;
            }

            switch (vertical) {
                case FontAlignment.Middle:
                    yOffset = -size.Y * 0.5f;
                    break;
                case FontAlignment.Bottom:
                    yOffset = -size.Y;
                    break;
            }

            DrawText(x + xOffset, y + yOffset, message, font, fontSize);
        }
        public static void DrawText(Vector2F pos, string message, string font, int fontSize, FontAlignment horizontal, FontAlignment vertical) => DrawText(pos.x, pos.y, message, font, fontSize, horizontal, vertical);

        public static Color GetDrawColor(Color c) => _drawColor;
        public static void SetDrawColor(Color c) => _drawColor = c;
        public static void SetDrawColor(Color c, int alpha) => _drawColor = new(c.R, c.G, c.B, alpha);
        public static void SetDrawColor(int r, int g, int b) => _drawColor = new Color(r, g, b, 255);
        public static void SetDrawColor(int r, int g, int b, int a) => _drawColor = new Color(r, g, b, a);

        public static void DrawPixel(int x, int y) => Raylib.DrawPixel(offsetX(x), offsetY(x), _drawColor);
        public static void DrawPixel(Vector2F pos) => Raylib.DrawPixelV(AFV2ToSNV2(pos), _drawColor);

        public static void DrawLine(int startX, int startY, int endX, int endY) => Raylib.DrawLine(offsetX(startX), offsetY(startY), offsetX(endX), offsetY(endY), _drawColor);
        public static void DrawLine(float startX, float startY, float endX, float endY) => Raylib.DrawLine(offsetX(startX), offsetY(startY), offsetX(endX), offsetY(endY), _drawColor);
        public static void DrawLine(int startX, int startY, int endX, int endY, float thick) => Raylib.DrawLineEx(new Vector2(offsetX(startX), offsetY(startY)), new Vector2(offsetX(endX), offsetY(endY)), thick, _drawColor);
        public static void DrawLine(float startX, float startY, float endX, float endY, float thick) => Raylib.DrawLineEx(new Vector2(offsetXF(startX), offsetYF(startY)), new Vector2(offsetXF(endX), offsetYF(endY)), thick, _drawColor);
        public static void DrawLine(Vector2F start, Vector2F end) => Raylib.DrawLineV(AFV2ToSNV2(start), AFV2ToSNV2(end), _drawColor);
        public static void DrawLine(Vector2F start, Vector2F end, float width) => Raylib.DrawLineEx(AFV2ToSNV2(start), AFV2ToSNV2(end), width, _drawColor);

        public static void DrawLineStrip(Vector2F[] points) => Raylib.DrawLineStrip(Array.ConvertAll<Vector2F, Vector2>(points, AFV2ToSNV2), points.Length, _drawColor);

        public static void DrawLineBezier(Vector2F start, Vector2F end, float width = 1f) => Raylib.DrawLineBezier(AFV2ToSNV2(start), AFV2ToSNV2(end), width, _drawColor);

        public static void DrawCircle(int centerX, int centerY, float radius) => Raylib.DrawCircle(offsetX(centerX), offsetY(centerY), radius, _drawColor);
        public static void DrawCircle(Vector2F pos, float radius) => Raylib.DrawCircleV(AFV2ToSNV2(pos), radius, _drawColor);

        public static void DrawCircleSector(int centerX, int centerY, float radius, float startAngle, float endAngle, int segments = 32) => Raylib.DrawCircleSector(new Vector2(offsetX(centerX), offsetY(centerY)), radius, startAngle, endAngle, segments, _drawColor);
        public static void DrawCircleSector(float centerX, float centerY, float radius, float startAngle, float endAngle, int segments = 32) => Raylib.DrawCircleSector(new Vector2(offsetXF(centerX), offsetYF(centerY)), radius, startAngle, endAngle, segments, _drawColor);
        public static void DrawCircleSector(Vector2F pos, float radius, float startAngle, float endAngle, int segments = 32) => Raylib.DrawCircleSector(AFV2ToSNV2(pos), radius, startAngle, endAngle, segments, _drawColor);

        public static void DrawCircleSectorLines(int centerX, int centerY, float radius, float startAngle, float endAngle, int segments = 32) => Raylib.DrawCircleSectorLines(new Vector2(offsetX(centerX), offsetY(centerY)), radius, startAngle, endAngle, segments, _drawColor);
        public static void DrawCircleSectorLines(float centerX, float centerY, float radius, float startAngle, float endAngle, int segments = 32) => Raylib.DrawCircleSectorLines(new Vector2(offsetXF(centerX), offsetYF(centerY)), radius, startAngle, endAngle, segments, _drawColor);
        public static void DrawCircleSectorLines(Vector2F pos, float radius, float startAngle, float endAngle, int segments = 32) => Raylib.DrawCircleSectorLines(AFV2ToSNV2(pos), radius, startAngle, endAngle, segments, _drawColor);

        //add draw circle gradient, if we ever need it

        public static void DrawCircleLines(int centerX, int centerY, float radius) => Raylib.DrawCircleLines(offsetX(centerX), offsetY(centerY), radius, _drawColor);
        //draw ellipse, draw ellipse lines, draw ring, draw ring lines need implementations later

        public static void DrawRectangle(int x, int y, int width, int height) => Raylib.DrawRectangle(offsetX(x), offsetY(y), width, height, _drawColor);
        public static void DrawRectangle(float x, float y, float width, float height) => Raylib.DrawRectangle(offsetX(x), offsetY(y), (int)width, (int)height, _drawColor);
        public static void DrawRectangle(Vector2F pos, Vector2F size) => Raylib.DrawRectangleV(AFV2ToSNV2(pos), AFV2ToSNV2(size), _drawColor);
        public static void DrawRectangle(RectangleF rect) => Raylib.DrawRectangleRec(AFRToRLR(rect), _drawColor);
        public static void DrawRectangle(RectangleF rect, Vector2F origin, float rotation) => Raylib.DrawRectanglePro(NoOffset_AFRToRLR(rect), AFV2ToSNV2(origin), rotation, _drawColor);

        //notimplemented: drawrectanglegradientV, H, Ex

        public static void DrawRectangleOutline(int x, int y, int width, int height, float thickness = 1) => Raylib.DrawRectangleLinesEx(AFRToRLR(RectangleF.XYWH(x, y, width, height)), thickness, _drawColor);
        public static void DrawRectangleOutline(float x, float y, float width, float height, float thickness = 1) => Raylib.DrawRectangleLinesEx(AFRToRLR(RectangleF.XYWH(x, y, width, height)), thickness, _drawColor);
        public static void DrawRectangleOutline(Vector2F pos, Vector2F size, float thickness = 1) => Raylib.DrawRectangleLinesEx(AFRToRLR(RectangleF.FromPosAndSize(pos, size)), thickness, _drawColor);
        public static void DrawRectangleOutline(RectangleF rect, float thickness = 1) => Raylib.DrawRectangleLinesEx(AFRToRLR(rect), thickness, _drawColor);

        public static void DrawRectangleRounded(int x, int y, int width, int height, float roundness, int segments) => Raylib.DrawRectangleRounded(AFRToRLR(RectangleF.XYWH(x, y, width, height)), roundness, segments, _drawColor);
        public static void DrawRectangleRounded(float x, float y, float width, float height, float roundness, int segments) => Raylib.DrawRectangleRounded(AFRToRLR(RectangleF.XYWH(x, y, width, height)), roundness, segments, _drawColor);
        public static void DrawRectangleRounded(Vector2F pos, Vector2F size, float roundness, int segments) => Raylib.DrawRectangleRounded(AFRToRLR(RectangleF.FromPosAndSize(pos, size)), roundness, segments, _drawColor);
        public static void DrawRectangleRounded(RectangleF rect, float roundness, int segments) => Raylib.DrawRectangleRounded(AFRToRLR(rect), roundness, segments, _drawColor);

        public static void DrawRectangleRoundedOutline(int x, int y, int width, int height, float roundness, float thickness, int segments) => Raylib.DrawRectangleRoundedLines(AFRToRLR(RectangleF.XYWH(x, y, width, height)), roundness, segments, thickness, _drawColor);
        public static void DrawRectangleRoundedOutline(float x, float y, float width, float height, float roundness, float thickness, int segments) => Raylib.DrawRectangleRoundedLines(AFRToRLR(RectangleF.XYWH(x, y, width, height)), roundness, segments, thickness, _drawColor);
        public static void DrawRectangleRoundedOutline(Vector2F pos, Vector2F size, float roundness, float thickness, int segments) => Raylib.DrawRectangleRoundedLines(AFRToRLR(RectangleF.FromPosAndSize(pos, size)), roundness, segments, thickness, _drawColor);
        public static void DrawRectangleRoundedOutline(RectangleF rect, float roundness, float thickness, int segments) => Raylib.DrawRectangleRoundedLines(AFRToRLR(rect), roundness, segments, thickness, _drawColor);


        private static RectangleF _scissorRect;
        public static void ScissorRect() => Raylib.EndScissorMode();
        public static void ScissorRect(RectangleF rect) {
            Raylib.BeginScissorMode((int)rect.X, (int)rect.Y, (int)rect.W, (int)rect.H);
            _scissorRect = new(rect.X, rect.Y, rect.W, rect.H);
        }
        public static RectangleF GetScissorRect() => _scissorRect;

        public static void DrawImage(Texture2D texture, RectangleF space, Vector2F? origin = null, float rotation = 0, Vector3? hsvTransform = null) {
            if (hsvTransform.HasValue)
                SetHSV(hsvTransform.Value);
            beginShading();

            Raylib.DrawTexturePro(texture, new Rectangle(0, 0, texture.Width, texture.Height), AFRToRLR(space), AFV2ToSNV2(origin.HasValue ? origin.Value : new Vector2F(0, 0)), rotation, _drawColor);

            if (hsvTransform.HasValue)
                SetHSV(0, 1, 1);

            endShading();
        }
        public static void DrawImage(Texture2D texture, Vector2F pos, Vector2F size, Vector2F? origin = null, float rotation = 0) => DrawImage(texture, RectangleF.FromPosAndSize(pos, size), origin, rotation);
        public static void DrawRing(Vector2F center, float innerRadius, float outerRadius, float startAngle = 0, float endAngle = 360, int segments = 32) {
            Raylib.DrawRing(AFV2ToSNV2(center), innerRadius, outerRadius, startAngle, endAngle, segments, _drawColor);
        }
    }
}
