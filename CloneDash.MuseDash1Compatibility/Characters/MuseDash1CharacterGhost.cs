using AssetStudio;
using CloneDash.Compatibility.MuseDash;
using CloneDash.Compatibility.Unity;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace CloneDash.Characters;

public readonly record struct MuseDash1GhostMaterial(Vector4 Color, Vector4 Tint, Vector3 AddColor, float Alpha, float Size)
{
	public static readonly MuseDash1GhostMaterial Default = new(Vector4.One, Vector4.One, new(0, 0.5f, 1), 0, 0.001f);

	public static MuseDash1GhostMaterial From(Material material) {
		var properties = material.m_SavedProperties;

		float getFloat(string name, float fallback) {
			foreach (var pair in properties.m_Floats)
				if (pair.Key == name)
					return pair.Value;
			return fallback;
		}

		Vector4 getColor(string name, Vector4 fallback) {
			foreach (var pair in properties.m_Colors)
				if (pair.Key == name)
					return new(pair.Value.R, pair.Value.G, pair.Value.B, pair.Value.A);
			return fallback;
		}

		return new(
			getColor("_Color", Default.Color),
			getColor("_ColorX", Default.Tint),
			new(getFloat("_ColorR", Default.AddColor.X), getFloat("_ColorG", Default.AddColor.Y), getFloat("_ColorB", Default.AddColor.Z)),
			getFloat("_Alpha", Default.Alpha),
			getFloat("_Size", Default.Size)
		);
	}
}

public class MuseDash1GhostMotion
{
	readonly record struct State(List<(float time, float value, float inSlope, float outSlope)> Height, float StopTime);

	readonly float air;
	readonly float ground;
	readonly Dictionary<string, State> states = [];

	public MuseDash1GhostMotion(AnimatorController controller) {
		var names = new Dictionary<uint, string>();
		foreach (var pair in controller.m_TOS)
			names[pair.Key] = pair.Value;

		foreach (var stateMachine in controller.m_ControllerConstant.m_StateMachineArray)
			foreach (var state in stateMachine.m_StateConstantArray) {
				if (!names.TryGetValue(state.m_NameID, out var name) || state.m_BlendTreeConstantArray.Length == 0 || state.m_BlendTreeConstantArray[0].m_NodeArray.Length == 0)
					continue;

				var clipID = (int)state.m_BlendTreeConstantArray[0].m_NodeArray[0].m_ClipID;
				if (clipID >= controller.m_AnimationClips.Count || !controller.m_AnimationClips[clipID].TryGet(out var clip))
					continue;

				var decoded = DecodedClip.Decode(clip);
				var height = FindHeight(decoded);
				if (height != null)
					states[name] = new(height, decoded.StopTime);
			}

		ground = states.TryGetValue(ActionKeys.RUN, out var run) ? DecodedClip.Eval(run.Height, 0) : 0;
		air = states.TryGetValue(ActionKeys.JUMP_ATTACK_UP, out var upHit) ? upHit.Height.Max(x => x.value) : ground + 1;
	}

	public float GetHeight(string? action, double time) {
		if (action == null || !states.TryGetValue(action, out var state) || time > state.StopTime)
			return 0;

		return (DecodedClip.Eval(state.Height, (float)time) - ground) / (air - ground);
	}

	static List<(float time, float value, float inSlope, float outSlope)>? FindHeight(DecodedClip clip) {
		int component = 0;
		foreach (var curve in clip.Curves) {
			if (curve.Binding.typeID != ClassIDType.Transform || curve.Binding.attribute != 1 || curve.Binding.path != 0)
				continue;

			if (component++ == 1)
				return curve.Keys;
		}

		return null;
	}
}
