// TODO: This file needs to be split apart!

using Newtonsoft.Json;
using Nucleus.Core;
using Nucleus.ManagedMemory;
using Nucleus.ModelEditor.UI;
using Nucleus.Models;
using Nucleus.Types;
using Nucleus.UI;
using Nucleus.UI.Elements;
using Poly2Tri;
using Raylib_cs;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Triangle = Poly2Tri.Triangle;

namespace Nucleus.ModelEditor
{
	public enum EditMesh_Mode
	{
		Modify,
		Create,
		Delete,
		New,
		Reset
	}
	public class EditMeshOperator : Operator
	{
		public static ConVar meshedit_triangles = ConVar.Register("meshedit_triangles", "0", ConsoleFlags.Saved, "Visualizes the triangulated mesh attachment when using the Edit Mesh operator.");
		public static ConVar meshedit_dim = ConVar.Register("meshedit_dim", "0", ConsoleFlags.Saved, "Dims the mesh attachment's texture when using the Edit Mesh operator.");
		public static ConVar meshedit_isolate = ConVar.Register("meshedit_isolate", "0", ConsoleFlags.Saved, "Isolates the active mesh attachment when using the Edit Mesh operator.");

		public EditMesh_Mode CurrentMode { get; private set; } = EditMesh_Mode.Modify;
		public override string Name => $"Edit Mesh: {CurrentMode}";
		public override bool OverrideSelection => true;
		public override Type[]? SelectableTypes => [typeof(MeshVertex)];

		public void SetMode(EditMesh_Mode mode) {
			CurrentMode = mode;
			UpdateButtonState();
		}

		private Button ModifyButton, CreateButton, DeleteButton, NewButton, ResetButton;
		private Checkbox Triangles, Dim, Isolate;
		List<MeshVertex> WorkingLines = [];
		private void UpdateButtonState() {
			ModifyButton.Pulsing = CurrentMode == EditMesh_Mode.Modify;
			CreateButton.Pulsing = CurrentMode == EditMesh_Mode.Create;
			DeleteButton.Pulsing = CurrentMode == EditMesh_Mode.Delete;
			NewButton.Pulsing = CurrentMode == EditMesh_Mode.New;
			ResetButton.Pulsing = CurrentMode == EditMesh_Mode.Reset;
			WorkingLines.Clear();
		}

		public override void ChangeEditorProperties(CenteredObjectsPanel panel) {
			panel.Size = new(0, 182);

			var win = panel.Add<Window>();
			win.Size = new(330, panel.Size.H);
			win.Center();
			win.Title = "Edit Mesh";
			win.HideNonCloseButtons();

			var row1 = win.Add(new FlexPanel() { DockPadding = RectangleF.TLRB(0, 4, 4, 0), Dock = Dock.Top, Size = new(0, 32), Direction = Directional180.Horizontal, ChildrenResizingMode = FlexChildrenResizingMode.StretchToOppositeDirection });
			var row2 = win.Add(new FlexPanel() { DockPadding = RectangleF.TLRB(0, 4, 4, 0), Dock = Dock.Top, Size = new(0, 32), Direction = Directional180.Horizontal, ChildrenResizingMode = FlexChildrenResizingMode.StretchToOppositeDirection });

			ModifyButton = row1.Add(new Button() { Text = "Modify", AutoSize = true, TextPadding = new(32, 0) }); ModifyButton.MouseReleaseEvent += (_, _, _) => SetMode(EditMesh_Mode.Modify);
			CreateButton = row1.Add(new Button() { Text = "Create", AutoSize = true, TextPadding = new(32, 0) }); CreateButton.MouseReleaseEvent += (_, _, _) => SetMode(EditMesh_Mode.Create);
			DeleteButton = row1.Add(new Button() { Text = "Delete", AutoSize = true, TextPadding = new(32, 0) }); DeleteButton.MouseReleaseEvent += (_, _, _) => SetMode(EditMesh_Mode.Delete);

			NewButton = row2.Add(new Button() { Text = "New", AutoSize = true, TextPadding = new(32, 0) }); NewButton.MouseReleaseEvent += (_, _, _) => SetMode(EditMesh_Mode.New);
			ResetButton = row2.Add(new Button() { Text = "Reset", AutoSize = true, TextPadding = new(32, 0) }); ResetButton.MouseReleaseEvent += (_, _, _) => SetMode(EditMesh_Mode.Reset);

			var row3 = win.Add(new FlexPanel() { DockPadding = RectangleF.TLRB(0, 4, 4, 0), Dock = Dock.Top, Size = new(0, 32), Direction = Directional180.Horizontal, ChildrenResizingMode = FlexChildrenResizingMode.StretchToOppositeDirection });

			row3.Add(out Triangles); row3.Add(new Label() { Text = "Triangles", AutoSize = true });
			row3.Add(out Dim); row3.Add(new Label() { Text = "Dim", AutoSize = true });
			row3.Add(out Isolate); row3.Add(new Label() { Text = "Isolate", AutoSize = true });

			Triangles.BindToConVar(meshedit_triangles);
			Dim.BindToConVar(meshedit_dim);
			Isolate.BindToConVar(meshedit_isolate);

			UpdateButtonState();

			win.Removed += (s) => {
				if (IValidatable.IsValid(s)) {
					ModelEditor.Active.File.DeactivateOperator(true);
				}
			};
		}

