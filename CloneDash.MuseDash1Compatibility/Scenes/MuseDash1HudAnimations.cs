using Nucleus.Particles;
using Nucleus.Types;

namespace CloneDash.Scenes;

public enum MuseDash1ComboTextState
{
	None,
	In,
	Out
}

public sealed class MuseDash1ComboTextClips(float inLength, ParticleCurve inX, ParticleCurve inY, ParticleCurve standbyX, ParticleCurve standbyY, float outLength, ParticleCurve outX, ParticleCurve outY)
{
	const float STANDBY_LENGTH = 1;

	public readonly float OutLength = outLength;

	public Vector2F Sample(MuseDash1ComboTextState state, float time) {
		if (state == MuseDash1ComboTextState.Out)
			return new Vector2F(outX.Evaluate(time), outY.Evaluate(time));
		if (time < inLength)
			return new Vector2F(inX.Evaluate(time), inY.Evaluate(time));
		float standby = (time - inLength) % STANDBY_LENGTH;
		return new Vector2F(standbyX.Evaluate(standby), standbyY.Evaluate(standby));
	}
}

public static class MuseDash1HudAnimations
{
	const float INF = float.PositiveInfinity;

	public const float BELOW_REST = 540;
	public const float HP_ADD_LENGTH = 0.4f;
	public const float HP_ADD_SCALE = 1.2f;
	public const float HP_FILL_LENGTH = 0.3f;
	public const float HP_TRAIL_DELAY = 0.2f;
	public const float HP_TRAIL_LENGTH = 0.3f;
	public const float HURT_ALPHA = 0.559f;
	public const float HURT_LENGTH = 0.3f;
	public const float PAUSE_ALPHA = 0.3137f;
	public const float SCORE_REST = -10;
	public const float UP_REST = -540;

	public static readonly ParticleCurve BattleInBelow = new([new(0, 300, 0, 0), new(F(230), 300, INF, 1914.53f), new(F(250), 540, -46.343f, 0)]);
	public static readonly ParticleCurve BattleInScore = new([new(0, 130, 0, 0), new(F(230), 130, INF, -1716.013f), new(F(250), -10, 0, 0)]);
	public static readonly ParticleCurve BattleInUp = new([new(0, -270, 0, 0), new(F(230), -270, INF, -2238.062f), new(F(250), -540, 221.766f, 0)]);
	public static readonly ParticleCurve BattleOutBelow = new([new(0, 540, 0, 0), new(F(40), 540, INF, 0), new(F(55), 300, -2731.698f, 0)]);
	public static readonly ParticleCurve BattleOutScore = new([new(0, -10, 0, 0), new(F(40), -10, INF, 0), new(F(55), 130, 1425.139f, 0)]);
	public static readonly ParticleCurve BattleOutUp = new([new(0, -540, 0, 0), new(F(40), -540, INF, -245.738f), new(F(55), -400, 1862.046f, 0)]);

	public static readonly MuseDash1ComboTextClips ComboText = new(
		F(11),
		new([new(0, 0.7f, 0, 18.456f), new(F(3), 1.3f, 0, 0), new(F(7), 0.9f, 0, 0), new(F(11), 1, 0, 0)]),
		new([new(0, 0.7f, 0, 18.401f), new(F(3), 1.3f, 0, 0), new(F(7), 0.9f, 0, 0), new(F(11), 1, 0, 0)]),
		new([new(0, 1, 0, 0), new(F(30), 1, INF, 0), new(F(32), 0.85f, 0, 0), new(F(38), 1.1f, 0, 0), new(F(42), 1, 0, 0), new(F(60), 1, INF, 0)]),
		new([new(0, 1, 0, 0), new(F(30), 1, INF, 0), new(F(32), 1.15f, 0, 0), new(F(38), 0.9f, 0, 0), new(F(42), 1, 0, 0), new(F(60), 1, INF, 0)]),
		F(24),
		new([new(0, 1, 0, 0), new(F(14), 1, INF, 0), new(F(24), 0, -14.69f, 0)]),
		new([new(0, 1, 0, 0), new(F(14), 1, INF, 0), new(F(24), 0, -14.721f, 0)]));

	public static readonly MuseDash1ComboTextClips ComboTextGrooveCoaster = new(
		F(30),
		new([new(0, 0.8f, 0, 7.229f), new(F(3), 1.05f, 0, 0), new(F(10), 0.75f, 0, 0), new(F(15), 0.8f, 0, 0), new(F(30), 0.8f, INF, 0)]),
		new([new(0, 0.8f, 0, 8.03f), new(F(3), 1.05f, 0, 0), new(F(10), 0.75f, 0, 0), new(F(15), 0.8f, 0, 0), new(F(30), 0.8f, INF, 0)]),
		new([new(0, 0.8f, 0, 0.89f), new(F(5), 0.85f, 0, 0), new(F(60), 0.8f, 0, 0)]),
		new([new(0, 0.8f, 0, 0.814f), new(F(5), 0.85f, 0, 0), new(F(60), 0.8f, 0, 0)]),
		F(20),
		new([new(0, 0.8f, 0, 0), new(F(10), 0.8f, INF, 0), new(F(15), 1, 0, 0), new(F(20), 0, -14.69f, 0)]),
		new([new(0, 0.8f, 0, 0), new(F(10), 0.8f, INF, 0), new(F(15), 1, 0, 0), new(F(20), 0, -14.721f, 0)]));

	public static readonly ParticleCurve HpAdd = new([new(0, -0.00108f, 11.05405f, 11.05405f), new(0.16526f, 0.81886f, 1.8882f, 1.8882f), new(0.4f, 1, 0, 0), new(0.65f, 1, 0, 0), new(0.821f, 0.87997f, -1.80295f, -1.80295f), new(1.00072f, -0.00435f, -8.64889f, -8.64889f)]);
	public static readonly ParticleCurve HpTrail = new([new(0, 0, 0, 0), new(1, 1, 2, 2)]);

	public static float SampleHpAdd(float time) => HpAdd.Evaluate(time / HP_ADD_LENGTH * HpAdd.Keys[^1].Time);

	static float F(int frame) => frame / 60f;
}
