using Nucleus.Core;

namespace Nucleus.UI.Elements
{
    public enum ScrollbarAlignment
    {
        Horizontal,
        Vertical
    }

    public class Scrollbar : Panel
    {
        public float ScrollbarSize { get; set; } = 8;

        public Button Up { get; set; }
        public Button Down { get; set; }
        public Button Grip { get; set; }

        private float _minScroll, _maxScroll, _scroll;

        public float MinScroll {
            get { return _minScroll; }
            set { _minScroll = value; }
        }
        public float MaxScroll {
            get { return _maxScroll; }
            set { _maxScroll = value; _scroll = Math.Clamp(_scroll, 0, _maxScroll); }
        }
        public float Scroll {
            get { return _scroll; }
            set { _scroll = Math.Clamp(value, 0, (float)Math.Clamp(_maxScroll - _minScroll, 0, double.MaxValue)); }
        }

        private ScrollbarAlignment __alignment = ScrollbarAlignment.Vertical;
        public ScrollbarAlignment Alignment {
            get {
                return __alignment;
            }
            set {
                __alignment = value;
                if (Dock == Dock.None)
                    Dock = value == ScrollbarAlignment.Vertical ? Dock.Right : Dock.Bottom;
            }
        }
        protected override void PerformLayout(float width, float height) {
            if(Alignment == ScrollbarAlignment.Vertical) {
                Up.Dock = Dock.Top;
                Down.Dock = Dock.Bottom;
            }
            else {
                Up.Dock = Dock.Left;
                Down.Dock = Dock.Right;
            }
        }
        protected override void Initialize() {
            this.Size = new(18, 18);

            Up = this.Add<Button>();
            Down = this.Add<Button>();
            Grip = this.Add<Button>();

            Up.Size = new(18, 18);
            Down.Size = new(18, 18);

            Up.Dock = Dock.Top;
            Down.Dock = Dock.Bottom;
            Grip.Dock = Dock.Fill;

            Up.PaintOverride += Button_PaintOverride;
            Down.PaintOverride += Button_PaintOverride;
            Grip.PaintOverride += Grip_PaintOverride;

            Grip.MouseDragEvent += Grip_MouseDragEvent;

            Up.MouseScrollEvent += MouseScrolled;
            Down.MouseScrollEvent += MouseScrolled;
            Grip.MouseScrollEvent += MouseScrolled;
            this.MouseScrollEvent += MouseScrolled;
        }

        public void MouseScrolled(Element self, Types.FrameState state, Types.Vector2F delta) {
            Scroll += delta.Y * -30;
        }

        private void Grip_MouseDragEvent(Element self, Types.FrameState state, Types.Vector2F delta) {
            var pixelDistance = Alignment == ScrollbarAlignment.Vertical ? delta.Y : delta.X;
            var scrollGripTotal = Alignment == ScrollbarAlignment.Vertical ? Grip.RenderBounds.Height : Grip.RenderBounds.Width;
            var scrollbarCurSize = scrollGripTotal * (MinScroll / MaxScroll);

            Scroll += (float)NMath.Remap(pixelDistance, 0, scrollGripTotal - scrollbarCurSize, 0, MaxScroll);
            Scroll = Math.Clamp(Scroll, 0, MaxScroll);
        }

        private void Grip_PaintOverride(Element self, float width, float height) {
            var fore = MixColorBasedOnMouseState(self, TextColor, new(0, 1f, 1.22f, 1f), new(0, 1f, 0.6f, 1f));
            var gripsize = 4;
            Graphics2D.SetDrawColor(fore, 200);

            // Scrollbar height calculation
            if (Alignment == ScrollbarAlignment.Vertical) {
                var scrollbarHeight = height * (MinScroll / MaxScroll);
                Graphics2D.DrawRectangle((width / 2) - (gripsize / 2), (float)NMath.Remap(Scroll, 0, _maxScroll - _minScroll, 0, height - scrollbarHeight), gripsize, scrollbarHeight);
            }
            else {
                var scrollbarWidth = width * (MinScroll / MaxScroll);
                Graphics2D.DrawRectangle((float)NMath.Remap(Scroll, 0, _maxScroll - _minScroll, 0, width - scrollbarWidth), (height / 2) - (gripsize / 2), scrollbarWidth, gripsize);
            }
        }



        private void Button_PaintOverride(Element self, float width, float height) {
            var fore = MixColorBasedOnMouseState(self, TextColor, new(0, 1f, 1.22f, 1f), new(0, 1f, 0.6f, 1f));
            var down = self == Down;

            Graphics2D.SetDrawColor(fore, self.Hovered ? 220 : 200);
            Graphics2D.SetTexture(Alignment == ScrollbarAlignment.Vertical ?
                (down ? Level.Textures.LoadTextureFromFile("ui/down32.png") : Level.Textures.LoadTextureFromFile("ui/up32.png")) :
                (down ? Level.Textures.LoadTextureFromFile("ui/right32.png") : Level.Textures.LoadTextureFromFile("ui/left32.png")));
            Graphics2D.DrawImage(new(2), new(width - 4, height - 4));
        }


        public override void Paint(float width, float height) {

        }
    }
}
