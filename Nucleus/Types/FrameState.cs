using Nucleus.Input;
using Nucleus.UI;

namespace Nucleus.Types
{
	public class FrameState
    {
		public static FrameState Default {
			get {
				FrameState state = new();
				state.Reset();
				return state;
			}
		}

		public void Reset() {
			HoveredUIElement = null;
			WindowSize = new(0, 0);
			WindowX = 0;
			WindowY = 0;
			Mouse = new();
			Keyboard = new();
			Camera2D = new();
			Camera3D = new();
		}

		public Element? HoveredUIElement;
        public bool HoveringUIElement => IValidatable.IsValid(HoveredUIElement);

		public Vector2F WindowSize;
		public float WindowX;
		public float WindowY;

        public float WindowWidth {
            get { return WindowSize.X; }
            set { WindowSize = new Vector2F(value, WindowSize.Y); }
        }
        public float WindowHeight {
            get { return WindowSize.Y; }
            set { WindowSize = new Vector2F(WindowSize.X, value); }
        }
		public MouseState Mouse;
		public KeyboardState Keyboard;
		public Raylib_cs.Camera2D Camera2D;
		public Raylib_cs.Camera3D Camera3D;

        public bool MouseClicked(MouseButton button) => Mouse.Clicked(button);
        public bool MouseHeld(MouseButton button) => Mouse.Held(button);
        public bool MouseReleased(MouseButton button) => Mouse.Released(button);
    }
}
