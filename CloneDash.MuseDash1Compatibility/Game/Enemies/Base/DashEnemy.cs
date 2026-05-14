using CloneDash.Common.Gamemodes.MuseDash;
using CloneDash.Common.Gamemodes.MuseDash.V1;
using CloneDash.Common.Gamemodes.MuseDash.V1.Data;
using CloneDash.Common.Scenes;
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

namespace CloneDash.Game;

public class DashEnemyVisuals
{
	public required DashEnemy Enemy;
	public required IMuseDash1SceneInstance Scene;

	public ModelInstance? Model;
	public readonly AnimationHandler Animations;
	public ModelInstance? MountedHeart;
	public Nucleus.Models.Runtime.Animation? MountedHeartAnimation;
	public BoneInstance? MountBone;

	public Nucleus.Models.Runtime.Animation? ApproachAnimation;
	public Nucleus.Models.Runtime.Animation? GreatHitAnimation;
	public Nucleus.Models.Runtime.Animation? PerfectHitAnimation;

	public Nucleus.Models.Runtime.Animation? OutAnimation;

	public DashEnemyVisuals() {
		Animations = new AnimationHandler();
	}

	public void Reset() {
		Model?.SetToSetupPose();
		Animations.ClearAllAnimation();
	}
}

public class DashEnemy : Entity
{
	public DashEnemyVisuals[] Visuals = null!;

	public MuseDash1Game GetGameLevel() => Level.As<MuseDash1Game>();
	public StatisticsData GetStats() => Level.As<MuseDash1Game>().Stats;
	public Conductor GetConductor() => Level.As<MuseDash1Game>().Conductor;

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
	/// The low-end range of when a hit/pass is considered "great". <br></br><br></br> <i>Note that this is considered to be a positive value.</i>
	/// </summary>
	public float PreGreatRange { get; set; } = 0.08f;
	/// <summary>
	/// The high-end range of when a hit/pass is considered "great". <br></br><br></br> <i>Note that this is considered to be a positive value.</i>
	/// </summary>
	public float PostGreatRange { get; set; } = 0.08f;

	/// <summary>
	/// The low-end range of when a hit/pass is considered "perfect". <br></br><br></br> <i>Note that this is considered to be a positive value.</i>
	/// </summary>
	public float PrePerfectRange { get; set; } = 0.05f;
	/// <summary>
	/// The high-end range of when a hit/pass is considered "perfect". <br></br><br></br> <i>Note that this is considered to be a positive value.</i>
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

	/// When does this entity first appear on the screen, in seconds. <b>WILL NOT ACCOUNT FOR OFFSETS! See GetVisual/GetJudgement methods.</b>
	/// </summary>
	public double ShowTime { get; set; }

	/// <summary>
	/// When does this entity need to be hit, in seconds. <b>WILL NOT ACCOUNT FOR OFFSETS! See GetVisual/GetJudgement methods.</b>
	/// </summary>
	public double HitTime { get; set; }

	public void SetShowTimeViaLength(double length) => ShowTime = HitTime - length;

	public double GetVisualShowTime() => ShowTime + InputSettings.VisualOffset;
	public double GetVisualHitTime() => HitTime + InputSettings.VisualOffset;
	public double GetJudgementShowTime() => ShowTime + InputSettings.JudgementOffset;
	public double GetJudgementHitTime() => HitTime + InputSettings.JudgementOffset;
	public double GetVisualTimeUntilHit() => DistanceToHit + InputSettings.VisualOffset;
	public double GetVisualTimeUntilEnd() => DistanceToEnd + InputSettings.VisualOffset;
	public double GetJudgementTimeUntilHit() => DistanceToHit + InputSettings.JudgementOffset;
	public double GetJudgementTimeUntilEnd() => DistanceToEnd + InputSettings.JudgementOffset;


	/// <summary>
	/// How long does this entity need to be hit/sustained, in seconds
	/// </summary>
	public double Length { get; set; }
	public int Speed { get; set; }

	public virtual void OnSignalReceived(DashEnemy? from, EntitySignalType signalType, object? data = null) {

	}

	public void SendSignal(DashEnemy? to, EntitySignalType signalType, object? data = null) => GetGameLevel().SendEntitySignal(this, to, signalType, data);
	public void BroadcastSignal(EntitySignalType signalType, object? data = null) => GetGameLevel().BroadcastEntitySignal(this, signalType, data);

	/// <summary>
	/// Damages the player as a punishment (which also resets their combo)
	/// </summary>
	public void DamagePlayer() {
		var level = GetGameLevel();

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
		var level = GetGameLevel();

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
		GetGameLevel().ResetCombo();
	}

