using Nucleus.Core;
using Nucleus.Models.Runtime;
using Nucleus.Types;
using Raylib_cs;

namespace Nucleus.Engine
{

    public class ModelEntity : Entity
    {
        private ModelInstance __model;
        public ModelInstance Model {
            get {
                return __model;
            }
            set {
                __model = value;
            }
        }

		public double GetAnimationLength(string animation) => throw new NotImplementedException();


		public bool PlayingAnimation => false;

        public void SetModel(string modelname) {
            __model = Level.Models.CreateInstanceFromFile(modelname);
        }

        public bool Visible { get; set; } = true;

        public unsafe static ModelEntity Create(string model) {
            ModelEntity entity = new ModelEntity();
			entity.Level = EngineCore.Level;
            entity.__model = EngineCore.Level.Models.CreateInstanceFromFile(model);
            return entity;
        }

        public ModelEntity(string? model = null) {
            if (model != null)
                __model = EngineCore.Level.Models.CreateInstanceFromFile(model);
		}

        public override void Render(FrameState frameState) => Render();
        public void Render() {
            if (!Visible) return;
            if (Model == null) return;

            Model.Position = new(Position.X, Position.Y);
            Model.Render();
        }
    }
}
