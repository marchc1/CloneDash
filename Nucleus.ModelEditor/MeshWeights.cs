using Nucleus.Types;

namespace Nucleus.ModelEditor;

public static class MeshWeights
{
	// where every vertex is drawn right now
	private static Dictionary<EditorVertex, Vector2F> WorldPositions(EditorVertexAttachment attachment) {
		attachment.SetupWorldTransform();
		Dictionary<EditorVertex, Vector2F> world = [];
		foreach (var vertex in attachment.GetVertices())
			world[vertex] = attachment.CalculateVertexWorldPosition(attachment.WorldTransform, vertex);
		return world;
	}

	private static void SetPositions(EditorWeights weights, Dictionary<EditorVertex, Vector2F> world, bool onlyMissing) {
		foreach (var pair in world) {
			if (!onlyMissing || !weights.TryGetVertexPosition(pair.Key, out _))
				weights.SetVertexPos(pair.Key, weights.Bone.WorldTransform.WorldToLocal(pair.Value));
		}
	}

	public static List<EditorWeights> Bind(EditorVertexAttachment attachment, IEnumerable<EditorBone> bones) {
		var world = WorldPositions(attachment);
		List<EditorWeights> added = [];
		foreach (var bone in bones) {
			if (attachment.Weights.Any(x => x.Bone == bone)) continue;
			var weights = new EditorWeights { Bone = bone };
			SetPositions(weights, world, false);
			attachment.Weights.Add(weights);
			added.Add(weights);
		}
		// Imported meshes only store positions for bones with weight.
		foreach (var weights in attachment.Weights)
			SetPositions(weights, world, true);

		// An unweighted mesh needs every vertex on a bone.
		if (added.Count > 0 && added.Count == attachment.Weights.Count)
			Auto(attachment, null, null, world);
		return added;
	}

	/// <summary>
	/// Takes bones off a mesh and gives their weight to the rest.
	/// </summary>
	public static void Remove(EditorVertexAttachment attachment, IEnumerable<EditorWeights> remove) {
		var world = WorldPositions(attachment);
		var gone = remove.ToHashSet();
		if (attachment.Weights.RemoveAll(gone.Contains) == 0) return;

		if (attachment.Weights.Count == 0) {
			foreach (var pair in world) {
				var local = attachment.WorldTransform.WorldToLocal(pair.Value);
				pair.Key.X = local.X;
				pair.Key.Y = local.Y;
			}
			attachment.Invalidate();
			return;
		}

		foreach (var weights in attachment.Weights)
			SetPositions(weights, world, true);
		foreach (var pair in world)
			Normalize(attachment, pair.Key, pair.Value);
	}

	/// <summary>
	/// Rebinds the vertices where they are now, throwing away the old binding positions.
	/// </summary>
	public static void UpdateBindings(EditorVertexAttachment attachment) {
		if (attachment.Weights.Count == 0) return;
		var world = WorldPositions(attachment);
		foreach (var weights in attachment.Weights)
			SetPositions(weights, world, false);
		foreach (var pair in world) {
			var local = attachment.WorldTransform.WorldToLocal(pair.Value);
			pair.Key.X = local.X;
			pair.Key.Y = local.Y;
		}
	}

	/// <summary>
	/// Gives every bone a binding position for every vertex, from where the vertices are drawn.
	/// </summary>
	public static void EnsurePositions(EditorVertexAttachment attachment) {
		var world = WorldPositions(attachment);
		foreach (var weights in attachment.Weights)
			SetPositions(weights, world, true);
	}

