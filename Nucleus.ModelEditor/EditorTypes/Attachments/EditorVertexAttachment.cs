// TODO: This file needs to be split apart!

using Newtonsoft.Json;
using Nucleus.Models;
using Nucleus.Types;
using Poly2Tri;
using Raylib_cs;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Nucleus.ModelEditor
{
	public abstract class EditorVertexAttachment : EditorAttachment {
		private Vector2F pos, scale = new(1, 1);
		public List<EditorWeights> Weights = [];
		public HashSet<EditorVertex> SelectedVertices = [];
		[JsonIgnore] public Transformation WorldTransform;
		[JsonIgnore] public Shape Shape;


		public Vector2F Position { get => pos; set => pos = value; }
		public float Rotation { get; set; }
		public Vector2F Scale { get => scale; set => scale = value; }
		public Color Color { get; set; } = Color.White;


		protected List<Triangle> triangles = [];
		[JsonIgnore] public bool Invalidated { get; set; } = true;
		public bool Invalidate() => Invalidated = true;
		public virtual void UpdateShape(Shape shape) {

		}
		public void RefreshDelaunator() {
			if (Invalidated) {
				triangles.Clear();

				TriPoint[] triPoints = new TriPoint[ShapeEdges.Count];
				for (int i = 0; i < ShapeEdges.Count; i++) {
					var vertex = ShapeEdges[i];

					triPoints[i] = new() {
						X = vertex.X,
						Y = vertex.Y,
						AssociatedObject = vertex
					};
				}

				var workingShape = new Shape(triPoints);

				Dictionary<EditorVertex, HashSet<EditorVertex>> avoidDuplicateEdges = [];
				UpdateShape(workingShape);

				foreach (var constrainedFromTripoint in workingShape.GetAllPoints()) {
					var constrainedFrom = constrainedFromTripoint.AssociatedObject as EditorVertex ?? throw new Exception();
					if (!constrainedFrom.HasConstrainedEdges) continue;

					foreach (var constrainedTo in constrainedFrom.ConstrainedVertices) {
						// Find the tri point constrainedTo is associated with
						// This entire thing REALLY needs to be fixed up. I'm 
						// just trying to get it working for the sake of getting
						// it working, then optimize afterwards if needed
						var constrainedToTripoint = workingShape.GetAllPoints().FirstOrDefault(x => x.AssociatedObject == constrainedTo) ?? throw new Exception();

						if (!avoidDuplicateEdges.TryGetValue(constrainedFrom, out var fromHash)) {
							fromHash = []; avoidDuplicateEdges[constrainedFrom] = fromHash;
						}

						if (!avoidDuplicateEdges.TryGetValue(constrainedTo, out var toHash)) {
							toHash = []; avoidDuplicateEdges[constrainedTo] = toHash;
						}

						if (fromHash.Add(constrainedFrom) && fromHash.Add(constrainedTo)) {
							workingShape.ConstrainedEdges.Add(new(constrainedFromTripoint, constrainedToTripoint));
						}
					}
				}

				workingShape.Triangulate(triangles);
				Shape = workingShape;
				Invalidated = false;

				//foreach (var point in Shape.Points)
				//triPointToMeshVertex[point] = point.AssociatedObject as MeshVertex ?? throw new Exception("No mesh vertex association");

				//foreach (var point in Shape.SteinerPoints)
				//triPointToMeshVertex[point] = point.AssociatedObject as MeshVertex ?? throw new Exception("No mesh vertex association");
			}
		}

		public void SetupWorldTransform() {
			WorldTransform = Transformation.CalculateWorldTransformation(pos, Rotation, scale, Vector2F.Zero, TransformMode.Normal, Slot.Bone.WorldTransform);
		}

		public IEnumerable<Triangle> Triangles {
			get {
				foreach (var tri in triangles) {
					yield return tri;
				}
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

		public static ConCommand nm4_autouvmesh = ConCommand.Register("nm4_autouvmesh", (_, _) => {
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
				if (weightData.TryGetVertexInfo(vertex, out float weight, out Vector2F pos)) {
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
				// Get the delta weight
				var deltaWeight = weight - weightData.TryGetVertexWeight(vertex);
				// Add up all weights
				float[] allWeights = new float[Weights.Count];
				int refrain = -1;
				for (int i = 0; i < Weights.Count; i++) {
					allWeights[i] = Weights[i].TryGetVertexWeight(vertex);
					if (Weights[i].Bone == bone)
						refrain = i;
				}

				NormalizeWeights(allWeights, refrain);
				for (int i = 0; i < allWeights.Length; i++) {
					Weights[i].SetVertexWeight(vertex, allWeights[i]);
				}
			}

			weightData.SetVertexWeight(vertex, weight);
		}
		public Vector2F CalculateVertexWorldPosition(Transformation transform, EditorVertex vertex) {
			Vector2F basePosition = transform.LocalToWorld(vertex.X, vertex.Y);
			if (Weights.Count <= 0)
				return basePosition;

			Vector2F pos = Vector2F.Zero;
			foreach (var weightData in Weights) {
				if (weightData.IsEmpty) continue;
				var vertLocalPos = weightData.TryGetVertexPosition(vertex);
				var weight = weightData.TryGetVertexWeight(vertex);
				pos += weightData.Bone.WorldTransform.LocalToWorld(vertLocalPos) * weight;
			}

			return pos;
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
				float size = (isSelected ? 9f : 7f) + (isHighlighted ? 1f : 0f);
				float totalWeight = 0;
				EditorVertexAttachment attachment = vertex.Attachment;
				List<EditorWeights> weights = attachment.Weights;
				EditorBone? selectedBone = ModelEditor.Active.Weights.ActiveWeights?.Bone;
				for (int i = 0; i < weights.Count; i++) {
					var weightPair = weights[i];
					bool isSelectedBone = selectedBone == weightPair.Bone;
					float weight = weightPair.TryGetVertexWeight(vertex);

					Raylib.DrawCircleSector(
						drawPos,
						(size + ((isSelectedBone || isSelectedTruly) ? 6 : 0)) / camsize, totalWeight * 360, (totalWeight * 360) + (weight * 360),
						32,
						BoneWeightListIndexToColor(i, (isSelected || isHighlighted) ? 255 : 140)
						);
					totalWeight += weight;
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




		public static Color BoneWeightListIndexToColor(int index, int alpha = 255) {
			var baselineHue = 194 + (index * 90);
			return (new Vector3(baselineHue, 0.78f, 1.00f)).ToRGB((float)(alpha) / 255f);
		}
	}
}
