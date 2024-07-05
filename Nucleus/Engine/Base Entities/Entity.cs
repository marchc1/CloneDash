using Nucleus.Types;
using System.Numerics;

namespace Nucleus.Engine
{
    public abstract class Entity : IValidatable
    {
        public Level Level { get; set; }
        public T GetLevel<T>() where T : Level => (T)Level;
        private bool __markedForRemoval = false;
        public bool IsValid() => !__markedForRemoval;
        public void Remove() {
            if (__markedForRemoval)
                return;

            if (Level.HasEntity(this)) 
                Level.Remove(this);
            

            OnRemove();
            __markedForRemoval = true;
        }

        public bool Enabled { get; set; } = true;

        public virtual void Initialize() { }
        public virtual void OnRemove() { }

        public virtual void PreThink(ref FrameState frameState) { }
        public virtual void ModifyMouseState(ref MouseState mouseState) { }
        public virtual void ModifyKeyboardState(ref KeyboardState keyboardState) { }
        public virtual void Think(FrameState frameState) { }
        public virtual void PostThink(FrameState frameState) { }
        public virtual void PreRender(FrameState frameState) { }
        public virtual void Render(FrameState frameState) { }
        public virtual void Render2D(FrameState frameState) { }
        public virtual void PostRender(FrameState frameState) { }

        public DateTime Created { get; private set; } = DateTime.Now;
        public double Lifetime => (DateTime.Now - Created).TotalSeconds;

        public bool ThinksForItself { get; set; } = true;
        public bool RendersItself { get; set; } = true;

        public Vector2F Position { get; set; } = Vector2F.Zero;
        public Vector3 Rotation { get; set; } = Vector3.Zero;
        public Vector2F Scale { get; set; } = Vector2F.One;
    }
}
