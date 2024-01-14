using CloneDash.Game.Components;
using CloneDash.Systems;
using Raylib_cs;

namespace CloneDash.Game.Entities
{
    public class Raider : MapEntity
    {
        private ParabolaFrom3Points p;
        public Raider(DashGame game) : base(game, EntityType.Raider) {
            TextureSize = new(160);
            Interactivity = EntityInteractivity.Hit;
            DoesDamagePlayer = true;
            DoesPunishPlayer = true;
        }

        public override void Build() {
            Pathway pathway = Game.GetPathway(Pathway);

            float test = Pathway == PathwaySide.Top ? 0.33f : 0.14f;
            float spawnPoint = Game.ScreenManager.ScrWidth * 1f;
            p = Parabola.From3Points(
                new(spawnPoint, (Game.ScreenManager.ScrHeight/2) + TextureSize.H + 90),
                new(Raymath.Remap(0.7f, 0, 1, pathway.Position.X, spawnPoint), pathway.Position.Y + (Game.ScreenManager.ScrHeight * test)),
                pathway.Position
            );
        }

        private Vector2F position;

        public override void ChangePosition(ref Vector2F pos) {
            pos.y = p.CalculateY(pos.X);
            position = new(pos.X, pos.Y);
        }

        protected override void OnHit(PathwaySide side) {
            Kill();
        }

        protected override void OnMiss() {
            PunishPlayer();
            if (Game.PlayerController.Pathway == this.Pathway) {
                DamagePlayer();
            }
        }

        public override void Draw(Vector2F idealPosition) {
            if (Game.Debug) {
                Vector2F[] parabolacurve = new Vector2F[200];
                for (int i = 0; i < parabolacurve.Length; i++) {
                    var x = Raymath.Remap(i, 0, 200, 1600, 0);
                    parabolacurve[i] = new(x, p.CalculateY(x));
                }
                Graphics.SetDrawColor(Color.RED);
                Graphics.DrawLineStrip(parabolacurve);
            }
            
            if (this.Dead)
                return;
            
            // switch to remap conductor time from hittime to showtime to 0 to 1?

            Graphics.SetDrawColor(255, 255, 255, 255);
            //future calc
            float offset = 0.05f;
            Vector2F future = new(position.X + offset, p.CalculateY(position.X + offset));

            float dx = future.X - position.X, dy = future.Y - position.Y;
            double r = Math.Atan2(dy, dx);

            if (r < 0)
                r = Math.Abs(r);
            else
                r = 2 * Math.PI - r;

            r = r * (180d / Math.PI);

            DrawSpriteBasedOnPathway(TextureSystem.fightable_arrow, RectangleF.FromPosAndSize(idealPosition, TextureSize), TextureSize / 2, (float)-r);
        }
    }
}
