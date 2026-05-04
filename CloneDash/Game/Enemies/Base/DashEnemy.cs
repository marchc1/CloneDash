using CloneDash.Data;
using CloneDash.Game.Entities;
using CloneDash.Game.Statistics;
using CloneDash.Scenes;
using CloneDash.Settings;

using Nucleus;
using Nucleus.Common.Types;
using Nucleus.Engine;
using Nucleus.Entities;
using Nucleus.Models.Runtime;
using Nucleus.Types;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace CloneDash.Game;

public struct DashEnemyInfo()
{
	/// <summary>
	/// Does the death of this entity add to the characters combo score?
	/// </summary>
	public bool DeathAddsToCombo = true;

	/// <summary>
	/// Does the failure to kill/pass this entity damage the player?
	/// </summary>
	public bool DoesDamagePlayer = true;

	/// <summary>
	/// Does failure to kill the entity cause a combo loss?
	/// </summary>
	public bool DoesPunishPlayer = true;

	/// <summary>
	/// Does the killing of this entity reward the player, either with healing or score?
	/// </summary>
	public bool DoesRewardPlayer = true;

	/// <summary>
	/// How much health does the entity give (if any)
	/// </summary>
	public double HealthGiven;

	/// <summary>
	/// How much score does the entity give to the player?
	/// </summary>
	public int ScoreGiven = 0;

	/// <summary>
	/// Entity variant (usually not applicable). Mostly for determining models.
	/// </summary>
	public EntityVariant Variant = EntityVariant.NotApplicable;

	/// <summary>
	/// Not applicable for all entities. Determines if the entity uses a flipped model during <see cref="Build()"/>.
	/// <br/> If not implemented, will do nothing.
	/// <br/> (only used in <see cref="Hammer"/> and <see cref="Raider"/>)
	/// </summary>
	public bool Flipped;

	/// <summary>
	/// If set; means that a heart is attached to this entity and will give health when successfully hit
	/// </summary>
	public bool Blood;

	/// <summary>
	/// How much damage does the player take if failing to kill/pass this entity.
	/// </summary>
	public double DamageTaken;

	/// <summary>
	/// How much fever does the player get when killing/passing this entity.
	/// </summary>
	public double FeverGiven;

	/// <summary>
	/// The low-end range of when a hit/pass is considered "great". <br></br><br></br> <i>Note that this is considered to be a positive value.</i>
	/// </summary>
	public double PreGreatRange = 0.08;
	/// <summary>
	/// The high-end range of when a hit/pass is considered "great". <br></br><br></br> <i>Note that this is considered to be a positive value.</i>
	/// </summary>
	public double PostGreatRange = 0.08;

	/// <summary>
	/// The low-end range of when a hit/pass is considered "perfect". <br></br><br></br> <i>Note that this is considered to be a positive value.</i>
	/// </summary>
	public double PrePerfectRange = 0.05;
	/// <summary>
	/// The high-end range of when a hit/pass is considered "perfect". <br></br><br></br> <i>Note that this is considered to be a positive value.</i>
	/// </summary>
	public double PostPerfectRange = 0.05;

	/// <summary>
	/// Which direction does the entity come in from. Note that this only applies to some entities.
	/// </summary>
	public EntityEnterDirection EnterDirection;
	/// <summary>
	/// What pathway is this entity on
	/// </summary>
	public PathwaySide Pathway;

	/// When does this entity first appear on the screen, in seconds.
	/// </summary>
	public double ShowTime;

	/// <summary>
	/// When does this entity need to be hit, in seconds.
	/// </summary>
	public double HitTime;

	/// <summary>
	/// How long does this entity need to be hit/sustained, in seconds
	/// </summary>
	public double Length;

	// todo: isn't this basically HitTime - ShowTime? Should we just get rid of this?
	// The issue is that its mostly used for MD animations. Can we cope with that?
	public double Speed;
}

/// <summary>
/// The logical implementation of Clone Dash enemies. The rendering is deferred to scene runtimes.
/// </summary>
public abstract class BaseDashEnemy : IDashEnemy, IValidatable
{
	public bool HasBeenRemoved = false;
	public bool IsValid() => !HasBeenRemoved; // todo

	IDashGame game = null!;
	protected DashEnemyInfo info;
	protected bool DidPunishPlayer = false;
	protected bool DidDamagePlayer = false;
	protected bool DidRewardPlayer;

	public ref readonly DashEnemyInfo GetInfo() => ref info;
	public PathwaySide GetPathway() => info.Pathway;

	public double GetLength() => info.Length;

