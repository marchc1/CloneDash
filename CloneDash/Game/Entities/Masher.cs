using CloneDash.Systems;

namespace CloneDash.Game.Entities
{
    public class Masher : MapEntity
    {
        public Masher(DashGame game) : base(game, EntityType.Masher) {
            TextureSize = new(350);
            Warns = true;
            Interactivity = EntityInteractivity.Hit;
            DoesDamagePlayer = true;
        }

        public bool StartedHitting { get; private set; } = false;
        public int MaxHits => Math.Clamp((int)Math.Floor(this.Length * DashVars.MASHER_MAX_HITS_PER_SECOND), 1, int.MaxValue);


        private void CheckIfComplete() {
            if ((Hits >= MaxHits || Game.Conductor.Time > (HitTime + Length)) && !Dead) {
                Complete();
            }
        }

        private void Complete() {
            Game.GameplayManager.SpawnTextEffect($"PERFECT {Hits}/{MaxHits}", new(Game.TopPathway.Position.X, 0), DashVars.MultiColor);
            Kill();
            ForceDraw = false;
            Game.ExitMashState();
        }

        public override void WhenVisible() {
            CheckIfComplete();
        }

        protected override void OnHit(PathwaySide side) {
            if (MaxHits == 1) {
                Hits = 1;
                Complete();
                return;
            }

            if (Dead)
                return;

            if (StartedHitting == false) {
                Game.EnterMashState(this);
                StartedHitting = true;

                ForceDraw = true;
                OverrideDesiredPosForEnemy = Game.ScreenManager.ScrWidth * (DashVars.PATHWAY_XDISTANCE + 0.023f);
            }

            AudioSystem.PlaySound($"{Filesystem.Audio}punch.wav", 0.24f, pitch: 1 + ((float)Hits / 75f));

            CheckIfComplete();
        }

        public float OverrideDesiredPosForEnemy = 0;

        public override void Draw(Vector2F idealPosition) {
            if (Dead)
                return;

            var pos = new Vector2F(OverrideDesiredPosForEnemy == 0 ? idealPosition.X : OverrideDesiredPosForEnemy, 0);
            Graphics.DrawImage(TextureSystem.fightable_bag, RectangleF.FromPosAndSize(pos, TextureSize), TextureSize / 2, 0, hsvTransform: new(37, 1, 1));
            var text = $"{Hits} {(Hits == 1 ? "HIT" : "HITS")}";

            Game.DrawWarning(pos - new Vector2F(0, Game.ScreenManager.ScrHeight * 0.17f));
            Graphics.SetDrawColor(255, 255, 255);
            if (StartedHitting) {
                Graphics.DrawText(pos - new Vector2F(0, Game.ScreenManager.ScrHeight * 0.2f), text, "Arial", 36, FontAlignment.Center, FontAlignment.Center);
            }
        }
    }
}
