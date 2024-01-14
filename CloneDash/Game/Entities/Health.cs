using CloneDash.Systems;
using Raylib_cs;

namespace CloneDash.Game.Entities
{
    public class Health : MapEntity
    {
        public Health(DashGame game) : base(game, EntityType.Heart) {
            TextureSize = new(150);
            Interactivity = EntityInteractivity.SamePath;
            DeathAddsToCombo = false;
            DoesDamagePlayer = true;
        }

        // doesnt work?
        protected override void OnHit(PathwaySide side) {
            Kill();
            RewardPlayer(true);
        }
        protected override void OnReward() {
            Game.PlayerController.Heal(this.ScoreGiven);
            Game.GameplayManager.SpawnTextEffect($"+{this.ScoreGiven} HP", Game.GetPathway(Pathway).Position, color: new Color(255, 59, 25, 255));
        }

        public override void Draw(Vector2F idealPosition) {
            if (this.Dead)
                return;

            Graphics.DrawImage(TextureSystem.pickup_heart, RectangleF.FromPosAndSize(idealPosition, TextureSize), TextureSize / 2, 0, hsvTransform: new(0, 2, 1));
        }
    }
}
