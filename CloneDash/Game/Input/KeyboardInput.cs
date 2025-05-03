using Nucleus.Input;
using Nucleus.Types;

namespace CloneDash.Game.Input
{
	public class KeyboardInput : ICloneDashInputSystem
    {
        public static KeyboardKey[] TopKeys = [KeyboardLayout.USA.S, KeyboardLayout.USA.D, KeyboardLayout.USA.F, KeyboardLayout.USA.G];
        public static KeyboardKey[] BottomKeys = [KeyboardLayout.USA.H, KeyboardLayout.USA.J, KeyboardLayout.USA.K, KeyboardLayout.USA.L];
        public static KeyboardKey StartFever = KeyboardLayout.USA.Space;
        public static KeyboardKey Pause = KeyboardLayout.USA.Escape;

        public void Poll(ref FrameState frameState, ref InputState inputState) {
            foreach (var key in TopKeys) {
                inputState.TopClicked += frameState.Keyboard.WasKeyPressed(key) ? 1 : 0;
                inputState.TopHeld |= frameState.Keyboard.IsKeyDown(key);
            }

            foreach (var key in BottomKeys) {
                inputState.BottomClicked += frameState.Keyboard.WasKeyPressed(key) ? 1 : 0;
                inputState.BottomHeld |= frameState.Keyboard.IsKeyDown(key);
            }

            inputState.TryFever |= frameState.Keyboard.WasKeyPressed(StartFever);
            inputState.PauseButton |= frameState.Keyboard.WasKeyPressed(Pause);
        }
    }
}
