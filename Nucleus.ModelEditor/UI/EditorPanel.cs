using Nucleus.Core;
using Nucleus.Models;
using Nucleus.Types;
using Nucleus.UI;
using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Model = Nucleus.ModelEditor.EditorModel;

namespace Nucleus.ModelEditor
{
	public enum ViewportSelectMode
	{
		NotApplicable = -1,
		None = 0,

		Bones = 1,
		Images = 2,
		Other = 4,

		All = Bones | Images | Other
	}
	public enum EditorTransformMode
	{
		LocalCoordinates,
		ParentCoordinates,
		WorldCoordinates
	}
	public static class Checkerboard
	{
		private static Shader shader = Raylib.LoadShader(null, Filesystem.Resolve("checkerboard.fshader", "shaders"));
		private static Color defaultLight => new Color(60, 60, 63);
		private static Color defaultDark => new Color(46, 46, 49);
		public static void Draw(float gridSize = 50, float quadSize = 4096, Color? light = null, Color? dark = null) {
			Color c = light ?? defaultLight, d = dark ?? defaultDark;

			shader.SetShaderValue("scale", quadSize / gridSize);
			shader.SetShaderValue("lightColor", new Vector3(c.R / 255f, c.G / 255f, c.B / 255f));
			shader.SetShaderValue("darkColor", new Vector3(d.R / 255f, d.G / 255f, d.B / 255f));
			Raylib.BeginShaderMode(shader);
			Rlgl.DisableBackfaceCulling();
			Rlgl.Begin(DrawMode.QUADS);

			Rlgl.Color4f(1, 1, 1, 1);
			Rlgl.TexCoord2f(-1, -1);
			Rlgl.Vertex3f(-quadSize, quadSize, 0);

			Rlgl.Color4f(1, 1, 1, 1);
			Rlgl.TexCoord2f(1, -1);
			Rlgl.Vertex3f(quadSize, quadSize, 0);

			Rlgl.Color4f(1, 1, 1, 1);
			Rlgl.TexCoord2f(1, 1);
			Rlgl.Vertex3f(quadSize, -quadSize, 0);

			Rlgl.Color4f(1, 1, 1, 1);
			Rlgl.TexCoord2f(-1, 1);
			Rlgl.Vertex3f(-quadSize, -quadSize, 0);

			Rlgl.End();
			Rlgl.EnableBackfaceCulling();
			Raylib.EndShaderMode();
		}
	}
	public class TransformPanel : Panel
	{
		public delegate void FloatChange(int i, float value);
		public event FloatChange? FloatChanged;

		public event Element.MouseEventDelegate? OnSelected;
		private NumSlider[] sliders;

		private bool enableSliders = true;
		public bool EnableSliders {
			get => enableSliders;
			set {
				enableSliders = value;
				foreach (var slider in sliders) {
					var c = slider.TextColor;
					slider.TextColor = new(c.R, c.G, c.B, value ? 255 : 0);
				}
			}
		}
		public NumSlider GetNumSlider(int index) => sliders[index];