	public void RewardPlayer(bool heal = false) {
		var level = GetGameLevel();
		if (DidRewardPlayer) // Did the entity reward the player already
			return;

		if (!DoesRewardPlayer) // Does the entity reward the player
			return;

		if (level.InMashState) // Is the player mashing an entity
			return;

		if (heal) {
			level.Heal(HealthGiven);
			level.GetSceneUI()?.CreateHealthText(HealthGiven, Pathway);
		}

		if (Blood) {
			level.Heal(MD1_SongChartEntity.DEFAULT_HP);
			level.GetSceneUI()?.CreateHealthText(MD1_SongChartEntity.DEFAULT_HP, Pathway);
			level.GetActiveScene()?.PlaySound(SceneSound.GotHeart, 0);
		}

		OnReward();
		DidRewardPlayer = true;

		//Game.GameplayManager.SpawnTextEffect("PASS", color: new Color(200,200,200,255));
	}

	protected virtual void OnReward() {
		var game = GetGameLevel();
		game.AddScore(CDUtils.DetermineScoreMultiplied(game, ScoreGiven, game.LastPollResult));
	}

	/// <summary>
	/// Is the entity dead?
	/// </summary>
	public bool Dead { get; private set; } = false;

	/// <summary>
	/// Does the entity warn the player when it is visible?
	/// </summary>
	public bool Warns { get; set; } = false;

	/// <summary>
	/// Kills the entity, which removes a lot of functionality from the entity. Will also mark down FinalBlow time and the Dead field.
	/// </summary>
	public void Kill() {
		var level = GetGameLevel();
		Dead = true;

		if (DeathAddsToCombo)
			level.AddCombo();

		level.AddFever((int)this.FeverGiven);

		RewardPlayer();
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
		var level = GetGameLevel();

		var current = GetConductor().Time - timeOffset - (InputSettings.offset_visual.GetFloat() / 1000);
		var tickHit = this.GetVisualHitTime();
		var tickShow = this.GetVisualShowTime();
		var thisPos = NMath.Remap(current, (float)tickHit, (float)tickShow, level.GetPathwayPosition(Pathway).X, GetXPosTimeSpeedBase());
		return thisPos;
	}

	private double GetXPosTimeSpeedBase() {
		switch (Speed) {
			case 1: return 1130 / 200d;
			case 2: return 1430 / 200d;
			case 3: return 1780 / 200d;
			default: goto case 1;
		}
	}

	public bool Shown { get; protected set; } = false;

	public bool CheckVisTest() {
		var level = Level;
		var windowWidth = EngineCore.GetWindowWidth();
		var windowHeight= EngineCore.GetWindowHeight();

		XPos = XPosFromTimeOffset((float)-InputSettings.VisualOffset);
		float w = windowWidth / 200, h = windowHeight; // 200;

		var ret = VisTest(w, h, (float)XPos);
		if (Shown == false && ret == true) {
			Shown = true;
			OnFirstVisible();
		}

		return ret;
	}

	protected virtual void OnFirstVisible() {
		if (Variant.IsBoss())
			SendSignal(GetGameLevel().Boss, EntitySignalType.FirstAppearance);
	}

	public virtual bool VisTest(float gamewidth, float gameheight, float xPosition) {
		return xPosition >= -gamewidth * 1f && xPosition <= gamewidth / 1 && GetConductor().Time >= (GetVisualShowTime());
	}

