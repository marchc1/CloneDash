namespace CloneDash
{
    /// <summary>
    /// A component of a DashGame, which can have per-tick and gamespace/screenspace drawing functions during the game loop
    /// </summary>
    public abstract class DashGameComponent {
        public DashGame Game { get; set; }

        private bool _enabled = true;

        /// <summary>
        /// Components can set themselves to be "required components", which means that even if <see cref="Enabled"/> is set to false, they will be ran anyway.<br></br>
        /// By default, components are required, so the component would need to describe itself as non-essential
        /// </summary>
        public bool Required { get; protected set; } = true;

        /// <summary>
        /// Is the component enabled?<br></br>
        /// Note: <see cref="Required"/> will override this
        /// </summary>
        public bool Enabled {
            get { return Required || _enabled; }
            set { _enabled = value; }
        }

        public DashGameComponent(DashGame game) {
            Game = game;
        }
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        private DashGameComponent() { } // Not called, because game components require a reference to the DashGame
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        /// <summary>
        /// This runs every game tick, which occurs before drawing functions occur.<br></br>
        /// This should be used for any logic and input processing.
        /// </summary>
        public virtual void OnTick() { }

        /// <summary>
        /// This draws in screen space, which means that it's the default top-left to width-height of screenspace<br></br>
        /// This should be used if the component draws a UI element that shouldnt be scaled as much as game elements are<br></br>
        /// </summary>
        public virtual void OnDrawScreenSpace(float width, float height) { }

        /// <summary>
        /// This draws in game space, which is current set to be a vertically-centered coordinate system<br></br>
        /// X coordinate 0 is still the left side of the window, but Y coordinate 0 is the center of the screen<br></br>
        /// </summary>
        public virtual void OnDrawGameSpace() { }

        /// <summary>
        /// Actually calls the tick function, if <see cref="Enabled"/> is true
        /// </summary>
        public void Tick() {
            if (Enabled)
                OnTick();
        }
        /// <summary>
        /// Actually calls the draw function, if <see cref="Enabled"/> is true
        /// </summary>
        public void DrawScreenSpace() {
            if (Enabled)
                OnDrawScreenSpace(Game.ScreenManager.ScrWidth, Game.ScreenManager.ScrHeight);
        }
        /// <summary>
        /// Actually calls the draw function, if <see cref="Enabled"/> is true
        /// </summary>
        public void DrawGameSpace() {
            if (Enabled)
                OnDrawGameSpace();
        }
    }
}
