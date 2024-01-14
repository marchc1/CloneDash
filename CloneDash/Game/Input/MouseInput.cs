using Raylib_cs;

namespace CloneDash.Game.Input
{
    public class MouseInput : IPlayerInput
    {
        public static MouseButton[] TopKeys = [MouseButton.MOUSE_BUTTON_RIGHT]; //sdfg
        public static MouseButton[] BottomKeys = [MouseButton.MOUSE_BUTTON_LEFT]; //hjkl
        public static MouseButton? StartFever = null;

        public void Poll(ref InputState state) {
            foreach (var key in TopKeys) {
                state.TopClicked += Raylib.IsMouseButtonPressed(key) ? 1 : 0;
                state.TopHeld |= Raylib.IsMouseButtonDown(key);
            }

            foreach (var key in BottomKeys) {
                state.BottomClicked += Raylib.IsMouseButtonPressed(key) ? 1 : 0;
                state.BottomHeld |= Raylib.IsMouseButtonDown(key);
            }

            if (StartFever.HasValue)
                state.TryFever |= Raylib.IsMouseButtonPressed(StartFever.Value);
        }
    }
}
