namespace CloneDash.Game;

public enum CharacterAnimationType
{
	NotApplicable,

	In,
	Run,
	Die,
	Standby,

	AirGreat,
	AirPerfect,
	AirHurt,

	RoadGreat,
	RoadPerfect,
	RoadHurt,
	RoadMiss,

	Double,

	AirToGround,

	Jump,
	JumpHurt,

	Press,
	AirPressEnd,
	AirPressHurt,
	DownPressHit,
	UpPressHit
}