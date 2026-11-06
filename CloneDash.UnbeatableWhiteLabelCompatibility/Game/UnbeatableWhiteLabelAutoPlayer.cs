using CloneDash.Common.Gamemodes.MuseDash;
using CloneDash.Game.Input;
using CloneDash.Settings;
using Nucleus.Types;

namespace CloneDash.Compatibility.UnbeatableWhiteLabel;

public class UnbeatableWhiteLabelAutoPlayer(UnbeatableWhiteLabelChartComponent chart, UnbeatableWhiteLabelJudgeComponent judge, UnbeatableWhiteLabelPlayerComponent player) : IInputReceiver
{
	const double DODGE_LEAD = 0.04;

	public bool Enabled;

	public bool ClaimsGameplayInput => Enabled;

	public void Poll(FrameState frameState, ref InputState inputState, InputAction? actionFilter = null) {
		if (!Enabled)
			return;

		double position = judge.Position;
		bool top = false, low = false;

		foreach (var note in judge.Upcoming.Values) {
			if (note.GetAutoplayTime() is not double time || position < time)
				continue;

			if (note.CurrentPathway == UnbeatableWhiteLabelPathways.Top)
				top = true;
			else
				low = true;
		}

		if (GetNextDodge(UnbeatableWhiteLabelPathways.Low) is { } dodgeLow && position >= GetDodgeTime(dodgeLow, UnbeatableWhiteLabelPathways.Top))
			top = true;
		if (player.Height == UnbeatableWhiteLabelPathways.Top && GetNextDodge(UnbeatableWhiteLabelPathways.Top) is { } dodgeTop && position >= GetDodgeTime(dodgeTop, UnbeatableWhiteLabelPathways.Low))
			low = true;

		if (top)
			inputState.Click(UnbeatableWhiteLabelActions.Top);
		if (low)
			inputState.Click(UnbeatableWhiteLabelActions.Low);

		if (top || IsHolding(UnbeatableWhiteLabelPathways.Top, position))
			inputState.Hold(UnbeatableWhiteLabelActions.Top);
		if (low || IsHolding(UnbeatableWhiteLabelPathways.Low, position))
			inputState.Hold(UnbeatableWhiteLabelActions.Low);
	}

	double GetDodgeTime(UnbeatableWhiteLabelDodgeNote dodge, PathwayId pressPathway) {
		double time = dodge.TargetTime - DODGE_LEAD;
		double deadline = dodge.TargetTime + UnbeatableWhiteLabelJudgement.LENIENCY - DODGE_LEAD;

		foreach (var note in judge.Upcoming.Values) {
			if (note.GetAutoplayTime() is not double noteTime || noteTime <= time || noteTime > deadline)
				continue;
			if (note.CurrentPathway == UnbeatableWhiteLabelPathways.Mid || note.CurrentPathway == pressPathway)
				return noteTime;
		}

		return time;
	}

	UnbeatableWhiteLabelDodgeNote? GetNextDodge(PathwayId pathway) {
		foreach (var note in chart.Active)
			if (note is UnbeatableWhiteLabelDodgeNote { IsPast: false, Destroyed: false } dodge && dodge.CurrentPathway == pathway)
				return dodge;
		return null;
	}

	bool IsHolding(PathwayId pathway, double position) => IsHolding(player.HeldNote, pathway, position) || IsHolding(player.SecondHeldNote, pathway, position);

	static bool IsHolding(UnbeatableWhiteLabelHoldNote? note, PathwayId pathway, double position) => note != null && !note.Dead && note.CurrentPathway == pathway && position < note.EndTime;
}
