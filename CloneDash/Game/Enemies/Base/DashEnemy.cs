using CloneDash.Game.Entities;
using CloneDash.Scenes;

using Nucleus.Models.Runtime;

using System.Diagnostics.CodeAnalysis;

namespace CloneDash.Game;

public class DashEnemy : DashModelEntity
{
	public ModelInstance? MountedHeart;
	public Nucleus.Models.Runtime.Animation? MountedHeartAnimation;
	public BoneInstance? MountBone;

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


	protected void SetupHitAnimations(ISceneDescriptor scene) {
		GreatHitAnimation = Model.Data.FindAnimation(scene.GetEnemyHitAnimation(this, HitAnimationType.Great));
		PerfectHitAnimation = Model.Data.FindAnimation(scene.GetEnemyHitAnimation(this, HitAnimationType.Perfect));
	}
	protected void BasicSetup() {
		var level = GetGameLevel();
		var scene = level.Scene;

		Model = scene.GetEnemyModel(this).Instantiate();

		var animationName = scene.GetEnemyApproachAnimation(this, out var showtime);
		SetShowTimeViaLength(showtime);

		ApproachAnimation = Model.Data.FindAnimation(animationName);
		SetupHitAnimations(scene);

		Scale = new(level.GlobalScale);
	}

	protected DashEnemy(EntityType type) {
		Type = type;
	}

	public string DebuggingInfo { get; internal set; }

	public static bool TryCreateFromType(DashGameLevel game, EntityType type, [NotNullWhen(true)] out DashEnemy? entity) {
		if (!TypeConvert.TryGetValue(type, out var ctype)) {
			entity = null;
			return false;
		}
		entity = CreateFromType(game, ctype);
		return true;
	}
	public static DashEnemy CreateFromType(DashGameLevel game, EntityType type) => CreateFromType(game, TypeConvert[type]);
	public static DashEnemy CreateFromType(DashGameLevel game, Type type) {
		var enemy = game.Add((DashEnemy)Activator.CreateInstance(type));
		return enemy;
	}

	public override void Build() {
		base.Build();

		var lvl = GetGameLevel();
		var scene = lvl.Scene;

		if (Blood) {
			MountedHeart = scene.GetHP(out string mountAnimation).Instantiate();
			MountedHeartAnimation = MountedHeart.Data.FindAnimation(mountAnimation);
		}
	}

	public void SetMountBoneIfApplicable(BoneInstance bone) {
		if (Model == null) throw new NullReferenceException("Need model first!");
		MountBone = bone;
	}


	public Nucleus.Models.Runtime.Animation? ApproachAnimation;
	public Nucleus.Models.Runtime.Animation? GreatHitAnimation;
	public Nucleus.Models.Runtime.Animation? PerfectHitAnimation;

	public double AnimationTime => Math.Max(0, (GetVisualShowTime() - GetConductor().Time) * -1);
	private double tth => HitTime - ShowTime; // debugging, places enemy at exact frame position

	public virtual void DetermineAnimationPlayback() {
		ApproachAnimation?.Apply(Model, AnimationTime);
	}

	public override bool VisTest(float gamewidth, float gameheight, float xPosition) {
		return base.VisTest(gamewidth, gameheight, xPosition);
	}

	public void RenderHeartMount() {
		if (MountedHeart == null) return;
		if (MountedHeartAnimation == null) return;
		if (MountBone == null) return;
		if (Dead) return;

		MountedHeartAnimation.Apply(MountedHeart, AnimationTime);
		// Why do we have to do this weird 900 - worldY - 450 thing? Doesn't make sense but whatever
		MountedHeart.Position = new(MountBone.WorldTransform.X, (900 - MountBone.WorldTransform.Y) - 450);
		MountedHeart.Scale = Scale;
		MountedHeart.Render();
	}

	public override void Render() {
		if (!Visible) return;
		if (Model == null) return;
		if (AnimationTime == 0) return;

		DetermineAnimationPlayback();

		Model.Position = Position;
		Model.Scale = Scale;

		Model.Render();

		RenderHeartMount();
	}
}
