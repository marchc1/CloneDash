using Nucleus.Core;
using Nucleus.Types;
using Raylib_cs;

namespace Nucleus.UI
{
    public class UserInterface : Element
    {
        public Element? Focused;
        public Element? Hovered;
        public Element? Depressed;

        //public List<Element> Popups = [];
        public List<Element> Popups { get; private set; } = [];
        public List<Element> Elements { get; private set; } = [];
        public bool PopupActive => Popups.Count > 0;

        public void RemovePopup(Element e) {
            Popups.Remove(e);
        }

        public UserInterface() {
            Preprocess(new FrameState() {
                WindowWidth = Raylib.GetScreenWidth(),
                WindowHeight = Raylib.GetScreenHeight()
            });
        }

        protected override void Initialize() {
            UI = this;

            Preprocess(new FrameState() {
                WindowWidth = Raylib.GetScreenWidth(),
                WindowHeight = Raylib.GetScreenHeight()
            });
        }

        public void Preprocess(FrameState frameState) {
            if (frameState.WindowWidth != this.Size.W || frameState.WindowHeight != this.Size.H) {
                this.Position = new(0, 0);
                this.Size = new(frameState.WindowWidth, frameState.WindowHeight);
                RenderBounds = RectangleF.FromPosAndSize(this.Position, this.Size);
                InvalidateChildren(recursive: true, self: true);
            }
        }

        public override void PostRenderChildren() {
            var text = TooltipText;
            if (text != "" && text != null) {
                var font = "Open Sans";
                var fontsize = 20;
                var size = Graphics2D.GetTextSize(text, font, fontsize) + new Vector2F(8, 4);
                var mousepos = EngineCore.CurrentFrameState.MouseState.MousePos + new Vector2F(8);
                Graphics2D.SetDrawColor(50, 57, 65, 120);
                Graphics2D.DrawRectangle(mousepos, size);
                Graphics2D.SetDrawColor(10, 15, 25, 225);
                Graphics2D.SetDrawColor(235, 235, 235, 255);
                Graphics2D.DrawRectangleOutline(mousepos, size + new Vector2F(4, 4), 1);
                Graphics2D.DrawText(mousepos + new Vector2F(6, 4), text, font, fontsize);
            }
        }

        protected override void OnThink(FrameState frameState) {
            Clipping = false;
            //this.Position = new(0, 0);
            //this.Size = new(frameState.WindowWidth, frameState.WindowHeight);
            //RenderBounds = RectangleF.FromPosAndSize(this.Position, this.Size);
        }

        internal override void SetupLayout() {
            LayoutInvalidated = false;
        }

        public override void MouseClick(FrameState state, Types.MouseButton button) {
            EngineCore.KeyboardUnfocus(this, true);
        }

        private string _tooltipText = "";
        public override string TooltipText {
            get {
                if (Hovered != null && Hovered != this) {
                    return Hovered.TooltipText;
                }
                return _tooltipText;
            }
            set {
                _tooltipText = value;
            }
        }

        public override void Center() {
            var screen = Raylib.GetCurrentMonitor();
            var mpos = Raylib.GetMonitorPosition(screen).ToNucleus();
            var msize = new Vector2F(Raylib.GetMonitorWidth(screen), Raylib.GetMonitorHeight(screen));

            var mposCenter = mpos + (msize / 2);
            var mposFinal = mposCenter - new Vector2F(Raylib.GetScreenWidth() / 2f, Raylib.GetScreenHeight() / 2f);
            EngineCore.SetWindowPosition(mposFinal);
        }
        ~UserInterface() {
            MainThread.RunASAP(Remove);
        }
    }
}
