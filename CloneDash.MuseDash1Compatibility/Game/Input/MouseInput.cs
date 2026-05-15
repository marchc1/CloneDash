using CloneDash.Settings;
using Nucleus.Common.Input;
using Nucleus.Input;
using Nucleus.Types;

using System.Diagnostics.CodeAnalysis;

namespace CloneDash.Game.Input
{
	public class MouseInput : ICloneDashInputSystem
	{
		public static ButtonCode[] TopButtons;
		public static ButtonCode[] BottomButtons;
		public static ButtonCode[] StartFever;
		public static ButtonCode[] Pause;
		public MouseInput() {
			CD_InputSettings_OnSettingsChanged();
			InputSettings.OnSettingsChanged += CD_InputSettings_OnSettingsChanged;
		}

		[MemberNotNull(nameof(TopButtons), nameof(BottomButtons), nameof(StartFever), nameof(Pause))]
		private void CD_InputSettings_OnSettingsChanged() {
			TopButtons = InputSettings.GetButtonCodesOfAction(InputAction.AirAttack).ToArray();
			BottomButtons = InputSettings.GetButtonCodesOfAction(InputAction.GroundAttack).ToArray();
			StartFever = InputSettings.GetButtonCodesOfAction(InputAction.FeverStart).ToArray();
			Pause = InputSettings.GetButtonCodesOfAction(InputAction.PauseGame).ToArray();
		}

		public void Poll(FrameState frameState, ref InputState inputState, InputAction? actionFilter = null) {
			bool pollForTop = actionFilter == null || actionFilter == InputAction.AirAttack;
			bool pollForBottom = actionFilter == null || actionFilter == InputAction.GroundAttack;
			bool pollForFever = actionFilter == null || actionFilter == InputAction.FeverStart;

			if (pollForTop)
				foreach (var btn in TopButtons) {
					inputState.TopClicked += frameState.Mouse.Clicked(btn) ? 1 : 0;
					inputState.TopHeldCount += frameState.Mouse.Held(btn) ? 1 : 0;
				}

			if (pollForBottom)
				foreach (var btn in BottomButtons) {
					inputState.BottomClicked += frameState.Mouse.Clicked(btn) ? 1 : 0;
					inputState.BottomHeldCount += frameState.Mouse.Held(btn) ? 1 : 0;
				}

			if (pollForFever)
				foreach (var btn in StartFever)
					inputState.TryFever |= frameState.Mouse.Clicked(btn);
		}
	}
}
