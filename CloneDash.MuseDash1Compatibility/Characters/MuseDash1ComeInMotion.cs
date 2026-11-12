using AssetStudio;
using CloneDash.Compatibility.MuseDash;
using CloneDash.Compatibility.Unity;

namespace CloneDash.Characters;

public class MuseDash1ComeInMotion
{
	readonly List<(float time, float value, float inSlope, float outSlope)> x;

	MuseDash1ComeInMotion(List<(float time, float value, float inSlope, float outSlope)> x) {
		this.x = x;
	}

	public float GetOffset(double time) => DecodedClip.Eval(x, (float)time) - x[^1].value;

	public static MuseDash1ComeInMotion? From(AnimatorController controller) {
		foreach (var (name, clip) in MuseDash1GhostMotion.DecodeStates(controller))
			if (name == ActionKeys.COMEIN && MuseDash1GhostMotion.FindPosition(clip, 0) is { Count: > 0 } x)
				return new(x);

		return null;
	}
}