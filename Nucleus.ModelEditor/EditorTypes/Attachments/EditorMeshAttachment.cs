// TODO: This file needs to be split apart!

using Newtonsoft.Json;
using Nucleus.Core;
using Nucleus.ManagedMemory;
using Nucleus.ModelEditor.UI;
using Nucleus.Models;
using Nucleus.Rendering;
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
		public static ConVar meshedit_deformed = ConVar.Register("meshedit_deformed", "1", ConsoleFlags.Saved, "Renders the mesh deformed");

		public EditMesh_Mode CurrentMode { get; private set; } = EditMesh_Mode.Modify;
		public override string Name => $"Edit Mesh: {CurrentMode}";
		public override bool OverrideSelection => true;
		public override Type[]? SelectableTypes => [typeof(EditorVertex)];

		public void SetMode(EditMesh_Mode mode) {
			CurrentMode = mode;
			UpdateButtonState();
		}

		private Button ModifyButton, CreateButton, DeleteButton, NewButton, ResetButton;
		private Checkbox Triangles, Dim, Isolate, Deform;
		List<EditorVertex> WorkingLines = [];
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

			var row4 = win.Add(new FlexPanel() { DockPadding = RectangleF.TLRB(0, 4, 4, 0), Dock = Dock.Top, Size = new(0, 32), Direction = Directional180.Horizontal, ChildrenResizingMode = FlexChildrenResizingMode.StretchToOppositeDirection });
			row4.Add(out Deform); row4.Add(new Label() { Text = "Deformed", AutoSize = true });
			Deform.BindToConVar(meshedit_deformed);

			UpdateButtonState();

			win.Removed += (s) => {
				if (IValidatable.IsValid(s)) {
					ModelEditor.Active.File.DeactivateOperator(true);
				}
			};
		}

		public EditorVertex? AttachedVertex;
		public EditorVertex? HoveredVertex;
		/// <summary>
		/// In the context of <see cref="EditMesh_Mode.Create"/>, this is the created vertex.
		/// <br/>
		/// In the context of <see cref="EditMesh_Mode.Modify"/>, this is the clicked vertex.
		/// </summary>
		public EditorVertex? ClickedVertex;
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
				if (vertexDistance < closestVertexDist && vertex != ClickedVertex) {
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
					if (vertexDistance < closestVertexDist && vertex != ClickedVertex) {
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

		public Color Color { get; set; } = Color.White;

		public override void Clicked(ModelEditor editor, Vector2F mousePos) {
			base.Clicked(editor, mousePos);
			PatchAttachmentTransform();

			// Overrides, for the create vertex mode, but designed this way in case
			// other things need it
			EditorVertex? clickVertexOverride = null;
			bool? hoveredSteinerOverride = null;

			switch (CurrentMode) {
				case EditMesh_Mode.Create:
					var newPoint = Attachment.WorldTransform.WorldToLocal(editor.Editor.ScreenToGrid(mousePos));

					// TODO + HACK; the 0.05f thing avoids issues with duplicate points.
					// Really, I probably should just... wait to start dragging before
					// creating the vertex... just lazy right now
					EditorVertex vertex = EditorVertex.FromVector(newPoint + new Vector2F(0.05f), Attachment);
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
					EditorFile.UpdateVertexPositions(Attachment, onlyThisVertex: vertex);
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
						Attachment.Weights.Clear();
						RestoreAttachmentTransform();
						Attachment.ShapeEdges.AddRange(WorkingLines);
						Attachment.Invalidate();
						SetMode(EditMesh_Mode.Modify);
					}
					else {
						var gridpos = ModelEditor.Active.Editor.ScreenToGrid(mousePos);
						var attachpos = Attachment.WorldTransform.Translation;
						var attachClickPos = gridpos - attachpos;

						var newClickP = Attachment.WorldTransform.WorldToLocal(ClampVertexPosition(attachClickPos) + Attachment.WorldTransform.Translation);
						var newTexCoord = Attachment.GetVertexUV(newClickP);
						Logs.Info(newTexCoord);
						WorkingLines.Add(EditorVertex.FromVector(newClickP, newTexCoord, Attachment));
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
						EditorFile.UpdateVertexPositions(ClickedVertex.Attachment);

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

		public bool ShouldRenderUndeformed => CurrentMode == EditMesh_Mode.New || !meshedit_deformed.GetBool();
		public bool ShouldRenderGizmosThough => CurrentMode != EditMesh_Mode.New && !meshedit_deformed.GetBool();

		private Transformation StoredAttachTransform;
		private void PatchAttachmentTransform() {
			if (!ShouldRenderUndeformed) return;

			StoredAttachTransform = Attachment.WorldTransform;
			Attachment.WorldTransform = Transformation.CalculateWorldTransformation(StoredAttachTransform.Translation, 0, Vector2F.One, Vector2F.Zero, TransformMode.Normal);
			Attachment.SuppressWorldTransform = true;
		}
		private void RestoreAttachmentTransform() {
			if (!ShouldRenderUndeformed) return;
			Attachment.WorldTransform = StoredAttachTransform;
			Attachment.SuppressWorldTransform = false;
		}

		public override bool RenderOverride() {
			if (ShouldRenderUndeformed) {
				var camsize = ModelEditor.Active.Editor.CameraZoom;

				PatchAttachmentTransform();

				if (ShouldRenderGizmosThough) {
					Attachment.RenderStandalone();
					Attachment.RenderOverlay();
				}
				else {
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

				}
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
	public class EditorVertex : IEditorType
	{
		public override string ToString() {
			return $"EditorVertex [{X}, {Y}]";
		}
		public EditorModel GetModel() => Attachment.Slot.Bone.Model;
		public IEditorType? DeferPropertiesTo() => Attachment;
		public IEditorType? DeferTransformationsTo() => Attachment;
		public IEditorType? GetTransformParent() => Attachment;
		public float X;
		public float Y;
		public float U;
		public float V;
		public EditorVertexAttachment Attachment;


		public HashSet<EditorVertex> ConstrainedHashSet = [];
		[JsonIgnore] public bool HasConstrainedEdges => ConstrainedHashSet.Count > 0;

		/// <summary>
		/// Note that this only applies to <see cref="EditorMeshAttachment.SteinerPoints"/> -> <see cref="EditorMeshAttachment.ShapeEdges"/> or 
		/// <see cref="EditorMeshAttachment.SteinerPoints"/> -> <see cref="EditorMeshAttachment.SteinerPoints"/>. Shape edges inheritly are 
		/// constrained to the next <see cref="EditorVertex"/> in the array (or in the case of the last edge, the last point -> first point).
		/// </summary>
		public IEnumerable<EditorVertex> ConstrainedVertices {
			get {
				foreach (var vertex in ConstrainedHashSet)
					yield return vertex;
			}
		}

		public bool IsConstrainedTo(EditorVertex other) {
			Debug.Assert(this.Attachment != null);
			Debug.Assert(other.Attachment != null);

			if (this.Attachment != other.Attachment)
				throw new InvalidOperationException("Cannot constrain, or unconstrain, two vertices from two separate mesh attachments. Something has gone horribly wrong.");

			var otherContainsUs = other.ConstrainedHashSet.Contains(this);
			var weContainOther = this.ConstrainedHashSet.Contains(other);

			Debug.Assert(otherContainsUs == weContainOther);

			return otherContainsUs && weContainOther;
		}

		public void ConstrainTo(EditorVertex other) {
			Debug.Assert(this.Attachment != null);
			Debug.Assert(other.Attachment != null);

			if (this.Attachment != other.Attachment)
				throw new InvalidOperationException("Cannot constrain, or unconstrain, two vertices from two separate mesh attachments. Something has gone horribly wrong.");

			Debug.Assert(this != other, "Can not constrain a vertex to itself...");

			other.ConstrainedHashSet.Add(this);
			this.ConstrainedHashSet.Add(other);

			Attachment.Invalidate();
		}

		public void UnconstrainFrom(EditorVertex other) {
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
		public static EditorVertex FromVector(Vector2F vec, EditorVertexAttachment attachment) {
			EditorVertex vertex = new() { X = vec.X, Y = vec.Y, Attachment = attachment };

			// Bind *just* this vertex to the bone its parented to
			// Todo: auto-weigh to the bone somehow?
			attachment.SetVertexWeight(vertex, attachment.Slot.Bone, 1.0f, false);

			return vertex;
		}

		public static EditorVertex FromVector(Vector2F vec, Vector2F tex, EditorVertexAttachment attachment) {
			EditorVertex vertex = new() { X = vec.X, Y = vec.Y, U = tex.X, V = tex.Y, Attachment = attachment };

			// Bind *just* this vertex to the bone its parented to
			// Todo: auto-weigh to the bone somehow?
			attachment.SetVertexWeight(vertex, attachment.Slot.Bone, 1.0f, false);

			return vertex;
		}

		public static implicit operator Vector2F(EditorVertex v) => v.ToVector();

		public void OnMouseEntered() { }
		public void OnMouseLeft() { }

		public bool OnSelected() {
			Attachment.SelectVertex(this);
			return false;
		}

		internal void AutoUV() {
			var uvcoords = (Attachment as EditorMeshAttachment).GetVertexUV(this);
			U = uvcoords.X;
			V = uvcoords.Y;
		}
	}

	// So the reason this is structured so weirdly: Newtonsoft.Json doesn't seem to like serializing
	// MeshVertex types as keys. This weird pseudo-dictionary-only-really-o(1)-during-runtime structure
	// is *probably* good enough for the editor, but I hate it regardless and it's a mess
	public class EditorWeights
	{
		public EditorBone Bone;

		private List<EditorVertex> __vertices = [];
		private List<float> __weights = [];
		private List<Vector2F> __positions = [];

		public List<EditorVertex> Vertices { get => __vertices; set { Invalidated = true; __vertices = value; } }
		public List<float> Weights { get => __weights; set { Invalidated = true; __weights = value; } }
		public List<Vector2F> Positions { get => __positions; set { Invalidated = true; __positions = value; } }

		[JsonIgnore] private Dictionary<EditorVertex, float> TrueWeights = [];
		[JsonIgnore] private Dictionary<EditorVertex, Vector2F> TruePositions = [];
		[JsonIgnore] public bool Invalidated = true;

		public void SetVertexWeight(EditorVertex vertex, float weight) {
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
		public void SetVertexPos(EditorVertex vertex, Vector2F pos) {
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

		public bool AddVertex(EditorVertex vertex, float? weight = null, Vector2F? pos = null) {
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

		public bool RemoveVertex(EditorVertex vertex) {
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
		public float TryGetVertexWeight(EditorVertex vertex) {
			Validate();
			return TrueWeights.TryGetValue(vertex, out var value) ? value : 0;
		}
		public bool TryGetVertexWeight(EditorVertex vertex, [NotNullWhen(true)] out float weight) {
			Validate();
			return TrueWeights.TryGetValue(vertex, out weight);
		}

		/// <summary>
		/// Defaults to <see cref="Vector2F.Zero"/>
		/// </summary>
		/// <param name="vertex"></param>
		/// <returns></returns>
		public Vector2F TryGetVertexPosition(EditorVertex vertex) {
			Validate();
			return TruePositions.TryGetValue(vertex, out var value) ? value : Vector2F.Zero;
		}
		public bool TryGetVertexPosition(EditorVertex vertex, [NotNullWhen(true)] out Vector2F pos) {
			Validate();
			return TruePositions.TryGetValue(vertex, out pos);
		}

		public bool TryGetVertexInfo(EditorVertex vertex, out float weight, out Vector2F pos) {
			Validate();
			var gotWeight = TryGetVertexWeight(vertex, out weight);
			return TryGetVertexPosition(vertex, out pos) || gotWeight;
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

	[Nucleus.MarkForStaticConstruction]
	public class EditorMeshAttachment : EditorVertexAttachment
	{
		[JsonIgnore] public float LocalWidth => Slot.Bone.Model.Images.TextureAtlas.GetTextureRegion(Slot.Bone.Model.ResolveImage(Path).Name).Value.W;
		[JsonIgnore] public float LocalHeight => Slot.Bone.Model.Images.TextureAtlas.GetTextureRegion(Slot.Bone.Model.ResolveImage(Path).Name).Value.H;

		public override string SingleName => "mesh";
		public override string PluralName => "meshes";
		public override string EditorIcon => "models/mesh.png";

		public string GetPath() => Path ?? $"<{Name}>";
		public string? Path { get; set; } = null;
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

		/// <summary>
		/// Arbitrary points within the shapes edges.
		/// </summary>
		public List<EditorVertex> SteinerPoints = [];

		public override IEnumerable<EditorVertex> GetVertices() {
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

		public override void UpdateShape(Shape shape) {
			base.UpdateShape(shape);
			foreach (var steinerPoint in SteinerPoints)
				shape.SteinerPoints.Add(new(steinerPoint.X, steinerPoint.Y, steinerPoint));
		}

		[JsonIgnore] public bool SuppressWorldTransform = false;

		public override Color DetermineVertexColor(bool selected, bool highlighted) {
			var hS = highlighted ? 245 : 165;
			bool deleteMode = ModelEditor.Active.File.ActiveOperator is EditMeshOperator meshOp && meshOp.CurrentMode == EditMesh_Mode.Delete;
			Color color;
			if (highlighted && deleteMode)
				color = new Color(255, 60, 15);
			else
				color = selected ? new Color(highlighted ? 180 : 0, 255, 255) : new Color(hS - 15, hS, hS);
			return color;
		}

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

		[JsonIgnore] public static bool InEditMeshOperator => ModelEditor.Active.File.ActiveOperator is EditMeshOperator;
		[JsonIgnore] public bool IsActiveAttachment => ModelEditor.Active.File.ActiveOperator is EditMeshOperator op && op.Attachment == this;
		[JsonIgnore] public static bool RenderTriangles => InEditMeshOperator && EditMeshOperator.meshedit_triangles.GetBool();
		[JsonIgnore] public static bool ShouldDim => InEditMeshOperator && EditMeshOperator.meshedit_dim.GetBool();
		[JsonIgnore] public static bool ShouldIsolate => InEditMeshOperator && EditMeshOperator.meshedit_isolate.GetBool();

		public override bool RemoveVertex(EditorVertex vertex) {
			return base.RemoveVertex(vertex) || SteinerPoints.Remove(vertex);
		}

		public override void Render() {
			if (Hidden) return;
			RefreshDelaunator();

			if (!SuppressWorldTransform)
				SetupWorldTransform();

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

			var color = Slot.GetColor();

			if (IsActiveAttachment && ShouldDim) {
				color.R /= 2;
				color.G /= 2;
				color.B /= 2;
			}
			float srM = color.R / 255f, sgM = color.G / 255f, sbM = color.B / 255f, saM = color.A / 255f;
			float arM = Color.R / 255f, agM = Color.G / 255f, abM = Color.B / 255f, aaM = Color.A / 255f;

			Rlgl.Color4f(srM * arM, sgM * agM, sbM * abM, saM * aaM);
			if (triangles.Count > 0) {
				float uStart, uEnd, vStart, vEnd;
				uStart = (float)region.X / (float)tex.Width;
				uEnd = uStart + ((float)region.W / (float)tex.Width);

				vStart = ((float)region.Y / (float)tex.Height);
				vEnd = vStart + ((float)region.H / (float)tex.Height);

				bool block = false;
				foreach (var tri in triangles) {
					var points = tri.Points;

					var av1 = points[0].AssociatedObject as EditorVertex;
					var av2 = points[1].AssociatedObject as EditorVertex;
					var av3 = points[2].AssociatedObject as EditorVertex;

					if (av1 == null || av2 == null || av3 == null)
						continue;

					Vector2F p1 = new((float)points[0].X, (float)points[0].Y);
					Vector2F p2 = new((float)points[1].X, (float)points[1].Y);
					Vector2F p3 = new((float)points[2].X, (float)points[2].Y);

					Vector2F t1 = new((float)av1.U, (float)av1.V);
					Vector2F t2 = new((float)av2.U, (float)av2.V);
					Vector2F t3 = new((float)av3.U, (float)av3.V);

					float u1 = (float)NMath.Remap(t1.X, 0, 1, uStart, uEnd), v1 = (float)NMath.Remap(t1.Y, 0, 1, vEnd, vStart);
					float u2 = (float)NMath.Remap(t2.X, 0, 1, uStart, uEnd), v2 = (float)NMath.Remap(t2.Y, 0, 1, vEnd, vStart);
					float u3 = (float)NMath.Remap(t3.X, 0, 1, uStart, uEnd), v3 = (float)NMath.Remap(t3.Y, 0, 1, vEnd, vStart);

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

				var av1 = tp1.AssociatedObject as EditorVertex;
				var av2 = tp2.AssociatedObject as EditorVertex;
				var av3 = tp3.AssociatedObject as EditorVertex;

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

		public Transformation? TransformOverride = null;

		public override void RenderOverlay() {
			if (Hidden) return;

			base.RenderOverlay();
			var camsize = ModelEditor.Active.Editor.CameraZoom;

			EditMeshOperator? meshOp = ModelEditor.Active.File.ActiveOperator as EditMeshOperator;

			RenderTriangleLines();

			Graphics2D.SetDrawColor(245, 100, 20);
			var offset = Graphics2D.Offset;
			Graphics2D.ResetDrawingOffset();

			var transform = TransformOverride ?? WorldTransform;

			foreach (var tri in Triangles) {
				TriPoint tp1 = tri.Points[0], tp2 = tri.Points[1], tp3 = tri.Points[2];
				EditorVertex? av1 = tp1.AssociatedObject as EditorVertex,
							av2 = tp2.AssociatedObject as EditorVertex,
							av3 = tp3.AssociatedObject as EditorVertex;

				if (av1 == null || av2 == null || av3 == null) continue;

				//av1.Attachment = this;
				//av2.Attachment = this;
				//av3.Attachment = this;

				bool ic1 = av1.IsConstrainedTo(av2), ic2 = av2.IsConstrainedTo(av3), ic3 = av3.IsConstrainedTo(av1);

				Vector2F v1 = CalculateVertexWorldPosition(transform, av1),
						 v2 = CalculateVertexWorldPosition(transform, av2),
						 v3 = CalculateVertexWorldPosition(transform, av3);

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
					&& !ModelEditor.Active.Editor.IsTypeProhibitedByOperator(typeof(EditorVertex));

				var isSelected = SelectedVertices.Contains(edge1) || SelectedVertices.Count == 0;

				var isEdgeSelected = (SelectedVertices.Contains(edge1) && SelectedVertices.Contains(edge2)) || SelectedVertices.Count == 0;


				var lineColor = isEdgeSelected ? new Color(40, 255, 255) : new Color(20, 210, 210);

				var vertex1 = CalculateVertexWorldPosition(transform, edge1);
				var vertex2 = CalculateVertexWorldPosition(transform, edge2);

				Raylib.DrawLineV(vertex1.ToNumerics(), vertex2.ToNumerics(), lineColor);

				if (Selected) {
					RenderVertex(edge1, isHighlighted, vertex1);
				}
			}

			for (int i = 0; i < SteinerPoints.Count; i++) {
				var point = SteinerPoints[i];
				var isHighlighted =
					((point.Hovered && meshOp == null) || (meshOp != null && meshOp.HoveredVertex == point && meshOp.IsHoveredSteinerPoint))
					&& !ModelEditor.Active.Editor.IsTypeProhibitedByOperator(typeof(EditorVertex));

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

		public Vector2F GetVertexUV(Vector2F pos) {
			return new(
				(float)NMath.Remap(pos.X, LocalWidth / -2, LocalWidth / 2, 0, 1),
				(float)NMath.Remap(pos.Y, LocalHeight / -2, LocalHeight / 2, 0, 1)
			);
		}
	}
}
