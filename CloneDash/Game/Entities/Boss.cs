using CloneDash.Modding.Descriptors;
namespace CloneDash.Game.Entities;

public class BossInEvent(CD_GameLevel game) : CD_BaseEvent(game)
{
	public override void Activate() {
		Game.BossIn();
	}
}
public class BossOutEvent(CD_GameLevel game) : CD_BaseEvent(game)
{
	public override void Activate() {
		Game.BossOut();
	}
}
public class BossSingleHit(CD_GameLevel game) : CD_BaseEvent(game)
{
	public override void Activate() {
		Game.BossSingleHit();
	}
}
public class BossMasher(CD_GameLevel game) : CD_BaseEvent(game)
{
	public override void Activate() {
		Game.BossMasher();
	}
}
public class BossFar1Start(CD_GameLevel game) : CD_BaseEvent(game)
{
	public override void Activate() {
		Game.BossFar1Start();
	}
}
public class BossFar1End(CD_GameLevel game) : CD_BaseEvent(game)
{
	public override void Activate() {
		Game.BossFar1End();
	}
}
public class BossFar1To2(CD_GameLevel game) : CD_BaseEvent(game)
{
	public override void Activate() {
		Game.BossFar1To2();
	}
}
public class BossFar2Start(CD_GameLevel game) : CD_BaseEvent(game)
{
	public override void Activate() {
		Game.BossFar2Start();
	}
}
public class BossFar2End(CD_GameLevel game) : CD_BaseEvent(game)
{
	public override void Activate() {
		Game.BossFar2End();
	}
}
public class BossFar2To1(CD_GameLevel game) : CD_BaseEvent(game)
{
	public override void Activate() {
		Game.BossFar2To1();
	}
}
public class BossHide(CD_GameLevel game) : CD_BaseEvent(game)
{
	public override void Activate() {
		Game.BossHide();
	}
}



public class Boss : CD_BaseEnemy
{
	private SceneDescriptor scene;
	public Boss(SceneDescriptor scene) : base(EntityType.Boss) {
		Interactivity = EntityInteractivity.Noninteractive;
		this.scene = scene;
		Visible = false;
	}
	public override void Initialize() {
		Model = Level.Models.CreateInstanceFromFile("scene", scene.Boss.Model);
		Animations = new Nucleus.Models.Runtime.AnimationHandler(Model);
	}

	public override bool VisTest(float gamewidth, float gameheight, float xPosition) {
		return Visible;
	}
}