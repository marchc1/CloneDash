using Nucleus.Engine;
using Nucleus.Core;
using CloneDash.Game.Input;

using System.Numerics;
using Nucleus.Types;
using CloneDash.Game.Entities;
using Nucleus.UI;
using Nucleus;

namespace CloneDash.Game
{
    public partial class CD_GameLevel : Level
    {
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
        /// <summary>
        /// Current fever bar.<br></br>
        /// Default: 0
        /// </summary>
        public float Fever { get; private set; } = 0;

        /// <summary>
        /// How much fever needs to be obtained until entering fever state<br></br>
        /// Default: 120
        /// </summary>
        public float MaxFever { get; set; } = 120;

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
        /// Should the player exit fever?
        /// </summary>
        private bool ShouldExitFever => (Conductor.Time - WhenDidFeverStart) >= FeverTime;
        /// <summary>
        /// How much fever time is left?
        /// </summary>
        private double FeverTimeLeft => FeverTime - (Conductor.Time - WhenDidFeverStart);
        /// <summary>
        /// Returns the fever time left as a value of 0-1, where 0 is the end and 1 is the start. Good for animation.
        /// </summary>
        private double FeverRatio => 1f - ((Conductor.Time - WhenDidFeverStart) / FeverTime);
        /// <summary>
        /// Current score of the player.
        /// </summary>
        public int Score { get; private set; } = 0;
        /// <summary>
        /// Which entity is being held on the top pathway
        /// </summary>
        public CD_BaseMEntity? HoldingTopPathwaySustain { get; private set; } = null;
        /// <summary>
        /// Which entity is being held on the bottom pathway
        /// </summary>
        public CD_BaseMEntity? HoldingBottomPathwaySustain { get; private set; } = null;
        /// <summary>
        /// Is the player in the air right now?
        /// </summary>
        public bool InAir => Conductor.Time - __whenjump < __jumpmax;
        /// <summary>
        /// Can the player jump right now?
        /// </summary>
        public bool CanJump => !InAir;

        private double __jumpmax = 0.5d;
        private double __whenjump = -2000000000000d;

        public void Heal(float health) {
            Health = Math.Clamp(Health + health, 0, MaxHealth);
        }