		public MeshVertex? AttachedVertex;
		public MeshVertex? HoveredVertex;
		/// <summary>
		/// In the context of <see cref="EditMesh_Mode.Create"/>, this is the created vertex.
		/// <br/>
		/// In the context of <see cref="EditMesh_Mode.Modify"/>, this is the clicked vertex.
		/// </summary>
		public MeshVertex? ClickedVertex;
		public bool IsHoveredSteinerPoint;
		public bool IsClickedSteinerPoint;
		public EditorMeshAttachment Attachment => this.UIDeterminations.Last as EditorMeshAttachment ?? throw new Exception("Wtf?");

		public override void Think(ModelEditor editor, Vector2F mousePos) {
			EditorPanel editorPanel = editor.Editor;
			float closestVertexDist = 100000000;

			HoveredVertex = null;
			IsHoveredSteinerPoint = false;

			PatchAttachmentTransform();
			System.Numerics.Vector2 mp = (Attachment.WorldTransform.WorldToLocal(mousePos)).ToNumerics();

			var camsize = ModelEditor.Active.Editor.CameraZoom;
			var dist = 32f / camsize;
			var array = (CurrentMode == EditMesh_Mode.New ? WorkingLines : Attachment.ShapeEdges);
			for (int i = 0; i < array.Count; i++) {
				var vertex = array[i];
				var vertexDistance = mp.ToNucleus().Distance(vertex.ToVector());
				if (vertexDistance < closestVertexDist) {
					closestVertexDist = vertexDistance;
					HoveredVertex = vertex;
					IsHoveredSteinerPoint = false;
				}
			}

			if (closestVertexDist > dist && CurrentMode != EditMesh_Mode.New) {
				closestVertexDist = 1000000000;
				for (int i = 0; i < Attachment.SteinerPoints.Count; i++) {
					var vertex = Attachment.SteinerPoints[i];
					var vertexDistance = mp.ToNucleus().Distance(vertex.ToVector());
					if (vertexDistance < closestVertexDist) {
						closestVertexDist = vertexDistance;
						HoveredVertex = vertex;
						IsHoveredSteinerPoint = true;
					}
				}
			}


			if (closestVertexDist > dist) {
				HoveredVertex = null;
			}

			RestoreAttachmentTransform();
		}

		public Vector2F ClampVertexPosition(Vector2F pos) {
			return new(
				Math.Clamp(pos.X, Attachment.LocalWidth / -2, Attachment.LocalWidth / 2),
				Math.Clamp(pos.Y, Attachment.LocalHeight / -2, Attachment.LocalHeight / 2)
			);
		}

		public override bool SelectMultiple => true;
		public override void Selected(ModelEditor editor, IEditorType type) {
			base.Selected(editor, type);
		}

		public override void Clicked(ModelEditor editor, Vector2F mousePos) {
			base.Clicked(editor, mousePos);
			PatchAttachmentTransform();

			// Overrides, for the create vertex mode, but designed this way in case
			// other things need it
			MeshVertex? clickVertexOverride = null;
			bool? hoveredSteinerOverride = null;

			switch (CurrentMode) {
				case EditMesh_Mode.Create:
					var newPoint = Attachment.WorldTransform.WorldToLocal(editor.Editor.ScreenToGrid(mousePos));

					// TODO + HACK; the 0.05f thing avoids issues with duplicate points.
					// Really, I probably should just... wait to start dragging before
					// creating the vertex... just lazy right now
					MeshVertex vertex = MeshVertex.FromVector(newPoint + new Vector2F(0.05f), Attachment);
					Attachment.SteinerPoints.Add(vertex);
					Attachment.Invalidate();

					// Null hover-vertex means we're just creating a lone vertex, but if 
					// a vertex is hovered, then this creates a vertex with a constrained edge.

					// Later, I need to also resolve during dragging if the user has dragged
					// the vertex towards another vertex or not, and if so, remove the existing
					// created vertex, then connect the current hover vertex to the hover vertex
					// during the dragging context.

					if (HoveredVertex != null) {
						vertex.ConstrainTo(HoveredVertex);
						AttachedVertex = HoveredVertex;
					}

					clickVertexOverride = vertex;
					hoveredSteinerOverride = true;
					ModelEditor.Active.File.UpdateVertexPositions(Attachment, onlyThisVertex: vertex);
					break;
				case EditMesh_Mode.Delete:
					if (HoveredVertex != null) {
						Attachment.RemoveVertex(HoveredVertex);
						HoveredVertex = null;
						Attachment.Invalidate();
					}
					break;
				case EditMesh_Mode.New:
					if (WorkingLines.Count > 0 && HoveredVertex == WorkingLines.First()) {
						Attachment.ShapeEdges.Clear();
						RestoreAttachmentTransform();
						Attachment.ShapeEdges.AddRange(WorkingLines);
						Attachment.Invalidate();
						SetMode(EditMesh_Mode.Modify);
					}
					else {
						var newClickP = Attachment.WorldTransform.WorldToLocal(ClampVertexPosition(ModelEditor.Active.Editor.ScreenToGrid(mousePos) - Attachment.WorldTransform.Translation) + Attachment.WorldTransform.Translation);

						WorkingLines.Add(MeshVertex.FromVector(newClickP, Attachment));
					}
					break;
			}
			RestoreAttachmentTransform();

			ClickedVertex = clickVertexOverride ?? HoveredVertex;
			IsClickedSteinerPoint = hoveredSteinerOverride ?? IsHoveredSteinerPoint;
		}
		public override void DragStart(ModelEditor editor, Vector2F mousePos) {
			base.DragStart(editor, mousePos);
		}
		public override void Drag(ModelEditor editor, Vector2F startPos, Vector2F mousePos) {
			PatchAttachmentTransform();
			switch (CurrentMode) {
				case EditMesh_Mode.Create:
				case EditMesh_Mode.Modify:
					if (ClickedVertex != null) {
						var mouseGridPos = editor.Editor.ScreenToGrid(mousePos);
						var localized = Attachment.WorldTransform.WorldToLocal(mouseGridPos);
						var clamped = ClampVertexPosition(localized);

						ClickedVertex.SetPos(clamped);
						ModelEditor.Active.File.UpdateVertexPositions(ClickedVertex.Attachment);

						Attachment.Invalidate();
					}
					break;
			}
			RestoreAttachmentTransform();
		}
		public override void DragRelease(ModelEditor editor, Vector2F mousePos) {
			base.DragRelease(editor, mousePos);
			switch (CurrentMode) {
				case EditMesh_Mode.Create:
					if (HoveredVertex != ClickedVertex && HoveredVertex != null && ClickedVertex != null && AttachedVertex != null) {
						// Remove the clicked vertex
						ClickedVertex.Attachment.RemoveVertex(ClickedVertex);
						// Instead, constraint AttachedVertex to HoveredVertex
						AttachedVertex.ConstrainTo(HoveredVertex);
					}
					break;
			}

			AttachedVertex = null;
			ClickedVertex = null;
			IsClickedSteinerPoint = false;
		}