	/// <summary>
	/// Overridden method for when the entity is hit. Applicable to Hit, Avoid, and Sustain interactivity types.
	/// </summary>
	protected virtual void OnHit(PathwaySide side, double distanceToHit) {
		if (Variant.IsBoss())
			SendSignal(GetGameLevel().Boss, EntitySignalType.Hit);
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

	public delegate void EntityPathwayEvent(DashEnemy entity, PathwaySide side);
	public delegate void EntityNoArgumentEvent(DashEnemy entity);

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

		var level = Level.As<MuseDash1Game>();

		level.GetSceneUI()?.CreatePassText(0, Pathway);
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

	private Color? __hitColor;
	public Color HitColor {
		get { return __hitColor.HasValue ? __hitColor.Value : Level.As<MuseDash1Game>().GetPathway(Pathway).Color; }
		set { __hitColor = value; }
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

	public static Dictionary<EntityType, Type> TypeConvert { get; } = new() {
		{ EntityType.Single, typeof(SingleHitEnemy) },
		{ EntityType.Double, typeof(DoubleHitEnemy) },
		{ EntityType.Score, typeof(Score) },
		{ EntityType.Hammer, typeof(Hammer) },
		{ EntityType.Masher, typeof(Masher) },
		{ EntityType.Gear, typeof(Gear) },
		{ EntityType.Ghost, typeof(Ghost) },
		{ EntityType.Raider, typeof(Raider) },
		{ EntityType.Heart, typeof(Health) },
		{ EntityType.SustainBeam, typeof(SustainBeam) },
	};

	public virtual void OnReset() {
		foreach (var visual in Visuals) {
			visual.Reset();
		}
	}

	protected void SetupHitAnimations(DashEnemyVisuals visuals) {
		var scene = visuals.Scene;
		visuals.GreatHitAnimation = visuals.Model?.Data.FindAnimation(scene.GetEnemyHitAnimation(this, HitAnimationType.Great));
		visuals.PerfectHitAnimation = visuals.Model?.Data.FindAnimation(scene.GetEnemyHitAnimation(this, HitAnimationType.Perfect));
	}

	protected void BasicSetup(DashEnemyVisuals visuals) {
		var level = GetGameLevel();
		var scene = visuals.Scene;

		visuals.Model = scene.GetEnemyModel(this)?.Instantiate();

		var animationName = scene.GetEnemyApproachAnimation(this, out var showtime);
		SetShowTimeViaLength(showtime);

		visuals.ApproachAnimation = visuals.Model?.Data.FindAnimation(animationName);
		SetupHitAnimations(visuals);
	}

	protected DashEnemy(EntityType type) {
		Type = type;
		Scale = new(MuseDash1Game.GlobalScale);
	}

	public string? DebuggingInfo { get; internal set; }

	public static bool TryCreateFromType(MuseDash1Game game, EntityType type, [NotNullWhen(true)] out DashEnemy? entity) {
		if (!TypeConvert.TryGetValue(type, out var ctype)) {
			entity = null;
			return false;
		}
		entity = CreateFromType(game, ctype);
		return true;
	}
	public static DashEnemy CreateFromType(MuseDash1Game game, EntityType type) => CreateFromType(game, TypeConvert[type]);
	public static DashEnemy CreateFromType(MuseDash1Game game, Type type) {
		var enemy = game.Add((DashEnemy)Activator.CreateInstance(type)!)!;
		enemy.SortIndex = game.EnemySortIndexCounter++;
		return enemy;
	}

	public void PreBuildVisuals(MuseDash1Game game) {
		Visuals = new DashEnemyVisuals[game.GetNumScenes()];
	}

	public void BuildForScene(IMuseDash1SceneInstance instance) {
		DashEnemyVisuals visuals = Visuals[instance.GetSceneArrayIndex()] = new() {
			Enemy = this,
			Scene = instance
		};
		OnBuildVisuals(visuals);
	}

	public virtual void OnBuildVisuals(DashEnemyVisuals visuals) {
		var lvl = GetGameLevel();
		var scene = visuals.Scene;

		if (Blood) {
			string? mountAnimation = null;
			visuals.MountedHeart = scene?.GetHP(out mountAnimation)?.Instantiate();
			visuals.MountedHeartAnimation = visuals.MountedHeart?.Data.FindAnimation(mountAnimation);
		}
	}

	public void SetMountBoneIfApplicable(DashEnemyVisuals visuals, BoneInstance? bone) {
		if (bone == null) return;
		if (visuals.Model == null) throw new NullReferenceException("Need model first!");
		visuals.MountBone = bone;
	}


	public double AnimationTime => (GetVisualShowTime() - GetConductor().Time) * -1;
	private double tth => HitTime - ShowTime; // debugging, places enemy at exact frame position

	public virtual void DetermineAnimationPlayback(DashEnemyVisuals visuals) {
		visuals.ApproachAnimation?.Apply(visuals.Model, AnimationTime);
	}

	public void RenderHeartMount(DashEnemyVisuals visuals) {
		if (visuals.MountedHeart == null) return;
		if (visuals.MountedHeartAnimation == null) return;
		if (visuals.MountBone == null) return;
		if (Dead) return;

		visuals.MountedHeartAnimation.Apply(visuals.MountedHeart, AnimationTime);
		// Why do we have to do this weird 900 - worldY - 450 thing? Doesn't make sense but whatever
		visuals.MountedHeart.Position = new(
			visuals.MountBone.WorldTransform.X * MuseDash1Game.GlobalScale,
			(4.5f - (visuals.MountBone.WorldTransform.Y * MuseDash1Game.GlobalScale)) - 2.25f
		);
		visuals.MountedHeart.Scale = new(MuseDash1Game.GlobalScale);
		visuals.MountedHeart.Render();
	}

	public bool Visible = true;

	public DashEnemyVisuals GetActiveVisuals() {
		var game = Level.As<MuseDash1Game>();
		var visuals = Visuals[game.GetActiveSceneIdx()];
		return visuals;
	}
	public override void Render(FrameState frameState) {
		base.Render(frameState);
		Render();
	}
	public virtual void Render() {
		if (!Visible) return;

		var visuals = GetActiveVisuals();

		if (visuals.Model == null) return;
		if (AnimationTime == 0) return;

		DetermineAnimationPlayback(visuals);

		visuals.Model.Position = Position;
		visuals.Model.Scale = Scale;
		visuals.Model.Render();

		RenderHeartMount(visuals);
	}
}
