using Nucleus.Models;
using Nucleus.Types;
using Nucleus.UI;

namespace Nucleus.ModelEditor;

public record ActionStackData(string Name, Action Redo, Action Undo);
record ActionStackStep(object? Key, Action Redo, Action Undo);

public class ActionStack
{
	private MaxStack<ActionStackData> DidActions = new(256);
	private Stack<ActionStackData> UndidActions = [];

	/// <summary>
	/// Is an undo or redo running right now? Edits made during it aren't recorded
	/// </summary>
	public bool Applying { get; private set; }

	public event Action? Applied;

	/// <summary>
	/// Bumped on every recorded edit, undo and redo
	/// </summary>
	public int Version { get; private set; }

	public object? Top => DidActions.Count > 0 ? DidActions.Peek() : null;
	public bool HasPending =>
		batch.Count > 0 || pendingTransforms.Count > 0 || pendingAnimations.Count > 0 || pendingMeshes.Count > 0;

	public int UndoCount => DidActions.Count;
	public int RedoCount => UndidActions.Count;

	// Everything pushed before the next flush becomes one entry.
	private readonly List<ActionStackStep> batch = [];
	private string? batchName;

	/// <summary>
	/// Records an edit that already happened
	/// </summary>
	public void Push(string name, Action redo, Action undo) => Push(null, name, redo, undo);

	/// <summary>
	/// Repeat pushes with the same key in one batch only keep the first undo and the last redo
	/// </summary>
	public void Push(object? key, string name, Action redo, Action undo) {
		if (Applying) return;
		batchName ??= name;
		if (key != null) {
			int i = batch.FindIndex(x => Equals(x.Key, key));
			if (i >= 0) {
				batch[i] = batch[i] with { Redo = redo };
				return;
			}
		}
		batch.Add(new(key, redo, undo));
	}

	public void Undo() {
		Flush();
		if (DidActions.Count <= 0) return;
		ActionStackData data = DidActions.Pop();
		Version++;
		Apply(data.Undo);
		UndidActions.Push(data);
		Logs.Info($"Undo: {data.Name}");
	}

	public void Redo() {
		Flush();
		if (UndidActions.Count <= 0) return;
		ActionStackData data = UndidActions.Pop();
		Version++;
		Apply(data.Redo);
		DidActions.Push(data);
		Logs.Info($"Redo: {data.Name}");
	}

	private void Apply(Action action) {
		Applying = true;
		try { action(); }
		finally { Applying = false; }
		Applied?.Invoke();
	}

	public void Clear() {
		pendingTransforms.Clear();
		pendingAnimations.Clear();
		pendingMeshes.Clear();
		batch.Clear();
		batchName = null;
		DidActions.Clear();
		UndidActions.Clear();
	}

	// Transforms
	private readonly List<(IEditorType Item, TransformState Before)> pendingTransforms = [];

	/// <summary>
	/// Snapshots an item's transform before an edit
	/// </summary>
	public void TrackTransform(IEditorType item) {
		if (Applying) return;
		foreach (var pending in pendingTransforms) {
			if (pending.Item == item) return;
		}
		pendingTransforms.Add((item, TransformState.Capture(item)));
	}

	private bool wasMouseHeld;
	// Animations
	private readonly List<(EditorAnimation Animation, AnimationState Before)> pendingAnimations = [];

	/// <summary>
	/// Snapshots an animation before an edit
	/// </summary>
	public void TrackAnimation(EditorAnimation? animation) {
		if (Applying || animation == null) return;
		foreach (var pending in pendingAnimations) {
			if (pending.Animation == animation) return;
		}
		pendingAnimations.Add((animation, AnimationState.Capture(animation)));
	}

	// Meshes
	private readonly List<MeshState> pendingMeshes = [];

	/// <summary>
	/// Snapshots a mesh before an edit
	/// </summary>
	public void TrackMesh(EditorVertexAttachment? attachment) {
		if (Applying || attachment == null) return;
		foreach (var pending in pendingMeshes) {
			if (pending.Attachment == attachment) return;
		}
		pendingMeshes.Add(MeshState.Capture(attachment));
	}

	public void Think(bool mouseHeld) {
		// The release frame still sends one last drag.
		if (!mouseHeld && !wasMouseHeld)
			Flush();
		wasMouseHeld = mouseHeld;
	}

