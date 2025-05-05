using Nucleus.Input;
using Nucleus.Types;
using System.Diagnostics.CodeAnalysis;

namespace CloneDash.Game.Input
{
	public class KeyboardInput : ICloneDashInputSystem
	{
		public KeyboardKey[] TopKeys;
		public KeyboardKey[] BottomKeys;
		public KeyboardKey[] StartFever;
		public KeyboardKey[] Pause;

		public KeyboardInput() {
			CD_InputSettings_OnSettingsChanged();
			CD_InputSettings.OnSettingsChanged += CD_InputSettings_OnSettingsChanged;
		}

		[MemberNotNull(nameof(TopKeys), nameof(BottomKeys), nameof(StartFever), nameof(Pause))]
		private void CD_InputSettings_OnSettingsChanged() {
			TopKeys = CD_InputSettings.GetKeysOfAction(CD_InputAction.AirAttack).ToArray();
			BottomKeys = CD_InputSettings.GetKeysOfAction(CD_InputAction.GroundAttack).ToArray();
			StartFever = CD_InputSettings.GetKeysOfAction(CD_InputAction.FeverStart).ToArray();
			Pause = CD_InputSettings.GetKeysOfAction(CD_InputAction.PauseGame).ToArray();
		}

		public void Poll(ref FrameState frameState, ref InputState inputState) {
			foreach (var key in TopKeys) {
				inputState.TopClicked += frameState.Keyboard.WasKeyPressed(key) ? 1 : 0;
				inputState.TopHeldCount += frameState.Keyboard.IsKeyDown(key) ? 1 : 0;
			}

			foreach (var key in BottomKeys) {
				inputState.BottomClicked += frameState.Keyboard.WasKeyPressed(key) ? 1 : 0;
				inputState.BottomHeldCount += frameState.Keyboard.IsKeyDown(key) ? 1 : 0;
			}

			foreach (var key in StartFever)
				inputState.TryFever |= frameState.Keyboard.WasKeyPressed(key);

			foreach (var key in Pause)
				inputState.PauseButton |= frameState.Keyboard.WasKeyPressed(key);
		}
	}
}
