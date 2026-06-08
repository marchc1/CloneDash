using Nucleus.Core;
using Nucleus.Engine;
using Nucleus.ManagedMemory;
using Nucleus.Models.Runtime;
using Nucleus.Types;
using Raylib_cs;

namespace Nucleus.Entities
{

	public class ModelEntity : Entity
	{
		protected Dictionary<string, float> shaderlocs_float = [];
		public void SetShaderUniform(string name, float value) {
			shaderlocs_float[name] = value;
		}
		protected ModelInstance? __model;
		protected readonly AnimationHandler __anim = new();

		public ModelInstance? Model {
			get {
				return __model;
			}
			set {
				__model = value;
				__anim.SetModel(value);
			}
		}

		public AnimationHandler Animations {
			get => __anim;
		}

		public bool PlayingAnimation => __anim?.IsPlayingAnimation() ?? false;
		public bool AnimationQueued => __anim?.IsAnimationQueued() ?? false;

		public bool Visible { get; set; } = true;

		public static ModelEntity Create(ModelData data) {
			ModelEntity entity = new ModelEntity();
			entity.Level = EngineCore.Level;
			entity.__model = data.Instantiate();
			entity.__anim.SetModel(entity.__model);
			return entity;
		}
		public static ModelEntity Create(string pathID, string model) {
			ModelEntity entity = new ModelEntity();
			entity.Level = EngineCore.Level;
			entity.__model = EngineCore.Level.Models.CreateInstanceFromFile(pathID, model);
			entity.__anim.SetModel(entity.__model);
			return entity;
		}

		public ModelEntity(string modelPath = "models", string? model = null) {
			if (model == null) return;

			var data = Level.Models.LoadModelFromFile(modelPath, model);
			__model = data.Instantiate();
			__anim.SetModel(__model);
		}

		public IShader? Shader { get; set; }

		public override void Render(FrameState frameState) => Render();
		public double AnimationSpeed = 1;

		public virtual void Render() {
			if (!Visible) return;
			if (Model == null) return;

			if (!Level.Paused) __anim?.AddDeltaTime(Level.RendertimeDelta * AnimationSpeed);

			__anim?.Apply(Model);
			Model.Position = Position;
			Model.Scale = Scale;

			var shader = Shader;
			var isvalid = IValidatable.IsValid(shader);
			if (isvalid) {
				foreach (var fl in shaderlocs_float) shader?.SetUniform(fl.Key, fl.Value);
				shader?.Activate();
			}

			Model.Render(useDefaultShader: !isvalid);

			if (isvalid)
				shader?.Deactivate();
		}

		public void SetToSetupPose() => Model?.SetToSetupPose();
	}
}
