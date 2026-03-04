namespace CloneDash.Game;

public enum CharacterAnimationType
{
	NotApplicable,

	Run,
	In,

	Hurt,
	JumpHurt,
	Die,

	Press,

	AttackMiss,
	AttackGreat,
	AttackPerfect,

	Jump,
	JumpHit,

	DownHit,
	DownPress,

	UpHit,
	UpPressStart,
	UpPress,
	UpPressEnd,

	BigPress,

	// What are these?
	UpPressS2B,
	DownPressS2B,

	UpPressB2S,
	DownPressB2S,

	BigHit,

	UpPressSmall,
	DownPressSmall,

	UpPressHurt,

	JumpHitGreat
}

