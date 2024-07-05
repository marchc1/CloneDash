using Nucleus.Core;
using Nucleus.Types;
using Raylib_cs;

namespace Nucleus.Engine
{

    public class ModelEntity : Entity
    {
        private Model3 __model;
        public Model3 Model {
            get {
                return __model;
            }
            set {
                __model = value;
            }
        }

        public bool PlayingAnimation => Model.PlayingAnimation;

        public void SetModel(string modelname, string? animation = null, bool loopAnimation = false, string? fallback = null) {
            __model = Model3System.Load(modelname);
            if (animation != null)
                PlayAnimation(animation, loopAnimation, fallback);
        }

        private System.Numerics.Vector3 __tint = new(0, 1, 1);
        private Color __color = new(255, 255, 255, 255);

        public System.Numerics.Vector3 HSV {
            get { return __tint; }
            set {
                __tint = value;
                Model.HSV = value;
            }
        }
        public Color Color {
            get { return __color; }
            set {
                __color = value;
                Model.Color = value;
            }
        }

        public bool Visible { get; set; } = true;

        public unsafe static ModelEntity Create(string model) {
            ModelEntity entity = new ModelEntity();

            entity.__model = Model3System.Load(model);
            return entity;
        }

        public ModelEntity(string? model = null) {
            if (model != null)
                __model = Model3System.Load(model);
        }

        public void PlayAnimation(string animationName, bool loop = false, string? fallback = null) => Model.PlayAnimation(animationName, loop, fallback);
        public override void Render(FrameState frameState) => Render();
        public void Render() {
            if (!Visible)
                return;

            Model.Position = new(Position.X, Position.Y, 0);
            Model.Rotation = Rotation;
            Model.Render();
        }
    }
}
