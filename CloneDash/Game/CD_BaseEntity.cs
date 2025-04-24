using CloneDash.Data;
using CloneDash.Game.Entities;
using Nucleus;
using Nucleus.Core;
using Nucleus.Engine;
using Nucleus.Types;
using Raylib_cs;

namespace CloneDash.Game
{

	public class CD_BaseMEntity : ModelEntity
	{
		/// <summary>
		/// Does the death of this entity add to the characters combo score?
		/// </summary>
		public bool DeathAddsToCombo { get; protected set; } = true;

		/// <summary>
		/// Does the failure to kill/pass this entity damage the player?
		/// </summary>
		public bool DoesDamagePlayer { get; protected set; } = true;

		/// <summary>
		/// Has the player been damaged already?<br></br>
		/// Used internally to avoid applying damage over and over again
		/// </summary>
		public bool DidDamagePlayer { get; private set; } = false;

		/// <summary>
		/// Does failure to kill the entity cause a combo loss?
		/// </summary>
		public bool DoesPunishPlayer { get; protected set; } = true;
		/// <summary>
		/// Has the entity punished the player yet?
		/// </summary>
		public bool DidPunishPlayer { get; private set; } = false;

		/// <summary>
		/// <summary>
		/// Has the player been rewarded yet?
		/// </summary>
		public bool DidRewardPlayer { get; private set; }

		/// <summary>
		/// Does the killing of this entity reward the player, either with healing or score?
		/// </summary>
		public bool DoesRewardPlayer { get; private set; } = true;

		/// <summary>
		/// How much health does the entity give (if any)
		/// </summary>
		public float HealthGiven { get; set; }

		/// <summary>
		/// How much score does the entity give to the player?
		/// </summary>
		public int ScoreGiven { get; set; } = 0;

		/// <summary>
		/// Type of the entity
		/// </summary>
		public EntityType Type { get; set; } = EntityType.Unknown;

		/// <summary>
		/// Entity variant (usually not applicable). Mostly for determining models.
		/// </summary>
		public EntityVariant Variant { get; set; } = EntityVariant.NotApplicable;

		/// <summary>
		/// Not applicable for all entities. Determines if the entity uses a flipped model during <see cref="Build()"/>.
		/// <br/> If not implemented, will do nothing.
		/// <br/> (only used in <see cref="Hammer"/> and <see cref="Raider"/>)
		/// </summary>
		public bool Flipped { get; set; }

		/// <summary>
		/// If set; means that a heart is attached to this entity and will give health when successfully hit
		/// </summary>
		public bool Blood { get; set; }

		/// <summary>
		/// How much damage does the player take if failing to kill/pass this entity.
		/// </summary>
		public float DamageTaken { get; set; }

		/// <summary>
		/// How much fever does the player get when killing/passing this entity.
		/// </summary>
		public float FeverGiven { get; set; }

		/// <summary>
		/// The low-end range of when a hit/pass is considered "great". <br></br><br></br> <i>Note that this considered to be a positive value.</i>
		/// </summary>
		public float PreGreatRange { get; set; } = 0.08f;
		/// <summary>
		/// The high-end range of when a hit/pass is considered "great". <br></br><br></br> <i>Note that this considered to be a positive value.</i>
		/// </summary>
		public float PostGreatRange { get; set; } = 0.08f;

		/// <summary>
		/// The low-end range of when a hit/pass is considered "perfect". <br></br><br></br> <i>Note that this considered to be a positive value.</i>
		/// </summary>
		public float PrePerfectRange { get; set; } = 0.05f;
		/// <summary>
		/// The high-end range of when a hit/pass is considered "perfect". <br></br><br></br> <i>Note that this considered to be a positive value.</i>
		/// </summary>
		public float PostPerfectRange { get; set; } = 0.05f;

		/// <summary>
		/// Should the entity draw?<br></br> This overrides ForceDraw. Naming is weird, needs to be adjusted.
		/// </summary>
		public bool ShouldDraw { get; protected set; } = true;
		/// <summary>
		/// Forces the entity to draw to the screen even if it would fail a visibility test.<br></br>Note that ShouldDraw will override this value.
		/// </summary>
		public bool ForceDraw { get; protected set; } = false;

		/// <summary>
		/// The interactivity method of this entity. Different methods of the entity will be called based on this value.
		/// </summary>
		public EntityInteractivity Interactivity { get; set; } = EntityInteractivity.Noninteractive;
		/// <summary>
		/// Is the entity interactive?
		/// </summary>
		public bool Interactive => Interactivity != EntityInteractivity.Noninteractive;
		/// <summary>
		/// Which direction does the entity come in from. Note that this only applies to some entities.
		/// </summary>
		public EntityEnterDirection EnterDirection { get; set; }
		/// <summary>
		/// What pathway is this entity on
		/// </summary>
		public PathwaySide Pathway { get; set; }

