using CloneDash.Common.Game;
using CloneDash.Common.Gamemodes.MuseDash.V1;
using CloneDash.Compatibility.MuseDash;
using CloneDash.Scenes;
using Nucleus;
using Nucleus.Engine;

using Raylib_cs;

namespace CloneDash.Game.Entities;

public class Boss : DashEnemy
{
	public const int ANIMATION_CHANNEL_MAIN = 0;

	readonly Dictionary<IMuseDash1SceneInstance, MD1_SpineActionController> controllers = [];

	public Boss(DashEnemyComponents components) : base(MuseDash1EntityType.Boss, components) {
		Interactivity = EntityInteractivity.Noninteractive;
		Visible = false;
	}

	public bool Hidden { get; private set; }

	public override void Initialize() {

	}

	public void Advance(double time) {
		foreach (var visual in Visuals)
			visual.Animations.AddDeltaTime(time);
	}

	public bool HasClip(string clip) => GetActiveVisuals().Model?.Data.FindAnimation(clip) != null;

	public void Hide() {
		Hidden = true;
	}

	public void Play(string action) {
		Visible = true;
		foreach (var visual in Visuals)
			if (controllers.TryGetValue(visual.Scene, out var controller))
				controller.PlaySkeletonAction(new(action), false);
	}

	public void PlayClip(string clip, bool loop) {
		Visible = true;
		foreach (var visual in Visuals)
			visual.Animations.SetAnimation(ANIMATION_CHANNEL_MAIN, clip, loop);
	}

	public void QueueClip(string clip, bool loop) {
		foreach (var visual in Visuals)
			visual.Animations.AddAnimation(ANIMATION_CHANNEL_MAIN, clip, loop);
	}

	public void Show() {
		Hidden = false;
	}

	public void SyncScenes(IMuseDash1SceneInstance? prev, IMuseDash1SceneInstance? now) {
		// Patch up animations
		var prevVisuals = prev == null ? null : Visuals[prev.GetSceneArrayIndex()];
		var nowVisuals = now == null ? null : Visuals[now.GetSceneArrayIndex()];
		if (prevVisuals != null && nowVisuals != null)
			nowVisuals.Animations.Channels[ANIMATION_CHANNEL_MAIN].Time = prevVisuals.Animations.Channels[ANIMATION_CHANNEL_MAIN].Time;
	}

	public override void Render() {
		if (!Visible) return;
		var visuals = GetActiveVisuals();

		if (visuals.Model == null) return;

		if (!Level.Paused) visuals.Animations?.AddDeltaTime(Level.RendertimeDelta * RhythmGameLevel.musicspeed.GetDouble());
		if (Hidden) return;

		visuals.Animations?.Apply(visuals.Model);
		visuals.Model.Position = Position;
		visuals.Model.Scale = Scale;

		visuals.Model.Render();
	}

	public override void OnBuildVisuals(DashEnemyVisuals visuals) {
		base.OnBuildVisuals(visuals);
		visuals.Model = visuals.Scene.GetEnemyModel(this)?.Instantiate();
		if (visuals.Model != null) {
			visuals.Animations.SetModel(visuals.Model);
			visuals.Model.SetToSetupPose();
			controllers[visuals.Scene] = new(visuals.Scene.GetBossActions(), visuals.Animations);
		}
	}

	public override bool VisTest(float gamewidth, float gameheight, float xPosition) {
		return Visible;
	}

	public override void OnReset() {
		base.OnReset();
		Visible = false;
		Hidden = false;
	}
}
