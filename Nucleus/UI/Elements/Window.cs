using Nucleus.Core;
using Nucleus.Types;
using Raylib_cs;
using MouseButton = Nucleus.Types.MouseButton;

namespace Nucleus.UI.Elements
{
    public class Titlebar : Panel
    {
        public string? Image { get; set; } = null;
        public string Title { get; set; } = "Untitled Window";
        public Anchor TitlePos { get; set; } = Anchor.Center;

        public event MouseEventDelegate? OnClosePressed;
        public event MouseEventDelegate? OnMaximizePressed;
        public event MouseEventDelegate? OnMinimizePressed;
        public event MouseV2Delegate? OnTitlebarDragged;

        public Button CloseButton { get; private set; }
        public Button MaximizeButton { get; private set; }
        public Button MinimizeButton { get; private set; }

        protected override void OnThink(FrameState frameState) {
            if (Hovered)
                EngineCore.SetMouseCursor(MouseCursor.MOUSE_CURSOR_RESIZE_ALL);
        }

        protected override void Initialize() {
            base.Initialize();
            Dock = Dock.Top;
            Size = new(0, this.Parent is UserInterface ? 34 : 42);
            if (this.Parent is not UserInterface)
                DockMargin = RectangleF.TLRB(4);
            TextSize = 18;

            CloseButton = Add<Button>();
            CloseButton.Dock = Dock.Right;
            CloseButton.AutoSize = false;
            CloseButton.Size = new(48, 0);

            CloseButton.DockMargin = RectangleF.TLRB(3);
            CloseButton.MouseReleaseEvent += delegate (Element self, FrameState state, MouseButton button) {
                OnClosePressed?.Invoke(this, state, button);
            };
            MaximizeButton = Add<Button>();
            MaximizeButton.Dock = Dock.Right;
            MaximizeButton.AutoSize = false;
            MaximizeButton.Size = new(48, 0);

            MaximizeButton.DockMargin = RectangleF.TLRB(3);
            MaximizeButton.MouseReleaseEvent += delegate (Element self, FrameState state, MouseButton button) {
                OnMaximizePressed?.Invoke(this, state, button);
            };
            MinimizeButton = Add<Button>();
            MinimizeButton.Dock = Dock.Right;
            MinimizeButton.AutoSize = false;
            MinimizeButton.Size = new(48, 0);

            MinimizeButton.DockMargin = RectangleF.TLRB(3);
            MinimizeButton.MouseReleaseEvent += delegate (Element self, FrameState state, MouseButton button) {
                OnMinimizePressed?.Invoke(this, state, button);
            };

            CloseButton.Text = "X";
            MaximizeButton.Text = "";
            MinimizeButton.Text = "";

            MaximizeButton.PaintOverride += (self, width, height) => {
                (self as Button).Paint(width, height);
                Graphics2D.SetDrawColor(TextColor);
                var size = new Vector2F(10);
                var pos = new Vector2F((width / 2) - (size.X / 2), (height / 2) - (size.Y / 2));
                Graphics2D.DrawRectangleOutline(RectangleF.FromPosAndSize(
                    pos, size), 1);

                if (EngineCore.CurrentFrameState.KeyboardState.ShiftDown && self.Hovered) {
                    Graphics2D.DrawRectangleOutline(RectangleF.FromPosAndSize(
                    pos - new Vector2F(2), size + new Vector2F(4)), 1);
                    Graphics2D.DrawLine(pos + new Vector2F(-2, -2), new(4, 4));
                    Graphics2D.DrawLine(pos + new Vector2F(0, size.Y) + new Vector2F(-2, 2), new(4, height - 4));
                    Graphics2D.DrawLine(pos + new Vector2F(size.X, 0) + new Vector2F(2, -2), new(width - 4, 4));
                    Graphics2D.DrawLine(pos + new Vector2F(size.X, size.Y) + new Vector2F(2, 2), new(width - 4, height - 4));
                }
            };
            MinimizeButton.PaintOverride += (self, width, height) => {
                (self as Button).Paint(width, height);
                Graphics2D.SetDrawColor(TextColor);
                Graphics2D.DrawLine(new(14, height / 2), new(width - 14, height / 2));
            };

            CloseButton.BackgroundColor = CloseButton.BackgroundColor.ToHSV().SetHSV(hue: 0, saturation: 0.54f).ToRGB();
            CloseButton.ForegroundColor = CloseButton.ForegroundColor.ToHSV().SetHSV(hue: 0, saturation: 0.6f).ToRGB();
            CloseButton.TextColor = CloseButton.TextColor.ToHSV().SetHSV(hue: 0, saturation: 0.3f).ToRGB();
        }

