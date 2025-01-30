using System.Numerics;
using Raylib_cs;
using Nucleus.Types;

namespace Nucleus.Core
{
	public class Model3 : Model3Component
	{
		private static int model3ID = 0;

		public bool RespectsGlobalPause { get; set; } = true;

		private Color __color = Color.WHITE;
		private Vector3 __hsv = new(0, 1, 1);

		public Color Color {
			get {
				return __color;
			}
			set {
				__color = value;
			}
		}
		public Vector3 HSV {
			get {
				return __hsv;
			}
			set {
				__hsv = value;
			}
		}

		internal Model3() {
			model3ID += 1;
			ID = model3ID;

			Root = this;
			//BindPose = new TransformVQV(new(0, 0, 0), Quaternion.CreateFromYawPitchRoll(0, -90 / NMath.DEG2RAD, 0), new(1, 1, 1));
		}
		public int ID { get; private set; }
		public Model3Cache Model { get; internal set; }

		/// <summary>
		/// All bones in this Model
		/// </summary>
		public List<Model3Bone> Bones { get; set; } = [];
		public Model3Bone GetBoneByName(string name) {
			if (!BonesByName.ContainsKey(name))
				throw new KeyNotFoundException($"No bone in this skeleton exists with the name '{name}'");

			return BonesByName[name];
		}

		internal Matrix4x4[] LocalBoneTransforms { get; set; }
		internal Matrix4x4[] FinalBoneTransforms { get; set; }
		internal Dictionary<string, Model3Bone> BonesByName { get; set; }

		/// <summary>
		/// Calculates the bone transform matrices for each bone, ran every frame pre-rendering models
		/// </summary>
		internal void CalculateBoneTransforms() {
			for (int i = 0; i < Bones.Count; i++) {
				var bone = Bones[i];
				Matrix4x4 parentTransform = Matrix4x4.Identity;

				if (bone.Parent != Root)
					parentTransform = LocalBoneTransforms[(bone.Parent as Model3Bone).ID];

				Matrix4x4 animTransform = bone.Transform;
				LocalBoneTransforms[i] = parentTransform * animTransform;

				// t * wt * lbt * bibm // WorldTransform may be problematic?
				FinalBoneTransforms[i] = Transform * WorldTransform * LocalBoneTransforms[i] * bone.InverseBindMatrix;
			}
		}
		public Dictionary<string, Model3AnimationChannel> Animations { get; private set; } = [];
		public void PlayAnimation(string anim, bool loops = false, string? loopFallback = null) {
			var animation = this.Animations.FirstOrDefault(x => x.Key == anim).Value;

			if (animation == default) {
				Logs.Warn($"Model does not have animation {anim}!");
				return;
			}

			foreach (var animKVP in Animations)
				animKVP.Value.StopPlaying();

			animation.StartPlaying(loops, loopFallback);
		}

		public bool PlayingAnimation => this.Animations.FirstOrDefault(x => x.Value.Playing == true).Value != default;

		public void Render() {
			foreach (var mapair in Animations) {
				mapair.Value.Process();
			}

			foreach (var mmpair in Model.MeshMaterialPairs) {
				var materialCache = Model.Materials[mmpair.material];
				var mat = materialCache.Material;
				var mesh = mmpair.mesh;
				var meshName = Model.MeshNamePairs[mesh.VaoId];

				var shader = mat.Shader;

				CalculateBoneTransforms();

				bool doRender = true;
				float alpha = 1f;

				for (int i = 0; i < Bones.Count; i++) {
					var bone = Bones[i];
					shader.SetShaderValueMatrix($"bones[{i}].final", FinalBoneTransforms[i]);
					int activeSlot = 1;
					if (bone.ActiveSlot != 0)
						activeSlot = bone.ActiveSlot;

					var slots = bone.Cache.Slots;
					for (int i2 = 0; i2 < slots.Count; i2++) {
						var realIndex = i2 + 1;
						var isActive = realIndex == bone.ActiveSlot;
						var slotTarget = slots[i2];
						if (slotTarget == meshName) {
							if (!isActive) {
								doRender = false;
								break;
							}
							else {
								alpha = bone.ActiveSlotAlpha;
							}
						}
					}
				}

				if (!doRender)
					continue;

				var transformDefault = Matrix4x4.CreateTranslation(0, 0, 0) * Matrix4x4.CreateFromQuaternion(Quaternion.Identity) * Matrix4x4.CreateScale(1);
				var cd = shader.GetShaderLocation("colorMult");
				shader.SetShaderValue("colorMult", new Vector4(__color.R / 255f, __color.G / 255f, __color.B / 255f, (__color.A / 255f) * alpha), ShaderUniformDataType.SHADER_UNIFORM_VEC4);
				shader.SetShaderValue("inputHSV", __hsv, ShaderUniformDataType.SHADER_UNIFORM_VEC3);
				Rlgl.DisableBackfaceCulling();
				Raylib.DrawMesh(mesh, mat, transformDefault);
				Rlgl.EnableBackfaceCulling();
			}
		}

		public override string ToString() {
			return $"Model V3 #{ID}";
		}

		/// <summary>
		/// -1 refers to the Model3 itself
		/// </summary>
		/// <returns></returns>
		public Model3Component GetComponentByID(int ID) => ID == -1 ? this : GetBoneByID(ID);

		public Model3Bone GetBoneByID(int ID) => Bones.First(x => x.ID == ID);

		internal void Build() {
			foreach (var animCache in Model.Animations) {
				Animations[animCache.Name] = new Model3AnimationChannel() {
					AnimationData = animCache,
					AnimationPlayhead = 0,
					BoundTo = this,
					Loops = false,
					Speed = 1
				};
			}
		}
	}
}
