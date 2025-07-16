namespace CloneDash.Scenes;

public enum BossAnimationType
{
	In,
	Out,

	Standby0,
	Standby1,
	Standby2,

	From0To1,
	From0To2,
	From1To0,
	From1To2,
	From2To0,
	From2To1,

	AttackAir1,
	AttackAir2,
	AttackGround1,
	AttackGround2,

	CloseAttackSlow,
	CloseAttackFast,

	MultiAttack,
	MultiAttackEnd,
	MultiAttackHurt,
	MultiAttackHurtEnd,

	Hurt,
}