        /// <summary>
        /// Damage the player.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="damage"></param>
        public void Damage(CD_BaseMEntity? entity, float damage) {
            Health -= damage;
            ResetCombo();
        }
        /// <summary>
        /// Adds to the players fever value, and automatically enters fever when the player has maxed out the fever bar.
        /// </summary>
        /// <param name="fever"></param>
        public void AddFever(float fever) {
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
            WhenDidFeverStart = Conductor.Time;
            Sounds.PlaySound("fever.wav", true, 0.8f, 1.1f);
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
        /// Adds 1 to the players combo.
        /// </summary>
        public void AddCombo() {
            Combo++;
            __lastCombo = Conductor.Time;
        }

        /// <summary>
        /// Resets the players combo.
        /// </summary>
        public void ResetCombo() => Combo = 0;
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
        /// Checks if the player is currently sustaining a note on the given pathway.
        /// </summary>
        /// <param name="side"></param>
        /// <returns></returns>
        public bool IsSustaining(PathwaySide side) => side == PathwaySide.Top ? HoldingTopPathwaySustain != null : HoldingBottomPathwaySustain != null;
        public bool IsSustaining() => IsSustaining(PathwaySide.Top) || IsSustaining(PathwaySide.Bottom);
        public void SetSustain(PathwaySide side, CD_BaseMEntity entity) {
            var wasSustaining = IsSustaining(PathwaySide.Top) || IsSustaining(PathwaySide.Bottom);

            if (side == PathwaySide.Top)
                HoldingTopPathwaySustain = entity;
            else
                HoldingBottomPathwaySustain = entity;

            if (wasSustaining && !IsSustaining(PathwaySide.Top) && !IsSustaining(PathwaySide.Bottom))
                Player.PlayAnimation("Walk", loop: true);
        }
        /// <summary>
        /// Returns if the jump was successful. Mostly returns this for the sake of animation.
        /// </summary>
        /// <param name="force"></param>
        /// <returns></returns>
        /// 

        public delegate void AttackEvent(CD_GameLevel game, PathwaySide side);
        public event AttackEvent? OnAirAttack;
        public event AttackEvent? OnGroundAttack;

        public bool AttackAir(CD_BaseMEntity entity, bool force) {
            if (CanJump || force) {
                __whenjump = Conductor.Time;
                OnAirAttack?.Invoke(this, PathwaySide.Top);

                if (entity is SustainBeam)
                    Player.PlayAnimation("Holding", loop: true);
                else if (IsSustaining()) {
                    HologramPlayer.Visible = true;
                    HologramPlayer.PlayAnimation(GetRandomHitAnim(entity, PathwaySide.Top));
                }
                else {
                    Player.PlayAnimation(GetRandomHitAnim(entity, PathwaySide.Top), fallback: "Walk");
                }

                return true;
            }
            return false;
        }

		int lastAnim = 1;
		public string GetRandomHitAnim(CD_BaseMEntity? entity, PathwaySide? side) {
			if (!IValidatable.IsValid(entity)) {
				return side == PathwaySide.Top ? "Jump" : "Punch";
			}
			lastAnim += 1;
			if (lastAnim > 3)
				lastAnim = 1;
			return $"Hit{lastAnim}";
		}

        public void AttackGround(CD_BaseMEntity entity) {
            __whenjump = -2000000000000d;
            OnGroundAttack?.Invoke(this, PathwaySide.Bottom);

            if (entity is SustainBeam)
                Player.PlayAnimation("Holding", loop: true);
            else if (IsSustaining()) {
                HologramPlayer.Visible = true;
                HologramPlayer.PlayAnimation(GetRandomHitAnim(entity, PathwaySide.Bottom));
            }
            else {
                Player.PlayAnimation(GetRandomHitAnim(entity, PathwaySide.Bottom), fallback: "Walk");
            }
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

        /// <summary>
        /// Current combo of the player (how many successful hits/avoids in a row)
        /// </summary>
        public int Combo { get; private set; } = 0;
        private double __lastCombo = -2000; // Last time a combo occured in game-time

        public double LastCombo => __lastCombo;

        internal CD_Player_UIBar UIBar;
        internal class CD_Player_UIBar : Element
        {
            internal CD_GameLevel Level;
            public CD_Player_UIBar() {

            }
            protected override void Initialize() {
                base.Initialize();
                Dock = Dock.Bottom;
            }
            public override void Paint(float width, float height) {
                Graphics2D.SetDrawColor(255, 60, 42);
                Graphics2D.DrawRectangle(width / 4f, 0, (width / 2f) * (Level.Health / Level.MaxHealth), 24);
                Graphics2D.SetDrawColor(255 / 2, 60 / 2, 42 / 2);
                Graphics2D.DrawRectangleOutline(width / 4f, 0, (width / 2f), 24, 2);
                Graphics2D.SetDrawColor(255, 220, 200);
                Graphics2D.DrawText(width / 2f, 13, $"HP: {Level.Health}/{Level.MaxHealth}", "Noto Sans", 18, Anchor.Center);
                float feverRatio;
                if (Level.InFever)
                    feverRatio = (float)Level.FeverTimeLeft / Level.FeverTime;
                else
                    feverRatio = (float)Level.Fever / Level.MaxFever;

                Graphics2D.SetDrawColor(72, 160, 255);
                Graphics2D.DrawRectangle(width / 4f, 32, (width / 2f) * feverRatio, 24);
                Graphics2D.SetDrawColor(72 / 2, 160 / 2, 255 / 2);
                Graphics2D.DrawRectangleOutline(width / 4f, 32, (width / 2f), 24, 2);
                Graphics2D.SetDrawColor(200, 220, 255);
                Graphics2D.DrawText(width / 2f, 32 + 13, Level.InFever ? $"FEVER! {Math.Round(Level.FeverTimeLeft, 2)}s remaining" : $"FEVER: {Math.Round((Level.Fever / Level.MaxFever) * 100)}%", "Noto Sans", 18, Anchor.Center);
            }
        }

        internal CD_Player_Scorebar Scorebar;
        internal class CD_Player_Scorebar : Element
        {
            internal CD_GameLevel Level;
            public CD_Player_Scorebar() {

            }
            protected override void Initialize() {
                base.Initialize();
                Dock = Dock.Top;
            }
            public override void Paint(float width, float height) {
                Graphics2D.SetDrawColor(255, 255, 255, 255);
                //if (Level.AutoPlayer.Enabled)
                    //Graphics2D.DrawText(width / 2f, 32 + 48, $"AUTO", "Noto Sans", 32, Anchor.Center);
                
                Graphics2D.DrawText(width * 0.4f, 32 + 24, $"{Level.Combo}", "Noto Sans", (int)NMath.Remap(Level.Conductor.Time - Level.LastCombo, 0.2f, 0, 32, 40, clampOutput: true), Anchor.Center);
                Graphics2D.DrawText(width * 0.4f, 32 + 56, "COMBO", "Noto Sans", 24, Anchor.Center);

                Graphics2D.DrawText(width * 0.6f, 32 + 24, $"{Level.Score}", "Noto Sans", 32, Anchor.Center);
                Graphics2D.DrawText(width * 0.6f, 32 + 56, "SCORE", "Noto Sans", 24, Anchor.Center);
            }
        }
    }
}