		private Transformation StoredAttachTransform;
		private void PatchAttachmentTransform() {
			if (CurrentMode != EditMesh_Mode.New) return;

			StoredAttachTransform = Attachment.WorldTransform;
			Attachment.WorldTransform = Transformation.CalculateWorldTransformation(StoredAttachTransform.Translation, 0, Vector2F.One, Vector2F.Zero, TransformMode.Normal);
			Attachment.SuppressWorldTransform = true;
		}
		private void RestoreAttachmentTransform() {
			if (CurrentMode != EditMesh_Mode.New) return;
			Attachment.WorldTransform = StoredAttachTransform;
			Attachment.SuppressWorldTransform = false;
		}
		public override bool RenderOverride() {
			if (CurrentMode == EditMesh_Mode.New) {
				var camsize = ModelEditor.Active.Editor.CameraZoom;

				PatchAttachmentTransform();
				Attachment.RenderStandalone();

				var mp = ClampVertexPosition(ModelEditor.Active.Editor.ScreenToGrid(ModelEditor.Active.Editor.GetMousePos()) - Attachment.WorldTransform.Translation) + Attachment.WorldTransform.Translation;

				for (int i = 0; i < WorkingLines.Count; i++) {
					var edge1 = WorkingLines[i].ToVector();
					var edge2 = WorkingLines[(i + 1) % WorkingLines.Count].ToVector();

					edge1 = Attachment.WorldTransform.LocalToWorld(edge1);
					edge2 = Attachment.WorldTransform.LocalToWorld(edge2);

					Color c = i < WorkingLines.Count - 1 ? new Color(150, 150, 255) : new Color(60, 120, 255, 160);
					Raylib.DrawLineV(edge1.ToNumerics(), edge2.ToNumerics(), c);

					var isHighlighted = HoveredVertex != null && edge1 == HoveredVertex;
					Raylib.DrawCircleV(edge1.ToNumerics(), (isHighlighted ? 4.5f : 2f) / camsize, new Color(isHighlighted ? 235 : 200, isHighlighted ? 235 : 200, 255));
				}

				if (HoveredVertex == null)
					Raylib.DrawCircleV(mp.ToNumerics(), 5 / camsize, Color.Lime);

				RestoreAttachmentTransform();

				return true;
			}

			if (EditorMeshAttachment.ShouldIsolate) {
				Attachment.Render();
				Attachment.RenderOverlay();

				return true;
			}

			return false;
		}

		internal void DrawCreateGizmo() {
			var camsize = ModelEditor.Active.Editor.CameraZoom;
			var mp = ModelEditor.Active.Editor.ScreenToGrid(ModelEditor.Active.Editor.GetMousePos());

			Raylib.DrawCircleV(mp.ToNumerics(), 5 / camsize, new(100, 180, 100));
		}
	}

	public class MeshVertex : IEditorType
	{
		public IEditorType? DeferPropertiesTo() => Attachment;
		public IEditorType? DeferTransformationsTo() => Attachment;
		public IEditorType? GetTransformParent() => Attachment;
		public float X;
		public float Y;
		public EditorMeshAttachment Attachment;


		public HashSet<MeshVertex> ConstrainedHashSet = [];
		[JsonIgnore] public bool HasConstrainedEdges => ConstrainedHashSet.Count > 0;

		/// <summary>
		/// Note that this only applies to <see cref="EditorMeshAttachment.SteinerPoints"/> -> <see cref="EditorMeshAttachment.ShapeEdges"/> or 
		/// <see cref="EditorMeshAttachment.SteinerPoints"/> -> <see cref="EditorMeshAttachment.SteinerPoints"/>. Shape edges inheritly are 
		/// constrained to the next <see cref="MeshVertex"/> in the array (or in the case of the last edge, the last point -> first point).
		/// </summary>
		public IEnumerable<MeshVertex> ConstrainedVertices {
			get {
				foreach (var vertex in ConstrainedHashSet)
					yield return vertex;
			}
		}

		public bool IsConstrainedTo(MeshVertex other) {
			Debug.Assert(this.Attachment != null);
			Debug.Assert(other.Attachment != null);

			if (this.Attachment != other.Attachment)
				throw new InvalidOperationException("Cannot constrain, or unconstrain, two vertices from two separate mesh attachments. Something has gone horribly wrong.");

			var otherContainsUs = other.ConstrainedHashSet.Contains(this);
			var weContainOther = this.ConstrainedHashSet.Contains(other);

			Debug.Assert(otherContainsUs == weContainOther);

			return otherContainsUs && weContainOther;
		}

