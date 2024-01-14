using CloneDash.Systems;

namespace CloneDash.Game.Entities
{
    public class SingleHitEnemy : MapEntity
    {
        public SingleHitEnemy(DashGame game) : base(game, EntityType.Single) {
            TextureSize = new(148);
            Interactivity = EntityInteractivity.Hit;
            DoesDamagePlayer = true;
            Texture = TextureSystem.fightable_ball;
        }

        bool applyvel = false;
        float acceleration = 0.000f;
        float velocity = 0;
        float position = 0;

        protected override void OnHit(PathwaySide side) {
            Kill();
            applyvel = true;
            acceleration = 0.006f;
            velocity = Pathway == PathwaySide.Top ? -1.48f : 0.82f;
        }

        protected override void OnMiss() {
            PunishPlayer();
            if (Game.PlayerController.Pathway == this.Pathway) {
                DamagePlayer();
            }
        }

        public override void ChangePosition(ref Vector2F pos) {
            var scrw = Game.ScreenManager.ScrWidth;
            if (RelatedToBoss) {

            }
            else {
                var yOff = Ease.InCubic(Math.Clamp((float)DashMath.Remap(pos.X, scrw / 2, scrw, 0f, 1), 0f, 1f));
                switch (EnterDirection) {
                    case EntityEnterDirection.RightSide:
                        // no modification occurs
                        break;
                    case EntityEnterDirection.TopDown:
                        pos.y = (float)DashMath.Remap(yOff, 0, 1, pos.Y, (-Game.ScreenManager.ScrHeight / 2) - TextureSize.Y);
                        break;
                    case EntityEnterDirection.BottomUp:
                        pos.y = (float)DashMath.Remap(yOff, 0, 1, pos.Y, (Game.ScreenManager.ScrHeight / 2) + TextureSize.Y);
                        break;
                }
            }

            if (applyvel) {
                position += velocity;
                velocity += acceleration;

                pos.y += position;
            }

        }

        public override void Draw(Vector2F idealPosition) {
            DrawSpriteBasedOnPathway(Texture, RectangleF.FromPosAndSize(idealPosition, TextureSize), TextureSize / 2);
        }

        public override void Build() {
            Texture = RelatedToBoss ? TextureSystem.boss_projectile : TextureSystem.fightable_ball;
        }
    }
}
