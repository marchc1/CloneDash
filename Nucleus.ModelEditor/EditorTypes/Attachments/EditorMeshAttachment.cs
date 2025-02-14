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
		public EditMesh_Mode CurrentMode { get; private set; } = EditMesh_Mode.Modify;
		public override string Name => $"Edit Mesh: {CurrentMode}";
		public override bool OverrideSelection => false;
		public override Type[]? SelectableTypes => [];

		public void SetMode(EditMesh_Mode mode) {
			CurrentMode = mode;
			UpdateButtonState();
		}

		private Button ModifyButton, CreateButton, DeleteButton, NewButton, ResetButton;

		private void UpdateButtonState() {
			ModifyButton.Pulsing = CurrentMode == EditMesh_Mode.Modify;
			CreateButton.Pulsing = CurrentMode == EditMesh_Mode.Create;
			DeleteButton.Pulsing = CurrentMode == EditMesh_Mode.Delete;
			NewButton.Pulsing = CurrentMode == EditMesh_Mode.New;
			ResetButton.Pulsing = CurrentMode == EditMesh_Mode.Reset;
		}

		public override void ChangeEditorProperties(CenteredObjectsPanel panel) {
			panel.Size = new(0, 182);

			var win = panel.Add<Window>();
			win.Size = new(330, panel.Size.H);
			win.Center();
			win.Title = "Edit Mesh";
			win.HideNonCloseButtons();

			var row1 = win.Add(new FlexPanel() { DockPadding = RectangleF.TLRB(2), Dock = Dock.Top, Size = new(0, 24), Direction = Directional180.Horizontal, ChildrenResizingMode = FlexChildrenResizingMode.StretchToOppositeDirection });
			var row2 = win.Add(new FlexPanel() { DockPadding = RectangleF.TLRB(2), Dock = Dock.Top, Size = new(0, 24), Direction = Directional180.Horizontal, ChildrenResizingMode = FlexChildrenResizingMode.StretchToOppositeDirection });

			ModifyButton = row1.Add(new Button() { Text = "Modify", AutoSize = true, TextPadding = new(32, 0) }); ModifyButton.MouseReleaseEvent += (_, _, _) => SetMode(EditMesh_Mode.Modify);
			CreateButton = row1.Add(new Button() { Text = "Create", AutoSize = true, TextPadding = new(32, 0) }); CreateButton.MouseReleaseEvent += (_, _, _) => SetMode(EditMesh_Mode.Create);
			DeleteButton = row1.Add(new Button() { Text = "Delete", AutoSize = true, TextPadding = new(32, 0) }); DeleteButton.MouseReleaseEvent += (_, _, _) => SetMode(EditMesh_Mode.Delete);

			NewButton = row2.Add(new Button() { Text = "New", AutoSize = true, TextPadding = new(32, 0) }); NewButton.MouseReleaseEvent += (_, _, _) => SetMode(EditMesh_Mode.New);
			ResetButton = row2.Add(new Button() { Text = "Reset", AutoSize = true, TextPadding = new(32, 0) }); ResetButton.MouseReleaseEvent += (_, _, _) => SetMode(EditMesh_Mode.Reset);

			UpdateButtonState();

			win.Removed += (s) => {
				if (IValidatable.IsValid(s)) {
					ModelEditor.Active.File.DeactivateOperator(true);
				}
			};
		}

		public TriPoint? HoveredVertex;

		public EditorMeshAttachment Attachment => this.UIDeterminations.Last as EditorMeshAttachment ?? throw new Exception("Wtf?");

		public override void Think(ModelEditor editor, Vector2F mousePos) {
			EditorPanel editorPanel = editor.Editor;
			float closestVertexDist = 100000000;
			HoveredVertex = null;

			System.Numerics.Vector2 mp = (Attachment.WorldTransform.WorldToLocal(mousePos)).ToNumerics();

			foreach (var vertex in Attachment.Shape.Points) {
				var vertexDistance = System.Numerics.Vector2.Distance(mp, vertex.ToNumerics());
				if (vertexDistance < closestVertexDist) {
					HoveredVertex = vertex;
					closestVertexDist = vertexDistance;
				}
			}

			if (closestVertexDist > 4)
				HoveredVertex = null;
		}

		public override void Clicked(ModelEditor editor, Vector2F mousePos) {
			base.Clicked(editor, mousePos);
			switch (CurrentMode) {
				case EditMesh_Mode.Delete:
					if(HoveredVertex != null) {
						Attachment.Shape.Points.Remove(HoveredVertex);
						HoveredVertex = null;
						Attachment.Invalidate();
					}
					break;
			}
		}
		public override void DragStart(ModelEditor editor, Vector2F mousePos) {
			base.DragStart(editor, mousePos);
		}
		public override void Drag(ModelEditor editor, Vector2F startPos, Vector2F mousePos) {
			base.Drag(editor, startPos, mousePos);
		}
		public override void DragRelease(ModelEditor editor, Vector2F mousePos) {
			base.DragRelease(editor, mousePos);
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

		public Vector2F Position { get => pos; set => pos = value; }
		public float Rotation { get; set; }
		public Vector2F Scale { get => scale; set => scale = value; }

		public Shape Shape = new();

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

		private (Texture Texture, AtlasRegion Region, Vector2F TL, Vector2F TR, Vector2F BL, Vector2F BR) quadpoints() {
			var model = Slot.Bone.Model;

			ModelImage? image = model.ResolveImage(Path);
			if (image == null || Path == null) throw new Exception(":(");

			var region = model.Images.TextureAtlas.GetTextureRegion(image.Name) ?? throw new Exception("No region!");
			float width = region.H, height = region.W;
			float widthDiv2 = width / 2, heightDiv2 = height / 2;
			Texture tex = model.Images.TextureAtlas.Texture;

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
		private void RefreshDelaunator() {
			if (Invalidated)
				Shape.Triangulate(triangles);
			Invalidated = false;
		}

		public override void Render() {
			// todo ^^ missing texture (prob just purple-black checkerboard)
			RefreshDelaunator();

			WorldTransform = Transformation.CalculateWorldTransformation(pos, Rotation, scale, Vector2F.Zero, TransformMode.Normal, Slot.Bone.WorldTransform);
			var model = Slot.Bone.Model;

			ModelImage? image = model.ResolveImage(Path);
			if (image == null || Path == null) throw new Exception(":(");

			var region = model.Images.TextureAtlas.GetTextureRegion(image.Name) ?? throw new Exception("No region!");
			Texture tex = model.Images.TextureAtlas.Texture;

			Rlgl.Begin(DrawMode.TRIANGLES);
			Rlgl.SetTexture(((Texture2D)tex).Id);

			Rlgl.Color4ub(255, 255, 255, 255);
			if (triangles.Count > 0) {
				float uStart, uEnd, vStart, vEnd;
				uStart = (float)region.X / (float)tex.Width;
				uEnd = uStart + ((float)region.W / (float)tex.Width);

				vStart = ((float)region.Y / (float)tex.Height);
				vEnd = vStart + ((float)region.H / (float)tex.Height);

				bool block = false;
				foreach (var tri in triangles) {
					var points = tri.Points.ToArray();
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

			if (triangles.Count > 0) {
				foreach (var tri in triangles) {
					Raylib.DrawLineStrip([
						WorldTransform.LocalToWorld(tri.Points[0].ToNumerics().ToNucleus()).ToNumerics(),
						WorldTransform.LocalToWorld(tri.Points[1].ToNumerics().ToNucleus()).ToNumerics(),
						WorldTransform.LocalToWorld(tri.Points[2].ToNumerics().ToNucleus()).ToNumerics()
					], 3, new Color(140, 140, 160));
				}
			}

			for (int i = 0; i < Shape.Points.Count; i++) {
				var edge1 = Shape.Points[i].ToNumerics().ToNucleus();
				var edge2 = Shape.Points[(i + 1) % Shape.Points.Count].ToNumerics().ToNucleus();

				edge1 = WorldTransform.LocalToWorld(edge1);
				edge2 = WorldTransform.LocalToWorld(edge2);

				Raylib.DrawLineV(edge1.ToNumerics(), edge2.ToNumerics(), new Color(150, 150, 255));

				var isHighlighted = meshOp != null && meshOp.HoveredVertex == Shape.Points[i];
				Raylib.DrawCircleV(edge1.ToNumerics(), (isHighlighted ? 3f : 2f) / camsize, new Color(isHighlighted ? 235 : 200, isHighlighted ? 235 : 200, 255));

				if (i != Shape.Points.Count - 1)
					Raylib.DrawCircleV(edge2.ToNumerics(), 2f / camsize, new Color(200, 200, 255));
			}
		}

		public override void BuildOperators(Panel buttons, PreUIDeterminations determinations) {
			PropertiesPanel.OperatorButton<EditMeshOperator>(buttons, "Mesh Editor", "models/mesh.png");
		}

		public override bool HoverTest(Vector2F gridPos) {
			var quadpoints = this.quadpoints();
			return gridPos.TestPointInQuad(quadpoints.TL, quadpoints.TR, quadpoints.BL, quadpoints.BR);
		}
	}
}
