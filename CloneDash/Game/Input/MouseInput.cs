using Nucleus.Input;
using Nucleus.Types;
using System.Diagnostics;

namespace CloneDash.Game.Input
{
	public class MouseInput : ICloneDashInputSystem
    {
        public static MouseButton[] TopKeys = [MouseButton.MouseRight]; //sdfg
        public static MouseButton[] BottomKeys = [MouseButton.MouseLeft]; //hjkl
        public static MouseButton? StartFever = null;

        public void Poll(ref FrameState frameState, ref InputState inputState) {
            foreach (var key in TopKeys) {
                inputState.TopClicked += frameState.MouseState.Clicked(key) ? 1 : 0;
                inputState.TopHeld |= frameState.MouseState.Held(key);
            }

            foreach (var key in BottomKeys) {
                inputState.BottomClicked += frameState.MouseState.Clicked(key) ? 1 : 0;
                inputState.BottomHeld |= frameState.MouseState.Held(key);
            }

            if (StartFever != null)
                inputState.TryFever |= frameState.MouseState.Clicked(StartFever);
        }
    }
}
