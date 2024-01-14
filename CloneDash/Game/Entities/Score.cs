using CloneDash.Systems;
using Raylib_cs;

namespace CloneDash.Game.Entities
{
    public class Score : MapEntity
    {
        public Score(DashGame game) : base(game, EntityType.Score) {
            TextureSize = new(150);
            Interactivity = EntityInteractivity.SamePath;
            DeathAddsToCombo = false;
            DoesDamagePlayer = true; //????
        }

        protected override void OnHit(PathwaySide side) {
            RewardPlayer();
            Kill();
        }
        protected override void OnReward() {
            base.OnReward();
            Game.GameplayManager.SpawnTextEffect("+" + this.ScoreGiven, Game.GetPathway(Pathway).Position, color: new Color(150, 200, 255, 255));
        }

        public override void Draw(Vector2F idealPosition) {
            if (this.Dead)
                return;

            Graphics.DrawImage(TextureSystem.pickup_note, RectangleF.FromPosAndSize(idealPosition, TextureSize), TextureSize / 2, 0, hsvTransform: new(231, 1.1f, 1));
        }
    }
}
