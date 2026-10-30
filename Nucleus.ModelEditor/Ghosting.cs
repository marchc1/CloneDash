using Nucleus.Common.Types;
using Nucleus.Models;
using Nucleus.Types;

using Raylib_cs;

namespace Nucleus.ModelEditor;

/// <summary>
/// Ghosting (onion skin) settings, in timeline frames
/// </summary>
public class GhostSettings
{
	public bool FramesBefore, FramesAfter;
	public float BeforeFrames = 6;
	public float AfterFrames = 6;
	public float FrameStep = 3;
	public bool KeysBefore, KeysAfter;
	public float KeysBeforeFrames = 6;
	public float KeysAfterFrames = 6;
	public float KeyStep = 1;
	public bool Current;
	public Color BeforeColor = new(255, 90, 90), AfterColor = new(90, 170, 255);
	public bool Anchor;
	public bool OnTop;
	public bool Loop = true;
	/// <summary>
	/// Pixels per frame
	/// </summary>
	public float OffsetX, OffsetY;
	/// <summary>
	/// Only the selected bones' attachments
	/// </summary>
	public bool SelectedBones;

	public bool Enabled => FramesBefore || FramesAfter || KeysBefore || KeysAfter || Current;
}

/// <summary>
/// Draws skeleton poses from other timeline positions
/// </summary>
public static class Ghosting
{
	public static GhostSettings Settings { get; } = new();
	public static IReadOnlyList<double> LastFrames => lastFrames;

	private const float GHOST_ALPHA = 0.45f;
	private static readonly List<double> lastFrames = [];
	static readonly List<(double Frame, bool After)> ghosts = [];

	static TimelineManager timeline => ModelEditor.Active.File.Timeline;

	// Frames to ghost, relative to the current one
	private static void Collect(EditorAnimation animation) {
		ghosts.Clear();
		var settings = Settings;
		double fps = timeline.GetReferenceFPS();
		double current = timeline.GetPlayhead() * fps;
		double end = animation.CalculateMaxTime() * fps;
		bool loop = settings.Loop && timeline.Repeat && end > 0;

		if (settings.FramesBefore || settings.FramesAfter) {
			double step = Math.Max(0.01, settings.FrameStep);
			if (settings.FramesBefore) {
				for (double offset = step; offset <= settings.BeforeFrames + 1e-6; offset += step)
					Add(settings.Anchor ? Math.Ceiling((current - offset) / step - 1e-6) * step : current - offset, false, current);
			}
			if (settings.FramesAfter) {
				for (double offset = step; offset <= settings.AfterFrames + 1e-6; offset += step)
					Add(settings.Anchor ? Math.Floor((current + offset) / step + 1e-6) * step : current + offset, true, current);
			}
		}

		// key frame ghosts are always anchored
		if (settings.KeysBefore || settings.KeysAfter) {
			var keys = new List<double>();
			var seenKeys = new HashSet<double>();
			foreach (var animationTimeline in animation.Timelines) {
				foreach (var time in animationTimeline.GetKeyframeTimes()) {
					double keyFrame = Math.Round(time * fps, 4);
					if (seenKeys.Add(keyFrame))
						keys.Add(keyFrame);
				}
			}
			keys.Sort();
			int step = Math.Max(1, (int)Math.Round(settings.KeyStep));
			if (settings.KeysBefore) {
				var before = new List<double>();
				for (int i = keys.Count - 1; i >= 0; i--) {
					double key = keys[i];
					if (key < current - 1e-6 && key >= current - settings.KeysBeforeFrames - 1e-6)
						before.Add(key);
				}
				for (int i = step - 1; i < before.Count; i += step)
					Add(before[i], false, current);
			}
			if (settings.KeysAfter) {
				var after = new List<double>();
				foreach (var key in keys) {
					if (key > current + 1e-6 && key <= current + settings.KeysAfterFrames + 1e-6)
						after.Add(key);
				}
				for (int i = step - 1; i < after.Count; i += step)
					Add(after[i], true, current);
			}
		}

		for (int i = ghosts.Count - 1; i >= 0; i--) {
			var ghost = ghosts[i];
			if (ghost.Frame >= 0 && ghost.Frame <= end) continue;
			if (loop)
				ghosts[i] = ((ghost.Frame % end + end) % end, ghost.After);
			else
				ghosts.RemoveAt(i);
		}
		if (settings.Current)
			ghosts.Add((current, true));

		// the same frame only once
		var seen = new HashSet<double>();
		ghosts.RemoveAll(g => !seen.Add(Math.Round(g.Frame, 4)));
	}