        public override void MouseDrag(Element self, FrameState state, Vector2F delta) {
            OnTitlebarDragged?.Invoke(self, state, delta);
        }

        public override void Paint(float width, float height) {
            Graphics2D.SetDrawColor(BackgroundColor);
            Graphics2D.DrawRectangle(0, 0, width, height);

            Graphics2D.SetDrawColor(ForegroundColor);
            Graphics2D.DrawRectangleOutline(0, 0, width, height, BorderSize);

            Graphics2D.SetDrawColor(TextColor);
            var pnt = Anchor.CalculatePosition(new(TitlePos.Horizontal == 0 ? 8 : 0, 0), new(width, height), TitlePos);
            if (Image != null) {
                ImageOrientation = ImageOrientation.Centered;
                ImageDrawing(new(4, 4), new(height - 8, height - 8));
                pnt.X += 32;
            }

            Graphics2D.DrawText(pnt.X, pnt.Y, Title, "Noto Sans", 24, TitlePos);
        }
    }
    public class Taskbar : Element
    {

    }
    public class Window : Element
    {
        public static List<WeakReference<Window>> Windows { get; } = [];

        private string _title = "Untitled Window";
        public string Title {
            get => Titlebar == null ? _title : Titlebar.Title;
            set {
                if (Titlebar == null)
                    _title = value;
                else
                    Titlebar.Title = value;
            }
        }
        public Window() {
            Position = new(64, 64);
            Size = new(640, 480);
            Windows.Add(new(this));
        }
        ~Window() {
            MainThread.RunASAP(() => Windows.RemoveAll((x) => x.TryGetTarget(out Window? window) == true && window == this), ThreadExecutionTime.AfterFrame);
        }
        public Titlebar Titlebar { get; private set; }
        protected override void Initialize() {
            Titlebar = Add<Titlebar>();
            Titlebar.Title = _title;
            Titlebar.OnClosePressed += Titlebar_OnTitlebarClosePressed;
            Titlebar.OnTitlebarDragged += dragWindow;

            Panel ap = this.Add<Panel>();
            ap.Dock = Dock.Fill;
            ap.Size = new(0, 36);
            ap.PaintOverride += delegate (Element self, float width, float height) {
                Graphics2D.SetDrawColor(BackgroundColor);
                Graphics2D.DrawRectangle(0, 0, width, height);

                Graphics2D.SetDrawColor(ForegroundColor);
                Graphics2D.DrawRectangleOutline(0, 0, width, height, BorderSize);
            };
            ap.DockMargin = RectangleF.TLRB(4, 8, 8, 4);
            this.AddParent = ap;
            this.UsesRenderTarget = true;
        }

        private void Titlebar_OnTitlebarClosePressed(Element self, FrameState state, MouseButton button) {
            this.Remove();
        }

        private void dragWindow(Element self, FrameState state, Vector2F delta) {
            this.Position += delta;
        }

        protected override void OnThink(FrameState frameState) {
            if (Lifetime >= 0.5)
                UsesRenderTarget = false;
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

            Rlgl.PushMatrix();
            Rlgl.Scalef(1, 1.0f + ((1 - NMath.Ease.OutCubic(mul)) * 0.5f), 1);

            Opacity = mul;
        }
        public override void PostRender() {
            if (Lifetime < 0.5) {
                Rlgl.PopMatrix();
                Rlgl.PopMatrix();
                Raylib.EndMode2D();
            }
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

        public void HideNonCloseButtons() {
            Titlebar.MaximizeButton.Visible = false;
            Titlebar.MinimizeButton.Visible = false;
        }

        public void HideAllButtons() {
            Titlebar.MaximizeButton.Visible = false;
            Titlebar.MinimizeButton.Visible = false;
            Titlebar.CloseButton.Visible = false;
        }
    }
}
