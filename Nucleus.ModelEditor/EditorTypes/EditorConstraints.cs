using Newtonsoft.Json;

using Nucleus.Common.Types;
using Nucleus.Models;
using Nucleus.Models.Runtime;
using Nucleus.Types;

namespace Nucleus.ModelEditor;

// Data for constraints, events and path attachments
// behavior is in EditorConstraints.Behavior.cs

public abstract partial class EditorConstraint : IEditorType, IModel4Nameable
{
	public EditorModel Model { get; set; } = null!;
	public EditorModel GetModel() => Model;
	public string Name { get; set; } = "";
	public string GetName() => Name;
	/// <summary>
	/// Shared by every constraint type, lowest runs first
	/// </summary>
	public int Order { get; set; }
	public bool SkinRequired { get; set; }
	public List<EditorBone> Bones { get; set; } = [];

	[JsonIgnore] public bool Hovered { get; set; }
	[JsonIgnore] public bool Selected { get; set; }
	[JsonIgnore] public bool Hidden { get; set; }

	[JsonIgnore] public abstract string SingleName { get; }
	[JsonIgnore] public abstract string PluralName { get; }

	public abstract void ResetToSetupPose();
}

public partial class EditorIkConstraint : EditorConstraint
{
	public override string SingleName => "IK constraint";
	public override string PluralName => "IK constraints";

	public EditorBone Target { get; set; } = null!;
	public int BendDirection { get; set; } = 1;
	public bool Compress { get; set; }
	public bool Stretch { get; set; }
	public bool Uniform { get; set; }
	public float Mix { get; set; } = 1;
	public float Softness { get; set; }

	// animated values
	[JsonIgnore] public float CurrentMix;
	[JsonIgnore] public float CurrentSoftness;
	[JsonIgnore] public int CurrentBendDirection = 1;
	[JsonIgnore] public bool CurrentCompress;
	[JsonIgnore] public bool CurrentStretch;

	public override void ResetToSetupPose() {
		CurrentMix = Mix;
		CurrentSoftness = Softness;
		CurrentBendDirection = BendDirection;
		CurrentCompress = Compress;
		CurrentStretch = Stretch;
	}
}

public partial class EditorTransformConstraint : EditorConstraint
{
	public override string SingleName => "transform constraint";
	public override string PluralName => "transform constraints";

	public EditorBone Target { get; set; } = null!;
	public bool Local { get; set; }
	public bool Relative { get; set; }
	public float OffsetRotation { get; set; }
	public float OffsetX { get; set; }
	public float OffsetY { get; set; }
	public float OffsetScaleX { get; set; }
	public float OffsetScaleY { get; set; }
	public float OffsetShearY { get; set; }
	public float RotateMix { get; set; } = 1;
	public float TranslateMix { get; set; } = 1;
	public float ScaleMix { get; set; } = 1;
	public float ShearMix { get; set; } = 1;

	[JsonIgnore] public float CurrentRotateMix;
	[JsonIgnore] public float CurrentTranslateMix;
	[JsonIgnore] public float CurrentScaleMix;
	[JsonIgnore] public float CurrentShearMix;

	public override void ResetToSetupPose() {
		CurrentRotateMix = RotateMix;
		CurrentTranslateMix = TranslateMix;
		CurrentScaleMix = ScaleMix;
		CurrentShearMix = ShearMix;
	}
}

public partial class EditorPathConstraint : EditorConstraint
{
	public override string SingleName => "path constraint";
	public override string PluralName => "path constraints";

	/// <summary>
	/// Slot whose active attachment is the path
	/// </summary>
	public EditorSlot Target { get; set; } = null!;
	public PositionMode PositionMode { get; set; } = PositionMode.Percent;
	public SpacingMode SpacingMode { get; set; } = SpacingMode.Length;
	public RotateMode RotateMode { get; set; } = RotateMode.Tangent;
	public float OffsetRotation { get; set; }
	public float Position { get; set; }
	public float Spacing { get; set; }
	public float RotateMix { get; set; } = 1;
	public float TranslateMix { get; set; } = 1;

	[JsonIgnore] public float CurrentPosition;
	[JsonIgnore] public float CurrentSpacing;
	[JsonIgnore] public float CurrentRotateMix;
	[JsonIgnore] public float CurrentTranslateMix;

