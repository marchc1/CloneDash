using Nucleus.Models;
using Nucleus.Types;

namespace Nucleus.ModelEditor;

/// <summary>
/// Keys for draw order, events, constraints and deform from the current pose
/// </summary>
public static class PoseKeys
{
	private const float EPSILON = 1e-4f;

	public static bool Handles(KeyframeProperty property) =>
		property is KeyframeProperty.Model_DrawOrder or KeyframeProperty.Event or KeyframeProperty.Deform
			or KeyframeProperty.Ik_Constraint or KeyframeProperty.Transform_Constraint
			or KeyframeProperty.Path_Position or KeyframeProperty.Path_Spacing or KeyframeProperty.Path_Mix;

	public static EditorTimeline? Find(EditorAnimation animation, IEditorType item, KeyframeProperty property, bool create, out bool created) {
		created = false;
		var timelines = animation.Timelines;
		EditorTimeline? found = (item, property) switch {
			(EditorModel, KeyframeProperty.Model_DrawOrder) => FindTimeline<DrawOrderTimeline>(timelines, null),
			(EditorEvent, KeyframeProperty.Event) => FindTimeline<EventTimeline>(timelines, null),
			(EditorVertexAttachment attachment, KeyframeProperty.Deform) => FindTimeline<DeformTimeline>(timelines, t => t.Attachment == attachment.DeformAttachment && t.Slot == attachment.Slot),
			(EditorIkConstraint constraint, KeyframeProperty.Ik_Constraint) =>
				FindTimeline<IkConstraintTimeline>(timelines, t => t.Constraint == constraint),
			(EditorTransformConstraint constraint, KeyframeProperty.Transform_Constraint) =>
				FindTimeline<TransformConstraintTimeline>(timelines, t => t.Constraint == constraint),
			(EditorPathConstraint constraint, KeyframeProperty.Path_Position) =>
				FindTimeline<PathPositionTimeline>(timelines, t => t.Constraint == constraint),
			(EditorPathConstraint constraint, KeyframeProperty.Path_Spacing) =>
				FindTimeline<PathSpacingTimeline>(timelines, t => t.Constraint == constraint),
			(EditorPathConstraint constraint, KeyframeProperty.Path_Mix) =>
				FindTimeline<PathMixTimeline>(timelines, t => t.Constraint == constraint),
			_ => null
		};
		if (found != null || !create) return found;

		EditorTimeline? made = (item, property) switch {
			(EditorModel, KeyframeProperty.Model_DrawOrder) => new DrawOrderTimeline(),
			(EditorEvent, KeyframeProperty.Event) => new EventTimeline(),
			(EditorVertexAttachment attachment, KeyframeProperty.Deform) => NewDeform(attachment),
			(EditorIkConstraint constraint, KeyframeProperty.Ik_Constraint) =>
				new IkConstraintTimeline { Constraint = constraint },
			(EditorTransformConstraint constraint, KeyframeProperty.Transform_Constraint) =>
				new TransformConstraintTimeline { Constraint = constraint },
			(EditorPathConstraint constraint, KeyframeProperty.Path_Position) =>
				new PathPositionTimeline { Constraint = constraint },
			(EditorPathConstraint constraint, KeyframeProperty.Path_Spacing) =>
				new PathSpacingTimeline { Constraint = constraint },
			(EditorPathConstraint constraint, KeyframeProperty.Path_Mix) =>
				new PathMixTimeline { Constraint = constraint },
			_ => null
		};
		if (made == null) return null;
		timelines.Add(made);
		created = true;
		return made;
	}

	private static T? FindTimeline<T>(List<EditorTimeline> timelines, Func<T, bool>? match) where T : EditorTimeline {
		foreach (var timeline in timelines) {
			if (timeline is T typed && (match == null || match(typed)))
				return typed;
		}
		return null;
	}

	// One pair per vertex, or per vertex and bone when weighted, like the runtime
	private static DeformTimeline NewDeform(EditorVertexAttachment attachment) {
		var source = attachment.DeformAttachment;
		var timeline = new DeformTimeline { Slot = attachment.Slot, Attachment = source };
		foreach (var vertex in source.GetVertices()) {
			if (source.Weights.Count == 0) {
				timeline.Vertices.Add(vertex);
				timeline.Bones.Add(null);
				continue;
			}
			foreach (var weights in source.Weights) {
				if (weights.TryGetVertexWeight(vertex) > 0) {
					timeline.Vertices.Add(vertex);
					timeline.Bones.Add(weights.Bone);
				}
			}
		}
		return timeline;
	}

