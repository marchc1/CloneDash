using Raylib_cs;

namespace CloneDash.Game.Input
{
    public class KeyboardInput : IPlayerInput
    {
        public static KeyboardKey[] TopKeys = [KeyboardKey.KEY_S, KeyboardKey.KEY_D, KeyboardKey.KEY_F, KeyboardKey.KEY_G];
        public static KeyboardKey[] BottomKeys = [KeyboardKey.KEY_H, KeyboardKey.KEY_J, KeyboardKey.KEY_K, KeyboardKey.KEY_L];
        public static KeyboardKey StartFever = KeyboardKey.KEY_SPACE;
        public static KeyboardKey Pause = KeyboardKey.KEY_ESCAPE;

        public void Poll(ref InputState state) {
            foreach (var key in TopKeys) {
                state.TopClicked += Raylib.IsKeyPressed(key) ? 1 : 0;
                state.TopHeld |= Raylib.IsKeyDown(key);
            }

            foreach (var key in BottomKeys) {
                state.BottomClicked += Raylib.IsKeyPressed(key) ? 1 : 0;
                state.BottomHeld |= Raylib.IsKeyDown(key);
            }

            state.TryFever |= Raylib.IsKeyPressed(StartFever);
            state.PauseButton |= Raylib.IsKeyPressed(Pause);
        }
    }
}
