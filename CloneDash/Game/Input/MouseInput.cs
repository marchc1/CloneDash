using Nucleus.Input;
using Nucleus.Types;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace CloneDash.Game.Input
{
	public class MouseInput : ICloneDashInputSystem
    {
		public static MouseButton[] TopButtons;
		public static MouseButton[] BottomButtons;
		public static MouseButton[] StartFever;
		public static MouseButton[] Pause;
		public MouseInput() {
			CD_InputSettings_OnSettingsChanged();
			CD_InputSettings.OnSettingsChanged += CD_InputSettings_OnSettingsChanged;
		}

		[MemberNotNull(nameof(TopButtons), nameof(BottomButtons), nameof(StartFever), nameof(Pause))]
		private void CD_InputSettings_OnSettingsChanged() {
			TopButtons = CD_InputSettings.GetMouseButtonsOfAction(CD_InputAction.AirAttack).ToArray();
			BottomButtons = CD_InputSettings.GetMouseButtonsOfAction(CD_InputAction.GroundAttack).ToArray();
			StartFever = CD_InputSettings.GetMouseButtonsOfAction(CD_InputAction.FeverStart).ToArray();
			Pause = CD_InputSettings.GetMouseButtonsOfAction(CD_InputAction.PauseGame).ToArray();
		}

		public void Poll(ref FrameState frameState, ref InputState inputState) {
            foreach (var btn in TopButtons) {
                inputState.TopClicked += frameState.Mouse.Clicked(btn) ? 1 : 0;
                inputState.TopHeld |= frameState.Mouse.Held(btn);
            }

            foreach (var btn in BottomButtons) {
                inputState.BottomClicked += frameState.Mouse.Clicked(btn) ? 1 : 0;
                inputState.BottomHeld |= frameState.Mouse.Held(btn);
            }

			foreach (var btn in StartFever)
				inputState.TryFever |= frameState.Mouse.Clicked(btn);
        }
    }
}
