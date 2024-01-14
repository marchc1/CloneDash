using CloneDash.Game.Events;
using CloneDash.Systems;
using Raylib_cs;

namespace CloneDash.Game.Entities
{
    // This is a basic implementation; needs to be properly thought out how this should be done...
    public class Boss : MapEntity
    {
        public Boss(DashGame game) : base(game, EntityType.Boss) {
            TextureSize = new(220);
            Interactivity = EntityInteractivity.Noninteractive;
        }

        public double AnimateUpShooter = -200, AnimateDownShooter = -200;
        public double AnimationMax = 0.5d;
        public void OnEntityShown(MapEntity ent) {
            if (ent.Pathway == PathwaySide.Top)
                AnimateUpShooter = Game.Conductor.Time;
            else
                AnimateDownShooter = Game.Conductor.Time;
        }

        public double EnterTime { get; private set; }
        public double ExitTime { get; private set; }

        // Is the boss entering/exiting right now?
        public bool IsEntering { get; private set; } = false;
        public bool IsExiting { get; private set; } = false;

        // How long does the boss take to enter or exit?
        public double EnterMaxTime { get; private set; } = 1.4;
        public double ExitMaxTime { get; private set; } = 0.6;

        // Returns how many seconds have passed since either the enter or exit times
        public double EnterDeltaTime => Game.Conductor.Time - EnterTime;
        public double ExitDeltaTime => Game.Conductor.Time - ExitTime;

        // Entering/exiting value represented as a 0-1 value for animation
        public double EnteringValue => Math.Clamp(Raymath.Remap((float)EnterDeltaTime, 0f, (float)EnterMaxTime, 0f, 1f), 0f, 1f);
        public double ExitingValue => Math.Clamp(Raymath.Remap((float)ExitDeltaTime, 0f, (float)ExitMaxTime, 0f, 1f), 0f, 1f);

        public bool Visible => IsEntering || IsExiting;

        public void Enter(double time) {
            EnterTime = time;
            IsExiting = false;
            IsEntering = true;

            mapEvent = null;
            hit = false;
            masher = false;
        }

        public void Exit(double time) {
            ExitTime = time;
            IsEntering = false;
            IsExiting = true;
        }

        public override bool VisTest(float gamewidth, float gameheight, float xPosition) {
            return Visible;
        }

        MapEvent? mapEvent;
        double hitTime = -200;
        bool hit = false;
        bool masher = false;
        public void WindupForSingleAttack(MapEvent mapEvent) {
            this.mapEvent = mapEvent;
            hitTime = -200;
            hit = false;
            masher = false;

            if (mapEvent.AssociatedEntity != null) {
                mapEvent.AssociatedEntity.HitColor = DashVars.MultiColor;
                mapEvent.AssociatedEntity.OnHitEvent += delegate (MapEntity ent, PathwaySide side) {
                    hitTime = Game.Conductor.Time;
                    hit = true;
                };
            }
        }

        public void WindupForMasherAttack(MapEvent mapEvent) {
            this.mapEvent = mapEvent;
            hitTime = -200;
            hit = false;
            masher = true;

            if (mapEvent.AssociatedEntity != null) {
                mapEvent.AssociatedEntity.HitColor = DashVars.MultiColor;
                mapEvent.AssociatedEntity.OnHitEvent += delegate (MapEntity ent, PathwaySide side) {
                    hitTime = Game.Conductor.Time;
                    hit = true;
                };
            }
        }

        public override void WhenVisible() {

        }

        public override void ChangePosition(ref Vector2F pos) {
            float x = Game.ScreenManager.ScrWidth * 0.84f, y = 0;

            if (mapEvent != null) {
                if (!hit)
                    x = (float)DashMath.Remap(Ease.InBack((float)mapEvent.OffsetToTime), 0, 1, x, Game.ScreenManager.ScrWidth * DashVars.PATHWAY_XDISTANCE);
                else if (masher == false) {
                    x = (float)DashMath.Remap(Ease.OutBack((float)Math.Clamp(Game.Conductor.Time - hitTime, 0, 1)), 0, 1, Game.ScreenManager.ScrWidth * DashVars.PATHWAY_XDISTANCE, x);
                }
                else if (mapEvent.AssociatedEntity != null && mapEvent.AssociatedEntity.Dead == false) {
                    x = (Game.ScreenManager.ScrWidth * DashVars.PATHWAY_XDISTANCE) + 70;
                }
                else {
                    x = (Game.ScreenManager.ScrWidth * DashVars.PATHWAY_XDISTANCE) + 150 + (mapEvent.AssociatedEntity.SinceDeath * Game.ScreenManager.ScrWidth * 5);
                }
            }


            pos.x = x;
            pos.y = y;
        }

        private float Calc(float x) {
            return (float)DashMath.Remap(1 - Ease.OutBack((float)DashMath.Remap(x, 0, AnimationMax, 0, 1, clampInput: true)), 0, 1, 300, 200);
        }
        public override void Draw(Vector2F idealPosition) {
            if (!IsEntering && !IsExiting)
                return;

            var a = IsEntering ? EnteringValue : IsExiting ? 1 : 0;
            if (IsExiting)
                a = (1 - ExitingValue);

            var bossSize = new Vector2F(300);
            Graphics.SetDrawColor(255, 255, 255, (int)(a * 255));

            var idle = new Vector2F((float)Math.Sin(DashVars.Curtime * 5) * 2, (float)Math.Cos(DashVars.Curtime * 7) * 4);
            var idle2 = new Vector2F((float)Math.Sin(DashVars.Curtime * 2) * 2, (float)Math.Cos(DashVars.Curtime * 3) * 4);
            var idle3 = new Vector2F((float)Math.Sin(DashVars.Curtime * 3) * 2, (float)Math.Cos(DashVars.Curtime * 2) * 4);

            Graphics.DrawImage(TextureSystem.boss_main, RectangleF.FromPosAndSize(idealPosition + idle, bossSize), bossSize / 2, 0);

            var bossShooterPos = new Vector2F(230, DashVars.PATHWAY_YDISTANCE * (Game.ScreenManager.ScrHeight / 2));
            var bossShooterSize = new Vector2F(300);
            var bossShooterSizeUp = new Vector2F(Calc((float)(Game.Conductor.Time - AnimateUpShooter)), bossShooterSize.Y);
            var bossShooterSizeDown = new Vector2F(Calc((float)(Game.Conductor.Time - AnimateDownShooter)), bossShooterSize.Y);

            DrawSpriteBasedOnPathway(TextureSystem.boss_shooter, RectangleF.FromPosAndSize(new Vector2F(idealPosition.X + bossShooterPos.X, idealPosition.Y - bossShooterPos.Y) + idle2, bossShooterSizeUp), bossShooterSizeUp / 2, 0, pathway: PathwaySide.Top);
            DrawSpriteBasedOnPathway(TextureSystem.boss_shooter, RectangleF.FromPosAndSize(new Vector2F(idealPosition.X + bossShooterPos.X, idealPosition.Y + bossShooterPos.Y) + idle3, bossShooterSizeDown), bossShooterSizeDown / 2, 0, pathway: PathwaySide.Bottom);
        }
    }
}