		public void ConstrainTo(MeshVertex other) {
			Debug.Assert(this.Attachment != null);
			Debug.Assert(other.Attachment != null);

			if (this.Attachment != other.Attachment)
				throw new InvalidOperationException("Cannot constrain, or unconstrain, two vertices from two separate mesh attachments. Something has gone horribly wrong.");

			Debug.Assert(this != other, "Can not constrain a vertex to itself...");

			other.ConstrainedHashSet.Add(this);
			this.ConstrainedHashSet.Add(other);

			Attachment.Invalidate();
		}

		public void UnconstrainFrom(MeshVertex other) {
			Debug.Assert(this.Attachment != null);
			Debug.Assert(other.Attachment != null);

			if (this.Attachment != other.Attachment)
				throw new InvalidOperationException("Cannot constrain, or unconstrain, two vertices from two separate mesh attachments. Something has gone horribly wrong.");

			other.ConstrainedHashSet.Remove(this);
			this.ConstrainedHashSet.Remove(other);

			Attachment.Invalidate();
		}

		public string SingleName => "vertex";

		public string PluralName => "vertices";

		public bool Hovered { get; set; }
		public bool Selected { get; set; }
		public bool Hidden { get; set; }

		public void SetPos(float x, float y) {
			X = x;
			Y = y;
		}

		public bool HoverTest(Vector2F pos) {
			var dist = pos.Distance(Attachment.WorldTransform.LocalToWorld(X, Y));
			return dist < 2;
		}

		public void SetPos(Vector2F pos) => SetPos(pos.X, pos.Y);

		public Vector2F ToVector() => new(X, Y);
		public static MeshVertex FromVector(Vector2F vec, EditorMeshAttachment attachment) {
			MeshVertex vertex = new() { X = vec.X, Y = vec.Y, Attachment = attachment };

			// Bind *just* this vertex to the bone its parented to
			// Todo: auto-weigh to the bone somehow?
			attachment.SetVertexWeight(vertex, attachment.Slot.Bone, 1.0f, false);

			return vertex;
		}

		public static implicit operator Vector2F(MeshVertex v) => v.ToVector();

		public void OnMouseEntered() { }
		public void OnMouseLeft() { }

		public bool OnSelected() {
			Attachment.SelectVertex(this);
			return false;
		}
	}

	// So the reason this is structured so weirdly: Newtonsoft.Json doesn't seem to like serializing
	// MeshVertex types as keys. This weird pseudo-dictionary-only-really-o(1)-during-runtime structure
	// is *probably* good enough for the editor, but I hate it regardless and it's a mess
	public class EditorMeshWeights
	{
		public EditorBone Bone;

		private List<MeshVertex> __vertices = [];
		private List<float> __weights = [];
		private List<Vector2F> __positions = [];

		public List<MeshVertex> Vertices { get => __vertices; set { Invalidated = true; __vertices = value; } }
		public List<float> Weights { get => __weights; set { Invalidated = true; __weights = value; } }
		public List<Vector2F> Positions { get => __positions; set { Invalidated = true; __positions = value; } }

		[JsonIgnore] private Dictionary<MeshVertex, float> TrueWeights = [];
		[JsonIgnore] private Dictionary<MeshVertex, Vector2F> TruePositions = [];
		[JsonIgnore] public bool Invalidated = true;

		public void SetVertexWeight(MeshVertex vertex, float weight) {
			bool found = false;
			for (int i = 0; i < Vertices.Count; i++) {
				if (Vertices[i] == vertex) {
					Weights[i] = weight;
					found = true;
					break;
				}
			}

			if (found == false) {
				Vertices.Add(vertex);
				Weights.Add(weight);
				Positions.Add(Vector2F.Zero);
			}


			Invalidated = true;
		}
		public void SetVertexPos(MeshVertex vertex, Vector2F pos) {
			bool found = false;
			for (int i = 0; i < Vertices.Count; i++) {
				if (Vertices[i] == vertex) {
					Positions[i] = pos;
					found = true;
					break;
				}
			}

			if (found == false) {
				Vertices.Add(vertex);
				Weights.Add(0);
				Positions.Add(pos);
			}

			Invalidated = true;
		}

		public bool AddVertex(MeshVertex vertex, float? weight = null, Vector2F? pos = null) {
			for (int i = 0; i < Vertices.Count; i++) {
				if (Vertices[i] == vertex) {
					Weights[i] = weight ?? Weights[i];
					Positions[i] = pos ?? Positions[i];
					Invalidated = true;
					return true;
				}
			}

			return false;
		}

		public bool RemoveVertex(MeshVertex vertex) {
			for (int i = 0; i < Vertices.Count; i++) {
				if (Vertices[i] == vertex) {
					Vertices.RemoveAt(i);
					Positions.RemoveAt(i);
					Weights.RemoveAt(i);
					Invalidated = true;
					return true;
				}
			}

			return false;
		}

		public void Validate() {
			if (!Invalidated) return;

			TrueWeights.Clear();
			TruePositions.Clear();

			Debug.Assert(Vertices.Count == Weights.Count && Weights.Count == Positions.Count && Vertices.Count == Positions.Count);

			for (int i = 0; i < Vertices.Count; i++) {
				TrueWeights[Vertices[i]] = Weights[i];
				TruePositions[Vertices[i]] = Positions[i];
			}

			Invalidated = false;
		}


