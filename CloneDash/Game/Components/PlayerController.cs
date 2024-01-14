using CloneDash.Game.Entities;
using Raylib_cs;

namespace CloneDash.Game.Components
{
    public class PlayerController : DashGameComponent
    {
        public const string STRING_HP = "HP: {0}";
        public const string STRING_FEVERY = "FEVER! {0}s";
        public const string STRING_FEVERN = "FEVER: {0}/{1}";
        public const string STRING_COMBO = "COMBO";
        public const string STRING_SCORE = "SCORE";
        public const string FONT = "Arial";

        /// <summary>
        /// Current health of the player<br></br>
        /// Default: 250
        /// </summary>
        public float Health { get; private set; }

        /// <summary>
        /// Maximum health the player can have, the player will have this much health on spawn<br></br>
        /// Default: 250
        /// </summary>
        public float MaxHealth { get; set; } = 250;

        /// <summary>
        /// How much health does the player lose every second?<br></br>
        /// Default: 0
        /// </summary>
        public float HealthDrain { get; set; } = 0;

        public void Heal(float health) {
            Health = Math.Clamp(Health + health, 0, MaxHealth);
        }

        /// <summary>
        /// Damage the player.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="damage"></param>
        public void Damage(MapEntity? entity, float damage) {
            Health -= damage;
            ResetCombo();
        }

        /// <summary>
        /// Current fever bar.<br></br>
        /// Default: 0
        /// </summary>
        public float Fever { get; private set; } = 0;

        /// <summary>
        /// How much fever needs to be obtained until entering fever state<br></br>
        /// Default: 100
        /// </summary>
        public float MaxFever { get; set; } = 100;

        /// <summary>
        /// How much fever, in seconds, does a full fever bar provide?<br></br>
        /// Default: 6
        /// </summary>
        public float FeverTime { get; set; } = 6;
        /// <summary>
        /// Is the player currently in fever?
        /// </summary>
        public bool InFever { get; private set; } = false;
        /// <summary>
        /// When did the fever start?
        /// </summary>
        public double WhenDidFeverStart { get; private set; } = -1000000d;
        /// <summary>
        /// Adds to the players fever value, and automatically enters fever when the player has maxed out the fever bar.
        /// </summary>
        /// <param name="fever"></param>
        public void AddFever(int fever) {
            if (InFever)
                return;

            Fever = Math.Clamp(Fever + fever, 0, MaxFever);
            if (Fever >= MaxFever)
                EnterFever();
        }
        /// <summary>
        /// Enters fever.
        /// </summary>
        private void EnterFever() {
            InFever = true;
            WhenDidFeverStart = Game.Conductor.Time;
        }
        /// <summary>
        /// Exits fever.
        /// </summary>
        private void ExitFever() {
            InFever = false;
            Fever = 0;
            WhenDidFeverStart = -1000000d;
        }

        /// <summary>
        /// Should the player exit fever?
        /// </summary>
        private bool ShouldExitFever => (Game.Conductor.Time - WhenDidFeverStart) >= FeverTime;
        /// <summary>
        /// How much fever time is left?
        /// </summary>
        private double FeverTimeLeft => FeverTime - (Game.Conductor.Time - WhenDidFeverStart);
        /// <summary>
        /// Returns the fever time left as a value of 0-1, where 0 is the end and 1 is the start. Good for animation.
        /// </summary>
        private double FeverRatio => 1f - ((Game.Conductor.Time - WhenDidFeverStart) / FeverTime);

        /// <summary>
        /// Current combo of the player (how many successful hits/avoids in a row)
        /// </summary>
        public int Combo { get; private set; } = 0;
        private double __lastCombo = -2000; // Last time a combo occured in game-time

        /// <summary>
        /// Adds 1 to the players combo.
        /// </summary>
        public void AddCombo() {
            Combo++;
            __lastCombo = Game.Conductor.Time;
        }

        /// <summary>
        /// Resets the players combo.
        /// </summary>
        public void ResetCombo() => Combo = 0;

        /// <summary>
        /// Current score of the player.
        /// </summary>
        public int Score { get; private set; } = 0;
        /// <summary>
        /// Adds to the players score.
        /// </summary>
        /// <param name="score"></param>
        public void AddScore(int score) {
            float s = (float)score;
            Score += (int)s;
        }
        /// <summary>
        /// Removes from the players score.
        /// </summary>
        /// <param name="score"></param>
        public void RemoveScore(int score) => Score -= score;

        /// <summary>
        /// Which entity is being held on the top pathway
        /// </summary>
        public MapEntity? HoldingTopPathwaySustain { get; private set; } = null;
        /// <summary>
        /// Which entity is being held on the bottom pathway
        /// </summary>
        public MapEntity? HoldingBottomPathwaySustain { get; private set; } = null;

        /// <summary>
        /// Checks if the player is currently sustaining a note on the given pathway.
        /// </summary>
        /// <param name="side"></param>
        /// <returns></returns>
        public bool IsSustaining(PathwaySide side) => side == PathwaySide.Top ? HoldingTopPathwaySustain != null : HoldingBottomPathwaySustain != null;