	private static float[] DeformPose(DeformTimeline timeline) {
		var attachment = timeline.Attachment;
		var result = new float[timeline.Vertices.Count * 2];
		for (int i = 0; i < timeline.Vertices.Count; i++) {
			var bone = timeline.Bones[i];
			Vector2F offset = bone == null ? attachment.CurrentDeform?.GetValueOrDefault(timeline.Vertices[i]) ?? Vector2F.Zero
				: attachment.CurrentBoneDeform?.GetValueOrDefault((timeline.Vertices[i], bone)) ?? Vector2F.Zero;
			result[i * 2] = offset.X;
			result[i * 2 + 1] = offset.Y;
		}
		return result;
	}

	private static Keyframe<float> Key(FCurve<float> curve, double time, float value, bool stepped = false) {
		var keyframe = curve.AddKeyframe(new(time, value));
		keyframe.Interpolation = stepped ? KeyframeInterpolation.Constant : KeyframeInterpolation.Linear;
		return keyframe;
	}

	/// <summary>
	/// null if it can't be keyed that way
	/// </summary>
	public static EditorTimeline? Insert(EditorAnimation animation, IEditorType item, KeyframeProperty property, double time, out bool created) {
		var timeline = Find(animation, item, property, true, out created);
		switch (timeline) {
			case DrawOrderTimeline drawOrder:
				var model = (EditorModel)item;
				drawOrder.Keys.AddKeyframe(new(time, model.DrawOrder.ToList())).Interpolation =
					KeyframeInterpolation.Constant;
				break;
			case EventTimeline events:
				var editorEvent = (EditorEvent)item;
				events.Keys.TryFindKeyframe(time, out var existing);
				// new list, undo still has the old one
				var list = new List<EditorEventKey>();
				if (existing?.Value != null) {
					foreach (var eventKey in existing.Value) {
						if (eventKey.Event != editorEvent)
							list.Add(eventKey);
					}
				}
				list.Add(new EditorEventKey {
					Event = editorEvent,
					Int = editorEvent.CurrentInt,
					Float = editorEvent.CurrentFloat,
					String = editorEvent.CurrentString,
					Volume = editorEvent.CurrentVolume,
					Balance = editorEvent.CurrentBalance
				});
				events.Keys.AddKeyframe(new(time, list)).Interpolation = KeyframeInterpolation.Constant;
				break;
			case DeformTimeline deform:
				var pose = DeformPose(deform);
				if (deform.Curve.TryFindKeyframe(time, out var at)) {
					int index = (int)Math.Round(at.Value);
					if (index >= 0 && index < deform.Offsets.Count)
						deform.Offsets[index] = pose;
				}
				else {
					Key(deform.Curve, time, deform.Offsets.Count);
					deform.Offsets.Add(pose);
					KeyEdit.SyncDeform(deform);
				}
				break;
			case IkConstraintTimeline ikTimeline:
				var ikConstraint = ikTimeline.Constraint;
				Key(ikTimeline.Mix, time, ikConstraint.CurrentMix);
				Key(ikTimeline.Softness, time, ikConstraint.CurrentSoftness);
				Key(ikTimeline.BendDirection, time, ikConstraint.CurrentBendDirection, true);
				Key(ikTimeline.Compress, time, ikConstraint.CurrentCompress ? 1 : 0, true);
				Key(ikTimeline.Stretch, time, ikConstraint.CurrentStretch ? 1 : 0, true);
				break;
			case TransformConstraintTimeline transformTimeline:
				var transformConstraint = transformTimeline.Constraint;
				Key(transformTimeline.RotateMix, time, transformConstraint.CurrentRotateMix);
				Key(transformTimeline.TranslateMix, time, transformConstraint.CurrentTranslateMix);
				Key(transformTimeline.ScaleMix, time, transformConstraint.CurrentScaleMix);
				Key(transformTimeline.ShearMix, time, transformConstraint.CurrentShearMix);
				break;
			case PathPositionTimeline positionTimeline:
				Key(positionTimeline.Position, time, positionTimeline.Constraint.CurrentPosition);
				break;
			case PathSpacingTimeline spacingTimeline:
				Key(spacingTimeline.Spacing, time, spacingTimeline.Constraint.CurrentSpacing);
				break;
			case PathMixTimeline mixTimeline:
				Key(mixTimeline.RotateMix, time, mixTimeline.Constraint.CurrentRotateMix);
				Key(mixTimeline.TranslateMix, time, mixTimeline.Constraint.CurrentTranslateMix);
				break;
		}
		return timeline;
	}

	public static KeyframeState State(EditorAnimation animation, IEditorType item, KeyframeProperty property, double time) {
		var timeline = Find(animation, item, property, false, out _);
		bool keyed = timeline switch {
			EventTimeline events =>
				events.Keys.TryFindKeyframe(time, out var key) && (key.Value?.Any(x => x.Event == item) ?? false),
			null => false,
			_ => timeline.KeyframedAt(time) != KeyframeState.NotKeyframed
		};
		if (Changed(timeline, item, property, time)) return KeyframeState.PendingKeyframe;
		return keyed ? KeyframeState.Keyframed : KeyframeState.NotKeyframed;
	}