	public void Flush() {
		if (pendingTransforms.Count > 0) {
			List<(IEditorType Item, TransformState Before, TransformState After)> changed = [];
			foreach (var pending in pendingTransforms) {
				var after = TransformState.Capture(pending.Item);
				if (!pending.Before.Equals(after))
					changed.Add((pending.Item, pending.Before, after));
			}
			pendingTransforms.Clear();

			if (changed.Count > 0) {
				var name = changed.Count == 1
					? $"Transform {changed[0].Item.GetName()}"
					: $"Transform {changed.Count} items";
				Push(name,
					() => {
						foreach (var change in changed)
							change.After.Restore(change.Item);
					},
					() => {
						foreach (var change in changed)
							change.Before.Restore(change.Item);
					}
				);
			}
		}

		if (pendingAnimations.Count > 0) {
			foreach (var pending in pendingAnimations) {
				if (pending.Before.Matches(pending.Animation)) continue;
				var after = AnimationState.Capture(pending.Animation);
				Push($"Edit {pending.Animation.Name} keys", after.Restore, pending.Before.Restore);
			}
			pendingAnimations.Clear();
		}

		if (pendingMeshes.Count > 0) {
			foreach (var before in pendingMeshes) {
				if (before.Matches(before.Attachment)) continue;
				var after = MeshState.Capture(before.Attachment);
				Push($"Edit mesh {before.Attachment.GetName()}", after.Restore, before.Restore);
			}
			pendingMeshes.Clear();
		}

		if (batch.Count == 0) return;
		var steps = batch.ToArray();
		var entryName = batchName!;
		batch.Clear();
		batchName = null;

		UndidActions.Clear();
		Version++;
		DidActions.Push(new(entryName,
			() => {
				foreach (var step in steps)
					step.Redo();
			},
			() => {
				for (int i = steps.Length - 1; i >= 0; i--)
					steps[i].Undo();
			}
		));
	}
}

public readonly struct TransformState : IEquatable<TransformState>
{
	readonly float[] values;
	readonly bool raw;

	TransformState(float[] values, bool raw) {
		this.values = values;
		this.raw = raw;
	}

	public static TransformState Capture(IEditorType item) {
		if (item is EditorBone bone)
			return new([
				bone.SetupPositionX, bone.SetupPositionY, bone.SetupRotation,
				bone.SetupScaleX, bone.SetupScaleY, bone.SetupShearX, bone.SetupShearY,
				bone.PositionX, bone.PositionY, bone.Rotation,
				bone.ScaleX, bone.ScaleY, bone.ShearX, bone.ShearY
			], true);

		return new([
			item.GetTranslationX(), item.GetTranslationY(), item.GetRotation(),
			item.GetScaleX(), item.GetScaleY(), item.GetShearX(), item.GetShearY()
		], false);
	}

	public void Restore(IEditorType item) {
		if (raw && item is EditorBone bone) {
			// setup first, setting it resets the pose
			bone.SetupPositionX = values[0]; bone.SetupPositionY = values[1]; bone.SetupRotation = values[2];
			bone.SetupScaleX = values[3]; bone.SetupScaleY = values[4];
			bone.SetupShearX = values[5]; bone.SetupShearY = values[6];
			bone.PositionX = values[7]; bone.PositionY = values[8]; bone.Rotation = values[9];
			bone.ScaleX = values[10]; bone.ScaleY = values[11]; bone.ShearX = values[12]; bone.ShearY = values[13];
		}
		else {
			if (item.CanTranslate()) {
				item.EditTranslationX(values[0]);
				item.EditTranslationY(values[1]);
			}
			if (item.CanRotate())
				item.EditRotation(values[2]);
			if (item.CanScale()) {
				item.EditScaleX(values[3]);
				item.EditScaleY(values[4]);
			}
			if (item.CanShear()) {
				item.EditShearX(values[5]);
				item.EditShearY(values[6]);
			}
		}
		ModelEditor.Active.File.NotifyTransformed(item);
	}

	public bool Equals(TransformState other) => raw == other.raw && values.AsSpan().SequenceEqual(other.values);
	public override bool Equals(object? obj) => obj is TransformState other && Equals(other);
	public override int GetHashCode() => values.Length;
}

