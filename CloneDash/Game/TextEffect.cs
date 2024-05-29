using Nucleus;
using Nucleus.Core;
using Nucleus.Engine;
using Nucleus.Types;
using Raylib_cs;
using static Nucleus.NMath;

namespace CloneDash.Game
{
    public class TextEffect : Entity
    {
        public TextEffect(string text, Vector2F position, Color? c = null) {
            Text = text;
            Position = position;
            if (c.HasValue)
                Color = c.Value;
        }

        public bool SuppressAutoDeath { get; set; } = false;

        public string Text { get; set; } = "Not Set???";
        public Color Color { get; set; } = new(255, 255, 255, 255);
        public Vector2F Position { get; set; }

        public override void PostRender(FrameState frameState) {
            float ageToDie = 1;
            double lifetime;

            if (SuppressAutoDeath)
                lifetime = 0;
            else {
                lifetime = this.Lifetime;
                if (lifetime > ageToDie) {
                    this.Remove();
                    return;
                }
            }

            var pos0to1 = Ease.OutExpo(Raymath.Remap((float)lifetime, 0, ageToDie, 0, 1));
            var pos = pos0to1 * frameState.WindowHeight * 0.2f;

            Graphics2D.SetDrawColor(Color, (int)(Color.A * Raymath.Remap((float)lifetime, 0, ageToDie, 1, 0)));
            Graphics2D.DrawText(Position - new Vector2F(0, pos), Text, "Arial", (int)NMath.Remap(NMath.Ease.InExpo(NMath.Remap(lifetime, 0, ageToDie, 0, 1)), 0, 1, 34, 6), TextAlignment.Center, TextAlignment.Center);
        }
    }
}
