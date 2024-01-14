using CloneDash.Systems;
using Raylib_cs;

namespace CloneDash.Game.Entities
{
    public class Hammer : MapEntity
    {
        public Hammer(DashGame game) : base(game, EntityType.Hammer) {
            TextureSize = new(1024, 1024);
            Interactivity = EntityInteractivity.Hit;
            DoesDamagePlayer = true;
        }

        protected override void OnHit(PathwaySide side) {
            Kill();
        }

        protected override void OnMiss() {
            PunishPlayer();
            if(Game.PlayerController.Pathway == this.Pathway) {
                DamagePlayer();
            }
        }

        public override void Draw(Vector2F idealPosition) {
            var pathwayY = CloneDash.Game.Components.Pathway.ValueDependantOnPathway(Pathway, Game.TopPathway.Position.Y, Game.BottomPathway.Position.Y); // Pathway == PathwaySide.Top ? DashVars.UpPathway.Position.Y : DashVars.DownPathway.Position.Y;
            var timeToHit = (float)(HitTime - ShowTime);

            // to do: make this not look horrible
            if (EnterDirection == EntityEnterDirection.TopDown) {
                DrawSpriteBasedOnPathway(
                    TextureSystem.fightable_hammer,
                    RectangleF.FromPosAndSize(new(!Dead ? idealPosition.X : (DashVars.PATHWAY_XDISTANCE * Game.ScreenManager.ScrWidth) + (Raymath.Remap(SinceDeath, 0, timeToHit, 0, 1) * -100), //DashVars.PATHWAY_XDISTANCE * DashVars.GAMEWIDTH) + 64,
                    (pathwayY - TextureSize.Y) + 64), TextureSize),
                    new(TextureSize.X / 2, 0),
                    !Dead ? Raymath.Remap((float)Game.Conductor.Time, (float)ShowTime, (float)HitTime, -100, 0) : (Raymath.Remap(SinceDeath, 0, timeToHit, 0, 1) * -100)
                );
            }
            else {

            }
        }
    }
}