		/// <summary>
		/// When does this entity first appear on the screen, in seconds
		/// </summary>
		public double ShowTime { get; set; }
		/// <summary>
		/// When does this entity need to be hit, in seconds
		/// </summary>
		public double HitTime { get; set; }
		/// <summary>
		/// How long does this entity need to be hit/sustained, in seconds
		/// </summary>
		public double Length { get; set; }
		public int Speed { get; set; }

		public bool TellBossWhenSpawned { get; set; } = false;

		public virtual void OnSignalReceived(CD_BaseMEntity from, EntitySignalType signalType, object? data = null) {

		}
		public void SendSignal(CD_BaseMEntity to, EntitySignalType signalType, object? data = null) => GetGameLevel().SendEntitySignal(this, to, signalType, data);
		public void BroadcastSignal(EntitySignalType signalType, object? data = null) => GetGameLevel().BroadcastEntitySignal(this, signalType, data);

		/// <summary>
		/// Damages the player as a punishment (which also resets their combo)
		/// </summary>
		public void DamagePlayer() {
			var level = Level.As<CD_GameLevel>();

			if (DidDamagePlayer) // Is the player already hurt
				return;

			if (!DoesDamagePlayer) // Does the entity damage the player
				return;

			if (level.InMashState) // Is the player mashing an entity right now and can't even hit the entity anyway
				return;

			PunishPlayer(); // Reset combo
			level.Damage(this, DamageTaken);
			DidDamagePlayer = true;
		}

		/// <summary>
		/// Resets the players combo as a punishment
		/// </summary>
		public void PunishPlayer() {
			var level = Level.As<CD_GameLevel>();
			if (DidPunishPlayer) // Was the player punished
				return;

			if (!DoesPunishPlayer) // Does the entity punish the player
				return;

			if (level.InMashState) // Is the player in a mash state
				return;

			OnPunishment();
			DidPunishPlayer = true;
		}

		protected virtual void OnPunishment() {
			Level.As<CD_GameLevel>().ResetCombo();
		}

		public void RewardPlayer(bool heal = false) {
			var level = Level.As<CD_GameLevel>();
			if (DidRewardPlayer) // Did the entity reward the player already
				return;

			if (!DoesRewardPlayer) // Does the entity reward the player
				return;

			if (level.InMashState) // Is the player mashing an entity
				return;

			if (heal)
				level.Heal(HealthGiven);

			if (Blood)
				level.Heal(ChartEntity.BLOOD_HEALTH_GIVEN);

			OnReward();
			DidRewardPlayer = true;

			//Game.GameplayManager.SpawnTextEffect("PASS", color: new Color(200,200,200,255));
		}

		protected virtual void OnReward() {
			var game = Level.As<CD_GameLevel>();
			game.AddScore(CDUtils.DetermineScoreMultiplied(game, ScoreGiven, game.LastPollResult));
		}

		/// <summary>
		/// Optional method entities can use to adjust their drawing position.
		/// </summary>
		/// <param name="pos"></param>
		public virtual void ChangePosition(ref Vector2F pos) {

		}

		/// <summary>
		/// Is the entity dead?
		/// </summary>
		public bool Dead { get; private set; } = false;
		/// <summary>
		/// Is the entity marked for removal from the entities list?
		/// </summary>
		public bool MarkedForRemoval { get; set; } = false;

		/// <summary>
		/// Does the entity warn the player when it is visible?
		/// </summary>
		public bool Warns { get; set; } = false;

		/// <summary>
		/// Kills the entity, which removes a lot of functionality from the entity. Will also mark down FinalBlow time and the Dead field.
		/// </summary>
		public void Kill() {
			var level = Level.As<CD_GameLevel>();
			Dead = true;

			if (DeathAddsToCombo)
				level.AddCombo();

			level.AddFever((int)this.FeverGiven);

			FinalBlow = DateTime.Now;
			RewardPlayer();
		}

		/// <summary>
		/// The distance, in seconds, to when the entity needs to be hit. A negative value means that the player hit too late, a positive means the player hit too early.
		/// </summary>
		public double DistanceToHit => HitTime - Level.As<CD_GameLevel>().Conductor.Time;

		/// <summary>
		/// The distance, in seconds, to when the entity needs to be released.
		/// </summary>
		public double DistanceToEnd => (HitTime + Length) - Level.As<CD_GameLevel>().Conductor.Time;

		/// <summary>
		/// Where is the entity in game-space?
		/// </summary>
		public double XPos { get; protected set; }

		public double XPosFromTimeOffset(float timeOffset = 0) {
			var level = Level.As<CD_GameLevel>();

			var current = level.Conductor.Time - timeOffset;
			var tickHit = this.HitTime;
			var tickShow = this.ShowTime;
			var thisPos = NMath.Remap(current, (float)tickHit, (float)tickShow, level.XPos, 1500);
			return thisPos;
		}

		public bool Shown { get; protected set; } = false;

