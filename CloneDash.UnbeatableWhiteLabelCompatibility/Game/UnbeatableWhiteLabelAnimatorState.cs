using CloneDash.Compatibility.Unity;

namespace CloneDash.Compatibility.UnbeatableWhiteLabel;

public sealed class UnbeatableWhiteLabelAnimatorState(SceneAnimator? animator)
{
	public string? State { get; private set; }

	public void Play(string state, bool restart = false) {
		if (animator == null || (!restart && state == State))
			return;

		State = state;
		animator.Play(state);
	}

	public void Reset() {
		State = null;
		animator?.Rebind();
	}
}