	private static bool Differs(float a, float b) => Math.Abs(a - b) > EPSILON;
	private static float ValueAt(FCurve<float>? curve, double time, float setup) =>
		curve == null || curve.Count == 0 ? setup : curve.DetermineValueAtTime(time);

	// Is the pose different from what the animation gives at this time
	private static bool Changed(EditorTimeline? timeline, IEditorType item, KeyframeProperty property, double time) {
		switch (item) {
			case EditorModel model when property == KeyframeProperty.Model_DrawOrder:
				var keys = (timeline as DrawOrderTimeline)?.Keys;
				List<EditorSlot>? expected = keys != null && keys.Count > 0 && time >= keys.Keyframes[0].Time
					? keys.DetermineValueAtTime(time, KeyframeInterpolation.Constant) : null;
				return !model.DrawOrder.SequenceEqual(expected ?? model.Slots);
			case EditorEvent editorEvent:
				EditorEventKey? key = null;
				if (timeline is EventTimeline events && events.Keys.TryFindKeyframe(time, out var keyframe))
					key = keyframe.Value?.FirstOrDefault(x => x.Event == editorEvent);
				return key == null ? editorEvent.CurrentInt != editorEvent.Int || Differs(editorEvent.CurrentFloat, editorEvent.Float)
						|| editorEvent.CurrentString != editorEvent.String || Differs(editorEvent.CurrentVolume, editorEvent.Volume)
						|| Differs(editorEvent.CurrentBalance, editorEvent.Balance) : editorEvent.CurrentInt != key.Int
						|| Differs(editorEvent.CurrentFloat, key.Float) || editorEvent.CurrentString != key.String
						|| Differs(editorEvent.CurrentVolume, key.Volume) || Differs(editorEvent.CurrentBalance, key.Balance);
			case EditorVertexAttachment attachment:
				if (timeline is DeformTimeline deform) {
					var animated = deform.Evaluate(time);
					var pose = DeformPose(deform);
					for (int i = 0; i < pose.Length; i++) {
						if (Differs(pose[i], animated != null && i < animated.Length ? animated[i] : 0)) return true;
					}
					return false;
				}
				var source = attachment.DeformAttachment;
				return (source.CurrentDeform?.Values.Any(v => Differs(v.X, 0) || Differs(v.Y, 0)) ?? false)
					|| (source.CurrentBoneDeform?.Values.Any(v => Differs(v.X, 0) || Differs(v.Y, 0)) ?? false);
			case EditorIkConstraint constraint:
				var ik = timeline as IkConstraintTimeline;
				return Differs(constraint.CurrentMix, ValueAt(ik?.Mix, time, constraint.Mix))
					|| Differs(constraint.CurrentSoftness, ValueAt(ik?.Softness, time, constraint.Softness))
					|| constraint.CurrentBendDirection != (int)ValueAt(ik?.BendDirection, time, constraint.BendDirection)
					|| constraint.CurrentCompress != (ValueAt(ik?.Compress, time, constraint.Compress ? 1 : 0) != 0)
					|| constraint.CurrentStretch != (ValueAt(ik?.Stretch, time, constraint.Stretch ? 1 : 0) != 0);
			case EditorTransformConstraint constraint:
				var transform = timeline as TransformConstraintTimeline;
				return Differs(constraint.CurrentRotateMix, ValueAt(transform?.RotateMix, time, constraint.RotateMix))
					|| Differs(constraint.CurrentTranslateMix, ValueAt(transform?.TranslateMix, time, constraint.TranslateMix))
					|| Differs(constraint.CurrentScaleMix, ValueAt(transform?.ScaleMix, time, constraint.ScaleMix))
					|| Differs(constraint.CurrentShearMix, ValueAt(transform?.ShearMix, time, constraint.ShearMix));
			case EditorPathConstraint constraint:
				var mix = timeline as PathMixTimeline;
				return property switch {
					KeyframeProperty.Path_Position => Differs(constraint.CurrentPosition, ValueAt((timeline as PathPositionTimeline)?.Position, time, constraint.Position)),
					KeyframeProperty.Path_Spacing => Differs(constraint.CurrentSpacing, ValueAt((timeline as PathSpacingTimeline)?.Spacing, time, constraint.Spacing)),
					_ => Differs(constraint.CurrentRotateMix, ValueAt(mix?.RotateMix, time, constraint.RotateMix))
						|| Differs(constraint.CurrentTranslateMix, ValueAt(mix?.TranslateMix, time, constraint.TranslateMix))
				};
		}
		return false;
	}
}
