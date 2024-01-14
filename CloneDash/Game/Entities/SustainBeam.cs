using CloneDash.Game.Components;
using CloneDash.Systems;

namespace CloneDash.Game.Entities
{
    public class SustainBeam : MapEntity
    {
        public SustainBeam(DashGame game) : base(game, EntityType.SustainBeam) {
            TextureSize = new(96, 96);
            Interactivity = EntityInteractivity.Hit;
            DoesDamagePlayer = true;
        }

        public bool HeldState { get; private set; } = false;
        public bool StopAcceptingInput { get; private set; } = false;

        public Pathway PathwayCheck;

        protected override void OnHit(PathwaySide attackedPath) {
            if (HeldState == true)
                return;
            if (StopAcceptingInput == true)
                return;

            PathwayCheck = Game.GetPathway(attackedPath); ;
            HeldState = true;
            ForceDraw = true;
            Game.PlayerController.AddCombo();
        }

        protected override void OnMiss() {
            if (HeldState == false)
                PunishPlayer();
        }

        public override bool VisTest(float gamewidth, float gameheight, float xPosition) {
            return xPosition >= -TextureSize.X && xPosition <= gamewidth; // does this even need to be changed right now
        }

        public override void WhenFrame() {
            if (HeldState) {
                var endPos = DistanceToEnd;

                // check if sustain complete

                var sustainComplete = PathwayCheck.IsPressed && endPos <= 0;
                var sustainEarlyButStillSuccess = !PathwayCheck.IsPressed && DashMath.InRange(endPos, -0.05f, 0.05f);

                if (sustainComplete || sustainEarlyButStillSuccess) {
                    HeldState = false;
                    StopAcceptingInput = true;
                    ShouldDraw = false;
                    RewardPlayer();
                    Game.PlayerController.AddCombo();
                    AudioSystem.PlaySound($"{Filesystem.Audio}punch.wav", 0.24f);
                }
                // check if pathway being held
                else if (!PathwayCheck.IsPressed) {
                    HeldState = false;
                    StopAcceptingInput = true;
                    ShouldDraw = false;
                    PunishPlayer();
                }
            }
        }

        public float StartPosition { get; private set; }

        public override void Draw(Vector2F idealPosition) {
            if (!HeldState)
                StartPosition = (float)XPos;
            else if (!StopAcceptingInput)
                StartPosition = PathwayCheck.Position.X;

            float startPos = StartPosition, endPos = (float)XPosFromTimeOffset((float)Length);
            var YPos = CloneDash.Game.Components.Pathway.ValueDependantOnPathway(Pathway, Game.TopPathway.Position.Y, Game.BottomPathway.Position.Y);

            Graphics.SetDrawColor(CloneDash.Game.Components.Pathway.GetColor(Pathway), HeldState ? 230 : 127);
            Graphics.DrawLine(startPos, YPos, endPos, YPos, 28);
            Graphics.SetDrawColor(255, 255, 255, 255);

            DrawSpriteBasedOnPathway(TextureSystem.fightable_beam, RectangleF.FromPosAndSize(new(startPos, YPos), TextureSize), TextureSize / 2, 0);
            DrawSpriteBasedOnPathway(TextureSystem.fightable_beam, RectangleF.FromPosAndSize(new(endPos, YPos), TextureSize), TextureSize / 2, 180);
        }
    }
}