	/// <summary>
	/// The interactivity method of this entity. Different methods of the entity will be called based on this value.
	/// </summary>
	public EntityInteractivity Interactivity = EntityInteractivity.NonInteractive;

	/// <summary>
	/// Type of the entity
	/// </summary>
	public EntityType Type = EntityType.Unknown;

	public IDashGame GetGame() => game;

	public virtual void Initialize(IDashGame game, in DashEnemyInfo info) {
		this.game = game;

		// Type does not change from the struct, so we copy everything except that.
		// (kinda weird, may refactor)
		this.info = info;

		SortIndex = game.NewEnemySortIndexCounter();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public StatisticsData GetStats() => game.GetStatisticsData();
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Conductor GetConductor() => game.GetConductor();

	/// <summary>
	/// Is the entity interactive?
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool IsInteractive() => Interactivity != EntityInteractivity.NonInteractive;

	public void SetShowTimeViaLength(double length) => info.ShowTime = info.HitTime - length;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public double CalcVisualShowTime() => info.ShowTime + InputSettings.VisualOffset;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public double CalcVisualHitTime() => info.HitTime + InputSettings.VisualOffset;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public double CalcJudgementShowTime() => info.ShowTime + InputSettings.JudgementOffset;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public double CalcJudgementHitTime() => info.HitTime + InputSettings.JudgementOffset;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public double CalcVisualTimeUntilHit() => CalcDistanceToHit() + InputSettings.VisualOffset;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public double CalcVisualTimeUntilEnd() => CalcDistanceToEnd() + InputSettings.VisualOffset;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public double CalcJudgementTimeUntilHit() => CalcDistanceToHit() + InputSettings.JudgementOffset;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public double CalcJudgementTimeUntilEnd() => CalcDistanceToEnd() + InputSettings.JudgementOffset;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public double GetSpeed() => info.Speed;


	public virtual void OnSignalReceived<T>(IDashEnemy from, EntitySignalType signalType, T? data = default) {

	}

	/// <summary>
	/// Has the entity punished the player yet?
	/// </summary>
	public bool HasPunishedPlayer() => DidPunishPlayer;
	/// <summary>
	/// Has the player been damaged already?<br></br>
	/// Used internally to avoid applying damage over and over again
	/// </summary>
	public bool HasDamagedPlayer() => DidDamagePlayer;
	/// <summary>
	/// Has the player been rewarded yet?
	/// </summary>
	public bool HasRewardedPlayer() => DidRewardPlayer;

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void SendSignal<T>(IDashEnemy to, EntitySignalType signalType, T? data) => GetGame().SendEntitySignal(this, to, signalType, data);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void BroadcastSignal<T>(EntitySignalType signalType, T? data) => GetGame().BroadcastEntitySignal(this, signalType, data);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void SendSignal(IDashEnemy to, EntitySignalType signalType) => SendSignal<object>(to, signalType, null);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void BroadcastSignal(EntitySignalType signalType) => BroadcastSignal<object>(signalType, null);


	/// <summary>
	/// Damages the player as a punishment (which also resets their combo)
	/// </summary>
	public void DamagePlayer() {
		if (DidDamagePlayer) // Is the player already hurt
			return;

		if (!info.DoesDamagePlayer) // Does the entity damage the player
			return;

		if (game.IsMashing()) // Is the player mashing an entity right now and can't even hit the entity anyway
			return;

		PunishPlayer(); // Reset combo
		game.GivePlayerDamage(this, info.DamageTaken);
		DidDamagePlayer = true;
	}

	/// <summary>
	/// Resets the players combo as a punishment
	/// </summary>
	public void PunishPlayer() {
		if (DidPunishPlayer) // Was the player punished
			return;

		if (!info.DoesPunishPlayer) // Does the entity punish the player
			return;

		if (game.IsMashing()) // Is the player in a mash state
			return;

		OnPunishment();
		DidPunishPlayer = true;
	}

	protected virtual void OnPunishment() {
		game.ResetCombo();
	}

	public virtual bool IsForcingDraw() => false;

	public void RewardPlayer(bool heal = false) {
		if (DidRewardPlayer) // Did the entity reward the player already
			return;

		if (!info.DoesRewardPlayer) // Does the entity reward the player
			return;

		if (game.IsMashing()) // Is the player mashing an entity
			return;

		if (heal)
			game.GivePlayerHealth(this, info.HealthGiven);

		if (info.Blood)
			game.GivePlayerHealth(this, ChartEntity.DEFAULT_HP);

		OnReward();
		DidRewardPlayer = true;

		//Game.GameplayManager.SpawnTextEffect("PASS", color: new Color(200,200,200,255));
	}

	protected virtual void OnReward() {
		game.GivePlayerScorePoints(this, info.ScoreGiven);
	}

	bool Dead  = false;
	bool MarkedForRemoval = false;
	bool Warns = false;

	/// <summary>
	/// Is the entity dead?
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool IsDead() => Dead;
	/// <summary>
	/// Is the entity marked for removal from the entities list?
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool IsMarkedForRemoval() => MarkedForRemoval;
	/// <summary>
	/// Does the entity warn the player when it is visible?
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool IsWarning() => Warns;

	/// <summary>
	/// Kills the entity, which removes a lot of functionality from the entity. Will also mark down FinalBlow time and the Dead field.
	/// </summary>
	public void Kill() {
		Dead = true;

		if (info.DeathAddsToCombo)
			game.GivePlayerCombo(this);

		game.GivePlayerFeverPoints(this, info.FeverGiven);

		RewardPlayer();
	}

	/// <summary>
	/// The distance, in seconds, to when the entity needs to be hit. A negative value means that the player hit too late, a positive means the player hit too early.
	/// <br/>
	/// <b>WILL NOT ACCOUNT FOR OFFSETS! See GetVisual/GetJudgement methods.</b>
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public double CalcDistanceToHit() => info.HitTime - GetConductor().Time;

	/// <summary>
	/// The distance, in seconds, to when the entity needs to be released.
	/// <br/>
	/// <b>WILL NOT ACCOUNT FOR OFFSETS! See GetVisual/GetJudgement methods.</b>
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public double CalcDistanceToEnd() => (info.HitTime + info.Length) - GetConductor().Time;

	/// <summary>
	/// Overridden method for when the entity is hit. Applicable to Hit, Avoid, and Sustain interactivity types.
	/// </summary>
	protected virtual void OnHit(PathwaySide side, double distanceToHit) {
		if (info.Variant.IsBoss())
			SendSignal(game.GetBossEnemy(), EntitySignalType.Hit);
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

	public delegate void EntityPathwayEvent(BaseDashEnemy entity, PathwaySide side);
	public delegate void EntityNoArgumentEvent(BaseDashEnemy entity);

	/// <summary>
	/// Per-entity event hook for when an entity is hit.
	/// </summary>
	public event EntityPathwayEvent? OnHitEvent;
	public event EntityNoArgumentEvent? OnMissEvent;
	/// <summary>
	/// Per-entity event hook for when an entity is passed.
	/// </summary>
	public event EntityNoArgumentEvent? OnPassEvent;
	/// <summary>
	/// Per-entity event hook for when an entity is released.
	/// </summary>
	public event EntityNoArgumentEvent? OnReleaseEvent;

	/// <summary>
	/// Global event hook for when an entity is hit.
	/// </summary>
	public static event EntityNoArgumentEvent? GlobalOnHitEvent;
	/// <summary>
	/// Global event hook for when the player misses an entity.
	/// </summary>
	public static event EntityNoArgumentEvent? GlobalOnMissEvent;
	/// <summary>
	/// Global event hook for when an entity is passed.
	/// </summary>
	public static event EntityNoArgumentEvent? GlobalOnPassEvent;
	/// <summary>
	/// Global event hook for when an entity is released.
	/// </summary>
	public static event EntityNoArgumentEvent? GlobalOnReleaseEvent;

	public int Hits { get; set; } = 0;
	public bool WasHitPerfect { get; set; } = false;
	public double LastHitTime { get; set; }
	public void Hit(PathwaySide pathway, double distanceToHit) {
		Hits++;
		LastHitTime = GetConductor().Time;
		OnHit(pathway, distanceToHit);
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

	public bool RelatedToBoss { get; set; }

	public bool Shown;

	private Color? __hitColor;
	public Color HitColor {
		get { return __hitColor.HasValue ? __hitColor.Value : game.GetCurrentScene().GetPathwayColor(info.Pathway); }
		set { __hitColor = value; }
	}

	public void Reset() {
		Hits = 0;
		WasHitPerfect = false;
		LastHitTime = 0;

		MarkedForRemoval = false;

		DidDamagePlayer = false;
		DidRewardPlayer = false;
		DidPunishPlayer = false;
		DidMiss = false;
		DidPass = false;
		Shown = false;

		Dead = false;
		OnReset();
	}

	public int SortIndex;

	/// <summary>
	/// Allows extra reset logic to occur
	/// </summary>
	public virtual void OnReset() {

	}

	protected BaseDashEnemy(EntityType type) {
		Type = type;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public double CalcAnimationTime() => (CalcVisualShowTime() - GetConductor().Time) * -1;
}