	public override void ResetToSetupPose() {
		CurrentPosition = Position;
		CurrentSpacing = Spacing;
		CurrentRotateMix = RotateMix;
		CurrentTranslateMix = TranslateMix;
	}
}

public partial class EditorEvent : IEditorType, IModel4Nameable
{
	public EditorModel Model { get; set; } = null!;
	public EditorModel GetModel() => Model;
	public string Name { get; set; } = "";
	public string GetName() => Name;

	public int Int { get; set; }
	public float Float { get; set; }
	public string? String { get; set; }
	public string? AudioPath { get; set; }
	public float Volume { get; set; } = 1;
	public float Balance { get; set; }

	[JsonIgnore] public bool Hovered { get; set; }
	[JsonIgnore] public bool Selected { get; set; }
	[JsonIgnore] public bool Hidden { get; set; }

	[JsonIgnore] public string SingleName => "event";
	[JsonIgnore] public string PluralName => "events";

	// Values in animate mode, keyed with the event
	[JsonIgnore] public int CurrentInt;
	[JsonIgnore] public float CurrentFloat;
	[JsonIgnore] public string? CurrentString;
	[JsonIgnore] public float CurrentVolume = 1;
	[JsonIgnore] public float CurrentBalance;

	public void ResetToSetupPose() {
		CurrentInt = Int;
		CurrentFloat = Float;
		CurrentString = String;
		CurrentVolume = Volume;
		CurrentBalance = Balance;
	}
}

public class EditorEventKey
{
	public EditorEvent Event = null!;
	public int Int;
	public float Float;
	public string? String;
	public float Volume = 1;
	public float Balance;
}

/// <summary>
/// Path constraint spline, vertices go in triplets: control in, anchor, control out
/// </summary>
public partial class EditorPathAttachment : EditorVertexAttachment
{
	public override string SingleName => "path";
	public override string PluralName => "paths";
	public override string EditorIcon => "models/path.png";

	public bool Closed { get; set; }
	public bool ConstantSpeed { get; set; } = true;
	/// <summary>
	/// Length at the end of each curve, in setup pose
	/// </summary>
	public List<float> Lengths { get; set; } = [];
}

// Timelines

public abstract class ConstraintCurveTimeline<TConstraint> : CurveTimeline where TConstraint : EditorConstraint
{
	public TConstraint Constraint { get; set; } = null!;

	protected abstract FCurve<float>[] AllCurves { get; }
	public override IFCurve[] KeyCurves => AllCurves;

	public override double CalculateMaxTime() => AllCurves[0].Last?.Time ?? 0;
	public override KeyframeState KeyframedAt(double time) => AllCurves[0].TryFindKeyframe(time, out _) ? KeyframeState.Keyframed : KeyframeState.NotKeyframed;

	public override IEnumerable<TimelineKeyframePairs> GetKeyframes() {
		var curves = AllCurves;
		for (int i = 0; i < curves[0].Count; i++) {
			var keyframes = new IKeyframe[curves.Length];
			for (int j = 0; j < curves.Length; j++)
				keyframes[j] = (IKeyframe)curves[j][i];
			yield return new(this, curves, keyframes);
		}
	}

	public override IEnumerable<double> GetKeyframeTimes() {
		foreach (var keyframe in AllCurves[0].GetKeyframes())
			yield return keyframe.Time;
	}

	protected static float ValueAt(FCurve<float> curve, double time, float setup) => curve.Count == 0 ? setup : curve.DetermineValueAtTime(time);
}

/// <summary>
/// Bend (-1/1), compress (0/1) and stretch (0/1) are stepped
/// </summary>
public class IkConstraintTimeline : ConstraintCurveTimeline<EditorIkConstraint>
{
	public static readonly Color TIMELINE_COLOR_IK = new(255, 170, 60);
	public override Color Color => TIMELINE_COLOR_IK;

	public FCurve<float> Mix = new();
	public FCurve<float> Softness = new();
	public FCurve<float> BendDirection = new();
	public FCurve<float> Compress = new();
	public FCurve<float> Stretch = new();

	protected override FCurve<float>[] AllCurves => [Mix, Softness, BendDirection, Compress, Stretch];

