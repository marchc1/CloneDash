namespace CloneDash.Common.Gamemodes.MuseDash.V1;

public enum EventType
{
	NotApplicable,

	BossIn,
	BossOut,

	BossSingleHit,

	BossMasher,
	BossMasherEnd,

	BossFar1Start,
	BossFar1End,
	BossFar1To2,
	BossFar2Start,
	BossFar2End,
	BossFar2To1,

	AirSpeed1,
	AirSpeed2,
	AirSpeed3,

	GroundSpeed1,
	GroundSpeed2,
	GroundSpeed3,

	DoubleSpeed1,
	DoubleSpeed2,
	DoubleSpeed3,

	BossHide,
	SceneChange,

	ScreenScrollUp,
	ScreenScrollDown,
	ScreenScrollEnd,
	ScanlinesOn,
	ScanlinesOff,
	ChromaticAberrationOn,
	ChromaticAberrationOff,
	VignetteOn,
	VignetteOff,
	TVStaticOn,
	TVStaticOff,
	FlashbangStart,
	FlashbangHigh,
	FlashbangEnd,
	NoteFreeze,
	NoteUnfreeze,
	BgFreeze,
	BgUnfreeze,
	MosaicStart,
	MosaicEnd,
	SepiaStart,
	SepiaEnd,
}
