using Nucleus.Engine;
using Nucleus.UI;

namespace Nucleus.Types
{
    public struct FrameState
    {
        public Element? HoveredUIElement { get; set; }
        public bool HoveringUIElement => IValidatable.IsValid(HoveredUIElement);

        public Vector2F WindowSize { get; set; }

        public float WindowX { get; internal set; }
        public float WindowY { get; internal set; }

        public float WindowWidth {
            get { return WindowSize.X; }
            set { WindowSize = new Vector2F(value, WindowSize.Y); }
        }
        public float WindowHeight {
            get { return WindowSize.Y; }
            set { WindowSize = new Vector2F(WindowSize.X, value); }
        }
        public MouseState MouseState { get; set; }
        public KeyboardState KeyboardState { get; set; }
        public Raylib_cs.Camera3D Camera { get; internal set; }

        public bool MouseClicked(MouseButton button) => MouseState.Clicked(button);
        public bool MouseHeld(MouseButton button) => MouseState.Held(button);
        public bool MouseReleased(MouseButton button) => MouseState.Released(button);
    }
}