		public static TransformPanel New(Element parent, string text, int floats) {
			var panel = parent.Add<TransformPanel>();
			panel.DockPadding = RectangleF.TLRB(2);
			panel.BorderSize = 2;

			var select = panel.Add<Button>();
			select.Dock = Dock.Left;
			select.Text = text;
			select.Size = new(96);
			select.MouseReleaseEvent += (v1, v2, v3) => panel.OnSelected?.Invoke(panel, v2, v3);
			select.BorderSize = 0;

			var floatparts = panel.Add<FlexPanel>();
			floatparts.Dock = Dock.Fill;
			floatparts.Direction = Directional180.Horizontal;
			floatparts.ChildrenResizingMode = FlexChildrenResizingMode.StretchToFit;
			floatparts.DockPadding = RectangleF.Zero;
			floatparts.BorderSize = 0;

			panel.sliders = new NumSlider[floats];
			for (int i = 0; i < floats; i++) {
				var floatEdit = floatparts.Add<NumSlider>();
				panel.sliders[i] = floatEdit;
				floatEdit.HelperText = "";
				floatEdit.Value = 0;
				floatEdit.BorderSize = 0;
				floatEdit.OnValueChanged += (self, oldV, newV) => {
					panel.FloatChanged?.Invoke(i, (float)newV);
				};
			}

			return panel;
		}
	}
	public class EditorPanel : Panel
	{
		TransformPanel TransformRotation, TransformTranslation, TransformScale, TransformShear;
		Button LocalTransformButton, ParentTransformButton, WorldTransformButton;
		public ViewportSelectMode SelectMode { get; set; } = ViewportSelectMode.All;
		protected override void Initialize() {
			base.Initialize();
			CenteredObjectsPanel test;
			Add(out test);
			test.ForceHeight = false;

			test.Dock = Dock.Bottom;
			test.Size = new(128);

			var modePanel = test.Add<FlexPanel>();
			modePanel.Direction = Directional180.Vertical;
			modePanel.ChildrenResizingMode = FlexChildrenResizingMode.StretchToFit;
			modePanel.Size = new(98, 90);

			var poseBtn = modePanel.Add<Button>();
			poseBtn.Text = "Pose";

			var weightsBtn = modePanel.Add<Button>();
			weightsBtn.Text = "Weights";

			var createBtn = modePanel.Add<Button>();
			createBtn.Text = "Create";

			var transformPanel = test.Add<FlexPanel>();
			transformPanel.Direction = Directional180.Vertical;
			transformPanel.ChildrenResizingMode = FlexChildrenResizingMode.StretchToFit;
			transformPanel.Size = new(280, 115);

			TransformRotation = TransformPanel.New(transformPanel, "Rotate", 1);
			TransformTranslation = TransformPanel.New(transformPanel, "Translate", 2);
			TransformScale = TransformPanel.New(transformPanel, "Scale", 2);
			TransformShear = TransformPanel.New(transformPanel, "Shear", 2);

			TransformRotation.GetNumSlider(0).OnValueChanged += CHANGE_Rotation;
			TransformTranslation.GetNumSlider(0).OnValueChanged += CHANGE_TranslationX;
			TransformTranslation.GetNumSlider(1).OnValueChanged += CHANGE_TranslationY;
			TransformScale.GetNumSlider(0).OnValueChanged += CHANGE_ScaleX;
			TransformScale.GetNumSlider(1).OnValueChanged += CHANGE_ScaleY;
			TransformShear.GetNumSlider(0).OnValueChanged += CHANGE_ShearX;
			TransformShear.GetNumSlider(1).OnValueChanged += CHANGE_ShearY;

			var transformModePanel = test.Add<FlexPanel>();
			transformModePanel.Direction = Directional180.Vertical;
			transformModePanel.ChildrenResizingMode = FlexChildrenResizingMode.StretchToFit;
			transformModePanel.Size = new(70, 90);

			LocalTransformButton = transformModePanel.Add<Button>();
			LocalTransformButton.Text = "Local";
			LocalTransformButton.MouseReleaseEvent += (_, _, _) => SetTransformMode(EditorTransformMode.LocalCoordinates);

			ParentTransformButton = transformModePanel.Add<Button>();
			ParentTransformButton.Text = "Parent";
			ParentTransformButton.MouseReleaseEvent += (_, _, _) => SetTransformMode(EditorTransformMode.ParentCoordinates);

			WorldTransformButton = transformModePanel.Add<Button>();
			WorldTransformButton.Text = "World";
			WorldTransformButton.MouseReleaseEvent += (_, _, _) => SetTransformMode(EditorTransformMode.WorldCoordinates);

			ModelEditor.Active.SelectedChanged += Active_SelectedChanged;
			Active_SelectedChanged();
			SetTransformMode(EditorTransformMode.ParentCoordinates);
		}

		public EditorTransformMode TransformMode { get; private set; }
		private void SetTransformMode(EditorTransformMode mode) {
			TransformMode = mode;

			switch (TransformMode) {
				case EditorTransformMode.LocalCoordinates:
					LocalTransformButton.Pulsing = true;
					ParentTransformButton.Pulsing = false;
					WorldTransformButton.Pulsing = false;
					return;
				case EditorTransformMode.ParentCoordinates:
					LocalTransformButton.Pulsing = false;
					ParentTransformButton.Pulsing = true;
					WorldTransformButton.Pulsing = false;
					return;
				case EditorTransformMode.WorldCoordinates:
					LocalTransformButton.Pulsing = false;
					ParentTransformButton.Pulsing = false;
					WorldTransformButton.Pulsing = true;
					return;
			}
		}

