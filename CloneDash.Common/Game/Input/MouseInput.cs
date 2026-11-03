using CloneDash.Settings;
using Nucleus.Common.Input;
using Nucleus.Input;
using Nucleus.Types;

using System.Diagnostics.CodeAnalysis;

namespace CloneDash.Game.Input
{
	public class MouseInput : ICloneDashInputSystem
	{
		static readonly InputAction[] actions = Enum.GetValues<InputAction>();

		public static ButtonCode[][] ButtonsByAction;
		public static ButtonCode[] StartFever;
		public static ButtonCode[] Pause;
		public MouseInput() {
			CD_InputSettings_OnSettingsChanged();
			InputSettings.OnSettingsChanged += CD_InputSettings_OnSettingsChanged;
		}

		[MemberNotNull(nameof(ButtonsByAction), nameof(StartFever), nameof(Pause))]
		private void CD_InputSettings_OnSettingsChanged() {
			ButtonsByAction = new ButtonCode[InputState.ACTION_SLOTS][];
			for (int i = 0; i < ButtonsByAction.Length; i++)
				ButtonsByAction[i] = [];
			foreach (var action in actions)
				if (action is not (InputAction.FeverStart or InputAction.PauseGame))
					ButtonsByAction[(int)action] = InputSettings.GetButtonCodesOfAction(action).ToArray();
			StartFever = InputSettings.GetButtonCodesOfAction(InputAction.FeverStart).ToArray();
			Pause = InputSettings.GetButtonCodesOfAction(InputAction.PauseGame).ToArray();
		}

		public void Poll(FrameState frameState, ref InputState inputState, InputAction? actionFilter = null) {
			foreach (var action in actions) {
				if (actionFilter != null && actionFilter != action)
					continue;

				foreach (var btn in ButtonsByAction[(int)action]) {
					inputState.Click(action, frameState.Mouse.Clicked(btn) ? 1 : 0);
					inputState.Hold(action, frameState.Mouse.Held(btn) ? 1 : 0);
				}
			}

			bool pollForFever = actionFilter == null || actionFilter == InputAction.FeverStart;

			if (pollForFever)
				foreach (var btn in StartFever)
					inputState.TryFever |= frameState.Mouse.Clicked(btn);
		}
	}
}
