using CloneDash.Common.Gamemodes.MuseDash;
using CloneDash.Game.Entities;

using Nucleus;

namespace CloneDash.Game.Logic;

public class StackBasedSustainManager : ISustainManager
{
	private readonly Stack<SustainBeam>[] stacks = CreatePathwayStacks();

	public static Stack<SustainBeam>[] CreatePathwayStacks() {
		var ret = new Stack<SustainBeam>[PathwayLayout.MAX_PATHWAYS];
		for (int i = 0; i < ret.Length; i++)
			ret[i] = new();
		return ret;
	}

	private Stack<SustainBeam> StackOf(SustainBeam beam) => stacks[beam.GetGameLevel().PrimaryPathway(beam.Pathway).Index];

	private void callbackToLevel(SustainBeam beam, int prevCount) {
		var lvl = beam.GetGameLevel();
		var c = StackOf(beam).Count;
		lvl.OnSustainCallback(beam, lvl.PrimaryPathway(beam.Pathway), prevCount > 0, c > 0, c);
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

	public PathwaySet GetSustainState() {
		uint bits = 0;
		for (int i = 0; i < stacks.Length; i++)
			if (stacks[i].Count > 0)
				bits |= 1u << i;
		return PathwaySet.FromBits(bits);
	}

	public int GetSustainsActiveCount(PathwayId pathway) => stacks[pathway.Index].Count;
	public SustainBeam? GetActiveSustain(PathwayId pathway) => stacks[pathway.Index].TryPeek(out var beam) ? beam : null;

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
		else if (!pathwayCheck.IsPressed() && lvl.NeedsToHoldSustains()) {
			sustain.Fail();
		}
		else {
			sustain.Hold();
		}
	}

	public int ActiveSustains() {
		int count = 0;
		for (int i = 0; i < stacks.Length; i++)
			count += stacks[i].Count;
		return count;
	}

	public void Reset() {
		foreach (var stack in stacks)
			stack.Clear();
	}
}