        /// <summary>
        /// Is the player in the air right now?
        /// </summary>
        public bool InAir => Game.Conductor.Time - __whenjump < __jumpmax;
        /// <summary>
        /// Can the player jump right now?
        /// </summary>
        public bool CanJump => !InAir;

        private double __jumpmax = 0.5d;
        private double __whenjump = -2000000000000d;

        /// <summary>
        /// Returns if the jump was successful. Mostly returns this for the sake of animation.
        /// </summary>
        /// <param name="force"></param>
        /// <returns></returns>
        /// 

        public delegate void AttackEvent(DashGame game, PathwaySide side);
        public event AttackEvent? OnAirAttack;
        public event AttackEvent? OnGroundAttack;

        public bool AttackAir(bool force) {
            if (CanJump || force) {
                __whenjump = Game.Conductor.Time;
                OnAirAttack?.Invoke(this.Game, PathwaySide.Top);
                return true;
            }
            return false;
        }

        public void AttackGround() {
            __whenjump = -2000000000000d;
            OnGroundAttack?.Invoke(this.Game, PathwaySide.Bottom);
        }

        /// <summary>
        /// Gets the current pathway the player is on. Returns Top if jumping, else bottom.
        /// </summary>
        public PathwaySide Pathway => InAir ? PathwaySide.Top : PathwaySide.Bottom;

        public bool CanHit(PathwaySide pathway) {
            if (IsSustaining(pathway))
                return false;

            if (pathway == PathwaySide.Top && InAir)
                return false;

            return true;
        }

        public override void OnTick() {
            if (ShouldExitFever && InFever)
                ExitFever();

            if (Game.InputState.TopClicked > 0 && CanJump)
                __whenjump = Game.Conductor.Time;
        }

        public PlayerController(DashGame game) : base(game) {
            Health = MaxHealth;
        }

        private static void DrawProgressBar(string text, float w, float h, float barsize, float barheight, float bary, float percentageFilled, Color bar, Color outline) {
            Graphics.SetDrawColor(bar);
            Graphics.DrawRectangle((w / 2) - (barsize / 2), h - bary, barsize * percentageFilled, barheight);
            Graphics.SetDrawColor(outline);
            Graphics.DrawRectangleOutline((w / 2) - (barsize / 2), h - bary, barsize, barheight, 2);
            Graphics.SetDrawColor(255, 255, 255);
            Graphics.DrawText(w / 2, (h - bary) + (barheight / 2), text, "Arial", 18, FontAlignment.Center, FontAlignment.Center);
        }

        public override void OnDrawGameSpace() {
            float width = Game.ScreenManager.ScrWidth, height = Game.ScreenManager.ScrHeight;

            var characterSize = new Vector2F(50, 90);
            var characterPosition = new Vector2F(width * 0.12f, Game.GetPathway(Pathway).Position.Y) - (characterSize / 2);

            Graphics.SetDrawColor(255, 255, 255);
            Graphics.DrawRectangle(characterPosition, characterSize);
        }

        public override void OnDrawScreenSpace(float width, float height) {
            float w = Game.ScreenManager.ScrWidth, h = Game.ScreenManager.ScrHeight;
            DrawProgressBar(string.Format(STRING_HP, this.Health), width, height, width / 2f, 26, 96, this.Health / this.MaxHealth, new Color(255, 90, 79, 255), new Color(85, 15, 25, 255));
            DrawProgressBar(InFever ? string.Format(STRING_FEVERY, Math.Round(FeverTimeLeft, 2)) : string.Format(STRING_FEVERN, this.Fever, this.MaxFever), width, height, width / 2f, 26, 64, (float)((this.Fever / this.MaxFever) * (InFever ? FeverRatio : 1)), new(129, 140, 255, 255), new(15, 25, 85, 255));

            float combo_score_offset = Game.ScreenManager.ScrWidth * 0.1f;

            var combosize = Ease.OutQuad(Math.Clamp(Raymath.Remap((float)(Game.Conductor.Time - __lastCombo), 0, 0.05f, 0, 1), 0, 1));
            combosize = Raymath.Remap(combosize, 0, 1, 48, 36);

            Graphics.DrawText((w / 2) - combo_score_offset, h * 0.07f, Combo.ToString(), FONT, (int)combosize, FontAlignment.Center, FontAlignment.Bottom);
            Graphics.DrawText((w / 2) - combo_score_offset, h * 0.07f, STRING_COMBO, FONT, 24, FontAlignment.Center, FontAlignment.Top);

            Graphics.DrawText((w / 2) + combo_score_offset, h * 0.07f, Score.ToString(), FONT, 36, FontAlignment.Center, FontAlignment.Bottom);
            Graphics.DrawText((w / 2) + combo_score_offset, h * 0.07f, STRING_SCORE, FONT, 24, FontAlignment.Center, FontAlignment.Top);
        }
    }
}
