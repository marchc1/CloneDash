using CloneDash.Modding.Descriptors;
using Nucleus;
using Nucleus.Engine;
namespace CloneDash.Game.Entities;

public class BossInEvent(CD_GameLevel game) : CD_BaseEvent(game)
{
	public override void Activate() {
		Game.Boss.In();
	}
}
public class BossOutEvent(CD_GameLevel game) : CD_BaseEvent(game)
{
	public override void Activate() {
		Game.Boss.Out();
	}
}
public class BossSingleHit(CD_GameLevel game) : CD_BaseEvent(game)
{
	public override void Activate() {
		Game.Boss.SingleHit();
	}
}
public class BossMasher(CD_GameLevel game) : CD_BaseEvent(game)
{
	public override void Activate() {
		Game.Boss.Masher();
	}
}
public class BossFar1Start(CD_GameLevel game) : CD_BaseEvent(game)
{
	public override void Activate() {
		Game.Boss.Far1Start();
	}
}
public class BossFar1End(CD_GameLevel game) : CD_BaseEvent(game)
{
	public override void Activate() {
		Game.Boss.Far1End();
	}
}
public class BossFar1To2(CD_GameLevel game) : CD_BaseEvent(game)
{
	public override void Activate() {
		Game.Boss.Far1To2();
	}
}
public class BossFar2Start(CD_GameLevel game) : CD_BaseEvent(game)
{
	public override void Activate() {
		Game.Boss.Far2Start();
	}
}
public class BossFar2End(CD_GameLevel game) : CD_BaseEvent(game)
{
	public override void Activate() {
		Game.Boss.Far2End();
	}
}
public class BossFar2To1(CD_GameLevel game) : CD_BaseEvent(game)
{
	public override void Activate() {
		Game.Boss.Far2To1();
	}
}
public class BossHide(CD_GameLevel game) : CD_BaseEvent(game)
{
	public override void Activate() {
		Game.Boss.Hide();
	}
}

public class Boss : CD_BaseEnemy
{
	public Boss() : base(EntityType.Boss) {
		Interactivity = EntityInteractivity.Noninteractive;
		Visible = false;
	}
	public override void Initialize() {

	}

	public const int ANIMATION_CHANNEL_MAIN = 0;
	public const int ANIMATION_CHANNEL_FIRE = 1;
	public const int ANIMATION_CHANNEL_FIRE2 = 3;

	public void In() {
		var scene = GetGameLevel().Scene;
		Visible = true;
		Animations.SetAnimation(ANIMATION_CHANNEL_MAIN, scene.Boss.In, false);
		Animations.AddAnimation(ANIMATION_CHANNEL_MAIN, scene.Boss.Standby.Standby0, true);
	}
	public void Out() {
		var scene = GetGameLevel().Scene;
		Animations.SetAnimation(ANIMATION_CHANNEL_MAIN, scene.Boss.Out, false);
	}
	public void SingleHit() {

	}
	public void Masher() {

	}
	public void Far1Start() {
		var scene = GetGameLevel().Scene;
		Animations.SetAnimation(ANIMATION_CHANNEL_MAIN, scene.Boss.Transitions.From0.To1, false);
		Animations.AddAnimation(ANIMATION_CHANNEL_MAIN, scene.Boss.Standby.Standby1, true);
	}
	public void Far1End() {
		var scene = GetGameLevel().Scene;
		Animations.SetAnimation(ANIMATION_CHANNEL_MAIN, scene.Boss.Transitions.From1.To0, false);
		Animations.AddAnimation(ANIMATION_CHANNEL_MAIN, scene.Boss.Standby.Standby0, true);
	}
	public void Far1To2() {
		var scene = GetGameLevel().Scene;
		Animations.SetAnimation(ANIMATION_CHANNEL_MAIN, scene.Boss.Transitions.From1.To2, false);
		Animations.AddAnimation(ANIMATION_CHANNEL_MAIN, scene.Boss.Standby.Standby2, true);
	}
	public void Far2Start() {
		var scene = GetGameLevel().Scene;
		Animations.SetAnimation(ANIMATION_CHANNEL_MAIN, scene.Boss.Transitions.From0.To2, false);
		Animations.AddAnimation(ANIMATION_CHANNEL_MAIN, scene.Boss.Standby.Standby2, true);
	}
	public void Far2End() {
		var scene = GetGameLevel().Scene;
		Animations.SetAnimation(ANIMATION_CHANNEL_MAIN, scene.Boss.Transitions.From2.To0, false);
		Animations.AddAnimation(ANIMATION_CHANNEL_MAIN, scene.Boss.Standby.Standby0, true);
	}
	public void Far2To1() {
		var scene = GetGameLevel().Scene;
		Animations.SetAnimation(ANIMATION_CHANNEL_MAIN, scene.Boss.Transitions.From2.To1, false);
		Animations.AddAnimation(ANIMATION_CHANNEL_MAIN, scene.Boss.Standby.Standby1, true);
	}
	public void Hide() {

	}

	public override void OnSignalReceived(CD_BaseMEntity from, EntitySignalType signalType, object? data = null) {
		// If not visible, ignore the signal
		// Just so things don't get clogged up and a fire animation plays
		// when nothing is being fired.
		if (!Visible) return;
		var scene = GetGameLevel().Scene;

		switch (from) {
			case SingleHitEnemy she:
				// Confirm that this is boss related, and the first appearance
				if(she.Variant.IsBoss() && signalType == EntitySignalType.FirstAppearance) {
					// Figure out which animation to play.

					// Attack2 is defined with the same class as Attack1; less code typed out here
					// The JSON descriptor doesn't need to specify a whole object though for Attack2;
					// it can just specify a string and thats implicitly casted to the object type
					// during deserialization.

					var pathway = she.Pathway;
					var attackanims = she.Variant == EntityVariant.Boss1 ? scene.Boss.Attacks.Attack1 : scene.Boss.Attacks.Attack2;
					Animations.SetAnimation(
						ANIMATION_CHANNEL_FIRE,
						pathway == PathwaySide.Top ? attackanims.Air : attackanims.Ground,
						false);
				}
				break;
		}
	}

	public override void Render() {
		if (!Visible) return;
		if (Model == null) return;

		if (!Level.Paused) __anim.AddDeltaTime(Level.CurtimeDelta);

		__anim.Apply(Model);
		Model.Position = Position;
		Model.Scale = Scale;

		Model.Render();
	}

	public override void Build() {
		base.Build();
		Model = GetGameLevel().Scene.Boss.ModelData.Instantiate();
		Animations = new Nucleus.Models.Runtime.AnimationHandler(Model);

		Model.SetToSetupPose();
	}
	public override bool VisTest(float gamewidth, float gameheight, float xPosition) {
		return Visible;
	}
}