using Nucleus.Core;
using Nucleus.Models.Runtime;
using Nucleus.Types;
using Raylib_cs;

namespace Nucleus.Engine
{

	public class ModelEntity : Entity
	{
		private ModelInstance __model;
		private AnimationHandler __anim;
		public ModelInstance Model {
			get {
				return __model;
			}
			set {
				__model = value;
			}
		}

		public AnimationHandler Animations => __anim;
		public bool PlayingAnimation => __anim.IsPlayingAnimation();

		public bool Visible { get; set; } = true;

		public unsafe static ModelEntity Create(string pathID, string model) {
			ModelEntity entity = new ModelEntity();
			entity.Level = EngineCore.Level;
			entity.__model = EngineCore.Level.Models.CreateInstanceFromFile(pathID, model);
			entity.__anim = new(entity.__model.Data);
			return entity;
		}

		public ModelEntity(string modelPath = "models", string? model = null) {
			if (model == null) return;

			var data = Level.Models.LoadModelFromFile(modelPath, model);
			__model = data.Instantiate();
			__anim = new(data);
		}

		public override void Render(FrameState frameState) => Render();
		public void Render() {
			if (!Visible) return;
			if (Model == null) return;

			if (!Level.Paused) __anim.AddDeltaTime(Level.CurtimeDelta);

			__anim.Apply(Model);
			Model.Position = Position;
			Model.Scale = Scale;
			Model.Render();
		}
	}
}
