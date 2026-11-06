using CloneDash.Common.Gamemodes.MuseDash;
using CloneDash.Common.Gamemodes.MuseDash.V1;
using CloneDash.Game;
using CloneDash.Settings;
using Nucleus;
using Nucleus.Entities;
using Nucleus.Types;

namespace CloneDash.Common.Game;

public abstract class RhythmNote : Entity, IDashChunkable, IScrollable
{
	readonly Conductor conductor;
	readonly ScrollComponent scroll;

	protected RhythmNote(Conductor conductor, ScrollComponent scroll) {
		this.conductor = conductor;
		this.scroll = scroll;
	}

	public Conductor GetConductor() => conductor;

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
	public bool DoesRewardPlayer { get; protected set; } = true;

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
	/// What pathway is this entity on
	/// </summary>
	public PathwaySet Pathway { get; set; }

	/// <summary>
	/// When does this entity need to be hit, in seconds. <b>WILL NOT ACCOUNT FOR OFFSETS! See GetVisual/GetJudgement methods.</b>
	/// </summary>
	public double HitTime { get; set; }
	public double ShowTime;

	public virtual double GetShowTime() => ShowTime;

	public double GetVisualShowTime() => GetShowTime() + InputSettings.VisualOffset;
	public double GetVisualHitTime() => HitTime + InputSettings.VisualOffset;
	public double GetJudgementShowTime() => GetShowTime() + InputSettings.JudgementOffset;
	public double GetJudgementHitTime() => HitTime + InputSettings.JudgementOffset;
	public double GetVisualTimeUntilHit() => TimeRemap == null
		? DistanceToHit + InputSettings.VisualOffset
		: VisualDistance(HitTime + InputSettings.VisualOffset, GetConductor().Time);
	public double GetVisualTimeUntilEnd() => TimeRemap == null
		? DistanceToEnd + InputSettings.VisualOffset
		: VisualDistance(HitTime + Length + InputSettings.VisualOffset, GetConductor().Time);
	public double GetJudgementTimeUntilHit() => DistanceToHit + InputSettings.JudgementOffset;
	public double GetJudgementTimeUntilEnd() => DistanceToEnd + InputSettings.JudgementOffset;


	/// <summary>
	/// How long does this entity need to be hit/sustained, in seconds
	/// </summary>
	public double Length { get; set; }
	public int Speed { get; set; }
	public double Dt { get; set; }
	public int Layer { get; set; }
	public double ScrollSpeed { get; set; }

	public TimeRemap? TimeRemap { get; private set; }
	double remapChunkStart;
	double remapChunkLength;

	public void SetTimeRemap(TimeRemap? remap) {
		TimeRemap = remap == null || remap.KeyCount == 0 ? null : remap;
		if (TimeRemap == null)
			return;

		double margin = 4 + 2 * Math.Max(Dt, 0);
		TimeRemap.VisibleWindow(HitTime, HitTime + Length, margin, margin, 60, out double start, out double end);
		remapChunkStart = Math.Max(0, start);
		remapChunkLength = Math.Max(0, end - remapChunkStart);
	}

	public double VisualDistance(double target, double now) => TimeRemap == null ? target - now : TimeRemap.Distance(target, now);

	/// <summary>
	/// Damages the player as a punishment (which also resets their combo)
	/// </summary>
	public void DamagePlayer() {
		if (DidDamagePlayer) // Is the player already hurt
			return;

		if (!DoesDamagePlayer) // Does the entity damage the player
			return;

		if (!CanAffectPlayer()) // Is the player mashing an entity right now and can't even hit the entity anyway
			return;

		PunishPlayer(); // Reset combo
		OnDamagePlayer();
		DidDamagePlayer = true;
	}

	/// <summary>
	/// Resets the players combo as a punishment
	/// </summary>
	public void PunishPlayer() {
		if (DidPunishPlayer) // Was the player punished
			return;

		if (!DoesPunishPlayer) // Does the entity punish the player
			return;

		if (!CanAffectPlayer()) // Is the player in a mash state
			return;

		OnPunishment();
		DidPunishPlayer = true;
	}

	protected virtual void OnPunishment() { }

	public void RewardPlayer() {
		if (DidRewardPlayer) // Did the entity reward the player already
			return;

		if (!DoesRewardPlayer) // Does the entity reward the player
			return;

		if (!CanAffectPlayer()) // Is the player mashing an entity
			return;

		OnRewardPlayer();

		OnReward();
		DidRewardPlayer = true;

		//Game.GameplayManager.SpawnTextEffect("PASS", color: new Color(200,200,200,255));
	}

	protected virtual void OnReward() { }

	/// <summary>
	/// Is the entity dead?
	/// </summary>
	public bool Dead { get; private set; } = false;