		/// <summary>
		/// Defaults to 0
		/// </summary>
		/// <param name="vertex"></param>
		/// <returns></returns>
		public float TryGetVertexWeight(MeshVertex vertex) {
			Validate();
			return TrueWeights.TryGetValue(vertex, out var value) ? value : 0;
		}
		public bool TryGetVertexWeight(MeshVertex vertex, [NotNullWhen(true)] out float weight) {
			Validate();
			return TrueWeights.TryGetValue(vertex, out weight);
		}

		/// <summary>
		/// Defaults to <see cref="Vector2F.Zero"/>
		/// </summary>
		/// <param name="vertex"></param>
		/// <returns></returns>
		public Vector2F TryGetVertexPosition(MeshVertex vertex) {
			Validate();
			return TruePositions.TryGetValue(vertex, out var value) ? value : Vector2F.Zero;
		}
		public bool TryGetVertexPosition(MeshVertex vertex, [NotNullWhen(true)] out Vector2F pos) {
			Validate();
			return TruePositions.TryGetValue(vertex, out pos);
		}

		public int Count {
			get {
				Debug.Assert(Weights.Count == Positions.Count);
				return Weights.Count;
			}
		}

		public bool IsEmpty => Count <= 0;

		public void Clear() {
			Weights.Clear();
			Positions.Clear();
			Invalidated = true;
		}
	}

	public class EditorMeshAttachment : EditorAttachment
	{
		public List<EditorMeshWeights> Weights = [];

		public delegate void OnVertexSelected(MeshVertex vertex);
		public event OnVertexSelected? VertexSelected;

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