public class AnimationState
{
	readonly EditorAnimation animation;
	readonly EditorTimeline[] timelines;
	readonly (IFCurve Curve, object Saved)[] curves;
	readonly Dictionary<KeyframeProperty, EditorBone[]> separated;
	readonly (DeformTimeline Timeline, float[][] Offsets)[] deforms;
	readonly EditorSlot[] separatedColors;

	AnimationState(EditorAnimation animation, EditorTimeline[] timelines, (IFCurve, object)[] curves, Dictionary<KeyframeProperty, EditorBone[]> separated) {
		this.animation = animation;
		this.timelines = timelines;
		this.curves = curves;
		this.separated = separated;
		separatedColors = [.. animation.SeparatedColors];
		List<(DeformTimeline, float[][])> deformList = [];
		foreach (var timeline in timelines) {
			if (timeline is DeformTimeline deform)
				deformList.Add((deform, deform.Offsets.ToArray()));
		}
		deforms = deformList.ToArray();
	}

	public static AnimationState Capture(EditorAnimation animation) {
		var timelines = animation.Timelines.ToArray();
		List<(IFCurve, object)> savedCurves = [];
		foreach (var timeline in animation.Timelines) {
			foreach (var curve in timeline.GetCurves())
				savedCurves.Add((curve, curve.SaveKeyframes()));
		}
		return new(
			animation,
			timelines,
			savedCurves.ToArray(),
			animation.SeparatedProperties.ToDictionary(x => x.Key, x => x.Value.ToArray())
		);
	}

	public bool Matches(EditorAnimation now) {
		if (now != animation || !now.Timelines.SequenceEqual(timelines)) return false;
		int i = 0;
		foreach (var curve in now.Timelines.SelectMany(x => x.GetCurves())) {
			if (i >= curves.Length || curves[i].Curve != curve || !curve.MatchesKeyframes(curves[i].Saved)) return false;
			i++;
		}
		if (i != curves.Length) return false;
		foreach (var deform in deforms) {
			if (!deform.Timeline.Offsets.SequenceEqual(deform.Offsets)) return false;
		}

		List<KeyValuePair<KeyframeProperty, HashSet<EditorBone>>> nowSeparated = [];
		foreach (var pair in now.SeparatedProperties) {
			if (pair.Value.Count > 0)
				nowSeparated.Add(pair);
		}
		if (nowSeparated.Count != separated.Count(x => x.Value.Length > 0)) return false;
		foreach (var pair in nowSeparated) {
			if (!separated.TryGetValue(pair.Key, out var was) || !pair.Value.SetEquals(was)) return false;
		}
		return now.SeparatedColors.SetEquals(separatedColors);
	}

	public void Restore() {
		var file = ModelEditor.Active.File;
		HashSet<(KeyframeProperty, EditorBone)> wasSeparated = [];
		foreach (var pair in animation.SeparatedProperties) {
			foreach (var separatedBone in pair.Value)
				wasSeparated.Add((pair.Key, separatedBone));
		}

		animation.Timelines.Clear();
		animation.Timelines.AddRange(timelines);
		foreach (var curve in curves)
			curve.Curve.LoadKeyframes(curve.Saved);
		foreach (var deform in deforms)
			deform.Timeline.Offsets = [.. deform.Offsets];

		animation.SeparatedProperties.Clear();
		foreach (var pair in separated)
			animation.SeparatedProperties[pair.Key] = [.. pair.Value];

		HashSet<(KeyframeProperty, EditorBone)> isSeparated = [];
		foreach (var pair in animation.SeparatedProperties) {
			foreach (var separatedBone in pair.Value)
				isSeparated.Add((pair.Key, separatedBone));
		}
		foreach (var entry in wasSeparated.Union(isSeparated)) {
			if (wasSeparated.Contains(entry) != isSeparated.Contains(entry))
				file.NotifySeparated(entry.Item2, entry.Item1, isSeparated.Contains(entry));
		}

		var wasColors = animation.SeparatedColors.ToHashSet();
		animation.SeparatedColors.Clear();
		animation.SeparatedColors.UnionWith(separatedColors);
		foreach (var slot in wasColors.Union(separatedColors)) {
			if (wasColors.Contains(slot) != animation.SeparatedColors.Contains(slot))
				file.NotifyColorSeparated(slot, animation.SeparatedColors.Contains(slot));
		}

		file.NotifyAnimationEdited(animation);
		ModelEditor.Active.QueueAnimationUpdate();
	}
}

