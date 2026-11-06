// TODO: This file needs to be split apart!

using Newtonsoft.Json;

using Nucleus.Commands;
using Nucleus.Common.Types;
using Nucleus.Extensions;
using Nucleus.Models;
using Nucleus.Types;

using Poly2Tri;

using Raylib_cs;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Nucleus.ModelEditor
{
	public abstract class EditorVertexAttachment : EditorAttachment, IEditorType {
		// The key button next to it in the tree keys deform
		public bool CanKeyframe() => true;
		public bool GetKeyframeParameters(
			[System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out IEditorType? target,
			[System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out KeyframeProperty property,
			[System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out int arrayIndex) {
			target = this;
			property = KeyframeProperty.Deform;
			arrayIndex = -1;
			return true;
		}

		private Vector2F pos, scale = new(1, 1);
		public List<EditorWeights> Weights = [];
		public HashSet<EditorVertex> SelectedVertices = [];
		[JsonIgnore] public Transformation WorldTransform;


		public Vector2F Position { get => pos; set => pos = value; }
		public float Rotation { get; set; }
		public Vector2F Scale { get => scale; set => scale = value; }
		public Color Color { get; set; } = Color.White;

		/// <summary>
		/// Unweighted deform offsets in local space, null when undeformed
		/// </summary>
		[JsonIgnore] public Dictionary<EditorVertex, Vector2F>? CurrentDeform;
		/// <summary>
		/// Weighted deform offsets per bone, in that bone's space
		/// </summary>
		[JsonIgnore] public Dictionary<(EditorVertex, EditorBone), Vector2F>? CurrentBoneDeform;
		[JsonIgnore] public virtual EditorVertexAttachment DeformAttachment => this;


		protected List<EditorVertex> triangles = [];
		[JsonIgnore] public bool Invalidated { get; set; } = true;
		/// <summary>
		/// Imported triangles, used until the shape is edited
		/// </summary>
		public List<EditorVertex>? SourceTriangles;
		public bool Invalidate() {
			SourceTriangles = null;
			vertexList = null;
			return Invalidated = true;
		}

		private static readonly List<EditorVertex> triangulatePoints = [];
		private static readonly Dictionary<EditorVertex, int> triangulateIndices = [];
		static readonly HashSet<(float, float)> triangulateSeen = [];
		private static readonly List<int> triangulateOutput = [];

		/// <summary>
		/// Why the last triangulation failed
		/// </summary>
		[JsonIgnore] public Exception? TriangulationError { get; private set; }

		public void RefreshDelaunator() {
			if (!Invalidated) return;
			Invalidated = false;
			triangleVersion++;
			TriangulationError = null;
			if (SourceTriangles != null && UseSourceTriangles())
				return;

			var points = triangulatePoints;
			var indices = triangulateIndices;
			var seen = triangulateSeen;
			var output = triangulateOutput;
			points.Clear();
			indices.Clear();
			seen.Clear();
			output.Clear();

			var triangulator = Triangulator.Shared;
			triangulator.Clear();
			int hull = ShapeEdges.Count;
			foreach (var vertex in GetVertices()) {
				// Poly2Tri can't take inner points outside the hull or on top of another point.
				if (points.Count >= hull && (!seen.Add((vertex.X, vertex.Y)) || !InsideHull(vertex.X, vertex.Y)))
					continue;
				if (points.Count < hull)
					seen.Add((vertex.X, vertex.Y));
				indices[vertex] = points.Count;
				points.Add(vertex);
				triangulator.AddPoint(vertex.X, vertex.Y);
			}
			triangulator.AddLoop(0, hull);

			// Each edge once, from whichever end comes first
			foreach (var from in points) {
				if (!from.HasConstrainedEdges) continue;
				int fromIndex = indices[from];
				foreach (var to in from.ConstrainedVertices) {
					if (indices.TryGetValue(to, out int toIndex) && fromIndex < toIndex)
						triangulator.AddEdge(fromIndex, toIndex);
				}
			}

			try {
				triangulator.Triangulate(output);
			}
			catch (Exception ex) {
				TriangulationError = ex;
				HashSet<EditorVertex> alive = [.. GetVertices()];
				for (int i = triangles.Count - 3; i >= 0; i -= 3) {
					if (!alive.Contains(triangles[i]) || !alive.Contains(triangles[i + 1]) || !alive.Contains(triangles[i + 2]))
						triangles.RemoveRange(i, 3);
				}
				return;
			}

			triangles.Clear();
			foreach (int index in output)
				triangles.Add(points[index]);
		}

		/// <summary>
		/// Is a local position strictly inside the hull?
		/// </summary>
		public bool InsideHull(float x, float y) {
			bool inside = false;
			int n = ShapeEdges.Count;
			for (int i = 0, j = n - 1; i < n; j = i++) {
				EditorVertex a = ShapeEdges[i], b = ShapeEdges[j];
				if ((a.Y > y) != (b.Y > y) && x < (b.X - a.X) * (y - a.Y) / (b.Y - a.Y) + a.X)
					inside = !inside;
			}
			return inside;
		}

		private bool UseSourceTriangles() {
			var source = SourceTriangles!;
			HashSet<EditorVertex> current = [.. GetVertices()];
			if (source.Count == 0 || source.Count % 3 != 0 || !source.All(current.Contains)) {
				SourceTriangles = null;
				return false;
			}

			triangles.Clear();
			triangles.AddRange(source);
			keepSourceOrder = true;
			return true;
		}

		public void SetupWorldTransform() {
			WorldTransform = Transformation.CalculateWorldTransformation(pos, Rotation, scale, Vector2F.Zero, TransformMode.Normal, Slot.Bone.WorldTransform);
		}

		/// <summary>
		/// Three vertices per triangle, in draw order
		/// </summary>
		[JsonIgnore] public IReadOnlyList<EditorVertex> Triangles => DrawOrder();

		int triangleVersion;
		int sortedKey;
		int sourceKey;
		bool keepSourceOrder;
		readonly List<EditorVertex> sortedTriangles = [];
		readonly List<(int Rank, int Index)> sortKeys = [];

		// Each triangle belongs to the bone that weighs its vertices most
		// Higher in the Weights list draws on top
		private IReadOnlyList<EditorVertex> DrawOrder() {
			if (Weights.Count < 2) return triangles;

			var hash = new HashCode();
			hash.Add(triangleVersion);
			hash.Add(triangles.Count);
			foreach (var weights in Weights) {
				hash.Add(weights);
				hash.Add(weights.Version);
			}
			int key = hash.ToHashCode();
			// Imported triangles keep their order until something changes.
			if (keepSourceOrder) {
				keepSourceOrder = false;
				sourceKey = key;
			}
			if (key == sourceKey) return triangles;
			if (key == sortedKey && sortedTriangles.Count == triangles.Count) return sortedTriangles;
			sortedKey = key;

			sortKeys.Clear();
			for (int i = 0; i + 2 < triangles.Count; i += 3)
				sortKeys.Add((TopBone(triangles, i), i));
			// Lower in the list draws first, ties keep their order
			sortKeys.Sort((x, y) => x.Rank != y.Rank ? y.Rank.CompareTo(x.Rank) : x.Index.CompareTo(y.Index));

			sortedTriangles.Clear();
			foreach (var sortKey in sortKeys) {
				sortedTriangles.Add(triangles[sortKey.Index]);
				sortedTriangles.Add(triangles[sortKey.Index + 1]);
				sortedTriangles.Add(triangles[sortKey.Index + 2]);
			}
			return sortedTriangles;
		}

		private int TopBone(IReadOnlyList<EditorVertex> vertices, int start) {
			int best = 0;
			float bestWeight = -1;
			for (int i = 0; i < Weights.Count; i++) {
				var weights = Weights[i];
				float sum = weights.TryGetVertexWeight(vertices[start])
					+ weights.TryGetVertexWeight(vertices[start + 1])
					+ weights.TryGetVertexWeight(vertices[start + 2]);
				if (sum > bestWeight) {
					bestWeight = sum;
					best = i;
				}
			}
			return best;
		}

		/// <summary>
		/// Puts the Weights list back in the order the imported triangles were sorted by, if one fits.
		/// </summary>
		public void OrderWeightsFromTriangles() {
			var source = SourceTriangles;
			if (source == null || Weights.Count < 2 || source.Count % 3 != 0) return;

			// first bone seen ends up at the bottom
			List<EditorWeights> seen = [];
			EditorWeights? last = null;
			for (int i = 0; i + 2 < source.Count; i += 3) {
				var weights = Weights[TopBone(source, i)];
				if (weights == last) continue;
				if (seen.Contains(weights)) return;
				seen.Add(weights);
				last = weights;
			}

			var before = Weights.ToList();
			Weights.Clear();
			Weights.AddRange(Enumerable.Reverse(seen));
			Weights.AddRange(before.Where(x => !seen.Contains(x)));

			int previous = int.MaxValue;
			for (int i = 0; i + 2 < source.Count; i += 3) {
				int rank = TopBone(source, i);
				if (rank > previous) {
					Weights.Clear();
					Weights.AddRange(before);
					return;
				}
				previous = rank;
			}
		}

		/// <summary>
		/// Sequential edges to define the shape.
		/// </summary>
		public List<EditorVertex> ShapeEdges = [];

		public virtual IEnumerable<EditorVertex> GetVertices() {
			foreach (var co in ShapeEdges) {
				co.Attachment = this;
				yield return co;
			}
		}

		public static ConCommand nm4_autouvmesh = new("nm4_autouvmesh", (_, in _) => {
			var modeleditor = ModelEditor.Active;
			var selected = modeleditor.LastSelectedObject;
			if (selected is not EditorMeshAttachment meshAttachment) {
				Logs.Warn("Cannot perform this operation on a non-mesh attachment.");
				return;
			}

			foreach (var hullpoint in meshAttachment.ShapeEdges)
				hullpoint.AutoUV();
			foreach (var steinerpoint in meshAttachment.SteinerPoints)
				steinerpoint.AutoUV();

			int i = meshAttachment.ShapeEdges.Count + meshAttachment.SteinerPoints.Count;
			Logs.Info($"Auto-UV'd {i} vertices on mesh '{selected.GetName()}'");
		}, "Automatically calculates texture coordinates for the last selected object");

		public static void NormalizeWeights(float[] numbers, int refrainIndex) {
			if (numbers == null || numbers.Length == 0)
				throw new ArgumentException("Input array must not be null or empty.");
			if (refrainIndex < 0 || refrainIndex >= numbers.Length)
				throw new ArgumentOutOfRangeException(nameof(refrainIndex), "Index out of range.");

			float refrainValue = numbers[refrainIndex];
			float totalOther = 0f;

			for (int i = 0; i < numbers.Length; i++) {
				if (i != refrainIndex)
					totalOther += numbers[i];
			}

			float targetSum = 1f - refrainValue;

			if (totalOther == 0f) {
				for (int i = 0; i < numbers.Length; i++) {
					if (i != refrainIndex)
						numbers[i] = targetSum / (numbers.Length - 1);
				}
			}
			else {
				float scale = targetSum / totalOther;

				for (int i = 0; i < numbers.Length; i++) {
					if (i != refrainIndex)
						numbers[i] *= scale;
				}
			}

			numbers[refrainIndex] = refrainValue;
		}
		public bool GetVertexWeightInformation(EditorVertex vertex, [NotNullWhen(true)] out EditorBone[]? bones, [NotNullWhen(true)] out float[]? weights, [NotNullWhen(true)] out Vector2F[]? positions) {
			List<EditorBone> boneList = [];
			List<float> weightList = [];
			List<Vector2F> posList = [];

			foreach (var weightData in Weights) {
				// small negative weights are kept
				if (weightData.TryGetVertexInfo(vertex, out float weight, out Vector2F pos) && weight != 0) {
					boneList.Add(weightData.Bone);
					weightList.Add(weight);
					posList.Add(pos);
				}
			}

			Debug.Assert(boneList.Count == weightList.Count && weightList.Count == posList.Count && posList.Count == boneList.Count);
			if (boneList.Count == 0) {
				bones = null;
				weights = null;
				positions = null;
				return false;
			}

			bones = boneList.ToArray();
			weights = weightList.ToArray();
			positions = posList.ToArray();
			return true;
		}
		public void SetVertexWeight(EditorVertex vertex, EditorBone bone, float weight, bool validate = true) {
			EditorWeights? weightData = Weights.FirstOrDefault(x => x.Bone == bone);
			// Debug.Assert(weightData != null, "No weight data. Bone likely isn't bound.");
			if (weightData == null) return;

			if (validate) {
				// Add up all weights
				float[] allWeights = new float[Weights.Count];
				int refrain = -1;
				for (int i = 0; i < Weights.Count; i++) {
					allWeights[i] = Weights[i].TryGetVertexWeight(vertex);
					if (Weights[i].Bone == bone)
						refrain = i;
				}
				allWeights[refrain] = weight;

				NormalizeWeights(allWeights, refrain);
				for (int i = 0; i < allWeights.Length; i++) {
					Weights[i].SetVertexWeight(vertex, allWeights[i]);
				}
			}

			weightData.SetVertexWeight(vertex, weight);
		}
		public Vector2F CalculateVertexWorldPosition(Transformation transform, EditorVertex vertex) {
			var owner = vertex.Attachment == GeometrySource ? GeometrySource : this;
			var weights = owner.Weights;
			if (weights.Count <= 0) {
				var offset = GetDeformOffset(vertex);
				return transform.LocalToWorld(vertex.X + offset.X, vertex.Y + offset.Y);
			}

			var boneDeform = GetBoneDeform(vertex, out var deformKey);
			Vector2F pos = Vector2F.Zero;
			for (int i = 0; i < weights.Count; i++) {
				var weightData = weights[i];
				if (weightData.IsEmpty) continue;
				var vertLocalPos = weightData.TryGetVertexPosition(vertex);
				var weight = weightData.TryGetVertexWeight(vertex);
				if (boneDeform != null && boneDeform.TryGetValue((deformKey, weightData.Bone), out var offset))
					vertLocalPos += offset;
				pos += weightData.Bone.WorldTransform.LocalToWorld(vertLocalPos) * weight;
			}

			return pos;
		}

		/// <summary>
		/// Attachment whose vertices, weights and triangles this one is drawn with
		/// </summary>
		[JsonIgnore] public virtual EditorVertexAttachment GeometrySource => this;
		[JsonIgnore] public virtual int VertexCount => ShapeEdges.Count;

		List<EditorVertex>? vertexList;
		Dictionary<EditorVertex, int>? vertexIndex;
		private void ValidateVertexIndex() {
			if (vertexList != null && vertexList.Count == VertexCount) return;
			vertexList = GetVertices().ToList();
			vertexIndex = new(vertexList.Count);
			for (int i = 0; i < vertexList.Count; i++)
				vertexIndex[vertexList[i]] = i;
		}

		public int IndexOfVertex(EditorVertex vertex) {
			ValidateVertexIndex();
			return vertexIndex!.TryGetValue(vertex, out int index) ? index : -1;
		}

		public EditorVertex? VertexAt(int index) {
			ValidateVertexIndex();
			return index >= 0 && index < vertexList!.Count ? vertexList[index] : null;
		}

		/// <summary>
		/// Unweighted deform offset, zero in setup mode
		/// </summary>
		public Vector2F GetDeformOffset(EditorVertex vertex) {
			if (!(ModelEditor.Active?.AnimationMode ?? false)) return Vector2F.Zero;
			var deform = DeformAttachment.CurrentDeform;
			if (deform == null) return Vector2F.Zero;
			return deform.TryGetValue(DeformVertex(vertex), out var offset) ? offset : Vector2F.Zero;
		}

		/// <summary>
		/// Weighted deform offsets and the vertex they're keyed by
		/// </summary>
		public Dictionary<(EditorVertex, EditorBone), Vector2F>? GetBoneDeform(EditorVertex vertex, out EditorVertex key) {
			key = vertex;
			if (!(ModelEditor.Active?.AnimationMode ?? false)) return null;
			var deform = DeformAttachment.CurrentBoneDeform;
			if (deform == null || deform.Count == 0) return null;
			key = DeformVertex(vertex);
			return deform;
		}

		// Linked meshes keep their own copy of the vertices, match them up by index
		private EditorVertex DeformVertex(EditorVertex vertex) {
			var source = DeformAttachment;
			var owner = vertex.Attachment;
			if (owner == null || owner == source) return vertex;
			return source.VertexAt(owner.IndexOfVertex(vertex)) ?? vertex;
		}
		/// <summary>
		/// Removes a vertex while ensuring the removal of constraint references.
		/// </summary>
		/// <param name="vertex"></param>
		public virtual bool RemoveVertex(EditorVertex vertex) {
			while (vertex.HasConstrainedEdges)
				vertex.UnconstrainFrom(vertex.ConstrainedVertices.First());

			return ShapeEdges.Remove(vertex);
		}
		public void SelectVertex(EditorVertex vertex, bool multiselect = false) {
			if (multiselect) {
				bool wasIn = SelectedVertices.Remove(vertex);

				if (!wasIn)
					SelectedVertices.Add(vertex);

				return;
			}

			SelectedVertices.Clear();
			SelectedVertices.Add(vertex);
			VertexSelected?.Invoke(vertex);
		}
		public IEnumerable<EditorVertex> GetSelectedVertices() => SelectedVertices;
		public override bool OnSelected() {
			SelectedVertices.Clear();
			return true;
		}
		public override bool OnUnselected() {
			// Allows the ESCAPE key and clicking out-of-bounds to unselect all vertices
			if (SelectedVertices.Count > 0) {
				SelectedVertices.Clear();
				return false;
			}

			return true; // allow modeleditor to unselect
		}
		public override Vector2F GetWorldPosition() => WorldTransform.Translation;
		public override void SetWorldPosition(Vector2F pos, bool additive = false) {
			var bone = Slot.Bone.WorldTransform;
			this.pos = bone.WorldToLocal(additive ? bone.LocalToWorld(this.pos) - pos : pos);
		}
		public override float GetWorldRotation() => WorldTransform.LocalToWorldRotation(0) + GetRotation();
		public override float GetScreenRotation() {
			var wp = WorldTransform.Translation;
			var wl = WorldTransform.LocalToWorld(Scale.X, 0);
			var d = (wl - wp);
			var r = MathF.Atan2(d.Y, d.X).ToDegrees();
			return r;
		}
		public override bool CanTranslate() => true;
		public override bool CanRotate() => true;
		public override bool CanScale() => true;
		public override bool CanShear() => false;
		public override bool CanHide() => true;
		public override float GetTranslationX(UserTransformMode transform = UserTransformMode.LocalSpace) => Position.X;
		public override float GetTranslationY(UserTransformMode transform = UserTransformMode.LocalSpace) => Position.Y;
		public override float GetRotation(UserTransformMode transform = UserTransformMode.LocalSpace) => Rotation;
		public override float GetScaleX() => Scale.X;
		public override float GetScaleY() => Scale.Y;
		public override void EditTranslationX(float value, UserTransformMode transform = UserTransformMode.LocalSpace) => pos.X = value;
		public override void EditTranslationY(float value, UserTransformMode transform = UserTransformMode.LocalSpace) => pos.Y = value;
		public override void EditRotation(float value, bool localTo = true) {
			if (!localTo)
				value = WorldTransform.WorldToLocalRotation(value);

			Rotation = value;
		}
		public override void EditScaleX(float value) => scale.X = value;
		public override void EditScaleY(float value) => scale.Y = value;

		public virtual Color DetermineVertexColor(bool selected, bool highlighted) {
			return Color.White;
		}

		public void RenderVertex(EditorVertex vertex, bool isHighlighted, Vector2F? pos = null) {
			System.Numerics.Vector2 drawPos = (pos ?? CalculateVertexWorldPosition(WorldTransform, vertex)).ToNumerics();
			bool inWeightMode = ModelEditor.Active.Editor.InWeightsMode;
			var camsize = ModelEditor.Active.Editor.CameraZoom;

			var isSelectedTruly = SelectedVertices.Contains(vertex);
			var isSelected = isSelectedTruly || SelectedVertices.Count == 0;

			if (!inWeightMode) {
				float size = (isSelected ? 4f : 2f) + (isHighlighted ? 1f : 0f);

				Raylib.DrawCircleV(drawPos, (size) / camsize, Color.Black);
				Raylib.DrawCircleV(drawPos, (size - 1) / camsize, DetermineVertexColor(isSelected, isHighlighted));
			}
			else {
				// Pies only on the mesh the Weights tool is on
				bool pie = WeightsPanel.weights_pies.GetBool() && VertexWeightOperator.Target == this;
				if (pie && WeightsPanel.weights_selected.GetBool())
					pie = isSelectedTruly || ModelEditor.Active.Weights.SelectedBones.Any(w => w.TryGetVertexWeight(vertex) > 0);

				if (!pie) {
					Raylib.DrawCircleV(drawPos, 3f / camsize, Color.Black);
					Raylib.DrawCircleV(drawPos, 2f / camsize, new Color(40, 210, 210));
				}
				else {
					float radius = (isSelectedTruly ? 10f : 7f) / camsize;
					float start = 0;
					foreach (var weights in Weights) {
						float weight = weights.TryGetVertexWeight(vertex);
						if (weight <= 0) continue;
						Raylib.DrawCircleSector(drawPos, radius, start * 360, (start + weight) * 360, 32, WeightColor(weights));
						start += weight;
					}
				}

				if (isHighlighted) {
					Rlgl.DrawRenderBatchActive();
					Rlgl.SetLineWidth(3f);
					Raylib.DrawCircleLinesV(drawPos, 20 / camsize, Color.White);
					Rlgl.DrawRenderBatchActive();
					Rlgl.SetLineWidth(1f);
				}
			}
		}


		public delegate void OnVertexSelected(EditorVertex vertex);
		public event OnVertexSelected? VertexSelected;




		static readonly Color[] weightColors = [
			new(255, 136, 0), new(150, 235, 50), new(50, 150, 255), new(50, 235, 150), new(140, 70, 255),
			new(255, 220, 40), new(255, 70, 170), new(40, 220, 235), new(235, 55, 55), new(190, 120, 255)
		];

		public Color WeightColor(EditorWeights weights, int alpha = 255) {
			var color = weightColors[Math.Max(Weights.IndexOf(weights), 0) % weightColors.Length];
			return color with { A = (byte)alpha };
		}
	}
}