	public override void Apply(EditorModel model, double time) {
		var constraint = Constraint;
		constraint.CurrentMix = ValueAt(Mix, time, constraint.Mix);
		constraint.CurrentSoftness = ValueAt(Softness, time, constraint.Softness);
		constraint.CurrentBendDirection = (int)ValueAt(BendDirection, time, constraint.BendDirection);
		constraint.CurrentCompress = ValueAt(Compress, time, constraint.Compress ? 1 : 0) != 0;
		constraint.CurrentStretch = ValueAt(Stretch, time, constraint.Stretch ? 1 : 0) != 0;
	}
}

public class TransformConstraintTimeline : ConstraintCurveTimeline<EditorTransformConstraint>
{
	public static readonly Color TIMELINE_COLOR_TRANSFORM = new(110, 200, 255);
	public override Color Color => TIMELINE_COLOR_TRANSFORM;

	public FCurve<float> RotateMix = new();
	public FCurve<float> TranslateMix = new();
	public FCurve<float> ScaleMix = new();
	public FCurve<float> ShearMix = new();

	protected override FCurve<float>[] AllCurves => [RotateMix, TranslateMix, ScaleMix, ShearMix];

	public override void Apply(EditorModel model, double time) {
		var constraint = Constraint;
		constraint.CurrentRotateMix = ValueAt(RotateMix, time, constraint.RotateMix);
		constraint.CurrentTranslateMix = ValueAt(TranslateMix, time, constraint.TranslateMix);
		constraint.CurrentScaleMix = ValueAt(ScaleMix, time, constraint.ScaleMix);
		constraint.CurrentShearMix = ValueAt(ShearMix, time, constraint.ShearMix);
	}
}

public class PathPositionTimeline : ConstraintCurveTimeline<EditorPathConstraint>
{
	public static readonly Color TIMELINE_COLOR_PATH = new(255, 128, 0);
	public override Color Color => TIMELINE_COLOR_PATH;

	public FCurve<float> Position = new();
	protected override FCurve<float>[] AllCurves => [Position];

	public override void Apply(EditorModel model, double time) => Constraint.CurrentPosition = ValueAt(Position, time, Constraint.Position);
}

public class PathSpacingTimeline : ConstraintCurveTimeline<EditorPathConstraint>
{
	public override Color Color => PathPositionTimeline.TIMELINE_COLOR_PATH;

	public FCurve<float> Spacing = new();
	protected override FCurve<float>[] AllCurves => [Spacing];

	public override void Apply(EditorModel model, double time) => Constraint.CurrentSpacing = ValueAt(Spacing, time, Constraint.Spacing);
}

public class PathMixTimeline : ConstraintCurveTimeline<EditorPathConstraint>
{
	public override Color Color => PathPositionTimeline.TIMELINE_COLOR_PATH;

	public FCurve<float> RotateMix = new();
	public FCurve<float> TranslateMix = new();
	protected override FCurve<float>[] AllCurves => [RotateMix, TranslateMix];

	public override void Apply(EditorModel model, double time) {
		Constraint.CurrentRotateMix = ValueAt(RotateMix, time, Constraint.RotateMix);
		Constraint.CurrentTranslateMix = ValueAt(TranslateMix, time, Constraint.TranslateMix);
	}
}

/// <summary>
/// Vertex offsets over time for one attachment.
/// Key k of <see cref="Curve"/> has the value k, keep <see cref="Offsets"/> in key order.
/// </summary>
public class DeformTimeline : CurveTimeline, ISlotTimeline
{
	public static readonly Color TIMELINE_COLOR_DEFORM = new(200, 120, 255);
	public override Color Color => TIMELINE_COLOR_DEFORM;

	public EditorSlot Slot { get; set; } = null!;
	public EditorVertexAttachment Attachment { get; set; } = null!;
	public List<EditorVertex> Vertices { get; set; } = [];
	public List<EditorBone?> Bones { get; set; } = [];
	public FCurve<float> Curve = new();
	public List<float[]> Offsets { get; set; } = [];

	public float[]? Evaluate(double time) {
		int count = Curve.Count;
		if (count == 0 || Offsets.Count != count) return null;
		if (time <= Curve.Keyframes[0].Time) return Offsets[0];
		if (time >= Curve.Keyframes[Curve.Keyframes.Count - 1].Time) return Offsets[Offsets.Count - 1];

		int index = Curve.DetermineIndexAtTime(time);
		float percent = Curve.DetermineValueAtTime(time, index) - index;
		float[] before = Offsets[index];
		float[] after = Offsets[Math.Min(index + 1, count - 1)];
		float[] result = new float[before.Length];
		for (int i = 0; i < before.Length; i++)
			result[i] = before[i] + (after[i] - before[i]) * percent;
		return result;
	}

