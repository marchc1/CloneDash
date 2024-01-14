using CloneDash.Systems;

namespace CloneDash.Game.Entities
{
    public class DoubleHitEnemy : MapEntity
    {
        public DoubleHitEnemy(DashGame game) : base(game, EntityType.Double) {
            TextureSize = new(160);
            Interactivity = EntityInteractivity.Hit;
            DoesDamagePlayer = true;
        }

        protected override void OnHit(PathwaySide side) {
            Kill();
        }

        // because the double enemies are attached to each other, damage the player if they miss the entity
        protected override void OnMiss() {
            DamagePlayer();
        }

        public override void Draw(Vector2F idealPosition) {
            if (this.Dead)
                return;

            Graphics.SetDrawColor(255, 255, 255);
            var p = (idealPosition).X;

            if (this.Pathway == PathwaySide.Top) {
                Graphics.DrawImage(TextureSystem.fightable_double, RectangleF.FromPosAndSize(new(p, Game.TopPathway.Position.Y), TextureSize), TextureSize / 2, 0, new(30, 1.5f, 1));
            }
            if (this.Pathway == PathwaySide.Bottom) {
                Graphics.DrawImage(TextureSystem.fightable_double, RectangleF.FromPosAndSize(new(p, Game.BottomPathway.Position.Y), TextureSize), TextureSize / 2, 180, new(30, 1.5f, 1));
            }

            Graphics.SetDrawColor(220, 135, 70, 90);
            Graphics.DrawLine(p, Game.TopPathway.Position.Y, p, Game.BottomPathway.Position.Y, 60 * Game.ScreenManager.ScrHRatio);
        }
    }
}
