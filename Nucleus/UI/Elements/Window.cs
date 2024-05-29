using Nucleus.Core;
using Nucleus.Types;
using Raylib_cs;
using MouseButton = Nucleus.Types.MouseButton;

namespace Nucleus.UI.Elements
{
    public class Window : Element
    {
        public string Title { get; set; } = "Untitled Window";
        public Window() {
            Position = new(64, 64);
            Size = new(640, 480);
        }
        protected override void Initialize() {
            Panel p = this.Add<Panel>();
            p.Dock = Dock.Top;
            p.Size = new(0, 42);
            p.PaintOverride += delegate (Element self, float width, float height) {
                Graphics2D.SetDrawColor(BackgroundColor);
                Graphics2D.DrawRectangle(0, 0, width, height);

                Graphics2D.SetDrawColor(BackgroundColor.Adjust(0, -0.2, 4));
                Graphics2D.DrawRectangleOutline(0, 0, width, height, 1);

                Graphics2D.SetDrawColor(TextColor);
                Graphics2D.DrawText(width / 2, height / 2, Title, "Arial", 16, Types.Anchor.Center);
            };
            p.DockMargin = RectangleF.TLRB(4);
            p.MouseDragEvent += dragWindow;

            Button close = p.Add<Button>();
            close.Dock = Dock.Right;
            close.AutoSize = false;
            close.Size = new(48, 0);
            close.DockMargin = RectangleF.TLRB(4);
            close.MouseReleaseEvent += Close_MouseReleaseEvent;
            close.Text = "X";
            close.BackgroundColor = close.BackgroundColor.ToHSV().SetHSV(hue: 0, saturation: 0.54f).ToRGB();
            close.ForegroundColor = close.ForegroundColor.ToHSV().SetHSV(hue: 0, saturation: 0.6f).ToRGB();
            close.TextColor = close.TextColor.ToHSV().SetHSV(hue: 0, saturation: 0.3f).ToRGB();

            Panel ap = this.Add<Panel>();
            ap.Dock = Dock.Fill;
            ap.Size = new(0, 36);
            ap.PaintOverride += delegate (Element self, float width, float height) {
                Graphics2D.SetDrawColor(BackgroundColor);
                Graphics2D.DrawRectangle(0, 0, width, height);

                Graphics2D.SetDrawColor(BackgroundColor.Adjust(0, -0.2, 4));
                Graphics2D.DrawRectangleOutline(0, 0, width, height, 1);
            };
            ap.DockMargin = RectangleF.TLRB(4);
            this.AddParent = ap;
            this.UsesRenderTarget = true;
        }

        private void dragWindow(Element self, FrameState state, Vector2F delta) {
            this.Position += delta;
        }

        private void Close_MouseReleaseEvent(Element self, FrameState state, MouseButton button) {
            this.Remove();
        }
        protected override void OnThink(FrameState frameState) {
            if (Lifetime >= 0.5)
                UsesRenderTarget = false;
            //Opacity = (MathF.Sin(EngineCore.Level.CurtimeF * 4) + 1) / 2;
        }
        public override void PreRender() {
            if (Lifetime >= 0.5) return;
            float t = Lifetime % 0.5f;

            float mul = (t) * 2;
            float mulf = (0.5f + (mul / 2)) - 1;

            mul = NMath.Ease.OutCubic(mul);
            mulf = NMath.Ease.InCubic(mulf);

            Raylib.BeginMode2D(new Camera2D() {
                Offset = new((Position.X * -mulf) + ((Size.X / 2) * -mulf), (Position.Y * -mulf) + ((Size.Y / 2) * -mulf)),
                Rotation = 0,
                Target = new(0, mulf),
                Zoom = 1.0f + mulf
            });

            Opacity = mul;
        }
        public override void PostRender() {
            if (Lifetime < 0.5)
                Raylib.EndMode2D();
        }
        public override void PostRenderChildren() { 
            if (InputDisabled) {
                Graphics2D.SetDrawColor(0, 0, 0, 155);
                Graphics2D.DrawRectangle(4, 4, RenderBounds.Width - 8, 34);
                Graphics2D.DrawRectangle(4, 4 + 34 + 8, RenderBounds.Width - 8, RenderBounds.Height - 8 - 34 - 8);
            }
        }

        public void AttachWindowAndLockInput(Window window) {
            InputDisabled = true;
            window.Removed += delegate (Element self) {
                InputDisabled = false;
            };
        }

        protected override void Paint(float width, float height) {
            //this.Size = new(300, 200 + ((float)Math.Sin(EngineCore.Level.CurtimeF * 5) * 100));
            //Graphics.SetDrawColor(255, 255, 255);
            //Graphics.DrawRectangle(0, 0, width, height);
        }
    }
}