	public override void Apply(EditorModel model, double time) {
		var offsets = Evaluate(time);
		if (offsets == null) return;
		var deform = new Dictionary<EditorVertex, Vector2F>();
		var boneDeform = new Dictionary<(EditorVertex, EditorBone), Vector2F>();
		for (int i = 0; i < Vertices.Count && i * 2 + 1 < offsets.Length; i++) {
			var bone = i < Bones.Count ? Bones[i] : null;
			Vector2F offset = new(offsets[i * 2], offsets[i * 2 + 1]);
			if (bone == null)
				deform[Vertices[i]] = offset;
			else
				boneDeform[(Vertices[i], bone)] = offset;
		}
		Attachment.CurrentDeform = deform;
		Attachment.CurrentBoneDeform = boneDeform;
	}

	public override double CalculateMaxTime() => Curve.Last?.Time ?? 0;
	public override KeyframeState KeyframedAt(double time) => Curve.TryFindKeyframe(time, out _) ? KeyframeState.Keyframed : KeyframeState.NotKeyframed;

	public override IEnumerable<TimelineKeyframePairs> GetKeyframes() {
		for (int i = 0; i < Curve.Count; i++)
			yield return new(this, [Curve], [Curve[i]]);
	}

	public override IEnumerable<double> GetKeyframeTimes() {
		foreach (var keyframe in Curve.GetKeyframes())
			yield return keyframe.Time;
	}
}

/// <summary>
/// Draw order keys, each holding the full slot order (null for setup)
/// </summary>
public class DrawOrderTimeline : CurveTimeline
{
	public static readonly Color TIMELINE_COLOR_DRAW_ORDER = new(120, 190, 220);
	public override Color Color => TIMELINE_COLOR_DRAW_ORDER;

	public FCurve<List<EditorSlot>?> Keys = new();

	public override void Apply(EditorModel model, double time) {
		if (Keys.Count == 0 || time < Keys.Keyframes[0].Time) return;
		model.AnimatedDrawOrder = Keys.DetermineValueAtTime(time, KeyframeInterpolation.Constant);
	}

	public override double CalculateMaxTime() => Keys.Last?.Time ?? 0;
	public override KeyframeState KeyframedAt(double time) => Keys.TryFindKeyframe(time, out _) ? KeyframeState.Keyframed : KeyframeState.NotKeyframed;

	public override IEnumerable<TimelineKeyframePairs> GetKeyframes() {
		for (int i = 0; i < Keys.Count; i++)
			yield return new(this, [Keys], [Keys[i]]);
	}

	public override IEnumerable<double> GetKeyframeTimes() {
		foreach (var keyframe in Keys.GetKeyframes())
			yield return keyframe.Time;
	}
}

/// <summary>
/// Stepped event keys, one keyframe per time holding every event fired then
/// </summary>
public class EventTimeline : CurveTimeline
{
	public static readonly Color TIMELINE_COLOR_EVENT = new(255, 230, 90);
	public override Color Color => TIMELINE_COLOR_EVENT;

	public FCurve<List<EditorEventKey>> Keys = new();

	// On a key's frame its events show that key's values
	public override void Apply(EditorModel model, double time) {
		if (!Keys.TryFindKeyframe(time, out var key) || key.Value == null) return;
		foreach (var eventKey in key.Value) {
			eventKey.Event.CurrentInt = eventKey.Int;
			eventKey.Event.CurrentFloat = eventKey.Float;
			eventKey.Event.CurrentString = eventKey.String;
			eventKey.Event.CurrentVolume = eventKey.Volume;
			eventKey.Event.CurrentBalance = eventKey.Balance;
		}
	}

	public override double CalculateMaxTime() => Keys.Last?.Time ?? 0;
	public override KeyframeState KeyframedAt(double time) => Keys.TryFindKeyframe(time, out _) ? KeyframeState.Keyframed : KeyframeState.NotKeyframed;

	public override IEnumerable<TimelineKeyframePairs> GetKeyframes() {
		for (int i = 0; i < Keys.Count; i++)
			yield return new(this, [Keys], [Keys[i]]);
	}

	public override IEnumerable<double> GetKeyframeTimes() {
		foreach (var keyframe in Keys.GetKeyframes())
			yield return keyframe.Time;
	}
}