		public bool CheckVisTest(FrameState frameState) {
			var level = Level.As<CD_GameLevel>();

			XPos = XPosFromTimeOffset();
			float w = frameState.WindowWidth, h = frameState.WindowHeight;

			var ret = VisTest(w, h, (float)XPos);
			if (Shown == false && ret == true) {
				Shown = true;

				//if (RelatedToBoss)
				//level.OnEntityShown(this);
			}
			return ret;
		}

		public virtual bool VisTest(float gamewidth, float gameheight, float xPosition) {
			return xPosition >= -gamewidth / 1 && xPosition <= gamewidth / 1;
		}
		
		/// <summary>
		/// Overridden method for when the entity is hit. Applicable to Hit, Avoid, and Sustain interactivity types.
		/// </summary>
		protected virtual void OnHit(PathwaySide side) {

		}
		protected virtual void OnMiss() {

		}
		/// <summary>
		/// Overridden method for when the entity is passed by. Applicable to the SamePath and Avoid interactivity types.
		/// </summary>
		protected virtual void OnPass() {

		}
		/// <summary>
		/// Overridden method for when the entity is released. Only applicable to the Sustain interactivity type.
		/// </summary>
		protected virtual void OnRelease() {

		}

		public delegate void EntityPathwayEvent(CD_BaseMEntity entity, PathwaySide side);
		public delegate void EntityNoArgumentEvent(CD_BaseMEntity entity);

		/// <summary>
		/// Per-entity event hook for when an entity is hit.
		/// </summary>
		public event EntityPathwayEvent OnHitEvent;
		public event EntityNoArgumentEvent OnMissEvent;
		/// <summary>
		/// Per-entity event hook for when an entity is passed.
		/// </summary>
		public event EntityNoArgumentEvent OnPassEvent;
		/// <summary>
		/// Per-entity event hook for when an entity is released.
		/// </summary>
		public event EntityNoArgumentEvent OnReleaseEvent;

		/// <summary>
		/// Global event hook for when an entity is hit.
		/// </summary>
		public static event EntityNoArgumentEvent GlobalOnHitEvent;
		/// <summary>
		/// Global event hook for when the player misses an entity.
		/// </summary>
		public static event EntityNoArgumentEvent GlobalOnMissEvent;
		/// <summary>
		/// Global event hook for when an entity is passed.
		/// </summary>
		public static event EntityNoArgumentEvent GlobalOnPassEvent;
		/// <summary>
		/// Global event hook for when an entity is released.
		/// </summary>
		public static event EntityNoArgumentEvent GlobalOnReleaseEvent;

		public CD_GameLevel GetGameLevel() => Level.As<CD_GameLevel>();
		public Conductor GetConductor() => Level.As<CD_GameLevel>().Conductor;


		public int Hits { get; set; } = 0;
		public bool WasHitPerfect { get; set; } = false;
		public double LastHitTime { get; set; }
		public void Hit(PathwaySide pathway) {
			Hits++;
			LastHitTime = GetConductor().Time;
			OnHit(pathway);
			OnHitEvent?.Invoke(this, pathway);
			GlobalOnHitEvent?.Invoke(this);
		}
		public bool DidMiss { get; private set; }
		public void Miss() {
			if (DidMiss)
				return;
			Logs.Info("Miss");

			OnMiss();
			OnMissEvent?.Invoke(this);
			GlobalOnMissEvent?.Invoke(this);
			DidMiss = true;
		}

		public bool DidPass { get; private set; }
		public void Pass() {
			if (DidPass)
				return;

			var level = Level.As<CD_GameLevel>();

			level.SpawnTextEffect("PASS", level.GetPathway(this).Position, TextEffectTransitionOut.SlideUpThenToLeft, new Color(235, 235, 235, 255));
			OnPass();
			OnPassEvent?.Invoke(this);
			GlobalOnPassEvent?.Invoke(this);

			DidPass = true;
		}

		public void Release() {
			OnRelease();
			OnReleaseEvent?.Invoke(this);
			GlobalOnReleaseEvent?.Invoke(this);
		}

		public DateTime Created { get; private set; } = DateTime.Now;
		public double Lifetime => (DateTime.Now - Created).TotalSeconds;

		public DateTime FinalBlow { get; private set; } = DateTime.MinValue;
		public float SinceDeath => Dead ? (float)(DateTime.Now - FinalBlow).TotalSeconds : 0;

		public virtual void Build() {

		}

		public bool RelatedToBoss { get; set; }

		private Color? __hitColor;
		public Color HitColor {
			get { return __hitColor.HasValue ? __hitColor.Value : Level.As<CD_GameLevel>().GetPathway(Pathway).Color; }
			set { __hitColor = value; }
		}


		public void Reset() {
			Hits = 0;
			DidDamagePlayer = false;
			DidRewardPlayer = false;
			DidPunishPlayer = false;
			WasHitPerfect = false;
			DidPass = false;
			Dead = false;
			Shown = false;
			OnReset();
		}

		public virtual void OnReset() {

		}
	}
}
