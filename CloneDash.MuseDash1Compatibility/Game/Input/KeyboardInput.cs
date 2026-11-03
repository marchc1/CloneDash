using CloneDash.Settings;
using Nucleus.Common.Input;
using Nucleus.Input;
using Nucleus.Types;

using System.Diagnostics.CodeAnalysis;

namespace CloneDash.Game.Input
{
	public class KeyboardInput : ICloneDashInputSystem
	{
		static readonly InputAction[] actions = Enum.GetValues<InputAction>();

		public ButtonCode[][] KeysByAction;
		public ButtonCode[] StartFever;
		public ButtonCode[] Pause;

		public KeyboardInput() {
			CD_InputSettings_OnSettingsChanged();
			InputSettings.OnSettingsChanged += CD_InputSettings_OnSettingsChanged;
		}

		[MemberNotNull(nameof(KeysByAction), nameof(StartFever), nameof(Pause))]
		private void CD_InputSettings_OnSettingsChanged() {
			KeysByAction = new ButtonCode[InputState.ACTION_SLOTS][];
			for (int i = 0; i < KeysByAction.Length; i++)
				KeysByAction[i] = [];
			foreach (var action in actions)
				KeysByAction[(int)action] = InputSettings.GetKeysOfAction(action).ToArray();
			StartFever = InputSettings.GetKeysOfAction(InputAction.FeverStart).ToArray();
			Pause = InputSettings.GetKeysOfAction(InputAction.PauseGame).ToArray();
		}

		public void Poll(FrameState frameState, ref InputState inputState, InputAction? actionFilter = null) {
			foreach (var action in actions) {
				if (actionFilter != null && actionFilter != action)
					continue;

				foreach (var key in KeysByAction[(int)action]) {
					inputState.Click(action, frameState.Keyboard.WasKeyPressed(key) ? 1 : 0);
					inputState.Hold(action, frameState.Keyboard.IsKeyDown(key) ? 1 : 0);
				}
			}

			bool pollForFever = actionFilter == null || actionFilter == InputAction.FeverStart;
			bool pollForPause = actionFilter == null || actionFilter == InputAction.PauseGame;

			if (pollForFever)
				foreach (var key in StartFever)
					inputState.TryFever |= frameState.Keyboard.WasKeyPressed(key);

			if (pollForPause)
				foreach (var key in Pause)
					inputState.PauseButton |= frameState.Keyboard.WasKeyPressed(key);
		}
	}
}