public class MeshState
{
	public EditorVertexAttachment Attachment { get; }
	readonly EditorVertex[] hull;
	readonly EditorVertex[]? steiner;
	readonly (EditorVertex Vertex, float X, float Y, float U, float V, EditorVertex[] Constrained)[] vertices;
	readonly (EditorWeights Weights, EditorVertex[] Vertices, float[] Values, Vector2F[] Positions)[] weights;
	readonly EditorVertex[]? source;

	MeshState(EditorVertexAttachment attachment) {
		Attachment = attachment;
		hull = [.. attachment.ShapeEdges];
		steiner = attachment is EditorMeshAttachment mesh ? [.. mesh.SteinerPoints] : null;
		List<(EditorVertex, float, float, float, float, EditorVertex[])> vertexList = [];
		foreach (var vertex in attachment.GetVertices())
			vertexList.Add((vertex, vertex.X, vertex.Y, vertex.U, vertex.V, vertex.ConstrainedHashSet.ToArray()));
		vertices = vertexList.ToArray();
		List<(EditorWeights, EditorVertex[], float[], Vector2F[])> weightList = [];
		foreach (var weight in attachment.Weights)
			weightList.Add((weight, weight.Vertices.ToArray(), weight.Weights.ToArray(), weight.Positions.ToArray()));
		weights = weightList.ToArray();
		source = attachment.SourceTriangles?.ToArray();
	}

	public static MeshState Capture(EditorVertexAttachment attachment) => new(attachment);

	public bool Matches(EditorVertexAttachment now) {
		if (now != Attachment || !now.ShapeEdges.SequenceEqual(hull)) return false;
		if (steiner != null && !((EditorMeshAttachment)now).SteinerPoints.SequenceEqual(steiner)) return false;

		foreach (var saved in vertices) {
			var vertex = saved.Vertex;
			if (vertex.X != saved.X || vertex.Y != saved.Y || vertex.U != saved.U || vertex.V != saved.V || !vertex.ConstrainedHashSet.SetEquals(saved.Constrained)) return false;
		}

		if (now.Weights.Count != weights.Length) return false;
		for (int i = 0; i < weights.Length; i++) {
			var saved = weights[i];
			var weight = saved.Weights;
			if (now.Weights[i] != weight || !weight.Vertices.SequenceEqual(saved.Vertices) || !weight.Weights.SequenceEqual(saved.Values)
				|| !weight.Positions.SequenceEqual(saved.Positions)) return false;
		}

		if (source == null) return now.SourceTriangles == null;
		return now.SourceTriangles != null && now.SourceTriangles.SequenceEqual(source);
	}

	public void Restore() {
		var attachment = Attachment;
		attachment.ShapeEdges.Clear();
		attachment.ShapeEdges.AddRange(hull);
		if (steiner != null && attachment is EditorMeshAttachment mesh) {
			mesh.SteinerPoints.Clear();
			mesh.SteinerPoints.AddRange(steiner);
		}

		foreach (var saved in vertices) {
			var vertex = saved.Vertex;
			vertex.X = saved.X; vertex.Y = saved.Y; vertex.U = saved.U; vertex.V = saved.V;
			vertex.ConstrainedHashSet.Clear();
			vertex.ConstrainedHashSet.UnionWith(saved.Constrained);
		}

		attachment.Weights.Clear();
		foreach (var saved in weights) {
			var weight = saved.Weights;
			weight.Vertices = [.. saved.Vertices];
			weight.Weights = [.. saved.Values];
			weight.Positions = [.. saved.Positions];
			attachment.Weights.Add(weight);
		}

		attachment.Invalidate();
		attachment.SourceTriangles = source == null ? null : [.. source];

		HashSet<EditorVertex> alive = [.. hull];
		if (steiner != null)
			alive.UnionWith(steiner);
		List<EditorVertex> removed = [];
		foreach (var vertex in attachment.SelectedVertices) {
			if (!alive.Contains(vertex))
				removed.Add(vertex);
		}
		foreach (var gone in removed) {
			attachment.SelectedVertices.Remove(gone);
			if (gone.Selected)
				ModelEditor.Active.UnselectObject(gone);
		}
	}
}
