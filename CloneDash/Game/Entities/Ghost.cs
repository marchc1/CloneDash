using CloneDash.Systems;

namespace CloneDash.Game.Entities
{
    public class Ghost : MapEntity
    {
        public Ghost(DashGame game) : base(game, EntityType.Ghost) {
            TextureSize = new(128, 128);
            Interactivity = EntityInteractivity.Hit;
            DoesDamagePlayer = false;
            DoesPunishPlayer = false;
        }
        protected override void OnHit(PathwaySide side) {
            Kill();
        }
        public override void Draw(Vector2F idealPosition) {
            if (this.Dead)
                return;

            int alpha = (int)Math.Clamp(DashMath.Remap(DistanceToHit, 0.2, 1, 0, 255), 0, 255);

            Graphics.SetDrawColor(255, 255, 255, alpha);
            Graphics.DrawImage(TextureSystem.fightable_ghost, RectangleF.FromPosAndSize(idealPosition, TextureSize), TextureSize / 2, 0, hsvTransform: new(198, 0.78f, 1));
        }
    }
}
