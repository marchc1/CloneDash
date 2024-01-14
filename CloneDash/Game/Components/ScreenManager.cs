using Raylib_cs;

namespace CloneDash.Game.Components
{
    public class ScreenManager : DashGameComponent
    {
        public float ScrX { get; private set; } = 0;
        public float ScrY { get; private set; } = 0;
        public float ScrWidth { get; private set; }
        public float ScrHeight { get; private set; }

        // The game is designed in 1600x900 resolution. This is the ratio to convert.
        private const int DESIGNED_WIDTH = 1600, DESIGNED_HEIGHT = 900;
        public float ScrRatio { get; private set; }
        public float ScrWRatio => ScrWidth / DESIGNED_WIDTH;
        public float ScrHRatio => ScrHeight / DESIGNED_HEIGHT;
        public ScreenManager(DashGame game) : base(game) {
            OnTick();
        }

        /// <summary>
        /// Allows the game to be drawn in a different rectangle of the screen
        /// </summary>
        public RectangleF? DesiredScreenSize = null;

        public override void OnTick() {
            if (DesiredScreenSize.HasValue) {
                ScrX = DesiredScreenSize.Value.X;
                ScrY = DesiredScreenSize.Value.Y;
                ScrWidth = DesiredScreenSize.Value.W;
                ScrHeight = DesiredScreenSize.Value.H;
            }
            else {
                ScrX = 0;
                ScrY = 0;
                ScrWidth = Raylib.GetScreenWidth();
                ScrHeight = Raylib.GetScreenHeight();
            }
        }
    }
}