	/// <summary>
	/// Sets some bones' weights on a vertex, the other unlocked bones make up the difference.
	/// With nothing else weighing the vertex the bones can't go any lower
	/// </summary>
	public static void Adjust(EditorVertexAttachment attachment, EditorVertex vertex, IReadOnlyDictionary<EditorWeights, float> targets) {
		float locked = 0;
		float others = 0;
		foreach (var weights in attachment.Weights) {
			if (weights.Locked)
				locked += weights.TryGetVertexWeight(vertex);
			else if (!targets.ContainsKey(weights))
				others += weights.TryGetVertexWeight(vertex);
		}
		float room = MathF.Max(0, 1 - locked);

		float current = 0;
		float wanted = 0;
		foreach (var target in targets) {
			if (target.Key.Locked) continue;
			current += target.Key.TryGetVertexWeight(vertex);
			wanted += Math.Clamp(target.Value, 0, 1);
		}
		if (others <= 0 && wanted < current) return;

		float scale = wanted > room ? room / wanted : 1;
		float given = 0;
		foreach (var target in targets) {
			if (target.Key.Locked) continue;
			float value = Math.Clamp(target.Value, 0, 1) * scale;
			target.Key.SetVertexWeight(vertex, value);
			given += value;
		}

		float rest = room - given;
		if (others > 0) {
			foreach (var weights in attachment.Weights) {
				if (!weights.Locked && !targets.ContainsKey(weights))
					weights.SetVertexWeight(vertex, weights.TryGetVertexWeight(vertex) / others * rest);
			}
		}
		else if (given > 0 && rest > 0) {
			foreach (var weights in targets.Keys) {
				if (!weights.Locked)
					weights.SetVertexWeight(vertex, weights.TryGetVertexWeight(vertex) * room / given);
			}
		}
	}

	public static void Swap(EditorVertexAttachment attachment, EditorWeights first, EditorWeights second, IEnumerable<EditorVertex> vertices) {
		EnsurePositions(attachment);
		foreach (var vertex in vertices) {
			float firstWeight = first.TryGetVertexWeight(vertex);
			float secondWeight = second.TryGetVertexWeight(vertex);
			first.SetVertexWeight(vertex, secondWeight);
			second.SetVertexWeight(vertex, firstWeight);
		}
	}

	/// <summary>
	/// Makes a vertex's weights add up to 1, leaving locked bones alone where it can.
	/// </summary>
	public static void Normalize(EditorVertexAttachment attachment, EditorVertex vertex, Vector2F world) {
		float locked = 0;
		float free = 0;
		foreach (var weights in attachment.Weights) {
			float value = weights.TryGetVertexWeight(vertex);
			if (weights.Locked)
				locked += value;
			else
				free += value;
		}

		if (free > 0 && locked < 1) {
			float scale = (1 - locked) / free;
			foreach (var weights in attachment.Weights) {
				if (!weights.Locked)
					weights.SetVertexWeight(vertex, weights.TryGetVertexWeight(vertex) * scale);
			}
		}
		else if (locked + free > 0) {
			float scale = 1 / (locked + free);
			foreach (var weights in attachment.Weights)
				weights.SetVertexWeight(vertex, weights.TryGetVertexWeight(vertex) * scale);
		}
		else {
			var nearest = attachment.Weights.MinBy(w => DistanceToBone(w.Bone, world))!;
			foreach (var weights in attachment.Weights)
				weights.SetVertexWeight(vertex, weights == nearest ? 1 : 0);
		}
	}

	private static (Vector2F A, Vector2F B) BoneSegment(EditorBone bone)
		=> (bone.WorldTransform.LocalToWorld(0, 0), bone.WorldTransform.LocalToWorld(bone.Length, 0));

	private static Vector2F ClosestOnSegment(Vector2F a, Vector2F b, Vector2F p) {
		var ab = b - a;
		float lengthSquared = ab.X * ab.X + ab.Y * ab.Y;
		if (lengthSquared <= 0) return a;
		float t = Math.Clamp(((p.X - a.X) * ab.X + (p.Y - a.Y) * ab.Y) / lengthSquared, 0, 1);
		return a + ab * t;
	}

