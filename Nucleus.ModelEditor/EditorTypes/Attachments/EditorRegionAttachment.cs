using Newtonsoft.Json;
using Nucleus.ManagedMemory;
using Nucleus.Models;
using Nucleus.Types;
using Nucleus.UI;
using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.ModelEditor
{
	public class EditorRegionAttachment : EditorAttachment
	{
		public override string SingleName => "region";
		public override string PluralName => "regions";
		public override string EditorIcon => "models/region.png";

		public string GetPath() => Path ?? $"<{Name}>";
		public string? Path { get; set; } = null;

		private Vector2F pos, scale = new(1, 1);

		public Vector2F Position { get => pos; set => pos = value; }
		public float Rotation { get; set; }
		public Vector2F Scale { get => scale; set => scale = value; }

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

		public override void Render() {
			// todo ^^ missing texture (prob just purple-black checkerboard)

			WorldTransform = Transformation.CalculateWorldTransformation(pos, Rotation, scale, Vector2F.Zero, TransformMode.Normal, Slot.Bone.WorldTransform);

			var quadpoints = this.quadpoints();

			AtlasRegion region = quadpoints.Region;
			Texture tex = quadpoints.Texture;
			Vector2F ___BL = quadpoints.TL, ___BR = quadpoints.TR, ___TL = quadpoints.BL, ___TR = quadpoints.BR;

			Rlgl.Begin(DrawMode.TRIANGLES);
			Rlgl.SetTexture(((Texture2D)tex).Id);

			Rlgl.Color4ub(255, 255, 255, 255);

			float uStart, uEnd, vStart, vEnd;
			uStart = (float)region.X / (float)tex.Width;
			uEnd = uStart + ((float)region.W / (float)tex.Width);

			vStart = ((float)region.Y / (float)tex.Height);
			vEnd = vStart + ((float)region.H / (float)tex.Height);

			Rlgl.TexCoord2f(uStart, vEnd); Rlgl.Vertex3f(___BL.X, ___BL.Y, 0);
			Rlgl.TexCoord2f(uEnd, vStart); Rlgl.Vertex3f(___TR.X, ___TR.Y, 0);
			Rlgl.TexCoord2f(uStart, vStart); Rlgl.Vertex3f(___TL.X, ___TL.Y, 0);

			Rlgl.TexCoord2f(uEnd, vEnd); Rlgl.Vertex3f(___BR.X, ___BR.Y, 0);
			Rlgl.TexCoord2f(uEnd, vStart); Rlgl.Vertex3f(___TR.X, ___TR.Y, 0);
			Rlgl.TexCoord2f(uStart, vEnd); Rlgl.Vertex3f(___BL.X, ___BL.Y, 0);

			Rlgl.End();

			Rlgl.DrawRenderBatchActive();
			if(Selected || Hovered) {
				Color lineC = new Color(100, 160, 200);
				Color cornerC = new Color(170, 225, 255);

				var cornerSize = 8;

				Rlgl.SetLineWidth(1);
				Raylib.DrawLineV(___BL.ToNumerics(), ___BR.ToNumerics(), lineC);
				Raylib.DrawLineV(___BR.ToNumerics(), ___TR.ToNumerics(), lineC);
				Raylib.DrawLineV(___TR.ToNumerics(), ___TL.ToNumerics(), lineC);
				Raylib.DrawLineV(___TL.ToNumerics(), ___BL.ToNumerics(), lineC);
				Rlgl.DrawRenderBatchActive();
				
				Rlgl.SetLineWidth(4);

				Raylib.DrawLineV(___BL.ToNumerics(), (___BL + ((___BR - ___BL).Normalize() * cornerSize)).ToNumerics(), cornerC);
				Raylib.DrawLineV(___BL.ToNumerics(), (___BL + ((___TL - ___BL).Normalize() * cornerSize)).ToNumerics(), cornerC);

				Raylib.DrawLineV(___BR.ToNumerics(), (___BR + ((___BL - ___BR).Normalize() * cornerSize)).ToNumerics(), cornerC);
				Raylib.DrawLineV(___BR.ToNumerics(), (___BR + ((___TR - ___BR).Normalize() * cornerSize)).ToNumerics(), cornerC);

				Raylib.DrawLineV(___TR.ToNumerics(), (___TR + ((___TL - ___TR).Normalize() * cornerSize)).ToNumerics(), cornerC);
				Raylib.DrawLineV(___TR.ToNumerics(), (___TR + ((___BR - ___TR).Normalize() * cornerSize)).ToNumerics(), cornerC);

				Raylib.DrawLineV(___TL.ToNumerics(), (___TL + ((___TR - ___TL).Normalize() * cornerSize)).ToNumerics(), cornerC);
				Raylib.DrawLineV(___TL.ToNumerics(), (___TL + ((___BL - ___TL).Normalize() * cornerSize)).ToNumerics(), cornerC);

				Rlgl.DrawRenderBatchActive();

				Rlgl.SetLineWidth(1);
			}
		}

		public override bool HoverTest(Vector2F gridPos) {
			var quadpoints = this.quadpoints();
			return gridPos.TestPointInQuad(quadpoints.TL, quadpoints.TR, quadpoints.BL, quadpoints.BR);
		}
	}
}
