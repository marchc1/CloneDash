using Raylib_cs;

namespace Nucleus.Types
{
    public record MouseButton(int Button)
    {
        public static MouseButton Mouse1 { get; } = new(1);
        public static MouseButton MouseLeft { get; } = new(1);

        public static MouseButton Mouse2 { get; } = new(2);
        public static MouseButton MouseRight { get; } = new(2);

        public static MouseButton Mouse3 { get; } = new(3);
        public static MouseButton MouseMiddle { get; } = new(3);

        public static MouseButton Mouse4 { get; } = new(4);
        public static MouseButton MouseBack { get; } = new(4);

        public static MouseButton Mouse5 { get; } = new(5);
        public static MouseButton MouseForward { get; } = new(5);
    }
    public struct MouseState
    {
        // Pressed this frame
        public bool Mouse1Clicked = false;
        public bool Mouse2Clicked = false;
        public bool Mouse3Clicked = false;
        public bool Mouse4Clicked = false;
        public bool Mouse5Clicked = false;

        // Held/down in general
        public bool Mouse1Held = false;
        public bool Mouse2Held = false;
        public bool Mouse3Held = false;
        public bool Mouse4Held = false;
        public bool Mouse5Held = false;

        // Released this frame
        public bool Mouse1Released = false;
        public bool Mouse2Released = false;
        public bool Mouse3Released = false;
        public bool Mouse4Released = false;
        public bool Mouse5Released = false;

        public bool MouseClicked => Mouse1Clicked || Mouse2Clicked || Mouse3Clicked || Mouse4Clicked || Mouse5Clicked;
        public bool MouseHeld => Mouse1Held || Mouse2Held || Mouse3Held || Mouse4Held || Mouse5Held;
        public bool MouseReleased => Mouse1Released || Mouse2Released || Mouse3Released || Mouse4Released || Mouse5Released;

        public Vector2F GlobalMousePos {
            get {
                return Raylib.GetWindowPosition().ToNucleus() + MousePos;
            }
        }
        public bool Clicked(MouseButton button) {
            switch (button.Button) {
                case 1: return Mouse1Clicked;
                case 2: return Mouse2Clicked;
                case 3: return Mouse3Clicked;
                case 4: return Mouse4Clicked;
                case 5: return Mouse5Clicked;
            }
            throw new NotImplementedException("No Clicked handler for MouseButton " + button.Button);
        }

        public bool Held(MouseButton button) {
            switch (button.Button) {
                case 1: return Mouse1Held;
                case 2: return Mouse2Held;
                case 3: return Mouse3Held;
                case 4: return Mouse4Held;
                case 5: return Mouse5Held;
            }
            throw new NotImplementedException("No Held handler for MouseButton " + button.Button);
        }

        public bool Released(MouseButton button) {
            switch (button.Button) {
                case 1: return Mouse1Released;
                case 2: return Mouse2Released;
                case 3: return Mouse3Released;
                case 4: return Mouse4Released;
                case 5: return Mouse5Released;
            }
            throw new NotImplementedException("No Released handler for MouseButton " + button.Button);
        }

        /// <summary>
        /// Mouse position, localized to the window.
        /// </summary>
        public Vector2F MousePos { get; set; } = new(0);
        public Vector2F MouseDelta { get; set; } = new(0);
        public Vector2F MouseScroll { get; set; } = new(0);

        public MouseState() { }

        public override string ToString() {
            return $"Clicked [{(Mouse1Clicked ? "^" : "_")} {(Mouse2Clicked ? "^" : "_")} {(Mouse3Clicked ? "^" : "_")} {(Mouse4Clicked ? "^" : "_")} {(Mouse5Clicked ? "^" : "_")}] " +
                $"Held [{(Mouse1Held ? "^" : "_")} {(Mouse2Held ? "^" : "_")} {(Mouse3Held ? "^" : "_")} {(Mouse4Held ? "^" : "_")} {(Mouse5Held ? "^" : "_")}] " +
                $"Released [{(Mouse1Released ? "^" : "_")} {(Mouse2Released ? "^" : "_")} {(Mouse3Released ? "^" : "_")} {(Mouse4Released ? "^" : "_")} {(Mouse5Released ? "^" : "_")}] " +
                $"[pos: {MousePos}] [delta: {MouseDelta}] [scroll: {MouseScroll}]";
        }
    }
}