	private static float DistanceToBone(EditorBone bone, Vector2F point) {
		var segment = BoneSegment(bone);
		return (point - ClosestOnSegment(segment.A, segment.B, point)).Length;
	}

	private static float Cross(Vector2F o, Vector2F a, Vector2F b) =>
		(a.X - o.X) * (b.Y - o.Y) - (a.Y - o.Y) * (b.X - o.X);

	// touching at an end doesn't count
	private static bool Crosses(Vector2F p1, Vector2F p2, Vector2F q1, Vector2F q2) {
		float d1 = Cross(q1, q2, p1);
		float d2 = Cross(q1, q2, p2);
		float d3 = Cross(p1, p2, q1);
		float d4 = Cross(p1, p2, q2);
		return ((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) && ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0));
	}

	private const float FALLOFF = 4;
	private const float PRUNE = 0.01f;

	/// <summary>
	/// Auto weights by each bone's distance through the mesh
	/// </summary>
	/// <param name="only">Vertices to change, null for all</param>
	/// <param name="onlyBones">Only shares out the weight these bones already had</param>
	public static void Auto(EditorVertexAttachment attachment, IReadOnlyCollection<EditorVertex>? only = null, IReadOnlyCollection<EditorWeights>? onlyBones = null)
		=> Auto(attachment, only, onlyBones, WorldPositions(attachment));

	private static void Auto(EditorVertexAttachment attachment, IReadOnlyCollection<EditorVertex>? only, IReadOnlyCollection<EditorWeights>? onlyBones, Dictionary<EditorVertex, Vector2F> world) {
		foreach (var weights in attachment.Weights)
			SetPositions(weights, world, true);

		List<EditorWeights> bones = [];
		foreach (var weights in attachment.Weights) {
			if (!weights.Locked && (onlyBones == null || onlyBones.Count == 0 || onlyBones.Contains(weights)))
				bones.Add(weights);
		}
		if (bones.Count == 0) return;

		var vertices = attachment.GetVertices().ToList();
		int vertexCount = vertices.Count;
		var positions = new Vector2F[vertexCount];
		for (int i = 0; i < vertexCount; i++)
			positions[i] = world[vertices[i]];
		Dictionary<EditorVertex, int> index = [];
		for (int i = 0; i < vertexCount; i++)
			index[vertices[i]] = i;

		attachment.RefreshDelaunator();
		var triangles = attachment.Triangles;
		var neighbors = new List<int>[vertexCount];
		for (int i = 0; i < vertexCount; i++)
			neighbors[i] = [];
		for (int triangle = 0; triangle + 2 < triangles.Count; triangle += 3) {
			for (int k = 0; k < 3; k++) {
				if (!index.TryGetValue(triangles[triangle + k], out int i) || !index.TryGetValue(triangles[triangle + (k + 1) % 3], out int j)) continue;
				if (!neighbors[i].Contains(j))
					neighbors[i].Add(j);
				if (!neighbors[j].Contains(i))
					neighbors[j].Add(i);
			}
		}

		var hull = new Vector2F[attachment.ShapeEdges.Count];
		for (int i = 0; i < hull.Length; i++)
			hull[i] = world[attachment.ShapeEdges[i]];

		var distances = new float[bones.Count][];
		var queue = new PriorityQueue<int, float>();
		for (int boneIndex = 0; boneIndex < bones.Count; boneIndex++) {
			var segment = BoneSegment(bones[boneIndex].Bone);
			var boneDistances = distances[boneIndex] = new float[vertexCount];
			bool any = false;
			for (int i = 0; i < vertexCount; i++) {
				var closest = ClosestOnSegment(segment.A, segment.B, positions[i]);
				boneDistances[i] = InsideStraight(positions[i], closest, hull) ? (positions[i] - closest).Length : float.PositiveInfinity;
				any |= !float.IsInfinity(boneDistances[i]);
			}
			// bone is nowhere near the inside
			if (!any) {
				for (int i = 0; i < vertexCount; i++)
					boneDistances[i] = (positions[i] - ClosestOnSegment(segment.A, segment.B, positions[i])).Length;
			}

			queue.Clear();
			for (int i = 0; i < vertexCount; i++) {
				if (!float.IsInfinity(boneDistances[i]))
					queue.Enqueue(i, boneDistances[i]);
			}
			while (queue.TryDequeue(out int i, out float distance)) {
				if (distance > boneDistances[i]) continue;
				foreach (int j in neighbors[i]) {
					float throughNeighbor = distance + (positions[i] - positions[j]).Length;
					if (throughNeighbor < boneDistances[j]) {
						boneDistances[j] = throughNeighbor;
						queue.Enqueue(j, throughNeighbor);
					}
				}
			}
		}

		var raw = new float[bones.Count];
		for (int i = 0; i < vertexCount; i++) {
			var vertex = vertices[i];
			if (only != null && !only.Contains(vertex)) continue;

			float others = 0;
			foreach (var weights in attachment.Weights) {
				if (!bones.Contains(weights))
					others += weights.TryGetVertexWeight(vertex);
			}
			float room = MathF.Max(0, 1 - others);
			if (room <= 0) continue;
			float sum = 0;
			for (int boneIndex = 0; boneIndex < bones.Count; boneIndex++) {
				float distance = distances[boneIndex][i];
				raw[boneIndex] = float.IsInfinity(distance) ? 0 : 1 / MathF.Pow(MathF.Max(distance, 1), FALLOFF);
				sum += raw[boneIndex];
			}
			if (sum <= 0) {
				int nearest = 0;
				for (int boneIndex = 1; boneIndex < bones.Count; boneIndex++) {
					if (DistanceToBone(bones[boneIndex].Bone, positions[i]) < DistanceToBone(bones[nearest].Bone, positions[i]))
						nearest = boneIndex;
				}
				raw[nearest] = sum = 1;
			}

			float kept = 0;
			for (int boneIndex = 0; boneIndex < bones.Count; boneIndex++) {
				raw[boneIndex] /= sum;
				if (raw[boneIndex] < PRUNE)
					raw[boneIndex] = 0;
				kept += raw[boneIndex];
			}
			for (int boneIndex = 0; boneIndex < bones.Count; boneIndex++)
				bones[boneIndex].SetVertexWeight(vertex, raw[boneIndex] / kept * room);
		}
	}

	private static bool InsideStraight(Vector2F from, Vector2F to, Vector2F[] hull) {
		for (int i = 0; i < hull.Length; i++) {
			if (Crosses(from, to, hull[i], hull[(i + 1) % hull.Length])) return false;
		}
		return true;
	}

	/// <summary>
	/// Averages each vertex's weights with its neighbors along hull and manual edges.
	/// </summary>
	/// <param name="only">Vertices to smooth, null for all</param>
	public static void Smooth(EditorVertexAttachment attachment, IReadOnlyCollection<EditorVertex>? only = null) {
		if (attachment.Weights.Count == 0) return;
		EnsurePositions(attachment);

		var hull = attachment.ShapeEdges;
		Dictionary<EditorVertex, HashSet<EditorVertex>> neighbors = [];
		for (int i = 0; i < hull.Count; i++) {
			var current = hull[i];
			var next = hull[(i + 1) % hull.Count];
			if (current == next) continue;
			Of(current, neighbors).Add(next);
			Of(next, neighbors).Add(current);
		}
		foreach (var vertex in attachment.GetVertices()) {
			if (vertex.HasConstrainedEdges) {
				foreach (var other in vertex.ConstrainedVertices) {
					Of(vertex, neighbors).Add(other);
					Of(other, neighbors).Add(vertex);
				}
			}
		}

		// Everything reads the weights from before this pass.
		Dictionary<EditorVertex, float[]> before = [];
		foreach (var vertex in attachment.GetVertices()) {
			var values = new float[attachment.Weights.Count];
			for (int boneIndex = 0; boneIndex < attachment.Weights.Count; boneIndex++)
				values[boneIndex] = attachment.Weights[boneIndex].TryGetVertexWeight(vertex);
			before.Add(vertex, values);
		}
		foreach (var entry in neighbors) {
			var vertex = entry.Key;
			var near = entry.Value;
			if (only != null && only.Count > 0 && !only.Contains(vertex)) continue;
			if (!before.ContainsKey(vertex)) continue;

			float locked = 0;
			float sum = 0;
			var average = new float[attachment.Weights.Count];
			for (int boneIndex = 0; boneIndex < attachment.Weights.Count; boneIndex++) {
				if (attachment.Weights[boneIndex].Locked) {
					locked += before[vertex][boneIndex];
					continue;
				}
				float total = before[vertex][boneIndex];
				foreach (var other in near) {
					if (before.TryGetValue(other, out var otherWeights))
						total += otherWeights[boneIndex];
				}
				average[boneIndex] = total / (near.Count + 1);
				sum += average[boneIndex];
			}
			if (sum <= 0) continue;

			float room = MathF.Max(0, 1 - locked);
			for (int boneIndex = 0; boneIndex < attachment.Weights.Count; boneIndex++) {
				if (!attachment.Weights[boneIndex].Locked)
					attachment.Weights[boneIndex].SetVertexWeight(vertex, average[boneIndex] / sum * room);
			}
		}
	}

	private static HashSet<EditorVertex> Of(EditorVertex vertex, Dictionary<EditorVertex, HashSet<EditorVertex>> neighbors) =>
		neighbors.TryGetValue(vertex, out var set) ? set : neighbors[vertex] = [];

	/// <summary>
	/// Drops weights under <paramref name="threshold"/> and past <paramref name="maxBones"/> per vertex, skipping locked bones.
	/// </summary>
	/// <returns>How many weights were removed</returns>
	public static int Prune(EditorVertexAttachment attachment, int maxBones, float threshold) {
		int removed = 0;
		foreach (var vertex in attachment.GetVertices()) {
			List<EditorWeights> weighted = [];
			foreach (var weights in attachment.Weights) {
				if (weights.TryGetVertexWeight(vertex) > 0)
					weighted.Add(weights);
			}
			List<EditorWeights> candidates = [];
			foreach (var weights in weighted) {
				if (weights.Locked) continue;
				float value = weights.TryGetVertexWeight(vertex);
				int at = candidates.Count;
				while (at > 0 && candidates[at - 1].TryGetVertexWeight(vertex) > value)
					at--;
				candidates.Insert(at, weights);
			}
			if (candidates.Count == 0) continue;

			HashSet<EditorWeights> drop = [.. candidates.Where(w => w.TryGetVertexWeight(vertex) < threshold)];
			int over = weighted.Count - drop.Count - Math.Max(1, maxBones);
			foreach (var candidate in candidates) {
				if (over <= 0) break;
				if (drop.Add(candidate))
					over--;
			}
			// at least one unlocked bone holds on to the weight
			if (drop.Count == candidates.Count)
				drop.Remove(candidates[candidates.Count - 1]);
			if (drop.Count == 0) continue;

			float freed = drop.Sum(w => w.TryGetVertexWeight(vertex));
			List<EditorWeights> keep = [];
			foreach (var weights in candidates) {
				if (!drop.Contains(weights))
					keep.Add(weights);
			}
			float kept = keep.Sum(w => w.TryGetVertexWeight(vertex));
			foreach (var weights in drop)
				weights.SetVertexWeight(vertex, 0);
			foreach (var weights in keep)
				weights.SetVertexWeight(vertex, weights.TryGetVertexWeight(vertex) + freed * weights.TryGetVertexWeight(vertex) / kept);
			removed += drop.Count;
		}
		return removed;
	}
}
