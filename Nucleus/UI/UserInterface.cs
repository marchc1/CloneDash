using Nucleus.Types;
using Raylib_cs;

namespace Nucleus.UI
{
    public class UserInterface : Element
    {
        public Element? Focused;
        public Element? Hovered;
        public Element? Depressed;

        public List<Element> Popups = [];
        public List<Element> Elements { get; private set; } = [];
        public bool PopupActive => Popups.Count > 0;

        protected override void Initialize() {
            UI = this;

            OnThink(new FrameState() {
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
    }
}