	private static void Add(double frame, bool after, double current) {
		if (Math.Abs(frame - current) < 1e-6) return;
		ghosts.Add((frame, after));
	}

	class Snapshot
	{
		readonly List<(EditorBone Bone, Vector2F Position, float Rotation, Vector2F Scale, Vector2F Shear, TransformMode Mode)> bones = [];
		readonly List<(EditorSlot Slot, Color Color, Color Dark, EditorAttachment? Active)> slots = [];
		readonly List<(EditorVertexAttachment Attachment, Dictionary<EditorVertex, Vector2F>? Deform, Dictionary<(EditorVertex, EditorBone), Vector2F>? BoneDeform)> deforms = [];
		readonly List<(EditorIkConstraint Constraint, float Mix, float Softness, int Bend, bool Compress, bool Stretch)> ik = [];
		readonly List<(EditorTransformConstraint Constraint, float Rotate, float Translate, float Scale, float Shear)> transform = [];
		readonly List<(EditorPathConstraint Constraint, float Position, float Spacing, float Rotate, float Translate)> path = [];
		readonly List<(EditorEvent Event, int Int, float Float, string? String, float Volume, float Balance)> events = [];
		List<EditorSlot>? drawOrder;

		public Snapshot(EditorModel model) {
			foreach (var bone in model.GetAllBones())
				bones.Add((bone, bone.Position, bone.Rotation, bone.Scale, bone.Shear, bone.TransformMode));
			foreach (var slot in model.Slots) {
				slots.Add((slot, slot.Color, slot.DarkColor, slot.ActiveAttachment));
				foreach (var attachment in slot.Attachments) {
					if (attachment is EditorVertexAttachment vertexAttachment)
						deforms.Add((vertexAttachment, vertexAttachment.CurrentDeform, vertexAttachment.CurrentBoneDeform));
				}
			}
			foreach (var constraint in model.IkConstraints)
				ik.Add((constraint, constraint.CurrentMix, constraint.CurrentSoftness, constraint.CurrentBendDirection, constraint.CurrentCompress, constraint.CurrentStretch));
			foreach (var constraint in model.TransformConstraints)
				transform.Add((constraint, constraint.CurrentRotateMix, constraint.CurrentTranslateMix, constraint.CurrentScaleMix, constraint.CurrentShearMix));

			foreach (var constraint in model.PathConstraints)
				path.Add((constraint, constraint.CurrentPosition, constraint.CurrentSpacing, constraint.CurrentRotateMix, constraint.CurrentTranslateMix));
			foreach (var editorEvent in model.Events)
				events.Add((editorEvent, editorEvent.CurrentInt, editorEvent.CurrentFloat, editorEvent.CurrentString, editorEvent.CurrentVolume, editorEvent.CurrentBalance));
			drawOrder = model.AnimatedDrawOrder;
		}