		private void CHANGE_Rotation(NumSlider self, double oldValue, double newValue) {
			ModelEditor.Active.File.RotateSelected((float)newValue);
		}
		private void CHANGE_TranslationX(NumSlider self, double oldValue, double newValue) {
			ModelEditor.Active.File.TranslateXSelected((float)newValue);
		}
		private void CHANGE_TranslationY(NumSlider self, double oldValue, double newValue) {
			ModelEditor.Active.File.TranslateYSelected((float)newValue);
		}
		private void CHANGE_ScaleX(NumSlider self, double oldValue, double newValue) {
			ModelEditor.Active.File.ScaleXSelected((float)newValue);
		}
		private void CHANGE_ScaleY(NumSlider self, double oldValue, double newValue) {
			ModelEditor.Active.File.ScaleYSelected((float)newValue);
		}
		private void CHANGE_ShearX(NumSlider self, double oldValue, double newValue) {
			ModelEditor.Active.File.ShearXSelected((float)newValue);
		}
		private void CHANGE_ShearY(NumSlider self, double oldValue, double newValue) {
			ModelEditor.Active.File.ShearYSelected((float)newValue);
		}

		private void Active_SelectedChanged() {
			PreUIDeterminations determinations = ModelEditor.Active.GetDeterminations();

			bool supportRotation = false;
			bool supportTranslation = false;
			bool supportScale = false;
			bool supportShear = false;

			if (determinations.Count == 0) {
				SelectMode = ViewportSelectMode.All;
				goto end;
			}

			if (determinations.Last.SelectMode != ViewportSelectMode.NotApplicable) {
				SelectMode = determinations.Last.SelectMode;
			}

			bool first = true;
			float rotV = 0f, posX = 0f, posY = 0f, scaX = 0f, scaY = 0f, sheX = 0f, sheY = 0f;

			foreach (var item in determinations.Selected) {
				if (first) {
					supportRotation = item.CanRotate();
					rotV = item.GetRotation();
					TransformRotation.GetNumSlider(0).Value = rotV;

					supportTranslation = item.CanTranslate();
					posX = item.GetTranslationX();
					posY = item.GetTranslationY();
					TransformTranslation.GetNumSlider(0).Value = posX;
					TransformTranslation.GetNumSlider(1).Value = posY;

					supportScale = item.CanScale();
					scaX = item.GetScaleX();
					scaY = item.GetScaleY();
					TransformScale.GetNumSlider(0).Value = scaX;
					TransformScale.GetNumSlider(1).Value = scaY;

					supportShear = item.CanShear();
					sheX = item.GetShearX();
					sheY = item.GetShearY();
					TransformShear.GetNumSlider(0).Value = sheX;
					TransformShear.GetNumSlider(1).Value = sheY;

					first = false;
				}
				else {
					// When multiple items are selected, disable support if values differ. ie.
					// 1. unsupported && equals   == don't support
					// 2. supported && not equals == don't support
					//        (which leads to the former scenario happening for the rest of the selected items)

					supportRotation = supportRotation && item.GetRotation() == rotV;
					supportTranslation = supportTranslation && item.GetTranslationX() == posX && item.GetTranslationY() == posY;
					supportScale = supportScale && item.GetScaleX() == scaX && item.GetScaleY() == scaY;
					supportShear = supportShear && item.GetShearX() == sheX && item.GetShearY() == sheY;
				}
			}

		end:
			TransformRotation.EnableSliders = supportRotation;
			TransformRotation.InputDisabled = !supportRotation;
			TransformTranslation.EnableSliders = supportTranslation;
			TransformTranslation.InputDisabled = !supportTranslation;
			TransformScale.EnableSliders = supportScale;
			TransformScale.InputDisabled = !supportScale;
			TransformShear.EnableSliders = supportShear;
			TransformShear.InputDisabled = !supportShear;
		}

		public override void PreRender() {
			base.PreRender();
		}

		public float CameraX { get; set; } = 0;
		public float CameraY { get; set; } = 0;
		public float CameraZoom { get; set; } = 1;

