using Nucleus.Engine;
using Nucleus.Core;
using CloneDash.Game.Input;

using System.Numerics;
using Nucleus.Types;
using CloneDash.Game.Entities;
using Nucleus.UI;
using Nucleus;
using FMOD;
using Raylib_cs;

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

		public double AirTime => (Conductor.Time - __whenjump);
		public double TimeToAnimationEnds => __jumpAnimationStops - (Conductor.Time - __whenjump);

		public double Hologram_AirTime => (Conductor.Time - __whenHjump);
		public double Hologram_TimeToAnimationEnds => __jumpAnimationHStops - (Conductor.Time - __whenHjump);


		/// <summary>
		/// Can the player jump right now?
		/// </summary>
		public bool CanJump => !InAir;

		private double __jumpmax = 0.5d;
		private bool __firstJump = false;
		private double __jumpAnimationStops = 0.5d;
		private double __jumpAnimationHStops = 0.5d;
		private double __whenjump = -2000000000000d;
		private double __whenHjump = -2000000000000d;

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
		public double LastFeverIncreaseTime { get; private set; } = -2000;
		/// <summary>
		/// Adds to the players fever value, and automatically enters fever when the player has maxed out the fever bar.
		/// </summary>
		/// <param name="fever"></param>
		public void AddFever(float fever) {
			if (InFever)
				return;

			Fever = Math.Clamp(Fever + fever, 0, MaxFever);
			LastFeverIncreaseTime = Conductor.Time;
			if (Fever >= MaxFever)
				EnterFever();
		}
		/// <summary>
		/// Enters fever.
		/// </summary>
		private void EnterFever() {
			InFever = true;
			WhenDidFeverStart = Conductor.Time;
			Scene.PlayFever();
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
			var wasSustainingTop = IsSustaining(PathwaySide.Top);
			var wasSustainingBottom = IsSustaining(PathwaySide.Bottom);
			var wasSustaining = wasSustainingTop || wasSustainingBottom;

			if (side == PathwaySide.Top)
				HoldingTopPathwaySustain = entity;
			else
				HoldingBottomPathwaySustain = entity;

			var isSustainingTop = IsSustaining(PathwaySide.Top);
			var isSustainingBottom = IsSustaining(PathwaySide.Bottom);
			var isSustaining = isSustainingTop || isSustainingBottom;

			if (!wasSustaining && isSustaining) playeranim_startsustain = true;

			if (!wasSustainingTop && isSustainingTop) playeranim_startsustain_top = true;
			if (!wasSustainingBottom && isSustainingBottom) playeranim_startsustain_bottom = true;

			if (wasSustaining && !isSustaining)
				playeranim_endsustain = true;

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

		public float CharacterYRatio {
			get {
				return (float)(
					(__firstJump ? Math.Clamp(NMath.Ease.OutExpo(AirTime * 10), 0, 1) : 1) - (1 - Math.Clamp(NMath.Ease.OutExpo(TimeToAnimationEnds * 10), 0, 1))
				);
			}
		}
		public float HologramCharacterYRatio {
			get {
				return ((float)(
					(__firstJump ? Math.Clamp(NMath.Ease.OutExpo(Hologram_AirTime * 10), 0, 1) : 1) - (1 - Math.Clamp(NMath.Ease.OutExpo(Hologram_TimeToAnimationEnds * 10), 0, 1))
				));
			}
		}

		private bool playeranim_miss = false;
		private bool playeranim_jump = false;
		private bool playeranim_attackair = false;
		private bool playeranim_attackground = false;
		private bool playeranim_attackdouble = false;

		private bool playeranim_perfect = false;

		private bool playeranim_startsustain = false;
		private bool playeranim_startsustain_top = false;
		private bool playeranim_startsustain_bottom = false;
		private bool playeranim_insustain = false;
		private bool playeranim_endsustain = false;

		private void resetPlayerAnimState() {
			playeranim_miss = false;
			playeranim_jump = false;

			playeranim_attackair = false;
			playeranim_attackground = false;
			playeranim_attackdouble = false;

			playeranim_perfect = false;

			playeranim_startsustain = false;
			playeranim_startsustain_top = false;
			playeranim_startsustain_bottom = false;
			playeranim_endsustain = false;
			playeranim_insustain = IsSustaining();
		}
		private double lastHologramHitTime = -20000;
		private void logTests(string testStr) {
			//Logs.Debug($"PlayerAnimationState: {testStr}");
		}
		private void DrawPlayerState() {
			string[] lines = [
				$"__whenjump:                     {__whenjump}",
				$"__whenHjump:                    {__whenHjump}",
				$"playeranim_miss:                {playeranim_miss}",
				$"playeranim_jump:                {playeranim_jump}",
				$"playeranim_attackair:           {playeranim_attackair}",
				$"playeranim_attackground:        {playeranim_attackground}",
				$"playeranim_attackdouble:        {playeranim_attackdouble}",
				$"playeranim_perfect:             {playeranim_perfect}",
				$"playeranim_startsustain:        {playeranim_startsustain}",
				$"playeranim_startsustain_top:    {playeranim_startsustain_top}",
				$"playeranim_startsustain_bottom: {playeranim_startsustain_bottom}",
				$"playeranim_insustain:           {playeranim_insustain}",
				$"playeranim_endsustain:          {playeranim_endsustain}"
			];

			int y = 0;
			Graphics2D.SetDrawColor(255, 255, 255);
			foreach (var line in lines) {
				Graphics2D.DrawText(8, 8 + y, line, "Consolas", 14);
				y += 14;
			}
		}
		private void determinePlayerAnimationState() {
			ModelEntity playerTarget;
			bool suppress_hologram = false;
			// Call end sustain animation. But only if we're ending a sustain and not starting a new one immediately
			if (playeranim_endsustain && !playeranim_startsustain) {
				PlayerAnim_ExitSustain();
				logTests("Exiting sustain.");
			}
			else if (playeranim_endsustain && playeranim_startsustain) {
				// Suppress any hologram animations.
				logTests("Suppressing further hologram animations.");
				suppress_hologram = true;
			}

			if (playeranim_attackdouble) {
				PlayerAnim_ForceAttackDouble(Player);
				PlayerAnim_EnqueueRun(Player);

				resetPlayerAnimState();
				logTests("Double attack");
				__whenjump = -2000000000;
				__whenHjump = -2000000000;
				return;
			}

			if (playeranim_startsustain) {
				PlayerAnim_EnterSustain();

				logTests("Sustain started");

				if (playeranim_startsustain_bottom && playeranim_startsustain_top) {
					resetPlayerAnimState();
					logTests("Not allowing further animation; both sustains pressed at once!");
					return;
				}

				if (!suppress_hologram && playeranim_attackair && playeranim_startsustain_bottom && !playeranim_startsustain_top) {
					// Hologram player attacks the air, while the player starts the bottom sustain.
					PlayerAnim_ForceAttackAir(HologramPlayer, playeranim_perfect);
				}

				if (!suppress_hologram && playeranim_attackground && playeranim_startsustain_top && !playeranim_startsustain_bottom) {
					// Hologram player attacks the ground, while the player starts the top sustain.
					PlayerAnim_ForceAttackGround(HologramPlayer, playeranim_perfect);
				}

				resetPlayerAnimState();
				return;
			}

			if (!suppress_hologram && playeranim_insustain) { // We use the same things for playeranim_attack etc, but redirect it to the hologram player
				playerTarget = HologramPlayer;

				if (playeranim_startsustain_top || playeranim_startsustain_bottom) {
					resetPlayerAnimState();
					return;
				}

				if (playeranim_attackair || playeranim_attackground) {
					lastHologramHitTime = Conductor.Time;
					Logs.Info("Setting last hologram hit time");
				}

				if (playeranim_attackair) {
					PlayerAnim_ForceAttackAir(HologramPlayer, playeranim_perfect);
					__whenHjump = Conductor.Time;
					//EngineCore.Interrupt(() => DrawPlayerState(), false);
				}
				else if (playeranim_attackground) {
					PlayerAnim_ForceAttackGround(HologramPlayer, playeranim_perfect);
					//EngineCore.Interrupt(() => DrawPlayerState(), false);
					__whenHjump = -2000000000000d;
				}
			}
			else {
				playerTarget = Player;
				if (playeranim_attackair) {
					PlayerAnim_ForceAttackAir(Player, playeranim_perfect);
					__whenjump = Conductor.Time;
					PlayerAnim_EnqueueRun(Player);
				}
				else if (playeranim_attackground) {
					PlayerAnim_ForceAttackGround(Player, playeranim_perfect);
					__whenjump = -2000000000000d;
					PlayerAnim_EnqueueRun(Player);
				}
				else if (playeranim_jump) {
					PlayerAnim_ForceJump(Player);
					__whenjump = Conductor.Time;
				}
				else if (playeranim_miss) {
					__whenjump = -2000000000000d;
					PlayerAnim_ForceMiss(Player);
				}
			}

			/*FrameDebuggingStrings.Add($"PlayerAnimState:");

			FrameDebuggingStrings.Add($"    playeranim_miss                 : {playeranim_miss}");
			FrameDebuggingStrings.Add($"    playeranim_jump                 : {playeranim_jump}");
			FrameDebuggingStrings.Add($"    playeranim_attackair            : {playeranim_attackair}");
			FrameDebuggingStrings.Add($"    playeranim_attackground         : {playeranim_attackground}");
			FrameDebuggingStrings.Add($"    playeranim_attackdouble         : {playeranim_attackdouble}");
			FrameDebuggingStrings.Add($"    playeranim_perfect              : {playeranim_perfect}");
			FrameDebuggingStrings.Add($"    playeranim_startsustain         : {playeranim_startsustain}");
			FrameDebuggingStrings.Add($"    playeranim_startsustain_top     : {playeranim_startsustain_top}");
			FrameDebuggingStrings.Add($"    playeranim_startsustain_bottom  : {playeranim_startsustain_bottom}");
			FrameDebuggingStrings.Add($"    playeranim_insustain            : {playeranim_insustain}");
			FrameDebuggingStrings.Add($"    playeranim_endsustain           : {playeranim_endsustain}");*/

			resetPlayerAnimState();
		}

		public bool AttackAir(PollResult result) {
			if (InMashState) {
				playeranim_attackair = true;
				playeranim_perfect = true;

				OnAirAttack?.Invoke(this, PathwaySide.Top);
				return true;
			}

			if (CanJump || result.Hit) {
				var isDHE = result.Hit && result.HitEntity is DoubleHitEnemy;
				playeranim_attackdouble = isDHE;
				playeranim_attackair = result.Hit && !isDHE;
				playeranim_jump = CanJump;
				playeranim_perfect |= result.IsPerfect;

				OnAirAttack?.Invoke(this, PathwaySide.Top);

				return true;
			}

			return false;
		}

		public void AttackGround(PollResult result) {
			if (InMashState) {
				playeranim_attackdouble = true;
				playeranim_perfect = true;
				OnGroundAttack?.Invoke(this, PathwaySide.Bottom);
				return;
			}

			if (result.Hit) {
				if (result.HitEntity is DoubleHitEnemy) {
					playeranim_attackdouble = true;
					playeranim_miss = false;
				}
				else {
					playeranim_attackground = true;
					playeranim_miss = false;
				}
			}
			else {
				playeranim_attackground = false;
				playeranim_miss = true;
			}

			playeranim_perfect |= result.IsPerfect;
			OnGroundAttack?.Invoke(this, PathwaySide.Bottom);
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
			public CD_Player_UIBar() {

			}
			protected override void Initialize() {
				base.Initialize();
				Dock = Dock.Bottom;
			}
			public override void Paint(float width, float height) {
				var lvl = Level.As<CD_GameLevel>();

				var startAtX = width / 4f;
				var totalW = width / 2f;
				var endAtX = startAtX + totalW;

				Graphics2D.ScissorRect(RectangleF.XYWH(startAtX, 0, endAtX, height));

				Graphics2D.SetDrawColor(255, 60, 42);
				Graphics2D.DrawRectangle(width / 4f, 0, (width / 2f) * (lvl.Health / lvl.MaxHealth), 24);
				Graphics2D.SetDrawColor(255 / 2, 60 / 2, 42 / 2);
				Graphics2D.DrawRectangleOutline(width / 4f, 0, (width / 2f), 24, 2);
				Graphics2D.SetDrawColor(255, 220, 200);
				Graphics2D.DrawText(width / 2f, 12, $"HP: {lvl.Health}/{lvl.MaxHealth}", "Noto Sans", 22, Anchor.Center);
				float feverRatio;
				if (lvl.InFever)
					feverRatio = (float)lvl.FeverTimeLeft / lvl.FeverTime;
				else
					feverRatio = (float)lvl.Fever / lvl.MaxFever;

				var lastTimeHit = lvl.LastFeverIncreaseTime;

				Graphics2D.SetDrawColor(72, 160, 255);
				Graphics2D.DrawRectangle(width / 4f, 32, (width / 2f) * feverRatio, 24);

				// when hit gradient
				var gradSize = 48;
				var gradColor = new Color(162, 220, 255, (int)(float)NMath.Remap(lvl.Conductor.Time, lastTimeHit, lastTimeHit + .2f, 255, 0, clampOutput: true));
				Graphics2D.DrawGradient(new(startAtX + ((width / 2f) * feverRatio) - gradSize, 33), new(gradSize, 24 - 2), gradColor, new(gradColor.R, gradColor.G, gradColor.B, (byte)0), Dock.Left);



				Graphics2D.SetDrawColor(72 / 2, 160 / 2, 255 / 2);
				Graphics2D.DrawRectangleOutline(startAtX, 32, (width / 2f), 24, 2);
				Graphics2D.SetDrawColor(200, 220, 255);
				Graphics2D.DrawText(width / 2f, 32 + 12, lvl.InFever ? $"FEVER! {Math.Round(lvl.FeverTimeLeft, 2):0.00}s remaining" : $"FEVER: {Math.Round((lvl.Fever / lvl.MaxFever) * 100)}%", "Noto Sans", 22, Anchor.Center);

				Graphics2D.ScissorRect();
			}
		}

		internal CD_Player_Scorebar Scorebar;
		internal class CD_Player_Scorebar : Element
		{
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
				var lvl = Level.As<CD_GameLevel>();
				Graphics2D.DrawText(width * 0.4f, 32 + 24, $"{lvl.Combo}", "Noto Sans", (int)NMath.Remap(lvl.Conductor.Time - lvl.LastCombo, 0.2f, 0, 32, 40, clampOutput: true), Anchor.Center);
				Graphics2D.DrawText(width * 0.4f, 32 + 56, "COMBO", "Noto Sans", 24, Anchor.Center);

				Graphics2D.DrawText(width * 0.6f, 32 + 24, $"{lvl.Score}", "Noto Sans", 32, Anchor.Center);
				Graphics2D.DrawText(width * 0.6f, 32 + 56, "SCORE", "Noto Sans", 24, Anchor.Center);
			}
		}
	}
}