	/// <summary>
	/// Kills the entity, which removes a lot of functionality from the entity. Will also mark down FinalBlow time and the Dead field.
	/// </summary>
	public void Kill() {
		Dead = true;

		OnKill();

		RewardPlayer();
	}

	/// <summary>
	/// Kills the entity, but does not add to combo, fever, or reward player
	/// </summary>
	public void SoftKill() {
		Dead = true;
	}

	/// <summary>
	/// The distance, in seconds, to when the entity needs to be hit. A negative value means that the player hit too late, a positive means the player hit too early.
	/// <br/>
	/// <b>WILL NOT ACCOUNT FOR OFFSETS! See GetVisual/GetJudgement methods.</b>
	/// </summary>
	public double DistanceToHit => HitTime - GetConductor().Time;

	/// <summary>
	/// The distance, in seconds, to when the entity needs to be released.
	/// <br/>
	/// <b>WILL NOT ACCOUNT FOR OFFSETS! See GetVisual/GetJudgement methods.</b>
	/// </summary>
	public double DistanceToEnd => (HitTime + Length) - GetConductor().Time;

	/// <summary>
	/// Where is the entity in game-space?
	/// </summary>
	public double XPos { get; protected set; }

	public double XPosFromTimeOffset(float timeOffset = 0) {
		var scrollSpeed = scroll.GetScrollSpeed(this);

		if (TimeRemap != null) {
			var now = GetConductor().Time - (InputSettings.offset_visual.GetFloat() / 1000);
			return GetAnchorPosition().X + TimeRemap.Distance(this.GetVisualHitTime() + timeOffset, now) * scrollSpeed;
		}

		var current = GetConductor().Time - timeOffset - (InputSettings.offset_visual.GetFloat() / 1000);
		var tickHit = this.GetVisualHitTime();

		return GetAnchorPosition().X + (tickHit - current) * scrollSpeed;
	}

	public bool Shown { get; protected set; } = false;

	public bool CheckVisTest() {
		var level = Level;
		var windowWidth = EngineCore.GetWindowWidth();
		var windowHeight = EngineCore.GetWindowHeight();

		XPos = XPosFromTimeOffset((float)-InputSettings.VisualOffset);
		float w = windowWidth / 200, h = windowHeight; // 200;

		var ret = VisTest(w, h, (float)XPos);
		if (Shown == false && ret == true) {
			Shown = true;
			OnFirstVisible();
		}

		return ret;
	}

	protected virtual void OnFirstVisible() { }

	public virtual bool VisTest(float gamewidth, float gameheight, float xPosition) {
		return xPosition >= -gamewidth * 1.5f && (TimeRemap == null ? GetConductor().Time >= GetVisualShowTime() : AnimationTime >= 0);
	}

	/// <summary>
	/// Overridden method for when the entity is hit. Applicable to Hit, Avoid, and Sustain interactivity types.
	/// </summary>
	protected virtual void OnHit(PathwayId pathway, double distanceToHit) { }
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

	public delegate void EntityPathwayEvent(RhythmNote entity, PathwayId pathway);
	public delegate void EntityNoArgumentEvent(RhythmNote entity);

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
	public void Hit(PathwayId pathway, double distanceToHit) {
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

		OnPassFeedback();
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

	public void Reset() {
		Hits = 0;
		WasHitPerfect = false;
		LastHitTime = 0;

		DidDamagePlayer = false;
		DidRewardPlayer = false;
		DidPunishPlayer = false;
		DidMiss = false;
		DidPass = false;
		Shown = false;

		ShouldDraw = true;
		ForceDraw = false;
		XPos = 0;
		Dead = false;
		OnReset();
	}
	public int SortIndex;

	public virtual void OnReset() { }

	public double AnimationTime => TimeRemap == null
		? (GetVisualShowTime() - GetConductor().Time) * -1
		: (GetVisualHitTime() - GetVisualShowTime()) - VisualDistance(GetVisualHitTime(), GetConductor().Time);
	private double tth => HitTime - GetShowTime(); // debugging, places enemy at exact frame position

	protected virtual bool CanAffectPlayer() => true;
	protected abstract Vector2F GetAnchorPosition();
	protected virtual void OnDamagePlayer() { }
	protected virtual void OnKill() { }
	protected virtual void OnPassFeedback() { }
	protected virtual void OnRewardPlayer() { }

	public double GetChunkableTime() => TimeRemap == null ? HitTime : remapChunkStart;
	public double GetChunkablePostLength() => TimeRemap == null ? Length : remapChunkLength;
	public bool IsChunkableNow() => (CheckVisTest() || ForceDraw) && ShouldDraw;
	public int GetChunkableSortIndex() => SortIndex;
}
