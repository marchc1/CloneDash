using CloneDash.Common.Game;
using CloneDash.Common.Gamemodes.MuseDash;
using CloneDash.Common.Gamemodes.MuseDash.V1;
using CloneDash.Common.Gamemodes.MuseDash.V1.Data;
using CloneDash.Common.Scenes;
using CloneDash.Game.Entities;
using CloneDash.Game.Statistics;
using CloneDash.Scenes;
using CloneDash.Settings;

using Microsoft.Extensions.DependencyInjection;
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

	double? ShowTime;
	public double? GetShowTime() => ShowTime;
	public void SetShowTimeViaLength(double length, double hittime) => ShowTime = hittime - length;
	public void SetShowTimeDirect(double showtime) => ShowTime = showtime;
}

public class DashEnemy : RhythmNote
{
	public DashEnemyVisuals[] Visuals = null!;

	public StatisticsData GetStats() => Components.Stats;

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
	public MuseDash1EntityType Type { get; set; } = 0;

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
	/// Which direction does the entity come in from. Note that this only applies to some entities.
	/// </summary>
	public EntityEnterDirection EnterDirection { get; set; }

	public List<DashEnemy> SameTimeNotes { get; } = [];

	public virtual bool KilledPerfect => WasHitPerfect;

	public double JudgedTime { get; set; }

	public override double GetShowTime() => GetActiveVisuals().GetShowTime() ?? ShowTime;

	protected override void OnPunishment() {
		Components.NoteRules.Punish(this);
	}

	protected override void OnReward() {
		Components.Rules.AddScore(Components.Rules.DetermineScoreMultiplied(ScoreGiven, Components.Judge.LastPollResult, JudgedTime), this);
	}

	/// <summary>
	/// Does the entity warn the player when it is visible?
	/// </summary>
	public bool Warns { get; set; } = false;

	public bool RelatedToBoss { get; set; }
	public string? BossAction { get; set; }

	private Color? __hitColor;
	public Color HitColor {
		get { return __hitColor.HasValue ? __hitColor.Value : Components.Pathways.Layout[Components.Pathways.PrimaryPathway(Pathway)].Color; }
		set { __hitColor = value; }
	}

	public static Dictionary<MuseDash1EntityType, Type> TypeConvert { get; } = new() {
		{ MuseDash1EntityType.Single, typeof(SingleHitEnemy) },
		{ MuseDash1EntityType.Double, typeof(DoubleHitEnemy) },
		{ MuseDash1EntityType.Score, typeof(Score) },
		{ MuseDash1EntityType.Hammer, typeof(Hammer) },
		{ MuseDash1EntityType.Masher, typeof(Masher) },
		{ MuseDash1EntityType.Gear, typeof(Gear) },
		{ MuseDash1EntityType.Ghost, typeof(Ghost) },
		{ MuseDash1EntityType.Raider, typeof(Raider) },
		{ MuseDash1EntityType.Heart, typeof(Health) },
		{ MuseDash1EntityType.SustainBeam, typeof(SustainBeam) },
	};

	public override void OnReset() {
		foreach (var visual in Visuals) {
			visual.Reset();
		}
	}

	protected void SetupHitAnimations(DashEnemyVisuals visuals) {
		var scene = visuals.Scene;
		visuals.GreatHitAnimation = visuals.Model?.Data.FindAnimation(scene.GetEnemyHitAnimation(this, HitAnimationType.Great));
		visuals.PerfectHitAnimation = visuals.Model?.Data.FindAnimation(scene.GetEnemyHitAnimation(this, HitAnimationType.Perfect));
	}

	protected void XPosSetup(DashEnemyVisuals visuals) {

	}

	protected void BasicSetup(DashEnemyVisuals visuals) {
		var scene = visuals.Scene;

		visuals.Model = scene.GetEnemyModel(this)?.Instantiate();

		var animationName = scene.GetEnemyApproachAnimation(this, out var showtime);
		visuals.SetShowTimeViaLength(showtime, HitTime);
		visuals.ApproachAnimation = visuals.Model?.Data.FindAnimation(animationName);

		SetupHitAnimations(visuals);
	}

	protected DashEnemyComponents Components { get; }

	protected DashEnemy(MuseDash1EntityType type, DashEnemyComponents components) : base(components.Conductor, components.Scroll) {
		Components = components;
		Type = type;
		Scale = new(MuseDash1Gamemode.GlobalScale);
	}

	public string? DebuggingInfo { get; internal set; }

	public static bool TryCreateFromType(MuseDash1ChartComponent chart, RhythmGameLevel level, MuseDash1EntityType type, [NotNullWhen(true)] out DashEnemy? entity) {
		if (!TypeConvert.TryGetValue(type, out var ctype)) {
			entity = null;
			return false;
		}
		entity = CreateFromType(chart, level, ctype);
		return true;
	}
	public static DashEnemy CreateFromType(MuseDash1ChartComponent chart, RhythmGameLevel level, MuseDash1EntityType type) => CreateFromType(chart, level, TypeConvert[type]);
	public static DashEnemy CreateFromType(MuseDash1ChartComponent chart, RhythmGameLevel level, Type type) {
		var enemy = level.Add((DashEnemy)ActivatorUtilities.CreateInstance(level, type));
		enemy.SortIndex = chart.EnemySortIndexCounter++;
		return enemy;
	}

	public void PreBuildVisuals() {
		Visuals = new DashEnemyVisuals[Components.Scenes.GetNumScenes()];
	}

	public void BuildForScene(IMuseDash1SceneInstance instance) {
		DashEnemyVisuals visuals = Visuals[instance.GetSceneArrayIndex()] = new() {
			Enemy = this,
			Scene = instance
		};
		OnBuildVisuals(visuals);
	}

	public virtual void OnBuildVisuals(DashEnemyVisuals visuals) {
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
		visuals.MountedHeart.Position = new Vector2F(
			visuals.MountBone.WorldTransform.X * MuseDash1Gamemode.GlobalScale,
			(4.5f - (visuals.MountBone.WorldTransform.Y * MuseDash1Gamemode.GlobalScale)) - 2.25f
		) + Components.Scenes.GetPathwayRenderOffset(Components.Pathways.PrimaryPathway(Pathway));
		visuals.MountedHeart.Scale = new(MuseDash1Gamemode.GlobalScale);
		visuals.MountedHeart.Render();
	}

	public bool Visible = true;

	public DashEnemyVisuals GetActiveVisuals() {
		var visuals = Visuals[Components.Scenes.GetActiveSceneIdx()];
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

		if (!Components.Playfield.ShouldFreezeNoteAnimations())
			DetermineAnimationPlayback(visuals);

		visuals.Model.Position = Position;
		visuals.Model.Scale = Scale;
		var hueShifted = Components.Scenes.BeginPathwayHueShift(Components.Pathways.PrimaryPathway(Pathway));
		visuals.Model.Render(!hueShifted);
		if (hueShifted)
			Components.Scenes.EndPathwayHueShift();

		RenderHeartMount(visuals);
	}

	protected override bool CanAffectPlayer() => Components.NoteRules.CanAffectPlayer(this);
	protected override Vector2F GetAnchorPosition() => Components.Scenes.GetEntityPosition(Pathway);
	protected override void OnDamagePlayer() => Components.NoteRules.Damage(this);
	protected override void OnKill() => Components.NoteRules.Kill(this);
	protected override void OnPassFeedback() => Components.Hud.UI?.CreatePassText(0, Pathway);
	protected override void OnRewardPlayer() => Components.NoteRules.Reward(this);
}
