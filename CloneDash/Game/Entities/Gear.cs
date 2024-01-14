using CloneDash.Systems;

namespace CloneDash.Game.Entities
{
    public class Gear : MapEntity
    {
        public Gear(DashGame game) : base(game, EntityType.Gear) {
            TextureSize = new(160);
            Interactivity = EntityInteractivity.Avoid;
            DoesDamagePlayer = true;
        }

        protected override void OnPass() {
            this.RewardPlayer();
        }

        public override void Draw(Vector2F idealPosition) {
            Vector2F offset = Pathway == PathwaySide.Bottom ? new(0,TextureSize.Y / 2) : new(0, 0);
            
            var scissorrect = RectangleF.FromPosAndSize(offset + idealPosition + new Vector2F(0, Game.ScreenManager.ScrHeight / 2) - new Vector2F(TextureSize.X / 2, TextureSize.Y / 2), new(TextureSize.X, TextureSize.Y / 2));
            if (Pathway == PathwaySide.Bottom)
                Graphics.ScissorRect(scissorrect);

            DrawSpriteBasedOnPathway(TextureSystem.fightable_gear, RectangleF.FromPosAndSize(idealPosition + offset, TextureSize), TextureSize / 2, (float)(Game.Conductor.Time * -240d) % 360f);
            Graphics.ScissorRect();
        }
    }
}