		public void Restore(EditorModel model) {
			foreach (var item in bones) {
				item.Bone.Position = item.Position;
				item.Bone.Rotation = item.Rotation;
				item.Bone.Scale = item.Scale;
				item.Bone.Shear = item.Shear;
				item.Bone.TransformMode = item.Mode;
			}
			foreach (var item in slots) {
				item.Slot.Color = item.Color;
				item.Slot.DarkColor = item.Dark;
				item.Slot.ActiveAttachment = item.Active;
			}
			foreach (var item in deforms) {
				item.Attachment.CurrentDeform = item.Deform;
				item.Attachment.CurrentBoneDeform = item.BoneDeform;
			}
			foreach (var item in ik) {
				item.Constraint.CurrentMix = item.Mix;
				item.Constraint.CurrentSoftness = item.Softness;
				item.Constraint.CurrentBendDirection = item.Bend;
				item.Constraint.CurrentCompress = item.Compress;
				item.Constraint.CurrentStretch = item.Stretch;
			}
			foreach (var item in transform) {
				item.Constraint.CurrentRotateMix = item.Rotate;
				item.Constraint.CurrentTranslateMix = item.Translate;
				item.Constraint.CurrentScaleMix = item.Scale;
				item.Constraint.CurrentShearMix = item.Shear;
			}
			foreach (var item in path) {
				item.Constraint.CurrentPosition = item.Position;
				item.Constraint.CurrentSpacing = item.Spacing;
				item.Constraint.CurrentRotateMix = item.Rotate;
				item.Constraint.CurrentTranslateMix = item.Translate;
			}
			foreach (var item in events) {
				item.Event.CurrentInt = item.Int;
				item.Event.CurrentFloat = item.Float;
				item.Event.CurrentString = item.String;
				item.Event.CurrentVolume = item.Volume;
				item.Event.CurrentBalance = item.Balance;
			}
			model.AnimatedDrawOrder = drawOrder;
			model.Root?.InvalidateTransform();
			model.UpdateConstraints();
		}
	}

	private static Color Tint(Color color, Color ghost, float alpha)
		=> new((byte)(color.R * ghost.R / 255), (byte)(color.G * ghost.G / 255), (byte)(color.B * ghost.B / 255), (byte)(color.A * alpha));

	public static void Draw(EditorModel model, bool onTop, bool showImages) {
		var settings = Settings;
		if (settings.OnTop != onTop) return;
		lastFrames.Clear();
		var animation = model.ActiveAnimation;
		if (!settings.Enabled || animation == null || !ModelEditor.Active.AnimationMode) return;
		Collect(animation);
		if (ghosts.Count == 0) return;

		HashSet<EditorBone>? only = null;
		if (settings.SelectedBones) {
			var selected = new HashSet<EditorBone>();
			foreach (var selectedObject in ModelEditor.Active.SelectedObjects) {
				if (selectedObject is EditorBone bone)
					selected.Add(bone);
			}
			if (selected.Count > 0)
				only = selected;
		}

		double fps = timeline.GetReferenceFPS();
		double current = timeline.GetPlayhead() * fps;
		var snapshot = new Snapshot(model);
		// farthest first, nearest ends up on top
		foreach (var ghost in ghosts.OrderByDescending(g => Math.Abs(g.Frame - current))) {
			lastFrames.Add(ghost.Frame);
			model.ResetToSetupPose();
			KeyframeSettings.InterpolationOverride = timeline.Stepped ? KeyframeInterpolation.Constant : null;
			animation.Apply(ghost.Frame / fps);
			KeyframeSettings.InterpolationOverride = null;
			model.Root?.InvalidateTransform();
			model.UpdateConstraints();

			var color = ghost.After ? settings.AfterColor : settings.BeforeColor;
			foreach (var slot in model.Slots)
				slot.Color = Tint(slot.Color, color, GHOST_ALPHA);

			float delta = (float)(ghost.Frame - current);
			Rlgl.PushMatrix();
			Rlgl.Translatef(settings.OffsetX * delta, settings.OffsetY * delta, 0);
			foreach (var slot in model.DrawOrder) {
				var attachment = slot.GetActiveAttachment();
				if (slot.Bone.IsActive && (only == null || only.Contains(slot.Bone)) && (showImages || attachment is not (EditorRegionAttachment or EditorMeshAttachment)))
					attachment?.Render();
				model.Clipping.NextSlot(slot);
			}
			model.Clipping.End();
			Rlgl.DrawRenderBatchActive();
			Rlgl.PopMatrix();
		}
		snapshot.Restore(model);
	}
}