		public void SetVertexWeight(MeshVertex vertex, EditorBone bone, float weight, bool validate = true) {
			EditorMeshWeights? weightData = Weights.FirstOrDefault(x => x.Bone == bone);
			Debug.Assert(weightData != null, "No weight data. Bone likely isn't bound.");
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

		public Vector2F CalculateVertexWorldPosition(Transformation transform, MeshVertex vertex) {
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

		public override string SingleName => "mesh";
		public override string PluralName => "meshes";
		public override string EditorIcon => "models/mesh.png";

		public string GetPath() => Path ?? $"<{Name}>";
		public string? Path { get; set; } = null;

		private Vector2F pos, scale = new(1, 1);

		/// <summary>
		/// Removes a vertex while ensuring the removal of constraint references.
		/// </summary>
		/// <param name="vertex"></param>
		public bool RemoveVertex(MeshVertex vertex) {
			while (vertex.HasConstrainedEdges)
				vertex.UnconstrainFrom(vertex.ConstrainedVertices.First());

			return SteinerPoints.Remove(vertex) || ShapeEdges.Remove(vertex);
		}

		public void SelectVertex(MeshVertex vertex, bool multiselect = false) {
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

		public IEnumerable<MeshVertex> GetSelectedVertices() => SelectedVertices;

		public Vector2F Position { get => pos; set => pos = value; }
		public float Rotation { get; set; }
		public Vector2F Scale { get => scale; set => scale = value; }

		[JsonIgnore] public Shape Shape;

		public override bool CanTranslate() => true;
		public override bool CanRotate() => true;
		public override bool CanScale() => true;
		public override bool CanShear() => false;
		public override bool CanHide() => true;

		public HashSet<MeshVertex> SelectedVertices = [];


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

		public override Vector2F GetWorldPosition() => WorldTransform.Translation;
		public override float GetWorldRotation() => WorldTransform.LocalToWorldRotation(0) + GetRotation();
		public override float GetScreenRotation() {
			var wp = WorldTransform.Translation;
			var wl = WorldTransform.LocalToWorld(Scale.X, 0);
			var d = (wl - wp);
			var r = MathF.Atan2(d.Y, d.X).ToDegrees();
			return r;
		}

		public override void EditScaleX(float value) => scale.X = value;
		public override void EditScaleY(float value) => scale.Y = value;

		[JsonIgnore] public Transformation WorldTransform;

		public Color Color { get; set; } = Color.White;

		public float LocalWidth => Slot.Bone.Model.Images.TextureAtlas.GetTextureRegion(Slot.Bone.Model.ResolveImage(Path).Name).Value.W;
		public float LocalHeight => Slot.Bone.Model.Images.TextureAtlas.GetTextureRegion(Slot.Bone.Model.ResolveImage(Path).Name).Value.H;

		private (Texture Texture, AtlasRegion Region, Vector2F TL, Vector2F TR, Vector2F BL, Vector2F BR) quadpoints() {
			var model = Slot.Bone.Model;

			ModelImage? image = model.ResolveImage(Path);
			if (image == null || Path == null) throw new Exception(":(");

			var succeeded = model.Images.TextureAtlas.TryGetTextureRegion(image.Name, out AtlasRegion region);
			if (!succeeded) region = AtlasRegion.MISSING;

			float width = region.H, height = region.W;
			float widthDiv2 = width / 2, heightDiv2 = height / 2;
			Texture tex = succeeded ? model.Images.TextureAtlas.Texture : Texture.MISSING;

			Vector2F TL = WorldTransform.LocalToWorld(-heightDiv2, -widthDiv2);
			Vector2F TR = WorldTransform.LocalToWorld(heightDiv2, -widthDiv2);
			Vector2F BR = WorldTransform.LocalToWorld(heightDiv2, widthDiv2);
			Vector2F BL = WorldTransform.LocalToWorld(-heightDiv2, widthDiv2);

			return (
				tex,
				region,
				TL,
				TR,
				BL,
				BR
			);
		}

		private List<Triangle> triangles = [];
		[JsonIgnore] public bool Invalidated { get; set; } = true;
		public bool Invalidate() => Invalidated = true;

		/// <summary>
		/// Sequential edges to define the shape.
		/// </summary>
		public List<MeshVertex> ShapeEdges = [];
		/// <summary>
		/// Arbitrary points within the shapes edges.
		/// </summary>
		public List<MeshVertex> SteinerPoints = [];

		public IEnumerable<MeshVertex> GetVertices() {
			foreach (var co in ShapeEdges) {
				co.Attachment = this;
				yield return co;
			}

			foreach (var sp in SteinerPoints) {
				sp.Attachment = this;
				yield return sp;
			}
		}

		//[JsonIgnore] public Dictionary<TriPoint, MeshVertex> triPointToMeshVertex = [];

		private void RefreshDelaunator() {
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

				Dictionary<MeshVertex, HashSet<MeshVertex>> avoidDuplicateEdges = [];

				foreach (var steinerPoint in SteinerPoints)
					workingShape.SteinerPoints.Add(new(steinerPoint.X, steinerPoint.Y, steinerPoint));

				foreach (var constrainedFromTripoint in workingShape.GetAllPoints()) {
					var constrainedFrom = constrainedFromTripoint.AssociatedObject as MeshVertex ?? throw new Exception();
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

		[JsonIgnore] public bool SuppressWorldTransform = false;

		/// <summary>
		/// This part renders the image, standalone, with no transformation (beyond translation transformation) <br></br>
		/// It's only used during the Mesh Editor / New operator and nowhere else
		/// </summary>
		public void RenderStandalone() {
			// todo ^^ missing texture (prob just purple-black checkerboard)
			var quadpoints = this.quadpoints();

			AtlasRegion region = quadpoints.Region;
			Texture tex = quadpoints.Texture;
			Vector2F BL = quadpoints.TL, BR = quadpoints.TR, TL = quadpoints.BL, TR = quadpoints.BR;

			Rlgl.Begin(DrawMode.TRIANGLES);
			Rlgl.SetTexture(((Texture2D)tex).Id);

			var color = (byte)(ShouldDim ? 90 : 255);
			Rlgl.Color4ub(color, color, color, 255);

			float uStart, uEnd, vStart, vEnd;
			uStart = (float)region.X / (float)tex.Width;
			uEnd = uStart + ((float)region.W / (float)tex.Width);

			vStart = ((float)region.Y / (float)tex.Height);
			vEnd = vStart + ((float)region.H / (float)tex.Height);

			Rlgl.TexCoord2f(uStart, vEnd); Rlgl.Vertex3f(BL.X, BL.Y, 0);
			Rlgl.TexCoord2f(uEnd, vStart); Rlgl.Vertex3f(TR.X, TR.Y, 0);
			Rlgl.TexCoord2f(uStart, vStart); Rlgl.Vertex3f(TL.X, TL.Y, 0);

			Rlgl.TexCoord2f(uEnd, vEnd); Rlgl.Vertex3f(BR.X, BR.Y, 0);
			Rlgl.TexCoord2f(uEnd, vStart); Rlgl.Vertex3f(TR.X, TR.Y, 0);
			Rlgl.TexCoord2f(uStart, vEnd); Rlgl.Vertex3f(BL.X, BL.Y, 0);

			Rlgl.End();

			Rlgl.DrawRenderBatchActive();
		}

		public static bool InEditMeshOperator => ModelEditor.Active.File.ActiveOperator is EditMeshOperator;
		public bool IsActiveAttachment => ModelEditor.Active.File.ActiveOperator is EditMeshOperator op && op.Attachment == this;
		public static bool RenderTriangles => InEditMeshOperator && EditMeshOperator.meshedit_triangles.GetBool();
		public static bool ShouldDim => InEditMeshOperator && EditMeshOperator.meshedit_dim.GetBool();
		public static bool ShouldIsolate => InEditMeshOperator && EditMeshOperator.meshedit_isolate.GetBool();

		public override void Render() {
			if (Hidden) return;
			RefreshDelaunator();

			if (!SuppressWorldTransform)
				WorldTransform = Transformation.CalculateWorldTransformation(pos, Rotation, scale, Vector2F.Zero, TransformMode.Normal, Slot.Bone.WorldTransform);

			var model = Slot.Bone.Model;

			ModelImage? image = model.ResolveImage(Path);
			if (Path == null) throw new Exception(":(");

			AtlasRegion region = AtlasRegion.MISSING;
			var succeeded = image != null && model.Images.TextureAtlas.TryGetTextureRegion(image.Name, out region);
			if (!succeeded) region = AtlasRegion.MISSING;

			float width = region.H, height = region.W;
			float widthDiv2 = width / 2, heightDiv2 = height / 2;
			Texture tex = succeeded ? model.Images.TextureAtlas.Texture : Texture.MISSING;

			Rlgl.Begin(DrawMode.TRIANGLES);
			Rlgl.SetTexture(((Texture2D)tex).Id);

			var color = (byte)((IsActiveAttachment && ShouldDim) ? 90 : 255);
			Rlgl.Color4ub(color, color, color, 255);
			if (triangles.Count > 0) {
				float uStart, uEnd, vStart, vEnd;
				uStart = (float)region.X / (float)tex.Width;
				uEnd = uStart + ((float)region.W / (float)tex.Width);

				vStart = ((float)region.Y / (float)tex.Height);
				vEnd = vStart + ((float)region.H / (float)tex.Height);

				bool block = false;
				foreach (var tri in triangles) {
					var points = tri.Points;

					var av1 = points[0].AssociatedObject as MeshVertex;
					var av2 = points[1].AssociatedObject as MeshVertex;
					var av3 = points[2].AssociatedObject as MeshVertex;

					if (av1 == null || av2 == null || av3 == null)
						continue;

					Vector2F p1 = new((float)points[0].X, (float)points[0].Y);
					Vector2F p2 = new((float)points[1].X, (float)points[1].Y);
					Vector2F p3 = new((float)points[2].X, (float)points[2].Y);

					float u1 = (float)NMath.Remap(p1.X, -region.W / 2, region.W / 2, uStart, uEnd), v1 = (float)NMath.Remap(p1.Y, -region.H / 2, region.H / 2, vEnd, vStart);
					float u2 = (float)NMath.Remap(p2.X, -region.W / 2, region.W / 2, uStart, uEnd), v2 = (float)NMath.Remap(p2.Y, -region.H / 2, region.H / 2, vEnd, vStart);
					float u3 = (float)NMath.Remap(p3.X, -region.W / 2, region.W / 2, uStart, uEnd), v3 = (float)NMath.Remap(p3.Y, -region.H / 2, region.H / 2, vEnd, vStart);

					p1 = CalculateVertexWorldPosition(WorldTransform, av1);
					p2 = CalculateVertexWorldPosition(WorldTransform, av2);
					p3 = CalculateVertexWorldPosition(WorldTransform, av3);

					Rlgl.TexCoord2f(u1, v1); Rlgl.Vertex3f(p1.X, p1.Y, 0);
					Rlgl.TexCoord2f(u2, v2); Rlgl.Vertex3f(p2.X, p2.Y, 0);
					Rlgl.TexCoord2f(u3, v3); Rlgl.Vertex3f(p3.X, p3.Y, 0);
				}
			}

			Rlgl.End();
		}

		public IEnumerable<Triangle> Triangles {
			get {
				foreach (var tri in triangles) {
					yield return tri;
				}
			}
		}

		public void RenderTriangleLines() {
			if (!RenderTriangles) return;
			if (triangles.Count <= 0) return;

			Rlgl.DrawRenderBatchActive();
			Rlgl.SetLineWidth(2);

			Dictionary<TriPoint, HashSet<TriPoint>> avoidDuplicateLineDraws = [];

			foreach (var tri in triangles) {
				var tp1 = tri.Points[0];
				var tp2 = tri.Points[1];
				var tp3 = tri.Points[2];

				var av1 = tp1.AssociatedObject as MeshVertex;
				var av2 = tp2.AssociatedObject as MeshVertex;
				var av3 = tp3.AssociatedObject as MeshVertex;

				if (av1 == null || av2 == null || av3 == null)
					continue;

				bool ic1 = av1.IsConstrainedTo(av2);
				bool ic2 = av2.IsConstrainedTo(av3);
				bool ic3 = av3.IsConstrainedTo(av1);

				var offset = Graphics2D.Offset;
				Graphics2D.ResetDrawingOffset();

				if (!avoidDuplicateLineDraws.TryGetValue(tp1, out var h1)) { h1 = []; avoidDuplicateLineDraws[tp1] = h1; }
				if (!avoidDuplicateLineDraws.TryGetValue(tp2, out var h2)) { h2 = []; avoidDuplicateLineDraws[tp2] = h2; }
				if (!avoidDuplicateLineDraws.TryGetValue(tp3, out var h3)) { h3 = []; avoidDuplicateLineDraws[tp3] = h3; }

				var v1 = CalculateVertexWorldPosition(WorldTransform, av1);
				var v2 = CalculateVertexWorldPosition(WorldTransform, av2);
				var v3 = CalculateVertexWorldPosition(WorldTransform, av3);

				Graphics2D.SetDrawColor(245, 100, 20);
				if (ic1) Graphics2D.DrawLine(v1, v2);
				if (ic2) Graphics2D.DrawLine(v2, v3);
				if (ic3) Graphics2D.DrawLine(v1, v1);

				Graphics2D.SetDrawColor(140, 140, 160);

				if (!ic1 && h1.Add(tp2) && h2.Add(tp1))
					Graphics2D.DrawDottedLine(v1, v2, 0.5f);
				if (!ic2 && h2.Add(tp3) && h3.Add(tp2))
					Graphics2D.DrawDottedLine(v2, v3, 0.5f);
				if (!ic3 && h3.Add(tp1) && h1.Add(tp3))
					Graphics2D.DrawDottedLine(v3, v1, 0.5f);

				Graphics2D.OffsetDrawing(offset);
			}

			Rlgl.DrawRenderBatchActive();
			Rlgl.SetLineWidth(1);
		}


		public void RenderVertex(MeshVertex vertex, bool isHighlighted, Vector2F? pos = null) {
			bool deleteMode = ModelEditor.Active.File.ActiveOperator is EditMeshOperator meshOp && meshOp.CurrentMode == EditMesh_Mode.Delete;

			System.Numerics.Vector2 drawPos = (pos ?? CalculateVertexWorldPosition(WorldTransform, vertex)).ToNumerics();
			bool inWeightMode = ModelEditor.Active.Editor.InWeightsMode;
			var camsize = ModelEditor.Active.Editor.CameraZoom;

			var isSelectedTruly = SelectedVertices.Contains(vertex);
			var isSelected = isSelectedTruly || SelectedVertices.Count == 0;

			var hS = isHighlighted ? 245 : 165;

			if (!inWeightMode) {
				float size = (isSelected ? 4f : 2f) + (isHighlighted ? 1f : 0f);
				Color color;
				if (isHighlighted && deleteMode)
					color = new Color(255, 60, 15);
				else
					color = isSelected ? new Color(isHighlighted ? 180 : 0, 255, 255) : new Color(hS - 15, hS, hS);

				Raylib.DrawCircleV(drawPos, (size) / camsize, Color.Black);
				Raylib.DrawCircleV(drawPos, (size - 1) / camsize, color);
			}
			else {
				float size = (isSelected ? 9f : 7f) + (isHighlighted ? 1f : 0f);
				float totalWeight = 0;
				EditorMeshAttachment attachment = vertex.Attachment;
				List<EditorMeshWeights> weights = attachment.Weights;
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

		public static Color BoneWeightListIndexToColor(int index, int alpha = 255) {
			var baselineHue = 194 + (index * 90);
			return (new Vector3(baselineHue, 0.78f, 1.00f)).ToRGB((float)(alpha) / 255f);
		}

		public override void RenderOverlay() {
			if (Hidden) return;

			base.RenderOverlay();
			var camsize = ModelEditor.Active.Editor.CameraZoom;

			EditMeshOperator? meshOp = ModelEditor.Active.File.ActiveOperator as EditMeshOperator;

			RenderTriangleLines();

			Graphics2D.SetDrawColor(245, 100, 20);
			var offset = Graphics2D.Offset;
			Graphics2D.ResetDrawingOffset();

			foreach (var tri in Triangles) {
				TriPoint tp1 = tri.Points[0], tp2 = tri.Points[1], tp3 = tri.Points[2];
				MeshVertex? av1 = tp1.AssociatedObject as MeshVertex,
							av2 = tp2.AssociatedObject as MeshVertex,
							av3 = tp3.AssociatedObject as MeshVertex;

				if (av1 == null || av2 == null || av3 == null) continue;

				bool ic1 = av1.IsConstrainedTo(av2), ic2 = av2.IsConstrainedTo(av3), ic3 = av3.IsConstrainedTo(av1);

				Vector2F v1 = CalculateVertexWorldPosition(WorldTransform, av1),
						 v2 = CalculateVertexWorldPosition(WorldTransform, av2),
						 v3 = CalculateVertexWorldPosition(WorldTransform, av3);

				if (ic1) Graphics2D.DrawLine(v1, v2);
				if (ic2) Graphics2D.DrawLine(v2, v3);
				if (ic3) Graphics2D.DrawLine(v1, v1);
			}

			Graphics2D.SetOffset(offset);

			for (int i = 0; i < ShapeEdges.Count; i++) {
				var edge1 = ShapeEdges[i];
				var edge2 = ShapeEdges[(i + 1) % ShapeEdges.Count];
				var isHighlighted =
						((edge1.Hovered && meshOp == null)
						|| (meshOp != null && meshOp.HoveredVertex == edge1 && !meshOp.IsHoveredSteinerPoint))
					&& !ModelEditor.Active.Editor.IsTypeProhibitedByOperator(typeof(MeshVertex));

				var isSelected = SelectedVertices.Contains(edge1) || SelectedVertices.Count == 0;

				var isEdgeSelected = (SelectedVertices.Contains(edge1) && SelectedVertices.Contains(edge2)) || SelectedVertices.Count == 0;


				var lineColor = isEdgeSelected ? new Color(40, 255, 255) : new Color(20, 210, 210);

				var vertex1 = CalculateVertexWorldPosition(WorldTransform, edge1);
				var vertex2 = CalculateVertexWorldPosition(WorldTransform, edge2);

				Raylib.DrawLineV(vertex1.ToNumerics(), vertex2.ToNumerics(), lineColor);

				if (Selected) {
					RenderVertex(edge1, isHighlighted, vertex1);
				}
			}

			for (int i = 0; i < SteinerPoints.Count; i++) {
				var point = SteinerPoints[i];
				var isHighlighted =
					((point.Hovered && meshOp == null) || (meshOp != null && meshOp.HoveredVertex == point && meshOp.IsHoveredSteinerPoint))
					&& !ModelEditor.Active.Editor.IsTypeProhibitedByOperator(typeof(MeshVertex));

				RenderVertex(point, isHighlighted);
				//Raylib.DrawCircleV(WorldTransform.LocalToWorld(point).ToNumerics(), (isHighlighted ? 3f : 2f) / camsize, new Color(isHighlighted ? 235 : 200, isHighlighted ? 235 : 200, 255));
			}

			// hack; but has to be done with the current rendering order
			if (
				ModelEditor.Active.File.ActiveOperator is EditMeshOperator editMeshOp
				&& editMeshOp.CurrentMode == EditMesh_Mode.Create
				&& editMeshOp.HoveredVertex == null
			) {
				editMeshOp.DrawCreateGizmo();
			}
		}

		public override void BuildOperators(Panel buttons, PreUIDeterminations determinations) {
			PropertiesPanel.OperatorButton<EditMeshOperator>(buttons, "Mesh Editor", "models/mesh.png");
		}

		public override bool HoverTest(Vector2F gridPos) {
			RefreshDelaunator();

			var localizedPos = WorldTransform.WorldToLocal(gridPos);
			foreach (var tri in triangles)
				if (localizedPos.InTriangle(new(
						tri.Points[0].ToNumerics().ToNucleus(),
						tri.Points[1].ToNumerics().ToNucleus(),
						tri.Points[2].ToNumerics().ToNucleus()
					)))
					return true;

			return false;

			// var quadpoints = this.quadpoints();
			// return gridPos.TestPointInQuad(quadpoints.TL, quadpoints.TR, quadpoints.BL, quadpoints.BR);
		}
	}
}
