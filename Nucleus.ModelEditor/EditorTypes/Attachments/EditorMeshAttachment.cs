using DelaunatorSharp;
using Newtonsoft.Json;
using Nucleus.ManagedMemory;
using Nucleus.ModelEditor.UI;
using Nucleus.ModelEditor.UI.Operators;
using Nucleus.Models;
using Nucleus.Types;
using Nucleus.UI;
using Nucleus.UI.Elements;
using Poly2Tri;
using Raylib_cs;
using System.Net.Mail;
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

		public MeshVertex? HoveredVertex;
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
			var dist = 16f / camsize;
			var array = (CurrentMode == EditMesh_Mode.New ? WorkingLines : Attachment.ConstrainedEdges);
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

			switch (CurrentMode) {
				case EditMesh_Mode.Create:
					if (HoveredVertex == null) {
						var newPoint = Attachment.WorldTransform.WorldToLocal(editor.Editor.ScreenToGrid(mousePos));
						Attachment.SteinerPoints.Add(MeshVertex.FromVector(newPoint));
						Attachment.Invalidate();
					}
					break;
				case EditMesh_Mode.Delete:
					if (HoveredVertex != null) {
						if (Attachment.SteinerPoints.Contains(HoveredVertex))
							Attachment.SteinerPoints.Remove(HoveredVertex);
						else
							Attachment.ConstrainedEdges.Remove(HoveredVertex);
						HoveredVertex = null;
						Attachment.Invalidate();
					}
					break;
				case EditMesh_Mode.New:
					if (WorkingLines.Count > 0 && HoveredVertex == WorkingLines.First()) {
						Attachment.ConstrainedEdges.Clear();
						RestoreAttachmentTransform();
						Attachment.ConstrainedEdges.AddRange(WorkingLines);
						Attachment.Invalidate();
						SetMode(EditMesh_Mode.Modify);
					}
					else {
						var newClickP = Attachment.WorldTransform.WorldToLocal(ClampVertexPosition(ModelEditor.Active.Editor.ScreenToGrid(mousePos) - Attachment.WorldTransform.Translation) + Attachment.WorldTransform.Translation);

						WorkingLines.Add(MeshVertex.FromVector(newClickP));
					}
					break;
			}
			RestoreAttachmentTransform();
			ClickedVertex = HoveredVertex;
			IsClickedSteinerPoint = IsHoveredSteinerPoint;
		}
		public override void DragStart(ModelEditor editor, Vector2F mousePos) {
			base.DragStart(editor, mousePos);
		}
		public override void Drag(ModelEditor editor, Vector2F startPos, Vector2F mousePos) {
			PatchAttachmentTransform();
			switch (CurrentMode) {
				case EditMesh_Mode.Modify:
					if (ClickedVertex != null) {
						var mouseGridPos = editor.Editor.ScreenToGrid(mousePos);
						var localized = Attachment.WorldTransform.WorldToLocal(mouseGridPos);
						var clamped = ClampVertexPosition(localized);

						ClickedVertex.SetPos(clamped);

						Attachment.Invalidate();
					}
					break;
			}
			RestoreAttachmentTransform();
		}
		public override void DragRelease(ModelEditor editor, Vector2F mousePos) {
			base.DragRelease(editor, mousePos);
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
					Raylib.DrawCircleV(mp.ToNumerics(), 5 / camsize, Color.LIME);

				RestoreAttachmentTransform();

				return true;
			}

			return false;
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
		public Dictionary<EditorBone, float> Weights = [];

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
		public static MeshVertex FromVector(Vector2F vec) => new() { X = vec.X, Y = vec.Y };

		public static implicit operator Vector2F(MeshVertex v) => v.ToVector();

		public void OnMouseEntered() { }
		public void OnMouseLeft() { }

		public bool OnSelected() {
			Attachment.SelectVertex(this);
			return false;
		}
	}

	public class EditorMeshAttachment : EditorAttachment
	{
		public override string SingleName => "mesh";
		public override string PluralName => "meshes";
		public override string EditorIcon => "models/mesh.png";

		public string GetPath() => Path ?? $"<{Name}>";
		public string? Path { get; set; } = null;

		private Vector2F pos, scale = new(1, 1);

		public void SelectVertex(MeshVertex vertex, bool multiselect = false) {
			if (multiselect) {
				bool wasIn = SelectedVertices.Remove(vertex);

				if (!wasIn)
					SelectedVertices.Add(vertex);

				return;
			}

			SelectedVertices.Clear();
			SelectedVertices.Add(vertex);
		}

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

		public Color Color { get; set; } = Color.WHITE;

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

		public List<MeshVertex> ConstrainedEdges = [];
		public List<MeshVertex> SteinerPoints = [];

		public IEnumerable<MeshVertex> GetVertices() {
			foreach (var co in ConstrainedEdges) {
				co.Attachment = this;
				yield return co;
			}

			foreach (var sp in SteinerPoints) {
				sp.Attachment = this;
				yield return sp;
			}
		}

		private void RefreshDelaunator() {
			if (Invalidated) {
				Span<float> x = stackalloc float[ConstrainedEdges.Count];
				Span<float> y = stackalloc float[ConstrainedEdges.Count];
				for (int i = 0; i < ConstrainedEdges.Count; i++) {
					x[i] = ConstrainedEdges[i].X;
					y[i] = ConstrainedEdges[i].Y;
				}
				triangles.Clear();
				Shape = new Shape(x, y);

				foreach (var steinerPoint in SteinerPoints)
					Shape.SteinerPoints.Add(new(steinerPoint.X, steinerPoint.Y));

				Shape.Triangulate(triangles);
				Invalidated = false;
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
					Vector2F p1 = new((float)points[0].X, (float)points[0].Y);
					Vector2F p2 = new((float)points[1].X, (float)points[1].Y);
					Vector2F p3 = new((float)points[2].X, (float)points[2].Y);

					float u1 = (float)NMath.Remap(p1.X, -region.W / 2, region.W / 2, uStart, uEnd), v1 = (float)NMath.Remap(p1.Y, -region.H / 2, region.H / 2, vEnd, vStart);
					float u2 = (float)NMath.Remap(p2.X, -region.W / 2, region.W / 2, uStart, uEnd), v2 = (float)NMath.Remap(p2.Y, -region.H / 2, region.H / 2, vEnd, vStart);
					float u3 = (float)NMath.Remap(p3.X, -region.W / 2, region.W / 2, uStart, uEnd), v3 = (float)NMath.Remap(p3.Y, -region.H / 2, region.H / 2, vEnd, vStart);

					p1 = WorldTransform.LocalToWorld(p1);
					p2 = WorldTransform.LocalToWorld(p2);
					p3 = WorldTransform.LocalToWorld(p3);

					Rlgl.TexCoord2f(u1, v1); Rlgl.Vertex3f(p1.X, p1.Y, 0);
					Rlgl.TexCoord2f(u2, v2); Rlgl.Vertex3f(p2.X, p2.Y, 0);
					Rlgl.TexCoord2f(u3, v3); Rlgl.Vertex3f(p3.X, p3.Y, 0);
				}
			}

			Rlgl.End();
		}

		public override void RenderOverlay() {
			base.RenderOverlay();
			var camsize = ModelEditor.Active.Editor.CameraZoom;

			EditMeshOperator? meshOp = ModelEditor.Active.File.ActiveOperator as EditMeshOperator;

			if (RenderTriangles && triangles.Count > 0) {
				foreach (var tri in triangles) {
					Raylib.DrawLineStrip([
						WorldTransform.LocalToWorld(tri.Points[0].ToNumerics().ToNucleus()).ToNumerics(),
						WorldTransform.LocalToWorld(tri.Points[1].ToNumerics().ToNucleus()).ToNumerics(),
						WorldTransform.LocalToWorld(tri.Points[2].ToNumerics().ToNucleus()).ToNumerics()
					], 3, new Color(140, 140, 160));
				}
			}

			for (int i = 0; i < ConstrainedEdges.Count; i++) {
				var edge1 = ConstrainedEdges[i];
				var edge2 = ConstrainedEdges[(i + 1) % ConstrainedEdges.Count];
				var isHighlighted = (edge1.Hovered && meshOp == null) || (meshOp != null && meshOp.HoveredVertex == edge1 && !meshOp.IsHoveredSteinerPoint);

				var vertex1 = WorldTransform.LocalToWorld(edge1);
				var vertex2 = WorldTransform.LocalToWorld(edge2);

				Raylib.DrawLineV(vertex1.ToNumerics(), vertex2.ToNumerics(), new Color(0, 255, 255));

				if (Selected) {
					Raylib.DrawCircleV(vertex1.ToNumerics(), (isHighlighted ? 6f : 4f) / camsize, Color.BLACK);
					Raylib.DrawCircleV(vertex1.ToNumerics(), (isHighlighted ? 5f : 3f) / camsize, new Color(isHighlighted ? 180 : 0, 255, 255));
				}
			}

			for (int i = 0; i < SteinerPoints.Count; i++) {
				var point = SteinerPoints[i];
				var isHighlighted = (point.Hovered && meshOp == null) || (meshOp != null && meshOp.HoveredVertex == point && meshOp.IsHoveredSteinerPoint);
				Raylib.DrawCircleV(WorldTransform.LocalToWorld(point).ToNumerics(), (isHighlighted ? 3f : 2f) / camsize, new Color(isHighlighted ? 235 : 200, isHighlighted ? 235 : 200, 255));
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
