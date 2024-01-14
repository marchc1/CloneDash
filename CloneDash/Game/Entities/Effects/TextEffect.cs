using Raylib_cs;

namespace CloneDash.Game.Entities
{
    public class TextEffect : MapEntity
    {
        public TextEffect(DashGame game, string text, Vector2F position, Color? c = null) : base(game, EntityType.Effect) {
            TextureSize = new(96, 96);
            Text = text;
            Position = position;
            if (c.HasValue)
                Color = c.Value;
        }
        public override bool VisTest(float gamewidth, float gameheight, float xPosition) {
            return true;
        }

        public bool SuppressAutoDeath { get; set; } = false;

        public string Text { get; set; } = "Not Set???";
        public Color Color { get; set; } = new(255, 255, 255, 255);
        public Vector2F Position { get; set; }

        public override void Draw(Vector2F idealPosition) {
            float ageToDie = 1;
            double lifetime;

            if (SuppressAutoDeath)
                lifetime = 0;
            else {
                lifetime = this.Lifetime;
                if (lifetime > ageToDie) {
                    this.MarkedForRemoval = true;
                    return;
                }
            }

            var pos0to1 = Ease.OutExpo(Raymath.Remap((float)lifetime, 0, ageToDie, 0, 1));
            var pos = pos0to1 * Game.ScreenManager.ScrHeight * 0.2f;

            Graphics.SetDrawColor(Color, (int)(Color.A * Raymath.Remap((float)lifetime, 0, ageToDie, 1, 0)));
            Graphics.DrawText(Position - new Vector2F(0, pos), Text, "Arial", 34, FontAlignment.Center, FontAlignment.Center);
        }
    }
}
