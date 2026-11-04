using CloneDash.Game.Input;
using CloneDash.Settings;
using Nucleus;
using Nucleus.Engine;
using Nucleus.Types;

namespace CloneDash.Common.Game;

public class InputComponent(Level level)
{
	public readonly List<IInputReceiver> Receivers = [];

	// Player input system
	InputState state;

	public ref readonly InputState State => ref state;

	public void Initialize() {
		Receivers.AddRange(ICloneDashInputSystem.InstantiateAllInputSystems());
	}

	public void Poll(FrameState frameState) {
		IInputReceiver? claimant = null;
		foreach (var receiver in Receivers) {
			if (receiver.ClaimsGameplayInput) {
				claimant = receiver;
				break;
			}
		}

		if (claimant != null) {
			claimant.Poll(frameState, ref state);
			foreach (var receiver in Receivers)
				if (receiver != claimant)
					receiver.Poll(frameState, ref state, InputAction.PauseGame);
		}

		else if (!IValidatable.IsValid(level.RootPanel.GetKeyboardFocusedElement())) {
			foreach (var receiver in Receivers)
				receiver.Poll(frameState, ref state);
		}
	}

	public void Reset() {
		state.Reset();
	}
}
