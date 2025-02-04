using Nucleus.Core;
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
	public enum ViewportSelectMode {
		NotApplicable = -1,
		None = 0,

		Bones = 1,
		Images = 2,
		Other = 4,

		All = Bones | Images | Other
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

			ModelEditor.Active.SelectedChanged += Active_SelectedChanged;
			Active_SelectedChanged();
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

					}
				}
			}

			HoveredObject = hovered;
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
		public override void MouseClick(FrameState state, Types.MouseButton button) {
			base.MouseClick(state, button);
			ClickPos = FromScreenPosToGridPos(GetMousePos());
			switch (HoveredObject) {

			}
		}
		public override void MouseDrag(Element self, FrameState state, Vector2F delta) {
			base.MouseDrag(self, state, delta);
			var dragPos = FromScreenPosToGridPos(GetMousePos());
			CameraX -= dragPos.X - ClickPos.X;
			CameraY -= dragPos.Y - ClickPos.Y;
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
			/*Rlgl.PushMatrix();
			bone.EditMatrix();
			var cameraFOV = cam.FovY;
			// Depending on cameraFOV, we shrink this down a bit...
			var limitBy = 500;
			var byHowMuch = cameraFOV < limitBy ? 1 - ((limitBy - cameraFOV) / limitBy) : 1;

			var color = bone.Color;

			if (bone.Length <= 0) {
				var radius = 5f;
				var sizeWhenActiveAxis = 12f;
				var sizeWhenNotActiveAxis = 5f;
				var lineSize = 1.5f;

				if (cameraFOV < limitBy) {
					radius *= byHowMuch;
					sizeWhenActiveAxis *= byHowMuch;
					sizeWhenNotActiveAxis *= byHowMuch;
					lineSize *= byHowMuch;
				}

				Raylib.DrawRing(new(0, 0), radius / 1.5f, radius, 0, 360, 16, color);
				radius *= 1.5f;

				Raylib.DrawCube(new(radius + (sizeWhenActiveAxis / 2), 0, 0), sizeWhenActiveAxis, lineSize, 1, color);
				Raylib.DrawCube(new(-radius - (sizeWhenNotActiveAxis / 2), 0, 0), sizeWhenNotActiveAxis, lineSize, 1, color);
				Raylib.DrawCube(new(0, radius + (sizeWhenNotActiveAxis / 2), 0), lineSize, sizeWhenNotActiveAxis, 1, color);
				Raylib.DrawCube(new(0, -radius - (sizeWhenNotActiveAxis / 2), 0), lineSize, sizeWhenNotActiveAxis, 1, color);
			}
			else {
				var length = bone.Length;
				var radius = (float)NMath.Remap(length, 0.2f, 150f, 1f, 5f, clampInput: true);
				radius *= byHowMuch;
				Rlgl.Begin(DrawMode.TRIANGLES);
				var limit = 230;
				var lengthClamped = Math.Clamp(length, 0, limit);
				var lengthNormalized = lengthClamped / limit;

				Rlgl.TexCoord2f(0, 0);
				Rlgl.Color4ub(color.R, color.B, color.G, color.A);

				Rlgl.Vertex3f(0, 0, 0);
				Rlgl.Vertex3f(lengthNormalized * 25, lengthNormalized * 5 * byHowMuch, 0);
				Rlgl.Vertex3f(lengthNormalized * 25, lengthNormalized * -5 * byHowMuch, 0);

				Rlgl.Vertex3f(lengthNormalized * 25, lengthNormalized * -5 * byHowMuch, 0);
				Rlgl.Vertex3f(lengthNormalized * 25, lengthNormalized * 5 * byHowMuch, 0);
				Rlgl.Vertex3f(length, 0, 0);

				Rlgl.End();

				Raylib.DrawRing(new(0, 0), radius / 1.4f, radius, 0, 360, 16, color);
				Raylib.DrawCircleV(new(0, 0), radius / 1.4f, Color.DARKGRAY);
			}
			foreach (var child in bone.Children) {
				DrawBone(child);
			}
			Rlgl.PopMatrix();*/
		}
		/// <summary>
		/// Draws all models in the working model list
		/// </summary>
		public void DrawModels() {
			foreach (var model in ModelEditor.Active.File.Models) {
				foreach (var bone in model.GetAllBones()) {
					DrawBone(bone);
				}
			}
		}
		public override void Paint(float width, float height) {
			cam = new Camera3D() {
				Projection = CameraProjection.CAMERA_ORTHOGRAPHIC,
				FovY = EngineCore.GetWindowHeight() / CameraZoom,
				Position = new System.Numerics.Vector3(CameraX, CameraY, -500),
				Target = new System.Numerics.Vector3(CameraX, CameraY, 0),
				Up = new(0, -1, 0)
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
