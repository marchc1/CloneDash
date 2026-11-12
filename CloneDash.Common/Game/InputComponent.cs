using CloneDash.Game.Input;
using Nucleus;
using Nucleus.Engine;
using Nucleus.Types;

namespace CloneDash.Common.Game;

public class InputComponent(Level level) : IRhythmGameThinker
{
	public KeyBindSet? Binds;
	public Func<bool> CanPoll = () => true;
	public readonly List<IInputReceiver> Receivers = [];

	// Player input system
	InputState state;

	public RhythmGameThinkPhase Phase => RhythmGameThinkPhase.Input;

	public ref readonly InputState State => ref state;

	public void Initialize() {
		Receivers.Add(new BindInput(this));
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
					receiver.Poll(frameState, ref state, CommonActions.Pause);
		}

		else if (!IValidatable.IsValid(level.RootPanel.GetKeyboardFocusedElement())) {
			foreach (var receiver in Receivers)
				receiver.Poll(frameState, ref state);
		}
	}

	public void Reset() {
		state.Reset();
	}

	public void Think(FrameState frameState) {
		Reset();
		if (CanPoll())
			Poll(frameState);
	}
}