		public IEditorType? HoveredObject { get; private set; }
		public Vector2F HoverGridPos { get; private set; }
		protected override void OnThink(FrameState frameState) {
			// Hover determination
			HoverGridPos = FromScreenPosToGridPos(GetMousePos());

			bool canHoverTest_Bones = (SelectMode & ViewportSelectMode.Bones) == ViewportSelectMode.Bones;
			bool canHoverTest_Images = (SelectMode & ViewportSelectMode.Images) == ViewportSelectMode.Images;

			IEditorType? hovered = null;

			foreach (var model in ModelEditor.Active.File.Models) {
				if (canHoverTest_Bones) {
					foreach (var bone in model.GetAllBones()) {
						if (bone.HoverTest(HoverGridPos))
							hovered = bone;
					}
				}
			}

			if (HoveredObject != null) {
				HoveredObject.Hovered = false;
				HoveredObject.OnMouseLeft();
			}

			HoveredObject = hovered;

			if (hovered != null) {
				hovered.Hovered = true;
				hovered.OnMouseEntered();
			}
		}
		public Vector2F FromScreenPosToGridPos(Vector2F screenPos) {
			Vector2F screenCoordinates = Vector2F.Remap(screenPos, new(0), RenderBounds.Size, new(0, 0), EngineCore.GetWindowSize());
			Vector2F halfScreenSize = EngineCore.GetWindowSize() / 2;
			Vector2F centeredCoordinates = screenCoordinates - halfScreenSize;

			Vector2F withoutCamVars = centeredCoordinates * new Vector2F(RenderBounds.W / widthMultiplied, 1);
			Vector2F accountingForCamVars = (withoutCamVars / CameraZoom) + new Vector2F(CameraX, CameraY);
			return accountingForCamVars;
		}
		Vector2F ClickPos;
		bool DraggableCamera;
		public override void MouseClick(FrameState state, Types.MouseButton button) {
			base.MouseClick(state, button);
			ClickPos = FromScreenPosToGridPos(GetMousePos());
			if (HoveredObject != null && button == Types.MouseButton.Mouse1) {
				ModelEditor.Active.SelectObject(HoveredObject, state.KeyboardState.ShiftDown);
			}

			DraggableCamera = button == Types.MouseButton.Mouse2;
		}
		public override void MouseDrag(Element self, FrameState state, Vector2F delta) {
			base.MouseDrag(self, state, delta);
			if (DraggableCamera) {
				var dragPos = FromScreenPosToGridPos(GetMousePos());
				CameraX -= dragPos.X - ClickPos.X;
				CameraY -= dragPos.Y - ClickPos.Y;
			}
		}
		public override void MouseRelease(Element self, FrameState state, Types.MouseButton button) {
			base.MouseRelease(self, state, button);
			DraggableCamera = false;
		}
		public override void MouseScroll(Element self, FrameState state, Vector2F delta) {
			CameraZoom = Math.Clamp(CameraZoom + (delta.Y / 5 * CameraZoom), 0.05f, 10);
		}
		Camera3D cam;
		float widthMultiplied;
		public void Draw3DCursor() {
			Raylib.DrawSphere(new(HoverGridPos.X, HoverGridPos.Y, 0), 2, Color.RED);
		}

