using CloneDash.Systems;
using Nucleus;
using Nucleus.Core;
using Nucleus.Engine;
using Nucleus.Types;
using Raylib_cs;

namespace CloneDash.Game.Entities
{
    public class SustainBeam : CD_BaseEnemy
    {
        public SustainBeam() : base(EntityType.SustainBeam) {
            Interactivity = EntityInteractivity.Hit;
            DoesDamagePlayer = true;
        }
        public override void Initialize() {
            base.Initialize();
            SetModel("sustainbeam.glb", "Idle", true);
        }

        public bool HeldState { get; private set; } = false;
        public bool StopAcceptingInput { get; private set; } = false;

        public Pathway PathwayCheck;

        protected override void OnHit(PathwaySide attackedPath) {
            if (HeldState == true)
                return;
            if (StopAcceptingInput == true)
                return;

            PathwayCheck = Level.As<CD_GameLevel>().GetPathway(attackedPath);
            HeldState = true;
            ForceDraw = true;
            Level.As<CD_GameLevel>().SetSustain(Pathway, this);
            PlayAnimation("Open", loop: false);
            Level.As<CD_GameLevel>().AddCombo();
            Level.As<CD_GameLevel>().AddFever(FeverGiven);
        }

        protected override void OnMiss() {
            if (HeldState == false) {
                Level.As<CD_GameLevel>().SetSustain(Pathway, null);
                PunishPlayer();
            }
        }

        public override bool VisTest(float gamewidth, float gameheight, float xPosition) {
            return xPosition >= -96 && xPosition <= gamewidth; // does this even need to be changed right now
        }

        public override void Think(FrameState frameState) {
            if (HeldState) {
                var endPos = DistanceToEnd;

                // check if sustain complete

                var sustainComplete = PathwayCheck.IsPressed && endPos <= 0;
                var sustainEarlyButStillSuccess = !PathwayCheck.IsPressed && NMath.InRange(endPos, -0.05f, 0.05f);

                if (sustainComplete || sustainEarlyButStillSuccess) {
                    HeldState = false;
                    StopAcceptingInput = true;
                    ShouldDraw = false;
                    RewardPlayer();
                    Level.As<CD_GameLevel>().AddCombo();
                    Level.As<CD_GameLevel>().AddFever(FeverGiven);
                    Level.As<CD_GameLevel>().SetSustain(Pathway, null);
                    AudioSystem.PlaySound(Filesystem.Resolve("punch.wav", "audio"), 0.24f);
                }
                // check if pathway being held
                else if (!PathwayCheck.IsPressed) {
                    HeldState = false;
                    StopAcceptingInput = true;
                    ShouldDraw = false;
                    Level.As<CD_GameLevel>().SetSustain(Pathway, null);
                    PunishPlayer();
                }
            }
        }

        public float StartPosition { get; private set; }

        public override void Render(FrameState frameState) {
            var game = Level.As<CD_GameLevel>();

            if (!HeldState)
                StartPosition = (float)XPos;
            else if (!StopAcceptingInput)
                StartPosition = PathwayCheck.Position.X;

            float startPos = StartPosition, endPos = (float)XPosFromTimeOffset(frameState, (float)Length);
            var YPos = Game.Pathway.ValueDependantOnPathway(Pathway, game.TopPathway.Position.Y, game.BottomPathway.Position.Y);

            //Graphics.SetDrawColor(Game.Pathway.GetColor(Pathway), HeldState ? 230 : 127);
            //Graphics.DrawLine(startPos, YPos, endPos, YPos, 28);
            //Graphics.SetDrawColor(255, 255, 255, 255);

            for (int i = 64 - 1; i >= 0; i -= 4) {
                Raylib.DrawCubeV(new((startPos + endPos) / 2f, YPos, 10), new(endPos - startPos, i, 0.2f), Game.Pathway.GetColor(Pathway, HeldState ? 9 : 5));
            }

            var mS = Model.Model;

            Model.Position = new(startPos, YPos, 0);
            Model.Rotation = new(0, 0, 0);
            Model.Render();

            Model.Position = new(endPos, YPos, 0);
            Model.Rotation = new(0, 0, 180);
            Model.Render();

            //Console.WriteLine($"AnimationFrame {AnimationFrame} CurrentAnimation.HasValue {CurrentAnimation.HasValue}");
            //DrawSpriteBasedOnPathway(TextureSystem.LoadTexture("fightable_beam"), RectangleF.FromPosAndSize(new(startPos, YPos), TextureSize), TextureSize / 2, 0);
            //DrawSpriteBasedOnPathway(TextureSystem.fightable_beam, RectangleF.FromPosAndSize(new(endPos, YPos), TextureSize), TextureSize / 2, 180);
        }
        public override void Build() {
            HSV = new(Pathway == PathwaySide.Top ? 200 : 285, 1, 1);
        }
    }
}