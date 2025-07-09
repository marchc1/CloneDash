using CloneDash.Game.Entities;

using Nucleus;

namespace CloneDash.Game.Logic;

public class StackBasedSustainManager : ISustainManager
{
	private Stack<SustainBeam> TopPathway = [];
	private Stack<SustainBeam> BottomPathway = [];

	private Stack<SustainBeam> StackOf(PathwaySide side) => side == PathwaySide.Top ? TopPathway : BottomPathway;
	private Stack<SustainBeam> StackOf(SustainBeam beam) => StackOf(beam.Pathway);

	private void callbackToLevel(SustainBeam beam, int prevCount) {
		var lvl = beam.GetGameLevel();
		var c = StackOf(beam).Count;
		lvl.OnSustainCallback(beam, beam.Pathway, prevCount > 0, c > 0, c);
	}

	public void StartSustainBeam(SustainBeam sustain) {
		var stk = StackOf(sustain);
		var prevCount = StackOf(sustain).Count;
		stk.Push(sustain);
		callbackToLevel(sustain, prevCount);
	}
	public void FailSustainBeam(SustainBeam sustain) {
		if (!sustain.WasHit) return;

		var stk = StackOf(sustain);
		var prevCount = StackOf(sustain).Count;
		stk.Pop();
		callbackToLevel(sustain, prevCount);
	}
	public void CompleteSustainBeam(SustainBeam sustain) {
		if (!sustain.WasHit) return;

		var stk = StackOf(sustain);
		var prevCount = StackOf(sustain).Count;
		stk.Pop();
		callbackToLevel(sustain, prevCount);
	}

	public PathwaySide GetSustainState() {
		bool topC = TopPathway.Count > 0;
		bool bottomC = BottomPathway.Count > 0;

		return topC && bottomC
			? PathwaySide.Both
			: topC
				? PathwaySide.Top
				: bottomC
					? PathwaySide.Bottom
					: PathwaySide.None;
	}

	public IEnumerable<SustainBeam> GetSustainsActive(PathwaySide pathway) {
		switch (pathway) {
			case PathwaySide.Top: foreach (var sustain in TopPathway) yield return sustain; break;
			case PathwaySide.Bottom: foreach (var sustain in BottomPathway) yield return sustain; break;
			case PathwaySide.Both:
				foreach (var sustain in TopPathway) yield return sustain;
				foreach (var sustain in BottomPathway) yield return sustain;
				break;
			default: break;
		}
	}

	public int GetSustainsActiveCount(PathwaySide pathway) {
		switch (pathway) {
			case PathwaySide.Top: return TopPathway.Count;
			case PathwaySide.Bottom: return TopPathway.Count;
			case PathwaySide.Both: return TopPathway.Count + BottomPathway.Count;
			default: return 0;
		}
	}
	public void ThinkSustainBeam(SustainBeam sustain) {
		var stack = StackOf(sustain);
		var lvl = sustain.GetGameLevel();
		var pathwayCheck = lvl.GetPathway(sustain);

		var endTimeDist = sustain.GetJudgementTimeUntilEnd();
		var sustainComplete = pathwayCheck.IsPressed() && endTimeDist <= 0;
		var sustainEarlyButStillSuccess = !pathwayCheck.IsPressed() && NMath.InRange(endTimeDist, -0.05f, 0.05f);
		if (sustainComplete || sustainEarlyButStillSuccess) {
			sustain.Complete();
		}
		// check if pathway being held
		else if (!pathwayCheck.IsPressed()) {
			sustain.Fail();
		}
		else {
			sustain.Hold();
		}
	}

	public int ActiveSustains(PathwaySide side) => side == PathwaySide.Top ? TopPathway.Count : BottomPathway.Count;
}