		/// <summary>
		/// Draws a bone and then its children based on its posing
		/// </summary>
		/// <param name="bone"></param>
		public void DrawBone(EditorBone bone) {
			var wt = bone.WorldTransform;
			var wp = wt.Translation.ToNumerics();
			var cameraFOV = cam.FovY;
			// Depending on cameraFOV, we shrink this down a bit...
			var limitBy = 500;
			var byHowMuch = cameraFOV < limitBy ? 1 - ((limitBy - cameraFOV) / limitBy) : 1;

			var color = bone.Selected ? Color.SKYBLUE : bone.Hovered ? bone.Color.Adjust(0, -0.3f, 0.3f) : bone.Color.Adjust(0, 0, -0.15f);
			ManagedMemory.Texture boneTex;

			if (bone.Length > 0) {
				boneTex = Level.Textures.LoadTextureFromFile("models/lengthbonetex.png");
				var lengthMul = (float)NMath.Remap(bone.Length, 0, 150, 0, 1, true);
				var r = 7 * lengthMul;
				var rI = 5 * lengthMul;
				Raylib.DrawRing(wp, rI * byHowMuch, r * byHowMuch, 0, 360, 32, color);
				Raylib.DrawCircleV(wp, rI * byHowMuch, color.Adjust(0, 0, -0.8f));
			}
			else
				boneTex = Level.Textures.LoadTextureFromFile("models/lengthlessbonetex.png");

			bone.GetTexCoords(byHowMuch, out var baseBottom, out var baseTop, out var tipBottom, out var tipTop, out var lengthLimit);

			if (bone.Length > 0) {
				Rlgl.Begin(DrawMode.TRIANGLES);

				Rlgl.Color4ub(color.R, color.G, color.B, color.A);
				Rlgl.SetTexture(((Texture2D)boneTex).Id);
				Rlgl.TexCoord2f(0, 0); Rlgl.Vertex3f(tipTop.X, tipTop.Y, 0);
				Rlgl.TexCoord2f(1, 1); Rlgl.Vertex3f(baseBottom.X, baseBottom.Y, 0);
				Rlgl.TexCoord2f(1, 0); Rlgl.Vertex3f(tipBottom.X, tipBottom.Y, 0);

				Rlgl.TexCoord2f(1, 1); Rlgl.Vertex3f(baseBottom.X, baseBottom.Y, 0);
				Rlgl.TexCoord2f(0, 0); Rlgl.Vertex3f(tipTop.X, tipTop.Y, 0);
				Rlgl.TexCoord2f(0, 1); Rlgl.Vertex3f(baseTop.X, baseTop.Y, 0);

				// Inverted because for some reason disabling backface culling doesn't want to work
				// (or maybe something else is going on that I don't understand, regardless, that's
				// why this is repeated but in reverse)

				Rlgl.TexCoord2f(1, 0); Rlgl.Vertex3f(tipBottom.X, tipBottom.Y, 0);
				Rlgl.TexCoord2f(1, 1); Rlgl.Vertex3f(baseBottom.X, baseBottom.Y, 0);
				Rlgl.TexCoord2f(0, 0); Rlgl.Vertex3f(tipTop.X, tipTop.Y, 0);

				Rlgl.TexCoord2f(0, 1); Rlgl.Vertex3f(baseTop.X, baseTop.Y, 0);
				Rlgl.TexCoord2f(0, 0); Rlgl.Vertex3f(tipTop.X, tipTop.Y, 0);
				Rlgl.TexCoord2f(1, 1); Rlgl.Vertex3f(baseBottom.X, baseBottom.Y, 0);

				Rlgl.End();
			}
			else {
				var size = 32f * byHowMuch;
				Rlgl.PushMatrix();
				var wps = bone.WorldTransform.LocalToWorld(0, 0).ToNumerics();
				var wd = bone.WorldTransform.LocalToWorld(1, 0).ToNumerics();
				var dir = (wd - wps);
				var rot = MathF.Atan2(dir.Y, dir.X).ToDegrees() + 90;
				Rlgl.Translatef(wps.X, wps.Y, 0);
				Rlgl.Rotatef(rot, 0, 0, 1);
				Raylib.DrawTexturePro(
					boneTex,
					new(0, 0, boneTex.Width, boneTex.Height),
					new(-size / 2f, -size / 2f, size, size),
					new(0),
					0,
					color
				);
				Rlgl.PopMatrix();
			}
			foreach (var child in bone.Children) {
				DrawBone(child);
			}
		}
		/// <summary>
		/// Draws all models in the working model list
		/// </summary>
		public void DrawModels() {
			Rlgl.DisableBackfaceCulling();
			foreach (var model in ModelEditor.Active.File.Models) {
				foreach (var bone in model.GetAllBones()) {
					DrawBone(bone);
				}
			}
			Rlgl.EnableBackfaceCulling();
		}
		public override void Paint(float width, float height) {
			cam = new Camera3D() {
				Projection = CameraProjection.CAMERA_ORTHOGRAPHIC,
				FovY = EngineCore.GetWindowHeight() / CameraZoom,
				Position = new System.Numerics.Vector3(CameraX, CameraY, -500),
				Target = new System.Numerics.Vector3(CameraX, CameraY, 0),
				Up = new(0, -1, 0),
			};
			var globalpos = Graphics2D.Offset;
			var oldSize = EngineCore.GetScreenSize();
			var currentAspectRatio = width / height;
			var intendedAspectRatio = oldSize.W / oldSize.H;
			var accomodation = intendedAspectRatio / currentAspectRatio;
			widthMultiplied = width * accomodation;

			Rlgl.Viewport((int)(globalpos.X - ((widthMultiplied - width) / 2)), (int)(0), (int)(widthMultiplied), (int)height);
			Raylib.BeginMode3D(cam);

			Checkerboard.Draw();
			Raylib.DrawLine3D(new(-10000, 0, 0), new(10000, 0, 0), Color.BLACK);
			Raylib.DrawLine3D(new(0, -10000, 0), new(0, 10000, 0), Color.BLACK);

			DrawModels();
			Draw3DCursor();

			Raylib.EndMode3D();
			Graphics2D.SetDrawColor(255, 255, 255);

			Graphics2D.DrawText(new(4, height - 18), $"gridpos:  {HoverGridPos}", "Consolas", 12, Anchor.BottomLeft);
			Graphics2D.DrawText(new(4, height - 4), $"mousepos: {GetMousePos()}", "Consolas", 12, Anchor.BottomLeft);


			Rlgl.Viewport(0, 0, (int)oldSize.W, (int)oldSize.H);
		}
	}
}
